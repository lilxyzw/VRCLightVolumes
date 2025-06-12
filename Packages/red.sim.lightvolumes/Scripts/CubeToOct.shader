Shader "Hidden/CubeToOct" {

    Properties {
        _CubeTex("Cubemap", CUBE) = "" {}
        _Padding("Padding (in pixels)", Float) = 0
        _TextureSize("Texture Size", Float) = 128
    }

    SubShader {

        Tags { "RenderType" = "Opaque" }
        Cull Off ZWrite Off ZTest Always

        Pass {

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            samplerCUBE _CubeTex;
            float _Padding;
            float _TextureSize;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float3 UnpackNormalOctQuadEncode(float2 f) {
                float3 n = float3(f.x, f.y, 1.0 - (f.x < 0 ? -f.x : f.x) - (f.y < 0 ? -f.y : f.y));
                float t = max(-n.z, 0.0);
                n.xy += float2(n.x >= 0.0 ? -t : t, n.y >= 0.0 ? -t : t);
                return normalize(n);
            }

            v2f vert(appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                float padAsp = _Padding / _TextureSize;
                float2 uv = (padAsp * 2 + 1) * i.uv - padAsp;
                float3 dir = UnpackNormalOctQuadEncode(saturate(uv)  * 2.0 - 1.0);
                return texCUBE(_CubeTex, dir);
            }

            ENDCG

        }
    }

    FallBack Off

}