Shader "Unlit/FractalRenderer"
{
    Properties
    {
        _Cx ("Cx", Float) = 0
        _Cy ("Cy", Float) = 0
        _LineColor ("Line color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            float _Cx;
            float _Cy;
            float4 _LineColor;

            float4 _MainTex_ST;

            

            float2 MultiplyComplex(float2 a, float2 b)
            {
                return float2(a.x * b.x - a.y * b.y, a.x * b.y - a.y * b.x);
            }

            float SqrMagnitude(float2 a)
            {
                return a.x * a.x + a.y * a.y;
            }


            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                return float4(i.uv.x, i.uv.y, 1,1) * _LineColor;
            }
            ENDCG
        }
    }
}
