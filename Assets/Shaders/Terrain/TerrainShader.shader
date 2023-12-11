Shader "TerrainShader"
{
    Properties
    {
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

            struct GradientKey
            {
                float Time;
                float4 Color;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD1;
            };

            StructuredBuffer<GradientKey> _Gradient;
            int _GradientSize;
            int _BlendGradient;

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
                while (!(_Gradient[keyIndex].Time <= time && time <= _Gradient[keyIndex + 1].Time) && keyIndex < _GradientSize - 1)
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
                    return float4(0, 0, 0, 1);
                }

                float newTime = float(time - lowerKey.Time) / float(upperKey.Time - lowerKey.Time);
                return lerp(lowerKey.Color, upperKey.Color, newTime);
            }


            v2f vert(float4 pos : POSITION, float3 normal : NORMAL, float2 uv : TEXCOORD0)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(pos);
                o.uv = uv;
                o.normal = UnityObjectToWorldNormal(normal);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                return SampleColor((dot(float3(0, 1, 0), i.normal) + 1) / 2);
            }
            ENDCG
        }
    }
}
