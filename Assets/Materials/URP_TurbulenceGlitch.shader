Shader "Custom/URP_TurbulenceGlitch"
{
    Properties
    {
        [Header(Turbulence)]
        _Amplitude   ("Амплитуда смещения", Range(0, 0.15))   = 0.022
        _Frequency   ("Частота шума", Range(0.1, 10))         = 2.4
        _Speed       ("Скорость анимации", Range(0, 2))       = 0.18

        [Header(Chromatic Split)]
        _ChromaSplit ("Сила разделения спектра", Range(0, 10)) = 2.1

        [Header(Glitch Tear)]
        _TearStrength ("Сила блочных разрывов", Range(0, 1)) = 1.0
        _TearFreqency  ("Частота срабатывания разрывов", Range(0.5, 10)) = 4.0

        [Header(Look)]
        _GrainStrength ("Зерно", Range(0, 0.15)) = 0.03
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "TurbulenceGlitch"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos  : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float _Amplitude;
            float _Frequency;
            float _Speed;
            float _ChromaSplit;
            float _TearStrength;
            float _TearFreqency;
            float _GrainStrength;

            // ---------------------------------------------------------
            // Ashima simplex noise 2D (порт из GLSL, синтаксис под HLSL)
            // ---------------------------------------------------------
            float3 mod289(float3 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
            float2 mod289(float2 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
            float3 permute(float3 x) { return mod289(((x * 34.0) + 1.0) * x); }

            float snoise(float2 v)
            {
                const float4 C = float4(0.211324865405187, 0.366025403784439,
                                        -0.577350269189626, 0.024390243902439);
                float2 i  = floor(v + dot(v, C.yy));
                float2 x0 = v - i + dot(i, C.xx);
                float2 i1 = (x0.x > x0.y) ? float2(1.0, 0.0) : float2(0.0, 1.0);
                float4 x12 = x0.xyxy + C.xxzz;
                x12.xy -= i1;
                i = mod289(i);
                float3 p = permute(permute(i.y + float3(0.0, i1.y, 1.0)) + i.x + float3(0.0, i1.x, 1.0));
                float3 m = max(0.5 - float3(dot(x0, x0), dot(x12.xy, x12.xy), dot(x12.zw, x12.zw)), 0.0);
                m = m * m;
                m = m * m;
                float3 x = 2.0 * frac(p * C.www) - 1.0;
                float3 h = abs(x) - 0.5;
                float3 ox = floor(x + 0.5);
                float3 a0 = x - ox;
                m *= 1.79284291400159 - 0.85373472095314 * (a0 * a0 + h * h);
                float3 g;
                g.x = a0.x * x0.x + h.x * x0.y;
                g.yz = a0.yz * x12.xz + h.yz * x12.yw;
                return 130.0 * dot(m, g);
            }

            float fbm(float2 p)
            {
                float v = 0.0;
                float amp = 0.5;
                [unroll]
                for (int i = 0; i < 4; i++)
                {
                    v += amp * snoise(p);
                    p *= 2.02;
                    amp *= 0.5;
                }
                return v;
            }

            float hash1(float n) { return frac(sin(n) * 43758.5453123); }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.screenPos = ComputeScreenPos(OUT.positionCS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;

                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 p = (screenUV - 0.5) * float2(aspect, 1.0);

                float t = _Time.y * _Speed;

                float2 q = float2(
                    fbm(p * _Frequency + float2(0.0, 0.0) + t * 0.6),
                    fbm(p * _Frequency + float2(5.2, 1.3) - t * 0.6)
                );
                float2 r = float2(
                    fbm(p * _Frequency + 1.8 * q + float2(1.7, 9.2) + t * 0.35),
                    fbm(p * _Frequency + 1.8 * q + float2(8.3, 2.8) + t * 0.28)
                );
                float2 dv = r * _Amplitude;

                float band = floor((p.y * 0.5 + 0.5) * 46.0);
                float triggerSeed = floor(_Time.y * _TearFreqency);
                float trigger = step(0.972, hash1(band * 13.17 + triggerSeed * 7.31));
                float tearAmt = (hash1(band * 3.7 + triggerSeed * 1.9) - 0.5) * 0.05 * trigger * _TearStrength;
                dv.x += tearAmt;

                float mag = length(dv);

                float2 dir = mag > 1e-6 ? dv / mag : float2(0.0, 0.0);
                float splitAmt = _ChromaSplit * mag;
                float2 splitOffset = dir * splitAmt;

                float2 uvG = screenUV + dv;
                float2 uvR = uvG + splitOffset;
                float2 uvB = uvG - splitOffset;

                uvR = saturate(uvR);
                uvG = saturate(uvG);
                uvB = saturate(uvB);

                float3 colR = SampleSceneColor(uvR);
                float3 colG = SampleSceneColor(uvG);
                float3 colB = SampleSceneColor(uvB);

                float3 color = float3(colR.r, colG.g, colB.b);

                if (_GrainStrength > 0.0001)
                {
                    float g = (hash1(dot(IN.positionCS.xy, float2(12.9898, 78.233)) + _Time.y * 60.0) - 0.5);
                    color += g * _GrainStrength;
                }

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
