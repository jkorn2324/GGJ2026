Shader "GGJ2026/S_DrawingSinglePass"
{
    Properties
    {
        _StrokeTex ("Stroke Data Tex", 2D) = "black" {}
        _StrokeCount ("Stroke Count", Float) = 0

        _TapeTex ("Tape Data Tex", 2D) = "black" {}
        _TapeCount ("Tape Count", Float) = 0

        _TargetSize ("Target Size (w,h,1/w,1/h)", Vector) = (512,512,0.001953125,0.001953125)
        _Background ("Background", Color) = (0,0,0,0)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Overlay" "RenderType"="Opaque"
        }
        ZWrite Off
        ZTest Always
        Cull Off
        Blend Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "S_DrawingUtil.hlsl"

            #ifndef MAX_STROKES
            #define MAX_STROKES 256
            #endif

            #ifndef MAX_TAPES
            #define MAX_TAPES 128
            #endif

            sampler2D _StrokeTex;
            float4 _StrokeTex_TexelSize;
            float _StrokeCount;

            sampler2D _TapeTex;
            float4 _TapeTex_TexelSize;
            float _TapeCount;

            float4 _TargetSize; // (w,h,1/w,1/h)
            float4 _Background;

            struct appdata
            {
                float4 vertex:POSITION;
                float2 uv:TEXCOORD0;
            };

            struct v2f
            {
                float4 pos:SV_POSITION;
                float2 uv:TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 load_stroke_texel(int texel_index)
            {
                float u = (texel_index + 0.5) * _StrokeTex_TexelSize.x;
                return tex2D(_StrokeTex, float2(u, 0.5));
            }

            float4 load_tape_texel(int texel_index)
            {
                float u = (texel_index + 0.5) * _TapeTex_TexelSize.x;
                return tex2D(_TapeTex, float2(u, 0.5));
            }

            // Returns:
            //   -1  => no tape covered at stroke time
            //   >=0 => tape index that covered at stroke time (topmost by tape index)
            int find_top_tape_covering_pixel_at_stroke(float2 pUV, int strokeIndex, float2 invTarget)
            {
                int tape_count = (int)min(_TapeCount, (float)MAX_TAPES);
                int best_tape = -1;
                [loop]
                for (int t = 0; t < MAX_TAPES; t++)
                {
                    if (t >= tape_count) break;

                    int base_texel = t * 4;
                    float4 t0 = load_tape_texel(base_texel + 0);
                    float4 t1 = load_tape_texel(base_texel + 1);
                    float4 t3 = load_tape_texel(base_texel + 3);

                    float start_stroke = t1.y;
                    float finished_stroke = t1.z;
                    // Tape affects strokes strictly after placement
                    if ((float)strokeIndex <= start_stroke)
                    {
                        continue;
                    }
                    // If tape was removed and the stroke happened after removal, tape wasn't present then
                    if (finished_stroke >= 0.0 && (float)strokeIndex > finished_stroke)
                    {
                        continue;
                    }
                    float2 aUV = t0.xy;
                    float2 bUV = t0.zw;
                    float width_pixels = max(0.0, t1.x);
                    float smoothness_pixels = max(0.0, t3.x);
                    float roundness = t3.y;
                    // Expensive check last
                    float cov = segment_coverage(pUV, aUV, bUV, width_pixels, smoothness_pixels, roundness, invTarget);
                    if (cov <= 0.5)
                    {
                        continue;
                    }
                    // Later tape indices are on top (back-to-front tape ordering)
                    best_tape = t;
                }

                return best_tape;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 pUV = i.uv;
                float2 invTarget = float2(_TargetSize.z, _TargetSize.w);

                int strokeCount = (int)min(_StrokeCount, (float)MAX_STROKES);
                int tapeCount = (int)min(_TapeCount, (float)MAX_TAPES);

                float4 baseAccum = float4(0, 0, 0, 0);
                float4 tapeAccum = float4(0, 0, 0, 0);

                // Strokes x Tapes, ensure that 
                [loop]
                for (int s = 0; s < MAX_STROKES; s++)
                {
                    if (s >= strokeCount)
                    {
                        break;
                    }\
                    int baseTexel = s * 3;
                    float4 st0 = load_stroke_texel(baseTexel + 0);
                    float4 st1 = load_stroke_texel(baseTexel + 1);
                    float4 st2 = load_stroke_texel(baseTexel + 2);

                    float2 aUV = st0.xy;
                    float2 bUV = st0.zw;

                    float3 rgb = st1.rgb;
                    float widthPixels = max(0.0, st1.a);

                    float alpha = saturate(st2.r);
                    float featherPixels = max(0.0, st2.g);
                    float roundCaps = st2.b;

                    float cov = segment_coverage(pUV, aUV, bUV, widthPixels, featherPixels, roundCaps, invTarget);
                    float segA = cov * alpha;

                    // If this stroke doesn't contribute at this pixel, skip tape scan
                    if (segA <= 0.0001)
                    {
                        continue;
                    }
                    // Find which tape (if any) covered this pixel at the time this stroke was drawn
                    int tapeIndexAtStroke = -1;
                    if (tapeCount > 0)
                    {
                        tapeIndexAtStroke = find_top_tape_covering_pixel_at_stroke(pUV, s, invTarget);
                    }
                    if (tapeIndexAtStroke < 0)
                    {
                        baseAccum = over(float4(rgb, segA), baseAccum);
                        continue;
                    }
                    // If the covering tape is removed now, paint drawn on it disappears
                    {
                        int tBase = tapeIndexAtStroke * 4;
                        float4 t1 = load_tape_texel(tBase + 1);
                        float finishedStroke = t1.z;

                        if (finishedStroke >= 0.0)
                        {
                            continue;
                        }
                    }
                    tapeAccum = over(float4(rgb, segA), tapeAccum);
                }

                // Tape Overlay Everything.
                float4 tapeOverlayAccum = float4(0, 0, 0, 0);
                [loop]
                for (int t = 0; t < MAX_TAPES; t++)
                {
                    if (t >= tapeCount) break;

                    int baseTexel = t * 4;
                    float4 t0 = load_tape_texel(baseTexel + 0);
                    float4 t1 = load_tape_texel(baseTexel + 1);
                    float4 t2 = load_tape_texel(baseTexel + 2);
                    float4 t3 = load_tape_texel(baseTexel + 3);

                    float finishedStroke = t1.z;
                    if (finishedStroke >= 0.0)
                    {
                        continue;
                    }
                    float2 aUV = t0.xy;
                    float2 bUV = t0.zw;

                    float widthPixels = max(0.0, t1.x);

                    float4 tapeColor = t2;
                    float featherPixels = max(0.0, t3.x);
                    float roundCaps = t3.y;

                    float cov = segment_coverage(pUV, aUV, bUV, widthPixels, featherPixels, roundCaps, invTarget);
                    float a = cov * tapeColor.a;

                    if (a > 0.0001)
                    {
                        tapeOverlayAccum = over(float4(tapeColor.rgb, a), tapeOverlayAccum);
                    }
                }
                // Composite Everything together.
                float4 outCol = _Background;
                outCol = over(baseAccum, outCol);
                outCol = over(tapeOverlayAccum, outCol);
                outCol = over(tapeAccum, outCol);
                return outCol;
            }
            ENDHLSL
        }
    }
    Fallback Off
}