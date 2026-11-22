// Traffic light, pedestrian signal and Belisha beacon lamps: one mesh per junction, lit by a property block.
// Each lamp's UVs carry its signal group (whole part of u) and aspect (whole part of v); _Phase holds a bit mask per
// group (x, y = the two sets of traffic lights, z = the pedestrian signals, w = the beacons): 1 red, 2 amber, 4 green,
// 8 red man, 16 green man. Unlit and cheap: lamps glow by day and night.
Shader "ASBO/Signal"
{
    Properties
    {
        _MainTex ("Lamp faces", 2D) = "white" {}
        _Phase ("Phase", Vector) = (4, 1, 8, 0)
        _Glow ("Glow", Float) = 2.2
        _Off ("Off", Float) = 0.1
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Lamps"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST; float4 _Phase; float _Glow; float _Off;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float2 ga : TEXCOORD1; float fog : TEXCOORD2; };
            Varyings vert(Attributes i)
            {
                Varyings o; VertexPositionInputs p = GetVertexPositionInputs(i.positionOS.xyz);
                o.positionCS = p.positionCS; o.ga = floor(i.uv + 0.0001); o.uv = i.uv - o.ga; o.fog = ComputeFogFactor(p.positionCS.z);
                return o;
            }
            half4 frag(Varyings i) : SV_Target
            {
                float g = i.ga.x, a = i.ga.y;
                float bits = g < 0.5 ? _Phase.x : (g < 1.5 ? _Phase.y : (g < 2.5 ? _Phase.z : _Phase.w));
                float on = fmod(floor(bits / exp2(a) + 0.01), 2.0);
                half4 t = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half3 col = t.rgb * lerp(_Off, _Glow, on);
                col = MixFog(col, i.fog);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
