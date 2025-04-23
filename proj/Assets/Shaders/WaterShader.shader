Shader "Custom/WaterShader"
{
    Properties
    {
        _MainColor ("Water Color", Color) = (0.2, 0.5, 0.7, 0.6)
        _ReflectionStrength ("Reflection Strength", Range(0, 1)) = 0.7
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
                float3 worldPos : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
                float3 worldViewDir : TEXCOORD3;
            };

            float4 _MainColor;
            float _ReflectionStrength;
            float _WaveSpeed;
            float _WaveAmplitude;

            samplerCUBE _Skybox;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldViewDir = normalize(WorldSpaceViewDir(v.vertex));

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Get the normalized view direction
                float3 viewDir = normalize(i.worldViewDir);

                // Get the surface normal
                float3 normal = normalize(i.worldNormal);

                // Calculate reflection vector
                float3 reflectionDir = reflect(- viewDir, normal);

                // Sample the skybox using the reflection direction
                fixed4 reflectionColor = texCUBE(_Skybox, reflectionDir);

                // Calculate dot product between view direction and normal
                // Closer to 0 = more parallel to surface (more reflective)
                // Closer to 1 = more perpendicular to surface (more refractive)
                float dotProduct = dot(viewDir, normal);

                // Invert and scale the dot product to get reflectivity
                // (1 - dotProduct) is higher when looking at glancing angles (more reflective)
                float reflectivity = pow(1.0 - dotProduct, 4.0) * _ReflectionStrength;

                // Create water base color with transparency
                float4 waterColor = _MainColor;

                // Calculate transparency based on view angle (more transparent when looking straight down)
                // When dotProduct is close to 1 (looking straight down), water should be more transparent
                waterColor.a = lerp(_MainColor.a * 0.3, _MainColor.a, pow(dotProduct, 2.0));


                // Calculate final color by blending reflection and water color
                fixed4 finalColor = lerp(waterColor, reflectionColor, reflectivity);

                return finalColor;
            }
            ENDCG
        }
    }
}