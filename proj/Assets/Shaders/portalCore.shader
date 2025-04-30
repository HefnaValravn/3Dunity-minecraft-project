Shader "Custom/portalCore" {
    Properties {
        _MainColor ("Main Color", Color) = (0.5, 0.1, 0.8, 0.8)
        _SecondaryColor ("Secondary Color", Color) = (0.7, 0.3, 1, 0.6)
        _Intensity ("Intensity", Range(0, 2)) = 1
        _Speed ("Animation Speed", Range(0, 10)) = 2
        _Transparency ("Transparency", Range(0, 1)) = 0.7
        _WaveStrength ("Wave Distortion", Range(0, 0.1)) = 0.05
        _WaveSpeed ("Wave Speed", Range(0, 5)) = 4
        _ParticleCount ("Particle Count", Range(1, 20)) = 8
        _ParticleSize ("Particle Size", Range(0.001, 0.05)) = 0.01
        _ParticleSpeed ("Particle Speed", Range(0.1, 2)) = 0.3
        _ParticleColor ("Particle Color", Color) = (0.05, 0.05, 0.05, 0.8)

    }
    SubShader {
        Tags {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass {
            Name "ForwardLit"
            Tags {"LightMode" = "UniversalForward"}

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _MainColor;
            float4 _SecondaryColor;
            float _Intensity;
            float _Speed;
            float _Transparency;
            float _WaveStrength;
            float _WaveSpeed;
            float _ParticleCount;
            float _ParticleSize;
            float _ParticleSpeed;
            float4 _ParticleColor;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct V2F {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            V2F vert(Attributes input) {
                V2F output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            // Hash function for randomization
            float hash(float2 p) {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            // Generate a single Minecraft - style portal particle
            float particleShape(float2 uv, float2 pos, float size, float squiggleAmount, float trailLength, float direction) {
                // Calculate basic distance but stretched along x - axis for longer particles
                float2 delta = uv - pos;

                // Apply squiggle deformation - make the line wavy
                // This shifts the sample point based on a sine wave
                float squiggleOffset = sin(delta.x * 10.0) * squiggleAmount;
                delta.y -= squiggleOffset;

                // Create directional bend based on movement direction
                float bendAmount = 0.2 * direction; // Positive bends one way, negative the other
                delta.y -= delta.x * bendAmount; // Bend based on x - distance from center

                // Create elongated shape - much longer on x - axis (direction of travel)
                float2 stretchedDelta = float2(delta.x * 0.5, delta.y * 2.0); // 4x longer than tall
                float dist = length(stretchedDelta) / size;

                // Create smooth falloff
                return smoothstep(trailLength, 0, dist); // Changed 'length' to 'trailLength'
            }

            half4 frag(V2F input) : SV_Target {
                float2 uv = input.uv;
                float2 center = float2(0.5, 0.5);
                float2 uvFromCenter = uv - center;
                float dist = length(uvFromCenter);

                // Time values for different wave effects
                float t1 = _Time.y * _WaveSpeed;
                float t2 = _Time.y * _WaveSpeed * 0.8;
                float t3 = _Time.y * _WaveSpeed * 1.2;

                // Create concentric ripples moving outward
                float wave1 = sin(dist * 20.0 - t1 * 2.0) * 0.5 + 0.5;
                float wave2 = sin(dist * 15.0 - t2 * 1.5) * 0.5 + 0.5;
                float wave3 = sin((uv.x + uv.y) * 12.0 - t3) * 0.5 + 0.5;

                // Combine waves and fade effect toward edges
                float combinedWave = (wave1 + wave2 + wave3) * 0.5;
                combinedWave *= smoothstep(0.8, 0.2, dist * 1.2); // Stronger in center

                // Apply wave distortion to UVs
                float2 direction = normalize(uvFromCenter + 0.00001); // Avoid division by zero
                float2 distortion = direction * combinedWave * _WaveStrength;
                float2 distortedUV = uv + distortion;

                // Sample the video texture
                half4 videoColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUV);

                // Time - based animation
                float t = _Time.y * _Speed;
                float animValue = sin(t) * 0.5 + 0.5;

                // Animated color mixing between main and secondary color
                float3 animColor = lerp(_MainColor.rgb, _SecondaryColor.rgb, animValue);

                // Minecraft - style particles
                float particles = 0.0;

                // Create multiple particles with different seeds
                for (int i = 0; i < int(_ParticleCount); i ++) {
                    // Generate unique random values for each particle
                    float randomSeed = float(i) / _ParticleCount;

                    // Starting positions (spread across portal height)
                    float startY = hash(float2(randomSeed, 0.789));

                    // Determine direction (left to right or right to left)
                    float direction = (hash(float2(randomSeed, 0.246)) > 0.5) ? 1.0 : - 1.0;

                    // Horizontal movement
                    float hSpeed = _ParticleSpeed * (0.3 + hash(float2(randomSeed, 0.456)) * 0.4);
                    float xTime = _Time.y * hSpeed;

                    // If moving right, start from left side and move right
                    // If moving left, start from right side and move left
                    float xPos = direction > 0 ?
                    frac(hash(float2(randomSeed, 0.123)) + xTime) :
                    frac(hash(float2(randomSeed, 0.123)) - xTime);

                    // Vertical ondulation
                    float waveHeight = 0.03 + 0.04 * hash(float2(randomSeed, 0.567));
                    float waveFreq = 3.0 + 4.0 * hash(float2(randomSeed, 0.891));
                    float yOffset = sin(_Time.y * hSpeed * waveFreq + randomSeed * 6.28) * waveHeight;
                    float yPos = frac(startY + yOffset);

                    // Final position
                    float px = xPos;
                    float py = yPos;

                    // Squiggle parameters - unique per particle
                    float squiggleAmount = 0.02 * (0.5 + hash(float2(randomSeed, 0.993)) * 1.0);
                    float trailLength = 0.3 + hash(float2(randomSeed, 0.771)) * 0.4; // Longer trails

                    // Make particles longer in direction of movement
                    float particleLength = direction * _ParticleSize * 15.0; // Much longer
                    float particleHeight = _ParticleSize * 0.3; // Thinner

                    // Make particles smaller and more transparent
                    float particleSize = _ParticleSize * 0.4 * (0.8 + hash(float2(randomSeed, 0.321)) * 0.4);

                    // Skip rotation - we'll use direction - based deformation instead

                    // Calculate particle shape with squiggle and bending
                    float particle = particleShape(uv, float2(px, py), particleSize, squiggleAmount, trailLength, direction);

                    // Fade in / out as particle moves horizontally or approaches edges
                    float fadeEffect = 1.0;
                    if (direction > 0) {
                        fadeEffect = smoothstep(0.0, 0.1, px) * smoothstep(1.0, 0.9, px);
                    } else {
                        fadeEffect = smoothstep(1.0, 0.9, px) * smoothstep(0.0, 0.1, px);
                    }
                    particle *= fadeEffect;

                    particles += particle;
                }

                // Clamp particles to avoid over - brightening
                particles = min(particles, 1.0);

                // Add particles to final color
                float3 particleEffect = _ParticleColor.rgb * particles * _ParticleColor.a * 5.0;
                // Combine video texture with animated color
                float3 baseColor = videoColor.rgb * animColor;
                float3 finalColor = baseColor + particleEffect * 1.5; // Use addition instead of lerp

                // Calculate alpha - oscillate between opaque (1.0) and your transparency value
                // This makes it pulse between completely opaque and your set transparency level
                float minAlpha = _Transparency; // The most transparent it will get
                float maxAlpha = 0.95; // Completely opaque
                float alphaOscillation = sin(t * 0.7) * 0.5 + 0.5; // Oscillation between 0 - 1
                float finalAlpha = lerp(minAlpha, maxAlpha, alphaOscillation);

                //if particle visible, increase alpha slightly
                finalAlpha = max(finalAlpha, particles * _ParticleColor.a * 0.4);


                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Unlit"
}
