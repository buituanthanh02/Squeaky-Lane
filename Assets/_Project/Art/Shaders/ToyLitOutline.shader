Shader "SqueakyLane/Toy Lit Outline"
{
    Properties
    {
        _MainTex ("Colormap", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [Header(016 Paint bands)]
        [Gamma] _TopTint ("Top Value (sRGB multiplier)", Range(0,1)) = 1
        _TopSaturation ("Top Saturation Multiplier", Range(0,2)) = 1
        _TopThreshold ("Top Normal Y Threshold", Range(0,1)) = 0.6
        _TopSmooth ("Top Edge Softness", Range(0.001,0.2)) = 0.04
        [Header(Two Tone Lighting)]
        _ShadowColor ("Shadow Tint", Color) = (0.549,0.525,0.690,1)
        _RampThreshold ("Ramp Threshold", Range(0,1)) = 0.64
        _RampSmooth ("Ramp Softness", Range(0.001,0.25)) = 0.075
        _AmbientStrength ("Ambient Strength", Range(0,1)) = 0.35
        [Header(Plastic Highlight)]
        _SpecColor ("Highlight Color", Color) = (1,1,1,1)
        _SpecStrength ("Highlight Strength", Range(0,1)) = 0.8
        _SpecPower ("Highlight Power", Range(1,64)) = 12
        _SpecSize ("Highlight Threshold", Range(0.01,0.99)) = 0.9
        _SpecSmooth ("Highlight Softness", Range(0.001,0.2)) = 0.03
        [Header(Rim Light)]
        _RimColor ("Rim Color", Color) = (1,0.96,0.87,1)
        _RimStrength ("Rim Strength", Range(0,1)) = 0.35
        _RimMin ("Rim Start", Range(0,1)) = 0.55
        _RimMax ("Rim End", Range(0,1)) = 0.75
        [Header(Outline)]
        _OutlineWidth ("Width (pixels per 1080 lines)", Range(0,5)) = 1.6
        _OutlineColor ("Outline Color", Color) = (0.169,0.129,0.251,1)
    }

    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        UsePass "SqueakyLane/Toy Lit/FORWARD"
        Pass
        {
            Name "OUTLINE"
            Cull Front

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float _OutlineWidth;
            half4 _OutlineColor;

            float4 vert(appdata v) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(v);
                float4 clipPos = UnityObjectToClipPos(v.vertex);
                float3 normalVS = mul((float3x3)UNITY_MATRIX_V, UnityObjectToWorldNormal(v.normal));
                float4 clipNormal = mul(UNITY_MATRIX_P, float4(normalVS, 0));
                // Chiếu hướng normal lên màn hình rồi chuẩn hóa bằng đơn vị pixel.
                float2 pixelDir = (clipNormal.xy * clipPos.w - clipPos.xy * clipNormal.w) * _ScreenParams.xy;
                pixelDir *= rsqrt(max(dot(pixelDir, pixelDir), 0.0001));
                float width = _OutlineWidth * _ScreenParams.y / 1080.0;
                clipPos.xy += pixelDir * (2.0 * width * clipPos.w / _ScreenParams.xy);
                return clipPos;
            }

            half4 frag() : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }
    }
}
