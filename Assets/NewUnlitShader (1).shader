Shader "Unlit/NewUnlitShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Bias ("Bias", Float) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque"  "LightMode"="ForwardBase"}
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
            #include "Packages/red.sim.lightvolumes/Shaders/LightVolumes.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
                float3 wpos : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }
            
            float shEvaluateDiffuseL1Geomerics(float L0, float3 L1, float3 n)
            {
                // average energy
                float R0 = L0;
                
                // avg direction of incoming light
                float3 R1 = 0.5f * L1;
                
                // directional brightness
                float lenR1 = length(R1);
                
                // linear angle between normal and direction 0-1
                //float q = 0.5f * (1.0f + dot(R1 / lenR1, n));
                //float q = dot(R1 / lenR1, n) * 0.5 + 0.5;
                float q = dot(normalize(R1), n) * 0.5 + 0.5;
                q = saturate(q); // Thanks to ScruffyRuffles for the bug identity.
                
                // power for q
                // lerps from 1 (linear) to 3 (cubic) based on directionality
                float p = 1.0f + 2.0f * lenR1 / R0;
                
                // dynamic range constant
                // should vary between 4 (highly directional) and 0 (ambient)
                float a = (1.0f - lenR1 / R0) / (1.0f + lenR1 / R0);
                
                return R0 * (a + (1.0f - a) * (p + 1.0f) * pow(q, p));
            }

            float _Bias;
            float4 frag (v2f i) : SV_Target
            {
                float3 L0, L1r, L1g, L1b;
                LightVolumeSH(i.wpos + i.normal *_Bias, L0, L1r, L1g, L1b);
                unity_SHAr = float4(L1r, L0.r);
                unity_SHAg = float4(L1g, L0.g);
                unity_SHAb = float4(L1b, L0.b);
                unity_SHBr = 0;
                unity_SHBg = 0;
                unity_SHBb = 0;
                unity_SHC = 0;
                
                //float3 shs = ShadeSH9(float4(i.normal, 1));
                
                float r = shEvaluateDiffuseL1Geomerics(unity_SHAr.w, unity_SHAr.xyz, i.normal);
                float g = shEvaluateDiffuseL1Geomerics(unity_SHAg.w, unity_SHAg.xyz, i.normal);
                float b = shEvaluateDiffuseL1Geomerics(unity_SHAb.w, unity_SHAb.xyz, i.normal);
                float3 shs = float3(r, g, b);

                return float4(shs, 1);
                //return any(shs < 0);
            }
            ENDCG
        }
    }
}
