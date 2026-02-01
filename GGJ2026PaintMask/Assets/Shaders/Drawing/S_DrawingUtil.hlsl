#ifndef GGJ2026_DRAWING_COMMON_INCLUDED
#define GGJ2026_DRAWING_COMMON_INCLUDED

float2 closest_point_on_segment(float2 p, float2 a, float2 b, out float t)
{
    float2 ab = b - a;
    float denominator = dot(ab, ab);
    t = (denominator > 1e-8) ? saturate(dot(p - a, ab) / denominator) : 0.0;
    return a + ab * t;
}

// Signed distance to an oriented rectangle centered on segment a->b with halfWidth
float sd_oriented_box(float2 p, float2 segment_a, float2 b, float half_width)
{
    float2 d = b - segment_a;
    float len = length(d);
    if (len < 1e-8)
    {
        return length(p - segment_a) - half_width;
    }
    float2 dir = d / len;
    float2 n = float2(-dir.y, dir.x);

    float2 center = 0.5 * (segment_a + b);
    float2 rel = p - center;

    float x = dot(rel, dir);
    float y = dot(rel, n);

    float2 q = abs(float2(x, y)) - float2(0.5 * len, half_width);
    float2 mq = max(q, 0.0);
    float outside = length(mq);
    float inside = min(max(q.x, q.y), 0.0);
    return outside + inside;
}

float segment_coverage(float2 p_uv, float2 a_uv, float2 b_uv,
                       float width_pixels, float smoothness_pixels, float roundness_01,
                       float2 inv_target_size)
{
    float pixel_size_uv = 0.5 * (inv_target_size.x + inv_target_size.y);
    float r_uv = max(0.0, 0.5 * width_pixels * pixel_size_uv);

    float aa_min_uv = pixel_size_uv * 0.75;
    float feather_uv = max(aa_min_uv, max(0.0, smoothness_pixels) * pixel_size_uv);

    float dist_edge;
    if (roundness_01 >= 0.5)
    {
        float t;
        float2 c = closest_point_on_segment(p_uv, a_uv, b_uv, t);
        dist_edge = length(p_uv - c) - r_uv;
    }
    else
    {
        dist_edge = sd_oriented_box(p_uv, a_uv, b_uv, r_uv);
    }
    return saturate(1.0 - smoothstep(0.0, feather_uv, dist_edge));
}

float4 over(float4 top, float4 bottom)
{
    float a = top.a + bottom.a * (1.0 - top.a);
    float3 rgb = top.rgb * top.a + bottom.rgb * (1.0 - top.a);
    return float4(rgb, a);
}

#endif
