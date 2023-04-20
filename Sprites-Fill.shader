Shader "Sprites/Fill"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        [Enum(Horizontal, 0 ,Vertical, 1, Radial360, 2)] _FillMethod ("FillMethod", Int) = 0
        [Enum(orient1, 0 ,orient2, 1)] _FillOrientation ("FillOrientation", Int) = 1
        _FillAmount ("FillAmount", Range(0, 1)) = 1
        [HDR]_HDRColor("HDR Color", Color) = (1, 1, 1, 1)
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

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            fixed4 _Color;
            int _FillMethod;
            int _FillOrientation;
            fixed _FillAmount;

            sampler2D _MainTex;
            float4 _MainTex_ST;
            uniform fixed4 _HDRColor;

            v2f vert(appdata_t i)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(i.vertex);
                o.uv = TRANSFORM_TEX(i.uv, _MainTex);
                o.color = i.color * _Color;
                return o;
            }

            fixed4 SampleSpriteTexture(float2 uv)
            {
                fixed4 color = tex2D(_MainTex, uv);
                float2 diff = float2(uv.x - 0.5, uv.y - 0.5);
                float theta = atan(diff.y / diff.x);
                float sign = lerp(-1, 1, _FillOrientation);
                const float PI = radians(180);
                color.a *= lerp(lerp(step(uv.x, _FillAmount), step(1 - _FillAmount, uv.x), _FillOrientation),// 水平填充
                                lerp(lerp(step(uv.y, _FillAmount), step(1 - _FillAmount, uv.y), _FillOrientation),// 垂直填充
                                     lerp(lerp(step(theta, (_FillAmount * 2 - 0.5) * PI),// 环形360 以中心为轴 填充
                                               step(- (2 * _FillAmount - 0.5) * PI, theta), _FillOrientation) *
                                          step(0, sign * diff.x),
                                          lerp(lerp(step(theta, (2 * (_FillAmount - 0.5) - 0.5) * PI),
                                                    step(- (2 * (_FillAmount - 0.5) - 0.5) * PI, theta), _FillOrientation),
                                               1, step(0, sign * diff.x)), step(0.5, _FillAmount)),
                                     step(2, _FillMethod)),
                                step(1, _FillMethod));
                return color;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 col = SampleSpriteTexture(IN.uv) * _Color;
                return col * _HDRColor;
            }
            ENDCG
        }
    }
}