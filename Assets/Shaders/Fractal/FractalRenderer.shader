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

            struct GradientKey
            {
                float Time;
                float4 Color;
            };

            float _Cx;
            float _Cy;
            int _MaxIterations;
            float _Treshold;

            float4 _PosAndSize;

            StructuredBuffer<GradientKey> _Gradient;
            int _GradientSize;
            int _BlendGradient;
            

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

            float4 SampleColor(float time)
            {
                if (_GradientSize == 0)
                {
                    return lerp(float4(0, 0, 0, 1), float4(1, 1, 1, 1), time);
                }

                if (_GradientSize == 1)
                {
                    return _Gradient[0].Color;
                }
    
                if (time <= _Gradient[0].Time)
                {
                    return _Gradient[0].Color;
                }

                int keyIndex = 0;
                while(!(_Gradient[keyIndex].Time <= time && time <= _Gradient[keyIndex + 1].Time) && keyIndex < _GradientSize - 1)
                {
                    keyIndex++;
                }

                if (keyIndex == _GradientSize - 1)
                {
                    return _Gradient[keyIndex].Color;
                }

                GradientKey lowerKey = _Gradient[keyIndex];
                GradientKey upperKey = _Gradient[keyIndex + 1];
    
                if (_BlendGradient == 0)
                {
                    return upperKey.Color;
                }
    
                if (time == lowerKey.Time)
                {
                    return float4(0,0,0,1);
                }

                float newTime = float(time - lowerKey.Time) / float(upperKey.Time - lowerKey.Time);
                // return lerp((0,0,0,1), (1,1,1,1), newTime);
                return lerp(lowerKey.Color, upperKey.Color, newTime);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
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
                return SampleColor(time);
            }
            ENDCG
        }
    }
}
