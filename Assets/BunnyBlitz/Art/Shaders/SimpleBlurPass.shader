Shader "Custom/SimpleBlurPass"
{
    HLSLINCLUDE
        #pragma target 3.5

		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        
        TEXTURE2D_X(_DownSampled);
        float _BlurAmount;
        
        float4 _SourceSize;
        float4 _DownSampleFactor;
        float _BlurStrength;

        #define BLUR_KERNEL 1

        #if BLUR_KERNEL == 0

        // Offsets & coeffs for optimized separable bilinear 3-tap gaussian (5-tap equivalent)
        const static int kTapCount = 3;
        const static float kOffsets[] = {
            -1.33333333,
             0.00000000,
             1.33333333
        };
        const static half kCoeffs[] = {
             0.35294118,
             0.29411765,
             0.35294118
        };

        #elif BLUR_KERNEL == 1

        // Offsets & coeffs for optimized separable bilinear 5-tap gaussian (9-tap equivalent)
        const static int kTapCount = 5;
        const static float kOffsets[] = {
            -3.23076923,
            -1.38461538,
             0.00000000,
             1.38461538,
             3.23076923
        };
        const static half kCoeffs[] = {
             0.07027027,
             0.31621622,
             0.22702703,
             0.31621622,
             0.07027027
        };

        #endif
        
		 half4 Blur(Varyings input, float2 dir, float premultiply)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);
		     
            float2 offset = _SourceSize.zw * _DownSampleFactor.zw * dir * _BlurStrength;
            half4 acc = 0.0;

            UNITY_UNROLL
            for (int i = 0; i < kTapCount; i++)
            {
                float2 sampCoord = uv + kOffsets[i] * offset;
                half4 sampColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, sampCoord);

                // Weight & pre-multiply to limit bleeding on the focused area
                half weight = 1.0;
                //half weight = saturate(1.0 - (samp0CoC - sampCoC));
                acc += half4(sampColor.xyz, 1.0) * kCoeffs[i] * weight;
            }
		     
            acc.xyz /= acc.w + 1e-4; // Zero-div guard
            return half4(acc.xyz, 1.0);
        }

        half4 FragBlurH(Varyings input) : SV_Target
        {
            return Blur(input, float2(1.0, 0.0), 1.0);
        }

        half4 FragBlurV(Varyings input) : SV_Target
        {
            return Blur(input, float2(0.0, 1.0), 0.0);
        }

        half4 FragComposite(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);

            half3 baseColor = LOAD_TEXTURE2D_X(_BlitTexture, _SourceSize.xy * uv).xyz;
            half3 downSampledColor = LOAD_TEXTURE2D_X(_DownSampled, _SourceSize.xy * _DownSampleFactor.xy * uv).xyz;

            return half4(lerp(baseColor, downSampledColor, _BlurAmount), 1.0f);
        }
	ENDHLSL
    
    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
		ZTest Always
		ZWrite Off
		Cull Off

        Pass
        {
            Name "Blur Horizontal"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragBlurH
            ENDHLSL
        }

        Pass
        {
            Name "Blur Vertical"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragBlurV
            ENDHLSL
        }

        Pass
        {
            Name "Compositing"
            
            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragComposite
            ENDHLSL
        }
    }
}
