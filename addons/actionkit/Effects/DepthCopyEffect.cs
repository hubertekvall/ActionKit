#nullable enable
using System;
using Godot;

namespace ActionKit.Effects;

/// <summary>
/// Copies the main camera's opaque depth buffer into an R32F texture after the opaque pass.
/// The result is exposed as a static Texture2Drd and as the global shader uniform "main_depth".
///
/// Setup:
///  - Project Settings > Shader Globals: add "main_depth" as sampler2D (leave empty).
///  - Put this effect ONLY on the main gameplay Camera3D's Compositor.
///  - Give other cameras (distortion, clouds) an empty Compositor.
///  - Shaders read it with texelFetch (raw reverse-Z depth; unproject with INV_PROJECTION_MATRIX).
/// </summary>
[Tool, GlobalClass]
public partial class DepthCopyEffect : CompositorEffect
{
	public const string GlobalUniformName = "main_depth";

	/// Shared main-camera depth. Safe to bind anywhere at any time; filled once the effect renders.
	public static Texture2Drd DepthTexture { get; } = new();

	private const string ComputeSource = """
        #version 450
        layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

        layout(set = 0, binding = 0) uniform sampler2D depth_tex;
        layout(r32f, set = 0, binding = 1) uniform restrict writeonly image2D out_depth;

        layout(push_constant, std430) uniform Params {
            vec2 size;
            vec2 pad;
        } p;

        void main() {
            ivec2 px = ivec2(gl_GlobalInvocationID.xy);
            if (px.x >= int(p.size.x) || px.y >= int(p.size.y)) return;
            float d = texelFetch(depth_tex, px, 0).r;
            imageStore(out_depth, px, vec4(d, 0.0, 0.0, 0.0));
        }
""";

	// Frames to keep a replaced texture alive so dependent uniform sets can switch over first.
	private const int DeferredFreeFrames = 3;

	private RenderingDevice _rd = null!;
	private Rid _shader;
	private Rid _pipeline;
	private Rid _sampler;
	private Rid _outTex;
	private Vector2I _size;
	private bool _initFailed;

	private Rid _pendingFree;
	private int _pendingFreeFrames;

	private readonly byte[] _pushConstant = new byte[16];

	private readonly RDUniform _depthUniform = new()
	{
		UniformType = RenderingDevice.UniformType.SamplerWithTexture,
		Binding = 0,
	};

	private readonly RDUniform _outUniform = new()
	{
		UniformType = RenderingDevice.UniformType.Image,
		Binding = 1,
	};

	private Godot.Collections.Array<RDUniform>? _uniforms;

	public DepthCopyEffect()
	{
		EffectCallbackType = EffectCallbackTypeEnum.PostOpaque;
		AccessResolvedDepth = true; // get a resolved depth buffer when MSAA is on
	}

	// ------------------------------------------------------------------ render thread

	public override void _RenderCallback(int effectCallbackType, RenderData renderData)
	{
		if (Engine.IsEditorHint()) return; // editor viewports would thrash the texture
		if (effectCallbackType != (int)EffectCallbackTypeEnum.PostOpaque) return;
		if (!EnsureInitialized()) return;

		TickPendingFree();

		if (renderData.GetRenderSceneBuffers() is not RenderSceneBuffersRD buffers) return;

		Vector2I size = buffers.GetInternalSize();
		if (size.X <= 0 || size.Y <= 0) return;

		if (size != _size || !IsTextureValid(_outTex))
		{
			if (!RecreateOutput(size)) return;
		}

		Rid depth = buffers.GetDepthLayer(0);
		if (!IsTextureValid(depth)) return;

		_depthUniform.ClearIds();
		_depthUniform.AddId(_sampler);
		_depthUniform.AddId(depth);

		_outUniform.ClearIds();
		_outUniform.AddId(_outTex);

		_uniforms ??= new Godot.Collections.Array<RDUniform> { _depthUniform, _outUniform };

		Rid uniformSet = UniformSetCacheRD.GetCache(_shader, 0, _uniforms);
		if (!uniformSet.IsValid) return;

		WritePushConstant(size);

		uint groupsX = (uint)((size.X + 7) / 8);
		uint groupsY = (uint)((size.Y + 7) / 8);

		long list = _rd.ComputeListBegin();
		_rd.ComputeListBindComputePipeline(list, _pipeline);
		_rd.ComputeListBindUniformSet(list, uniformSet, 0);
		_rd.ComputeListSetPushConstant(list, _pushConstant, (uint)_pushConstant.Length);
		_rd.ComputeListDispatch(list, groupsX, groupsY, 1);
		_rd.ComputeListEnd();
	}

