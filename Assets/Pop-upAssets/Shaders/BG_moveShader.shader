Shader "UI/ScrollingPattern"
{
    Properties
    {
        [PerRendererData] _MainTex ("Pattern", 2D) = "white" {}

        // #010058
        _Color ("Pattern Color", Color) = (0.003922, 0, 0.345098, 1)

        _SpeedY ("Scroll Speed", Float) = 0.1

        // Unity UI Mask / Stencil support
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
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        // Unity UI Mask support
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

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;

            fixed4 _Color;
            float _SpeedY;

            v2f vert(appdata_t v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Move the pattern downward
                float2 uv = i.uv;

                uv.y += _Time.y * _SpeedY;

                // Repeat forever
                uv.y = frac(uv.y);

                // Get pattern
                fixed4 pattern = tex2D(_MainTex, uv);

                // Apply #010058
                pattern.rgb *= _Color.rgb;

                // Preserve the pattern's transparency
                pattern.a *= _Color.a;

                return pattern;
            }

            ENDCG
        }
    }
}