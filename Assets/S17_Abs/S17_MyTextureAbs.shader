Shader "CG/S17_MyTextureAbs"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
    }
    SubShader
    {
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            sampler2D _BaseMap;

            void vert(float3 positionOS : POSITION,
                      float2 uv : TEXCOORD0,
                      out float2 uvOut : TEXCOORD0,
                      out float4 positionCS : SV_POSITION)
            {
                uvOut = uv;
                positionCS = TransformObjectToHClip(positionOS);
            }

            float4 frag(float2 uv : TEXCOORD0) : SV_Target
            {
                // Quad 왼쪽 위 UV 영역을 텍스처 오른쪽 아래 영역으로 이동한다.
                if (uv.x < 0.5 && uv.y >= 0.5)
                {
                    float2 p = uv + float2(0.5, -0.5);
                    return tex2D(_BaseMap, p);
                }

                return float4(0.5, 0.5, 0.5, 1);
            }
            ENDHLSL
        }
    }
}