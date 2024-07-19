Shader "Unlit/ConditionalBlend"
{
    Properties
    {
        _ResourceTex ("Resource Texture", 2D) = "white" {}
        _TargetTex("Target Texture", 2D) = "white" {}
    }
    SubShader
    {
		Tags { "RenderType"="Opaque" }
		LOD	   100
		ZTest  Off
		ZWrite Off
		Cull   Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _ResourceTex;
            sampler2D _TargetTex;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 resColor = tex2D(_ResourceTex, i.uv);
                fixed4 targetColor = tex2D(_TargetTex, i.uv);
                if (resColor.r >= 0.95 && resColor.g >= 0.95 && resColor.b >= 0.95)
                {
                    return targetColor;
                }
                return resColor;
            }
            ENDCG
        }
    }
}
