using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ClearAlphaFeature : ScriptableRendererFeature
{
	class Pass : ScriptableRenderPass
	{
		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			var cmd = CommandBufferPool.Get("ClearAlpha");
			cmd.ClearRenderTarget(false, true, new Color(0, 0, 0, 0));
			context.ExecuteCommandBuffer(cmd);
			CommandBufferPool.Release(cmd);
		}
	}

	Pass pass;

	public override void Create()
	{
		pass = new Pass
		{
			renderPassEvent = RenderPassEvent.BeforeRenderingTransparents
		};
	}

	public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
	{
		renderer.EnqueuePass(pass);
	}
}