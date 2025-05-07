Shader "Custom/Obsidian"
{
    Properties
    {
        _Color("Base Color", Color) = (0, 0, 0, 1)
        _HeightScale("Height Scale", Range(0, 0.1)) = 0.1
        _Smoothness("Smoothness", Range(0, 1)) = 0.8
        _Metallic("Metallic", Range(0, 1)) = 0.9
        _MainTex("Texture", 2D) = "white" {}

        [NoScaleOffset] _NormalMapTex("Normal Map Texture", 2D) = "bump" {}
        _DispMapBlend("Displacement Map Blend", Range(0, 1)) = 0.5
        _DispTexScale("Displacement Texture Scale", Range(1, 100)) = 10
        [Toggle] _ShowNormalMap("Show Normal Map", Float) = 0

    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }

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
            sampler2D _NormalMapTex;
            float _DispMapBlend;
            float _DispTexScale;
            float _ShowNormalMap;

            float dispMap(float2 uv)
            {
                return sin(uv.x * 100) * cos(uv.y * 100) * _HeightScale;
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
                // Check if we should display normal map visualization
                if (_ShowNormalMap > 0.5) {
                    // Sample normal map at scaled UV coordinates
                    float2 scaledUV = i.uv * _DispTexScale;
                    float3 normalMap = UnpackNormal(tex2D(_NormalMapTex, scaledUV));
                    // Convert normal from [-1,1] range to [0,1] for display
                    return half4(normalMap * 0.5 + 0.5, 1.0);
                }

                // Normal from dispMap function
                float procH = dispMap(i.uv);
                float procdx = dispMap(i.uv + float2(0.5, 0)) - procH;
                float procdy = dispMap(i.uv + float2(0, 0.5)) - procH;
                float3 procnormal = normalize(float3(- procdx, - procdy, 1.0));

                //normal from ChunkMeshGenerator
                float2 scaledUV = i.uv * _DispTexScale;
                float3 texNormal = UnpackNormal(tex2D(_NormalMapTex, scaledUV));

                //blend the two normals
                float3 normal = normalize(lerp(procnormal, texNormal, _DispMapBlend));

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

                // Specular term (Blinn - Phong)
                //half vector between light and view directions instead of the reflection vector
                float NdotH = max(0, dot(worldNormal, halfDir));
                //shininess / smoothness controlled by specPower
                float specPower = _Smoothness * 128.0;
                //sharpness of highlight (first part) multiplied by second part for flexibility
                half3 specular = pow(NdotH, specPower) * _Color.rgb * _Metallic;

                // Ambient lighting
                half3 ambient = 0.4 * baseColor;

                //subtle emissive component for more brightness
                half3 emissive = baseColor * 0.15;

                // Final color
                half3 finalColor = ambient + diffuse + specular + emissive;

                return half4(finalColor, 1.0);
            }
            ENDCG
        }
    }
}