Shader "SqueakyLane/Toy Ground"
{
    Properties
    {
        [Header(Ground Palette)]
        _MainTex ("Ground Color Atlas", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _AmbientColor ("Ambient Tint", Color) = (0.604,0.663,0.761,1)
        _AmbientStrength ("Ambient Tint Strength", Range(0,1)) = 0.12
        [Header(Grass Detail)]
        [NoScaleOffset] _DetailTex ("Grass Detail", 2D) = "white" {}
        _DetailScale ("Detail Tiles per World Unit", Range(0.05,4)) = 0.5
        _DetailStrength ("Detail Strength", Range(0,1)) = 0.35
    }

    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 worldXZ : TEXCOORD1;
                half3 worldNormal : TEXCOORD2;
            };

            sampler2D _MainTex, _DetailTex;
            float4 _MainTex_ST, _MainTex_TexelSize;
            half4 _Color, _AmbientColor;
            half _AmbientStrength, _DetailStrength;
            float _DetailScale;

            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldXZ = mul(unity_ObjectToWorld, v.vertex).xz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                // Tâm texel giữ đúng màu atlas cả trên backend dùng chung sampler/texture.
                float2 cell = min(floor(saturate(i.uv) * _MainTex_TexelSize.zw), _MainTex_TexelSize.zw - 1);
                float2 paletteUV = (cell + 0.5) * _MainTex_TexelSize.xy;
                // Chỉ khóa cột (không lẫn màu cột bên); theo hàng giữ nội suy để cỏ xa → gần chuyển mềm, không thành vệt ngang.
                paletteUV.y = clamp(i.uv.y, 0.5 * _MainTex_TexelSize.y, 1 - 0.5 * _MainTex_TexelSize.y);
                half3 color = tex2D(_MainTex, paletteUV).rgb * _Color.rgb;
                // Cột 0 là cỏ. Không nhuộm sơn cổng, vỉa hè hoặc mặt đứng của khay.
                half grassMask = (1 - step(0.5, cell.x)) * step(0.5001, normalize(i.worldNormal).y);
                half4 detail = tex2D(_DetailTex, i.worldXZ * _DetailScale);
                half3 grass = lerp(color * detail.rgb, detail.rgb, detail.a);
                color = lerp(color, grass, grassMask * _DetailStrength);
                return half4(color * lerp(1, _AmbientColor.rgb, _AmbientStrength), 1);
            }
            ENDCG
        }
    }
}
