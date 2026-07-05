Shader "Custom/AreaGlitch_URP"
{
    Properties
    {
        [Header(Glitch Timing)]
        _UpdateInterval ("Update interval", Range(0.02, 5)) = 1

        [Header(Glitch Strength)]
        _ShakePower       ("Shake power", Range(0,0.5)) = 0.1
        _ShakeBlockSize   ("Rows count", Range(2,200)) = 10
        _ShakeColorRate   ("Color split amount", Range(0,1)) = 0.1

        [Header(Overscan)]
        _Overscan ("Overscan", Range(0,0.5)) = 0.1
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+50" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }

        Pass
        {
            Name "AreaGlitch"
            ZWrite Off
            ZTest LEqual
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 screenPos   : TEXCOORD0;
                float2 localUV     : TEXCOORD1;
            };

            float _UpdateInterval;
            float _ShakePower;
            float _ShakeBlockSize;
            float _ShakeColorRate;
            float _Overscan;

            Varyings vert (Attributes v)
            {
                Varyings o;

                float2 centered = v.uv - 0.5;

                float2 expandedCentered = centered;
                expandedCentered.x *= (1.0 + _Overscan * 2.0);

                float2 deltaCentered = expandedCentered - centered;

                float3 posOS = v.positionOS.xyz;
                posOS.xy += deltaCentered;

                o.positionHCS = TransformObjectToHClip(posOS);
                o.screenPos = ComputeScreenPos(o.positionHCS);

                o.localUV = expandedCentered + 0.5;

                return o;
            }

            float random(float seed)
            {
                float2 st = float2(seed, seed * 0.918273);
                return frac(sin(dot(st, float2(12.9898, 78.233))) * 43758.5453123);
            }

            half4 frag (Varyings i) : SV_Target
            {
                float2 screenUV = i.screenPos.xy / i.screenPos.w;

                float t = fmod(_Time.y, 10000.0);
                float tick = floor(t / max(_UpdateInterval, 0.001));

                float shake = (random(floor(i.localUV.y * _ShakeBlockSize) / _ShakeBlockSize + tick) - 0.5) * _ShakePower;

                float dScreenU_dLocalU = fwidth(screenUV.x) / max(fwidth(i.localUV.x), 1e-6);
                float screenShake = shake * dScreenU_dLocalU;

                float srcLocalX = i.localUV.x - shake;

                clip((srcLocalX >= 0.0 && srcLocalX <= 1.0) ? 1.0 : -1.0);

                float2 fixedUV = screenUV;
                fixedUV.x += screenShake;

                float colorOffsetScreen = _ShakeColorRate * dScreenU_dLocalU;
                half3 col;
                col.g = (half)SampleSceneColor(fixedUV).g;
                col.r = (half)SampleSceneColor(fixedUV + float2(colorOffsetScreen, 0)).r;
                col.b = (half)SampleSceneColor(fixedUV - float2(colorOffsetScreen, 0)).b;

                return half4(col, 1.0h);
            }
            ENDHLSL
        }
    }
}