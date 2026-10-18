
Shader "Devorya/URP2D/ScreenGlitch"
{
    Properties
    {
        [NoScaleOffset]
        _GlitchTex ("Glitch Noise", 2D) = "white" {}

        _GlitchAmount ("Glitch Amount", Range(0, 1)) = 0
        _MaxOffset ("Horizontal Distortion", Range(0, 0.1)) = 0.012
        _RGBSplit ("RGB Split", Range(0, 0.03)) = 0.003
        _GlitchSpeed ("Glitch Speed", Range(1, 60)) = 18
        _GlitchCutAmountX ("Glitch Cut Amount X", Range(0.1, 10)) = 1
        _GlitchCutAmountY ("Glitch Cut Amount Y", Range(0.1, 10)) = 1

        [HideInInspector]
        _GlitchPhase ("Glitch Phase", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "ScreenGlitch"

            Cull Off
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM

            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_CameraSortingLayerTexture);
            SAMPLER(sampler_CameraSortingLayerTexture);

            TEXTURE2D(_GlitchTex);
            SAMPLER(sampler_GlitchTex);

            float _GlitchAmount;
            float _MaxOffset;
            float _RGBSplit;
            float _GlitchSpeed;
            float _GlitchCutAmountX;
            float _GlitchCutAmountY;
            float _GlitchPhase;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;

                output.positionCS =
                    TransformObjectToHClip(input.positionOS.xyz);

                output.uv = input.uv;

                return output;
            }

            // <변경부분>
            // 수평 밴드마다 독립적인 난수를 생성한다.
            float Hash11(float value)
            {
                return frac(
                    sin(value * 127.1) * 43758.5453
                );
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;

                float strength =
                    saturate(_GlitchAmount);

                // <변경부분>
                // 프레임 단위로 노이즈 패턴을 전환한다.
                float glitchFrame =
                    floor(_GlitchPhase * _GlitchSpeed);

                float bandIndex =
                    floor(uv.y * _GlitchCutAmountY * 28.0);

                float bandNoise =
                    Hash11(bandIndex + glitchFrame * 13.71);

                // <변경부분>
                // 외부 노이즈 텍스처가 없어도
                // 절차적 밴드 글리치가 작동한다.
                // 텍스처가 있으면 왜곡 강도를 추가로 변조한다.
                float2 noiseUV = frac(
                    float2(
                        uv.x * _GlitchCutAmountX
                            + glitchFrame * 0.137,
                        uv.y * _GlitchCutAmountY
                            + glitchFrame * 0.071
                    )
                );

                float textureNoise =
                    SAMPLE_TEXTURE2D(
                        _GlitchTex,
                        sampler_GlitchTex,
                        noiseUV
                    ).r;

                // <변경부분>
                // 특정 수평 밴드만 강하게 밀어낸다.
                float bandMask = step(
                    1.0 - strength * 0.65,
                    bandNoise
                );

                float horizontalOffset =
                    (bandNoise * 2.0 - 1.0)
                    * bandMask
                    * textureNoise
                    * _MaxOffset
                    * strength;

                // <변경부분>
                // RGB 분리는 화면 전체에 약하게 적용하고
                // 글리치 밴드에서는 더 강하게 적용한다.
                float rgbOffset =
                    _RGBSplit
                    * strength
                    * (0.35 + bandMask);

                float2 redUV = saturate(
                    uv + float2(
                        horizontalOffset + rgbOffset,
                        0.0
                    )
                );

                float2 greenUV = saturate(
                    uv + float2(
                        horizontalOffset - rgbOffset,
                        0.0
                    )
                );

                float2 blueUV = saturate(
                    uv + float2(
                        horizontalOffset,
                        rgbOffset * 0.4
                    )
                );

                half red = SAMPLE_TEXTURE2D(
                    _CameraSortingLayerTexture,
                    sampler_CameraSortingLayerTexture,
                    redUV
                ).r;

                half green = SAMPLE_TEXTURE2D(
                    _CameraSortingLayerTexture,
                    sampler_CameraSortingLayerTexture,
                    greenUV
                ).g;

                half blue = SAMPLE_TEXTURE2D(
                    _CameraSortingLayerTexture,
                    sampler_CameraSortingLayerTexture,
                    blueUV
                ).b;

                // <변경부분>
                // 강도가 0이면 Overlay를 완전히 투명하게 한다.
                // 강도가 0보다 크면 왜곡한 화면을 출력한다.
                half alpha =
                    step(0.0001, strength);

                return half4(
                    red,
                    green,
                    blue,
                    alpha
                );
            }

            ENDHLSL
        }
    }

    FallBack Off
}

