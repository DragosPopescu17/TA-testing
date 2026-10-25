Shader "UI/Moving Rainbow Border"
{
    Properties
    {
        [PerRendererData] _MainTex ("Painting", 2D) = "white" {}
        _BorderWidth ("Border Width", Range(0.001, 0.2)) = 0.05
        _CornerRadius ("Corner Radius", Range(0.0, 0.5)) = 0.1
        _Speed ("Rainbow Speed", Range(-2, 2)) = 0.3
        _Brightness ("Rainbow Brightness", Range(0, 3)) = 1.5
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float _BorderWidth;
            float _CornerRadius;
            float _Speed;
            float _Brightness;

            v2f vert(appdata_t v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color;

                return o;
            }

            // HSV -> RGB
            float3 HSVtoRGB(float3 hsv)
            {
                float3 rgb = clamp(
                    abs(fmod(hsv.x * 6.0 + float3(0, 4, 2), 6.0) - 3.0) - 1.0,
                    0.0,
                    1.0
                );

                rgb = rgb * rgb * (3.0 - 2.0 * rgb);

                return hsv.z * lerp(float3(1, 1, 1), rgb, hsv.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // ---------------------------------------
                // ROUNDED RECTANGLE
                // ---------------------------------------

                float2 centeredUV = abs(uv - 0.5);

                float2 rectangleSize = float2(0.5, 0.5);

                float2 cornerOffset =
                    rectangleSize - _CornerRadius;

                float2 q =
                    centeredUV - cornerOffset;

                float2 outside = max(q, 0.0);

                float distanceFromRoundedCorner =
                    length(outside) - _CornerRadius;

                // ---------------------------------------
                // BORDER
                // ---------------------------------------

                float outerBorder =
                    1.0 - smoothstep(
                        0.0,
                        0.01,
                        distanceFromRoundedCorner
                    );

                float innerDistance =
                    distanceFromRoundedCorner + _BorderWidth;

                float innerBorder =
                    smoothstep(
                        0.0,
                        0.01,
                        innerDistance
                    );

                float border =
                    outerBorder * innerBorder;

                // ---------------------------------------
                // RAINBOW POSITION
                // ---------------------------------------

                float perimeter;

                if (uv.y <= _BorderWidth)
                {
                    // Bottom
                    perimeter = uv.x * 0.25;
                }
                else if (uv.x >= 1.0 - _BorderWidth)
                {
                    // Right
                    perimeter = 0.25 + uv.y * 0.25;
                }
                else if (uv.y >= 1.0 - _BorderWidth)
                {
                    // Top
                    perimeter = 0.5 + (1.0 - uv.x) * 0.25;
                }
                else
                {
                    // Left
                    perimeter = 0.75 + (1.0 - uv.y) * 0.25;
                }

                float hue =
                    frac(perimeter + _Time.y * _Speed);

                float3 rainbow =
                    HSVtoRGB(
                        float3(
                            hue,
                            1.0,
                            _Brightness
                        )
                    );

                // ---------------------------------------
                // FINAL COLOR
                // ---------------------------------------

                return fixed4(
                    rainbow,
                    border
                );
            }

            ENDCG
        }
    }
}