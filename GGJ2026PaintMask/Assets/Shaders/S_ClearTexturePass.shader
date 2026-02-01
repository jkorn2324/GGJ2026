Shader "GGJ2026/S_ClearTexturePass"
{
    Properties
    {
        _ClearColor ("Clear Color", Color) = (0,0,0,0)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Overlay" "RenderType"="Opaque"
        }
        ZWrite Off ZTest Always Cull Off
        Blend Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            float4 _ClearColor;

            struct appdata
            {
                float4 vertex:POSITION;
                float2 uv:TEXCOORD0;
            };

            struct v2f
            {
                float4 pos:SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            float4 frag(v2f i) : SV_Target { return _ClearColor; }
            ENDHLSL
        }
    }
    Fallback Off
}