Shader "Unlit/PatternEffect"
{
    Properties
    {
        // _MainTex ("Texture", 2D)     = "white" {}
        _Color ("Color", Color) = (0, 0, 0, 0)
        _Tint ("Tint", Color) = (0, 0, 0, 0)
        _PatternTex ("Pattern", 2D) = "white" {}
        _ShineTex ("Shine", 2D) = "white" {}
        _Speed ("Speed", float) = 0
        _Alpha ("Alpha", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 100
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                // float2 uv : TEXCOORD0;
                float2 uvPattern : TEXCOORD0;
                float2 uvShine : TEXCOORD1;
            };

            struct v2f
            {
                // float2 uv : TEXCOORD0;
                float2 uvPattern : TEXCOORD0;
                float2 uvShine : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            // sampler2D _MainTex;
            // float4 _MainTex_ST;
            sampler2D _PatternTex;
            float4 _PatternTex_ST;
            sampler2D _ShineTex;
            float4 _ShineTex_ST;
            float4 _Color;
            float4 _Tint;
            float _Speed;
            float _Alpha;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                // o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uvPattern = TRANSFORM_TEX(v.uvPattern, _PatternTex);
                o.uvShine = TRANSFORM_TEX(v.uvShine, _ShineTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = _Color;
                i.uvShine.x += _Time.y * _Speed;
                col += tex2D(_PatternTex, i.uvPattern) * tex2D(_ShineTex, i.uvShine) * _Tint * fixed4(_Alpha, _Alpha, _Alpha, _Alpha);
                // fixed4 col = tex2D(_ShineTex, i.uvShine);
                return col;
            }
            ENDCG
        }
    }
}
