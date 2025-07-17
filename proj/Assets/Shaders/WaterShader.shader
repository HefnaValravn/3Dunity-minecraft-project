Shader "Custom/WaterShader"
{
    Properties
    {
        _MainColor ("Water Color", Color) = (0.2, 0.388, 0.698, 0.4)
        _ReflectionStrength ("Reflection Strength", Range(0, 1)) = 0.8
        _Transparency ("Transparency", Range(0, 1)) = 0.1
        _SkyboxTexture ("Skybox Texture", CUBE) = "" {}
        _MainTex ("Water Texture", 2D) = "white" {}

    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldNormal : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
            };

            float4 _MainColor;
            float _ReflectionStrength;
            float _Transparency;
            samplerCUBE _SkyboxTexture;
            sampler2D _MainTex;

            v2f vert (appdata v)
            {
                v2f o;
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                o.viewDir = normalize(UnityWorldSpaceViewDir(worldPos));

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Normalize view direction and normal
                float3 viewDir = normalize(i.viewDir);
                float3 normal = normalize(i.worldNormal);

                // Reflection: reflect view direction over surface normal
                float3 reflectionDir = reflect(-viewDir, normal);
                fixed4 reflectionColor = texCUBE(_SkyboxTexture, reflectionDir);

                // Refraction: base color with transparency
                fixed4 refractionColor = _MainColor;
                refractionColor.a = _Transparency;

                // Blend weight based on angle between view and normal
                float viewDot = saturate(dot(viewDir, normal)); // 0 (grazing) -> 1 (looking straight down)

                float reflectAmount = pow(1.0 - viewDot, 2.0) * _ReflectionStrength;
                reflectAmount = saturate(reflectAmount);
                reflectAmount = max(reflectAmount, 0.2);
                float refractAmount = 1.0 - reflectAmount;

                fixed4 texColor = tex2D(_MainTex, i.uv) * 3;
                texColor = saturate(texColor);
                refractionColor.rgb = lerp(_MainColor.rgb, _MainColor.rgb * texColor.rgb, 0.9);

                // Final blended color
                fixed4 finalColor = lerp(refractionColor, reflectionColor, reflectAmount * 1.3);

                finalColor.rgb = lerp(finalColor.rgb, finalColor.rgb * texColor.rgb, 0.3); // Overlay texture
                // Set final alpha (for transparency blending)
                finalColor.a = refractionColor.a * refractAmount + reflectAmount;
                return finalColor;
            }
            ENDCG
        }
    }
}
