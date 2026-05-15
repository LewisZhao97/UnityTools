Shader "Hidden/TextureMixer"
{
    Properties
    {
        _Metallic ("Metallic (R)", 2D) = "black" {}
        _Occlusion ("Occlusion (G)", 2D) = "black" {}
        _DetailMask ("DetailMask (B)", 2D) = "black" {}
        _Smoothness ("Smoothness (G)", 2D) = "black" {}
        _RGBSource ("RGB Source", 2D) = "black" {}
        _AlphaSource ("Alpha Source", 2D) = "black" {}
        _SwapRoughness ("Inverse Smoothness", Float) = 0.0
        _MixMode ("Mix Mode (0=Channels, 1=RGB+A)", Float) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque" "Queue"="Geometry"
        }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include "UnityCG.cginc"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
            };

            sampler2D _Metallic;
            float4 _Metallic_ST;
            sampler2D _Occlusion;
            float4 _Occlusion_ST;
            sampler2D _DetailMask;
            float4 _DetailMask_ST;
            sampler2D _Smoothness;
            float4 _Smoothness_ST;
            sampler2D _RGBSource;
            float4 _RGBSource_ST;
            sampler2D _AlphaSource;
            float4 _AlphaSource_ST;

            float _SwapRoughness;
            float _MixMode;

            Varyings Vertex(Attributes IN)
            {
                Varyings o;
                o.positionCS = UnityObjectToClipPos(IN.positionOS);
                o.uv = IN.uv;
                return o;
            }

            half4 Fragment(Varyings i) : SV_Target
            {
                if (_MixMode > 0.5)
                {
                    half3 rgb = tex2D(_RGBSource, TRANSFORM_TEX(i.uv, _RGBSource)).rgb;
                    half a = tex2D(_AlphaSource, TRANSFORM_TEX(i.uv, _AlphaSource)).r;
                    a = lerp(a, 1.0 - a, _SwapRoughness);
                    return half4(rgb, pow(a, 0.45));
                }
                half4 col = fixed4(0, 0, 0, 1);
                col.r = tex2D(_Metallic, TRANSFORM_TEX(i.uv, _Metallic)).r;
                col.g = tex2D(_Occlusion, TRANSFORM_TEX(i.uv, _Occlusion)).g;
                col.b = tex2D(_DetailMask, TRANSFORM_TEX(i.uv, _DetailMask)).b;
                half smoothG = tex2D(_Smoothness, TRANSFORM_TEX(i.uv, _Smoothness)).g;
                half a = lerp(smoothG, 1.0 - smoothG, _SwapRoughness);
                col.a = pow(a, 0.45);
                return col;
            }
            ENDHLSL
        }
    }
}