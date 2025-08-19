Shader "Hidden/ProfileMeshPreview"
{
    Properties
    {
        _RingCount("Ring Count", float) = 1.0
        _RingVertices("Ring Vertices", float) = 1.0
        _FaceOrientation("Show Face Orientation", float) = 0.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"            
            
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normal       : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float3 normal       : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            float _RingCount;
            float _RingVertices;
            float _ShowFaceOrientation;
            
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normal = IN.normal;
                OUT.uv = IN.uv;
                return OUT;
            }
        
            half4 frag(Varyings IN, bool facing : SV_IsFrontFace) : SV_Target
            {
                float3 facingColor = lerp(float3(1,0,0), float3(0,0,1), facing);
                float2 checkerUv = floor(IN.uv * float2(_RingCount, _RingVertices)) / 2.0f;
                float3 checker = lerp(0.5f, 1.0f, frac(checkerUv.x + checkerUv.y));
                return float4(checker * lerp(float3(0,1,1), float3(1,0,1), IN.uv.x) * lerp(1, facingColor, _ShowFaceOrientation), 1.0f);
            }
            ENDHLSL
        }
    }
}