Shader "Hidden/Universal Render Pipeline/BloomMaskTransparent"
{
    Properties
    {
        [MainTexture] _BaseMap("Texture", 2D) = "white" {}
        [TextureMultiplier]   _BaseMapMultiplier("BaseMap Channel Multiplier(RGBA)", Vector) = (0, 0, 0, 1)
        [MainColor]   _BaseColor("Color", Color) = (1, 1, 1, 1)
        _BloomFactor ("Bloom Factor", Float) = 0
    }

    SubShader
    {
        Tags {  "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "Bloom Mask Transparent"
            Tags{"LightMode" = "UniversalForward"}

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x

            #pragma vertex vert
            #pragma fragment frag

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

            CBUFFER_START(UnityPerMaterial)
                // The following line declares the _BaseColor variable, so that you
                // can use it in the fragment shader.
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float4 _BaseMapMultiplier;
                float _BloomFactor;        
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS       : POSITION;
                float2 uv               : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv        : TEXCOORD0;
                float4 vertex : SV_POSITION;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.vertex = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half2 uv = input.uv;
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
                half alpha = _BaseColor.a * dot(texColor.rgba, _BaseMapMultiplier);
                clip(alpha - 0.1f);

                return half4(_BloomFactor, 0, 0, 0);
            }
            ENDHLSL

        }
    }
}