	/// Lazily creates GPU resources on the render thread (the callback always runs there).
	private bool EnsureInitialized()
	{
		if (_pipeline.IsValid) return true;
		if (_initFailed) return false;

		_rd = RenderingServer.GetRenderingDevice();
		if (_rd == null)
		{
			// Compatibility renderer has no RenderingDevice.
			_initFailed = true;
			return false;
		}

		var source = new RDShaderSource
		{
			Language = RenderingDevice.ShaderLanguage.Glsl,
			SourceCompute = ComputeSource,
		};

		RDShaderSpirV spirv = _rd.ShaderCompileSpirVFromSource(source);
		if (!string.IsNullOrEmpty(spirv.CompileErrorCompute))
		{
			GD.PushError($"DepthCopyEffect: compute shader failed to compile:\n{spirv.CompileErrorCompute}");
			_initFailed = true;
			return false;
		}

		_shader = _rd.ShaderCreateFromSpirV(spirv, "DepthCopyEffect");
		if (!_shader.IsValid)
		{
			GD.PushError("DepthCopyEffect: ShaderCreateFromSpirV failed.");
			_initFailed = true;
			return false;
		}

		_pipeline = _rd.ComputePipelineCreate(_shader);
		if (!_pipeline.IsValid)
		{
			GD.PushError("DepthCopyEffect: ComputePipelineCreate failed.");
			_initFailed = true;
			return false;
		}

		_sampler = _rd.SamplerCreate(new RDSamplerState
		{
			MinFilter = RenderingDevice.SamplerFilter.Nearest,
			MagFilter = RenderingDevice.SamplerFilter.Nearest,
			RepeatU = RenderingDevice.SamplerRepeatMode.ClampToEdge,
			RepeatV = RenderingDevice.SamplerRepeatMode.ClampToEdge,
		});

		return true;
	}

	private void TickPendingFree()
	{
		if (!_pendingFree.IsValid) return;
		if (--_pendingFreeFrames > 0) return;

		if (_rd.TextureIsValid(_pendingFree)) _rd.FreeRid(_pendingFree);
		_pendingFree = default;
	}

	private bool RecreateOutput(Vector2I size)
	{
		// Defer freeing the old texture so materials / the global uniform can switch to the new one first.
		if (IsTextureValid(_outTex))
		{
			if (_pendingFree.IsValid && _rd.TextureIsValid(_pendingFree))
				_rd.FreeRid(_pendingFree); // two resizes in a row: drop the older one now

			_pendingFree = _outTex;
			_pendingFreeFrames = DeferredFreeFrames;
		}

		_outTex = default;
		_size = Vector2I.Zero;

		var format = new RDTextureFormat
		{
			Format = RenderingDevice.DataFormat.R32Sfloat,
			TextureType = RenderingDevice.TextureType.Type2D,
			Width = (uint)size.X,
			Height = (uint)size.Y,
			Depth = 1,
			ArrayLayers = 1,
			Mipmaps = 1,
			UsageBits = RenderingDevice.TextureUsageBits.StorageBit
					  | RenderingDevice.TextureUsageBits.SamplingBit,
		};

		Rid tex = _rd.TextureCreate(format, new RDTextureView());
		if (!tex.IsValid)
		{
			GD.PushError($"DepthCopyEffect: TextureCreate failed for size {size}.");
			return false;
		}

		_outTex = tex;
		_size = size;
		DepthTexture.TextureRdRid = _outTex;

		// Re-point the global every time the texture is recreated (window resize).
		Callable.From(() =>
			RenderingServer.GlobalShaderParameterSet(GlobalUniformName, DepthTexture)
		).CallDeferred();

		return true;
	}

	private void WritePushConstant(Vector2I size)
	{
		BitConverter.TryWriteBytes(_pushConstant.AsSpan(0, 4), (float)size.X);
		BitConverter.TryWriteBytes(_pushConstant.AsSpan(4, 4), (float)size.Y);
		// bytes 8..15 stay zero (padding to 16)
	}

	private bool IsTextureValid(Rid rid) => rid.IsValid && _rd.TextureIsValid(rid);

	// ------------------------------------------------------------------ cleanup

	public override void _Notification(int what)
	{
		if (what != NotificationPredelete || _rd == null) return;

		if (IsTextureValid(_pendingFree)) _rd.FreeRid(_pendingFree);
		if (IsTextureValid(_outTex)) _rd.FreeRid(_outTex);
		if (_sampler.IsValid) _rd.FreeRid(_sampler);
		if (_shader.IsValid) _rd.FreeRid(_shader); // also frees the pipeline

		_pendingFree = default;
		_outTex = default;
		_sampler = default;
		_shader = default;
		_pipeline = default;
	}
}
