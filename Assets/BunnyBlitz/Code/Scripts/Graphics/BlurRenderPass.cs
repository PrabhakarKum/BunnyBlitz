using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace BunnyBlitz
{
    public class BlurRenderPass2D : ScriptableRenderPass2D
    {
        public Material FxMaterial { get; set; }
        public float BlurAmount { get; set; }

        public float BlurStrength { get; set; }
    
        private static readonly int SourceSizeID = Shader.PropertyToID("_SourceSize");
        private static readonly int BlurStrengthID = Shader.PropertyToID("_BlurStrength");
        private static readonly int BlurAmountID = Shader.PropertyToID("_BlurAmount");
        private static readonly int DownSampleFactorID = Shader.PropertyToID("_DownSampleFactor");
        private static readonly int DownSampledTextureID = Shader.PropertyToID("_DownSampled");

        class PassData
        {
            public TextureHandle CameraColorHandle;
            public TextureHandle CameraColorCopyHandle;
            public TextureHandle DownSampledTexture1Handle;
            public TextureHandle DownSampledTexture2Handle;
            public Material FxMaterial;
        }
    
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            const int DownSampleSize = 4;

            using (var builder = renderGraph.AddUnsafePass<PassData>("Blur Layer", out var passData))
            {
                // Get the frame data
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            
                var sceneColorCopyDesc = renderGraph.GetTextureDesc(resourceData.cameraColor);
                sceneColorCopyDesc.name = "_CameraColorFullScreenPass";
                sceneColorCopyDesc.clearBuffer = false;
                var sceneColorCopy = renderGraph.CreateTexture(sceneColorCopyDesc);

                var sceneDownScaleDesc = renderGraph.GetTextureDesc(resourceData.cameraColor);
                sceneDownScaleDesc.name = "_CameraColorDownScale";
                sceneDownScaleDesc.clearBuffer = false;
                sceneDownScaleDesc.width /= DownSampleSize;
                sceneDownScaleDesc.height /= DownSampleSize;
                sceneDownScaleDesc.filterMode = FilterMode.Bilinear;
                var sceneDownScale1 = renderGraph.CreateTexture(sceneDownScaleDesc);
                var sceneDownScale2 = renderGraph.CreateTexture(sceneDownScaleDesc);
            
                passData.CameraColorHandle = resourceData.cameraColor;
                builder.UseTexture(passData.CameraColorHandle);
            
                passData.CameraColorCopyHandle = sceneColorCopy;
                builder.UseTexture(passData.CameraColorCopyHandle, AccessFlags.ReadWrite);

                passData.DownSampledTexture1Handle = sceneDownScale1;
                builder.UseTexture(sceneDownScale1, AccessFlags.ReadWrite);
            
                passData.DownSampledTexture2Handle = sceneDownScale2;
                builder.UseTexture(sceneDownScale2, AccessFlags.ReadWrite);
            
                builder.AllowPassCulling(false);
            
                passData.FxMaterial = FxMaterial;
                FxMaterial.SetVector(SourceSizeID, new Vector4(sceneColorCopyDesc.width, sceneColorCopyDesc.height, 1.0f / sceneColorCopyDesc.width, 1.0f / sceneColorCopyDesc.height));
                FxMaterial.SetVector(DownSampleFactorID, new Vector4(1.0f/DownSampleSize, 1.0f/DownSampleSize, DownSampleSize, DownSampleSize));
                FxMaterial.SetFloat(BlurAmountID, BlurAmount);
                FxMaterial.SetFloat(BlurStrengthID, BlurStrength);
            
                builder.SetRenderFunc(static (PassData data, UnsafeGraphContext ctx) =>
                {
                    var cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                    var mat = data.FxMaterial;
                
                    Blitter.BlitCameraTexture(cmd, data.CameraColorHandle, data.CameraColorCopyHandle);
                
                    //downscale the texture
                    Blitter.BlitCameraTexture(cmd, data.CameraColorCopyHandle, data.DownSampledTexture1Handle);
                
                    //blur the downscaled texture horizontally
                    Blitter.BlitCameraTexture(cmd, data.DownSampledTexture1Handle, data.DownSampledTexture2Handle, data.FxMaterial, 0);
                    //blur vertically
                    Blitter.BlitCameraTexture(cmd, data.DownSampledTexture2Handle, data.DownSampledTexture1Handle, data.FxMaterial, 1);
                
                    mat.SetTexture(DownSampledTextureID, data.DownSampledTexture1Handle);
                    Blitter.BlitCameraTexture(cmd, data.CameraColorCopyHandle, data.CameraColorHandle, data.FxMaterial, 2);
                });
            }
        }
    }
}