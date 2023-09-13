Shader "Unlit/FractalRenderer"
{
    Properties
    {
        _Cx ("Cx", Float) = 0
        _Cy ("Cy", Float) = 0
        _MaxIterations ("MaxIterations", Int) = 20
        _Treshold ("Treshold", Float) = 2
        _PosAndSize ("PosAndSize", Vector) = (0, 0, 10, 10)
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
            int _MaxIterations;
            float _Treshold;

            float4 _PosAndSize;
            

            float2 SquareComplex(float2 a)
            {
                return float2(a.x * a.x - a.y * a.y, a.x * a.y * 2);
            }

            float2 AddComplex(float2 a, float2 b)
            {
                return float2(a.x + b.x, a.y + b.y);
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
                float2 z = _PosAndSize.xy - _PosAndSize.zw / 2 + _PosAndSize.zw * i.uv;
                float2 c = float2(_Cx, _Cy);
                float it = 0;
                while (it < _MaxIterations && SqrMagnitude(z) < _Treshold * _Treshold)
                {
                    z = AddComplex(SquareComplex(z), c);
                    it++;
                }

                float time = it / float(_MaxIterations);
                return float4(time, time, time, 1);
            }
            ENDCG
        }
    }
}