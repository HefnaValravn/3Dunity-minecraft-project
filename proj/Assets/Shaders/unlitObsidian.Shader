Shader "Custom/unlitObsidian"
{
    Properties
    {
        _Color("Base Color", Color) = (0, 0, 0, 1)
        _HeightScale("Height Scale", Range(0, 0.1)) = 0.05
        _Smoothness("Smoothness", Range(0, 1)) = 0.8
        _Metallic("Metallic", Range(0, 1)) = 0.9
        _MainTex("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            float _HeightScale;
            float _Smoothness;
            float _Metallic;
            fixed4 _Color;
            sampler2D _MainTex;

            float heightmap(float2 uv)
            {
                return sin(uv.x * 30) * cos(uv.y * 30) * _HeightScale;
            }

            v2f vert(appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                // Normal from heightmap
                float h = heightmap(i.uv);
                float dx = heightmap(i.uv + float2(0.01, 0)) - h;
                float dy = heightmap(i.uv + float2(0, 0.01)) - h;
                float3 normal = normalize(float3(-dx, -dy, 1.0));


                float3 worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, normal));
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 halfDir = normalize(lightDir + viewDir);

                // Sample and blend texture with base color
                half4 textureColor = tex2D(_MainTex, i.uv);
                half3 baseColor = lerp(_Color.rgb, textureColor.rgb, textureColor.a);

                // Diffuse term (Lambert)
                float NdotL = max(0, dot(worldNormal, lightDir));
                half3 diffuse = baseColor * NdotL;

                // Specular term (Blinn-Phong)
                //half vector between light and view directions instead of the reflection vector
                float NdotH = max(0, dot(worldNormal, halfDir));
                //shininess/smoothness controlled by specPower
                float specPower = _Smoothness * 128.0;
                //sharpness of highlight (first part) multiplied by second part for flexibility
                half3 specular = pow(NdotH, specPower) * _Color.rgb * _Metallic;

                // Ambient (minimal, just to avoid full darkness)
                half3 ambient = 0.2 * baseColor;

                // Final color
                half3 finalColor = ambient + diffuse + specular;

                return half4(finalColor, 1.0);
            }
            ENDCG
        }
    }
}