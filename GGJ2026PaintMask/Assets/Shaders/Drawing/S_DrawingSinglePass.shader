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
                return tex2Dlod(_StrokeTex, float4(u, 0.5, 0, 0)); // LOD 0
            }

            float4 load_tape_texel(int texel_index)
            {
                float u = (texel_index + 0.5) * _TapeTex_TexelSize.x;
                return tex2Dlod(_TapeTex, float4(u, 0.5, 0, 0));   // LOD 0
            }

            // Returns:
            //   -1  => no tape covered at stroke time
            //   >=0 => tape index that covered at stroke time (topmost by tape index)
            int find_top_tape_covering_pixel_at_stroke(float2 p_uv, int stroke_index, float2 inv_target)
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

                    float start_stroke_index = t1.y;
                    float affected_stroke_count = t1.z;
                    bool is_finished_flag = t1.w > 0.5;
                    // Tape affects strokes strictly after placement
                    if ((float)stroke_index < start_stroke_index)
                    {
                        continue;
                    }
                    float finished_item_index = affected_stroke_count + start_stroke_index;
                    // If tape was removed and the stroke happened after removal, tape wasn't present then
                    if (is_finished_flag && (float)stroke_index > finished_item_index)
                    {
                        continue;
                    }
                    float2 a_uv = t0.xy;
                    float2 b_uv = t0.zw;
                    float width_pixels = max(0.0, t1.x);
                    float smoothness_pixels = max(0.0, t3.x);
                    float roundness = t3.y;
                    // Expensive check last
                    float cov = segment_coverage(p_uv, a_uv, b_uv, width_pixels, smoothness_pixels, roundness, inv_target);
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

                int stroke_count = (int)min(_StrokeCount, (float)MAX_STROKES);
                int tape_count = (int)min(_TapeCount, (float)MAX_TAPES);

                float4 base_accum = float4(0, 0, 0, 0);
                float4 tape_accum = float4(0, 0, 0, 0);

                [loop]
                for (int stroke_index = 0; stroke_index < MAX_STROKES; stroke_index++)
                {
                    if (stroke_index >= stroke_count)
                    {
                        break;
                    }
                    int baseTexel = stroke_index * 3;
                    float4 st0 = load_stroke_texel(baseTexel + 0);
                    float4 st1 = load_stroke_texel(baseTexel + 1);
                    float4 st2 = load_stroke_texel(baseTexel + 2);

                    float2 aUV = st0.xy;
                    float2 bUV = st0.zw;

                    float3 rgb = st1.rgb;
                    float widthPixels = max(0.0, st1.a);

                    float alpha = saturate(st2.r);
                    float smoothness_pixels = max(0.0, st2.g);
                    float round_corners = st2.b;

                    float cov = segment_coverage(pUV, aUV, bUV, widthPixels, smoothness_pixels, round_corners, invTarget);
                    float segA = cov * alpha;

                    // If this stroke doesn't contribute at this pixel, skip tape scan
                    if (segA <= 0.0001)
                    {
                        continue;
                    }
                    // Find which tape (if any) covered this pixel at the time this stroke was drawn
                    int tapeIndexAtStroke = -1;
                    if (tape_count > 0)
                    {
                        tapeIndexAtStroke = find_top_tape_covering_pixel_at_stroke(pUV, stroke_index, invTarget);
                    }
                    if (tapeIndexAtStroke < 0)
                    {
                        base_accum = over(float4(rgb, segA), base_accum);
                        continue;
                    }
                    // If the covering tape is removed now, paint drawn on it disappears
                    {
                        int tBase = tapeIndexAtStroke * 4;
                        float4 t1 = load_tape_texel(tBase + 1);
                        bool did_finish_flag = t1.w > 0.5;
                        if (did_finish_flag)
                        {
                            continue;
                        }
                    }
                    tape_accum = over(float4(rgb, segA), tape_accum);
                }

                // Tape Overlay Everything.
                float4 tapeOverlayAccum = float4(0, 0, 0, 0);
                [loop]
                for (int t = 0; t < MAX_TAPES; t++)
                {
                    if (t >= tape_count) break;

                    int baseTexel = t * 4;
                    float4 t0 = load_tape_texel(baseTexel + 0);
                    float4 t1 = load_tape_texel(baseTexel + 1);
                    float4 t2 = load_tape_texel(baseTexel + 2);
                    float4 t3 = load_tape_texel(baseTexel + 3);

                    bool is_finished_flag = t1.w > 0.5;
                    if (is_finished_flag)
                    {
                        continue;
                    }
                    float2 a_uv = t0.xy;
                    float2 b_uv = t0.zw;
                    float width_pixels = max(0.0, t1.x);
                    float4 tape_color = t2;
                    float smoothness_pixels = max(0.0, t3.x);
                    float rounded_corners = t3.y;
                    float cov = segment_coverage(pUV, a_uv, b_uv, width_pixels, smoothness_pixels, rounded_corners, invTarget);
                    float a = cov * tape_color.a;

                    if (a > 0.0001)
                    {
                        tapeOverlayAccum = over(float4(tape_color.rgb, a), tapeOverlayAccum);
                    }
                }
                // Composite Everything together.
                float4 outCol = _Background;
                outCol = over(base_accum, outCol);
                outCol = over(tapeOverlayAccum, outCol);
                outCol = over(tape_accum, outCol);
                return outCol;
            }
            ENDHLSL
        }
    }
    Fallback Off
}