Shader "GGJ2026/S_DiffRTReduceShader"
{
    Properties {}
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque" "Queue"="Overlay"
        }
        ZWrite Off ZTest Always Cull Off
        Blend Off

        Pass // 0: Diff
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _TexA; // The first texture, set by SetTexture.
            sampler2D _TexB; // The second texture, set by SetTexture.
            float _UseLuma;
            float _IgnoreAlpha;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float Luma(float3 rgb)
            {
                return dot(rgb, float3(0.2126, 0.7152, 0.0722));
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float4 a = tex2D(_TexA, i.uv);
                float4 b = tex2D(_TexB, i.uv);
                bool ignore_alpha = _IgnoreAlpha > 0.5;
                bool use_luma = _UseLuma > 0.5;
                if (use_luma)
                {
                    float err = abs(Luma(a.rgb) - Luma(b.rgb));
                    if (!ignore_alpha)
                    {
                        err = max(err, abs(a.a - b.a));
                    }
                    return float4(err, 0, 0, 1);
                }
                if (ignore_alpha)
                {
                    float3 d = abs(a.rgb - b.rgb);
                    float err = (d.x + d.y + d.z) / 3.0;
                    return float4(err, 0, 0, 1);
                }
                float4 delta = abs(a.rgba - b.rgba);
                float err = (delta.x + delta.y + delta.z + delta.w) / 4.0;
                return float4(err, 0, 0, 1);
            }
            ENDHLSL
        }

        Pass // 1: Reduce (avg 2x2)
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _TexReduction; // The Diff Texture/Texture Reduction.
            float4 _InvSrcSize; // (1/w, 1/h, w, h)

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                const float texel_offset = 0.5;
                float2 texel = _InvSrcSize.xy;
                // Sample a 2x2 neighborhood in source to create one pixel in half-res target.
                // Offsets chosen to cover the 2x2 block around this uv.
                float2 uv = i.uv;
                float e0 = tex2D(_TexReduction, uv + texel * float2(-texel_offset, -texel_offset)).r;
                float e1 = tex2D(_TexReduction, uv + texel * float2(texel_offset, -texel_offset)).r;
                float e2 = tex2D(_TexReduction, uv + texel * float2(-texel_offset, texel_offset)).r;
                float e3 = tex2D(_TexReduction, uv + texel * float2(texel_offset, texel_offset)).r;

                float avg = (e0 + e1 + e2 + e3) / 4.0;
                return float4(avg, 0, 0, 1);
            }
            ENDHLSL
        }
    }

    Fallback Off
}