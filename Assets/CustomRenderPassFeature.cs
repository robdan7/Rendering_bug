using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CustomRenderPassFeature : ScriptableRendererFeature
{
    class CustomRenderPass : ScriptableRenderPass
    {
        private RenderTargetIdentifier _colorBuffer, _cameraDepthTarget;
        private readonly Material _material;
        private RenderTextureDescriptor _descriptor;

        public CustomRenderPass()
        {
            this.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            this._material =
                CoreUtils.CreateEngineMaterial(Instantiate(Resources.Load("shaderTest", typeof(Shader))) as Shader);
        }
        
        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            this._colorBuffer = renderingData.cameraData.renderer.cameraColorTarget;
            this._descriptor = renderingData.cameraData.cameraTargetDescriptor;

            this._descriptor.colorFormat = RenderTextureFormat.ARGB32;
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, new ProfilingSampler("rendering tests")))
            {
                RenderTexture pixelBuffer = RenderTexture.GetTemporary(this._descriptor);
                pixelBuffer.Create();
                Blit(cmd,pixelBuffer,this._colorBuffer, this._material);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                RenderTexture.ReleaseTemporary(pixelBuffer);
                CommandBufferPool.Release(cmd);
            }
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }
    }

    CustomRenderPass m_ScriptablePass;

    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new CustomRenderPass();

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
#if UNITY_EDITOR
        if (renderingData.cameraData.isSceneViewCamera) return;
#endif
        renderer.EnqueuePass(m_ScriptablePass);
    }
}


