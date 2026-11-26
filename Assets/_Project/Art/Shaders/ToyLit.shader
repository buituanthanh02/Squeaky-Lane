Shader "SqueakyLane/Toy Lit"
{
    Properties
    {
        _MainTex ("Colormap", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
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
    }

    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        Pass
        {
            Name "FORWARD"
            Tags { "LightMode"="ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

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
                half3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half4 _Color, _ShadowColor, _RimColor; // _SpecColor đã khai báo sẵn trong UnityLightingCommon (Lighting.cginc)
            half _RampThreshold, _RampSmooth, _AmbientStrength;
            half _SpecStrength, _SpecPower, _SpecSize, _SpecSmooth;
            half _RimStrength, _RimMin, _RimMax;

            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half3 baseColor = tex2D(_MainTex, i.uv).rgb * _Color.rgb;
                half3 normal = normalize(i.worldNormal);
                half3 lightDir = normalize(UnityWorldSpaceLightDir(i.worldPos));
                // Camera trực giao có cùng hướng nhìn ở mọi điểm trên xe.
                half3 viewDir = normalize(lerp(UnityWorldSpaceViewDir(i.worldPos),
                    UNITY_MATRIX_V[2].xyz, unity_OrthoParams.w));
                half ndl = dot(normal, lightDir);
                half ramp = smoothstep(_RampThreshold - _RampSmooth, _RampThreshold + _RampSmooth,
                    ndl * 0.5h + 0.5h);
                half3 color = baseColor * lerp(_ShadowColor.rgb, _LightColor0.rgb, ramp);
                color += baseColor * max(ShadeSH9(half4(normal, 1)), 0) * _AmbientStrength;

                half3 halfway = lightDir + viewDir;
                halfway *= rsqrt(max(dot(halfway, halfway), 0.0001h));
                half gloss = pow(saturate(dot(normal, halfway)), _SpecPower);
                half spec = smoothstep(_SpecSize - _SpecSmooth, _SpecSize + _SpecSmooth, gloss);
                color += _SpecColor.rgb * (_SpecStrength * spec * ramp) * _LightColor0.rgb;

                half rim = 1 - saturate(dot(normal, viewDir));
                half rimEnd = max(_RimMax, _RimMin + 0.001h);
                color += _RimColor.rgb * (_RimStrength * smoothstep(_RimMin, rimEnd, rim)
                    * saturate(ndl + 0.3h));
                return half4(color, 1);
            }
            ENDCG
        }
    }
}
