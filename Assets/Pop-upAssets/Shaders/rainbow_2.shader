
Shader "UI/Moving Rainbow Border"
{
    Properties
    {
        [PerRendererData] _MainTex ("Border Sprite", 2D) = "white" {}
        _RainbowTex ("Rainbow Pattern", 2D) = "white" {}

        _PatternRepeats ("Pattern Repeats", Range(1, 1)) = 1
        _Speed ("Rainbow Speed", Range(-2, 2)) = 0.3
        _Brightness ("Rainbow Brightness", Range(0, 3)) = 1.5

        _Color ("Tint", Color) = (1,1,1,1)

        // UI masking support
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "CanUseSpriteAtlas" = "True"
            "PreviewType" = "Plane"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

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
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            sampler2D _RainbowTex;

            float4 _MainTex_ST;
            float4 _RainbowTex_ST;
            fixed4 _Color;

            float _PatternRepeats;
            float _Speed;
            float _Brightness;

            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f o;

                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Sample the border sprite.
                // Its alpha defines the rounded shape and glow.
                fixed4 sprite = tex2D(_MainTex, i.uv);

                // Position around the rectangular perimeter.
                // Position around the perimeter.
                // Use the nearest edge so the color progression
                // doesn't switch based on the border thickness.
                float2 uv = i.uv;

                float4 edgeDistances = float4(
                    uv.y,        // Bottom
                    1.0 - uv.x,  // Right
                    1.0 - uv.y,  // Top
                    uv.x         // Left
                );

                float perimeter;

                if (edgeDistances.x <= edgeDistances.y &&
                    edgeDistances.x <= edgeDistances.z &&
                    edgeDistances.x <= edgeDistances.w)
                {
                    perimeter = uv.x * 0.25;
                }
                else if (edgeDistances.y <= edgeDistances.z &&
                         edgeDistances.y <= edgeDistances.w)
                {
                    perimeter = 0.25 + uv.y * 0.25;
                }
                else if (edgeDistances.z <= edgeDistances.w)
                {
                    perimeter = 0.5 + (1.0 - uv.x) * 0.25;
                }
                else
                {
                    perimeter = 0.75 + (1.0 - uv.y) * 0.25;
                }

                // One continuous pattern around the entire border.
                float patternU = frac(
                    perimeter - _Time.y * _Speed
                );

                fixed4 rainbow = tex2D(
                    _RainbowTex,
                    float2(patternU, 0.5)
                );

                // Preserve the sprite's transparent areas.
                fixed alpha = sprite.a * i.color.a;

                // Use the rainbow texture instead of the sprite's color.
                fixed3 finalColor =
                    rainbow.rgb * _Brightness * i.color.rgb;

                // Respect Unity UI RectMask2D clipping.
                #ifdef UNITY_UI_CLIP_RECT
                alpha *= UnityGet2DClipping(
                    i.worldPosition.xy,
                    _ClipRect
                );
                #endif

                return fixed4(finalColor, alpha);
            }

            ENDCG
        }
    }
}
