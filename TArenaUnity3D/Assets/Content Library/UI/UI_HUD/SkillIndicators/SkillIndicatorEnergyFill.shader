Shader "TArena/Skill Indicator Energy Fill"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Mask", 2D) = "white" {}
        _FillTex ("Energy Fill", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
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
                float2 maskUv : TEXCOORD0;
                float2 fillUv : TEXCOORD1;
            };

            sampler2D _MainTex;
            sampler2D _FillTex;
            float4 _FillTex_ST;
            fixed4 _Color;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                o.maskUv = v.texcoord;
                o.fillUv = TRANSFORM_TEX(v.texcoord, _FillTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 mask = tex2D(_MainTex, i.maskUv);
                fixed4 fill = tex2D(_FillTex, i.fillUv);
                fixed4 result = fill * i.color;
                result.a *= mask.a;
                return result;
            }
            ENDCG
        }
    }
}
