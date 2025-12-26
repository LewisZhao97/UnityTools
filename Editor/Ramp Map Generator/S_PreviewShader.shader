Shader "Preview Ramp/S_PreviewShader"
{
    Properties
    {
        _RampPreviewTex ("Ramp Preview Tex", 2D) = "white" {}
        _SampleRoll ("Sample Roll", Int) = 1
        _LightArea ("Light Area", Range(0.0, 1.0)) = 0.5
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
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
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
            };

            sampler2D _RampPreviewTex;
            fixed4 _RampPreviewTex_ST;
            fixed _LightArea;
            fixed _SampleRoll;
            fixed _RollNum;
            fixed _InvertY;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.uv = TRANSFORM_TEX(v.uv, _RampPreviewTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed ndl = saturate(dot(UnityWorldSpaceLightDir(i.vertex), i.normal));
                fixed halfLambert = pow(saturate(ndl * 0.5 + 0.5), 2.0);
                fixed2 rampUV;
                rampUV.x = min(0.999, smoothstep(0.001, 1.0 - _LightArea, halfLambert));
                fixed rollnum = 1 / _RollNum;
                rampUV.y = (_SampleRoll - 0.5) * rollnum;
                fixed4 finalCol = tex2D(_RampPreviewTex, rampUV);

                return finalCol;
            }
            ENDCG
        }
    }
}