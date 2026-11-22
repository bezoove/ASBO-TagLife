// The river: two sets of ripples flowing downstream (UV0 = metres along and across the river), the sky and any
// reflection probe reflected more at a glancing angle, a sharp sun glint, deep dark water in the middle shading to
// paler shallows and a little scum and foam at the banks (UV1.x = 0 middle .. 1 edge). One transparent layer, no
// depth or opaque texture needed, so it stays cheap on phones.
Shader "ASBO/Water"
{
    Properties
    {
        _Deep ("Deep", Color) = (0.06, 0.10, 0.085, 0.94)
        _Shallow ("Shallow", Color) = (0.22, 0.27, 0.19, 0.72)
        _Ripples ("Ripples (normal map)", 2D) = "bump" {}
        _Foam ("Foam", 2D) = "black" {}
        _Flow ("Flow speed (m/s)", Float) = 0.45
        _Scale ("Ripple scale (repeats per m)", Float) = 0.11
        _Bump ("Ripple strength", Float) = 0.55
        _Glint ("Sun glint", Float) = 1.6
        _Reflect ("Reflection", Float) = 0.9
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-20" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "Water"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_Ripples); SAMPLER(sampler_Ripples);
            TEXTURE2D(_Foam); SAMPLER(sampler_Foam);
            CBUFFER_START(UnityPerMaterial)
                half4 _Deep; half4 _Shallow; float4 _Ripples_ST; float4 _Foam_ST;
                float _Flow; float _Scale; half _Bump; half _Glint; half _Reflect;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float4 tangentOS : TANGENT; float2 uv0 : TEXCOORD0; float2 uv1 : TEXCOORD1; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 rs : TEXCOORD0; float2 edge : TEXCOORD1; float3 positionWS : TEXCOORD2; float3 tangentWS : TEXCOORD3; float fog : TEXCOORD4; };
            Varyings vert(Attributes i)
            {
                Varyings o; VertexPositionInputs p = GetVertexPositionInputs(i.positionOS.xyz);
                o.positionCS = p.positionCS; o.positionWS = p.positionWS; o.rs = i.uv0; o.edge = i.uv1;
                o.tangentWS = TransformObjectToWorldDir(i.tangentOS.xyz); o.fog = ComputeFogFactor(p.positionCS.z);
                return o;
            }
            half4 frag(Varyings i) : SV_Target
            {
                float t = _Time.y;
                // river space: x downstream, y across; the slower water at the edges drags the ripples back
                float edge = saturate(i.edge.x);
                float speed = _Flow * (1.0 - edge * edge * 0.7);
                float2 p = i.rs * _Scale;
                float2 uvA = p + float2(-t * speed * _Scale, 0.0);
                float2 uvB = p * 1.73 + float2(-t * speed * _Scale * 1.31, t * 0.012);
                half3 nA = UnpackNormal(SAMPLE_TEXTURE2D(_Ripples, sampler_Ripples, uvA));
                half3 nB = UnpackNormal(SAMPLE_TEXTURE2D(_Ripples, sampler_Ripples, uvB));
                half2 slope = (nA.xy + nB.xy) * _Bump;
                float3 T = normalize(i.tangentWS); float3 B = normalize(cross(float3(0, 1, 0), T)); // across the river
                float3 N = normalize(float3(0, 1, 0) + T * slope.x + B * slope.y);
                float3 V = normalize(_WorldSpaceCameraPos - i.positionWS);
                half ndv = saturate(dot(N, V));
                half fres = 0.03 + (1.0 - 0.03) * pow(1.0 - ndv, 5.0);
                fres *= _Reflect;
                half3 R = reflect(-V, N);
                half3 refl = GlossyEnvironmentReflection(R, 0.06h, 1.0h);
                Light L = GetMainLight();
                half3 H = normalize(L.direction + V);
                half glint = pow(saturate(dot(N, H)), 220.0) * _Glint;
                half4 body = lerp(_Deep, _Shallow, edge * edge);
                half3 lit = body.rgb * (SampleSH(N) * 0.9 + L.color * saturate(dot(N, L.direction)) * 0.25);
                // scum and foam where the water laps the bank, drifting with the flow
                half foam = SAMPLE_TEXTURE2D(_Foam, sampler_Foam, i.rs * 0.32 + float2(-t * speed * 0.32, 0)).r;
                foam *= smoothstep(0.78, 0.98, edge) * 0.65;
                half3 col = lerp(lit, refl, fres) + glint * L.color + foam * half3(0.75, 0.74, 0.66);
                half a = saturate(lerp(body.a, 1.0, fres) + foam);
                col = MixFog(col, i.fog);
                return half4(col, a);
            }
            ENDHLSL
        }
    }
}
