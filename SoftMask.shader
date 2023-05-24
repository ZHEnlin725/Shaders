Shader "SoftMask"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _SoftMaskTex ("Soft Mask Texture", 2D) = "white" {}
        _Softness ("_Softness", Range(1, 2)) = 1.4

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

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
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
                float4 color : COLOR;
                float4 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                half4 texcoord0 : TEXCOORD0;
                half2 texcoord1 : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _Color;

            float _Softness;
            sampler2D _SoftMaskTex;

            uniform float4 _SoftMaskRect;
            uniform float4x4 _SoftMaskMatrix;

            v2f vert(appdata_t i)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(i.vertex);
                o.texcoord0 = i.texcoord;
                float4 uv = float4(0, 0, 0, 1);
                uv = mul(unity_ObjectToWorld, uv);
                uv = mul(_SoftMaskMatrix, uv);
                float2 factor = float2(unity_WorldToObject._11 / _SoftMaskMatrix._11,
                                       unity_WorldToObject._22 / _SoftMaskMatrix._22);
                uv.xy = i.vertex.xy / factor + uv.xy;
                uv.x /= _SoftMaskRect.x;
                uv.y /= _SoftMaskRect.y;
                uv.x += _SoftMaskRect.z;
                uv.y += _SoftMaskRect.w;
                o.texcoord1 = uv.xy;
                #ifdef UNITY_HALF_TEXEL_OFFSET
                o.vertex.xy -= (_ScreenParams.zw-1.0);     
                #endif
                o.color = i.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 mainCol = tex2D(_MainTex, i.texcoord0.xy / i.texcoord0.w);
                fixed4 maskCol = tex2D(_SoftMaskTex, i.texcoord1.xy);
                fixed4 color = mainCol * i.color * _Color;
                float alpha = color.a * maskCol.a;
                return fixed4(color.rgb, pow(alpha, _Softness));
            }
            ENDCG
        }
    }
}