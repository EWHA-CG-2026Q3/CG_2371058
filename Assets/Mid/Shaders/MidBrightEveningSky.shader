Shader "Mid/BrightEveningSky" {
SubShader { Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" } Cull Off ZWrite Off
Pass { HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
struct A {float4 p:POSITION;}; struct V {float4 p:SV_POSITION;float3 d:TEXCOORD0;};
V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.d=a.p.xyz;return o;}
half4 frag(V i):SV_Target {float y=normalize(i.d).y;float t=smoothstep(0,0.65,max(y,0));float3 horizon=float3(.87,.75,.61);float3 zenith=float3(.42,.63,.79);return half4(lerp(horizon,zenith,t),1);}
ENDHLSL }
} }
