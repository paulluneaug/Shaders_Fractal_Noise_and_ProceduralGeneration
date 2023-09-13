Shader "Unlit/PerlinNoise"
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
            
            int _GradientSizeX;
            int _GradientSizeY;

            StructuredBuffer<float2> _Gradient;

            float Smoothstep(float w)
            {
                if (w <= 0.0f)
                {
                    return 0.0f;
                }
                if (w >= 1.0f)
                {
                    return 1.0f;
                }
                return w * w * (3.0f - 2.0f * w);
            }
            
            float Interpolate(float a0, float a1, float w) 
            {
                 return a0 + (a1 - a0) * Smoothstep(w);
            }

            float DotGridGradient(int ix, int iy, float x, float y) 
            { 
                // Compute the distance vector
                float dx = x - (float)ix;
                float dy = y - (float)iy;
 
                 // Compute the dot-product
                 return (dx * _Gradient[ix + iy * _GradientSizeX].x + dy * _Gradient[ix + iy * _GradientSizeX].y);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float x = i.uv.x;
                float y = i.uv.y;
    
                // Determine grid cell coordinates
                int x0 = int(floor(x));
                int x1 = x0 + 1;
                int y0 = int(floor(y));
                int y1 = y0 + 1;
 
                 // Determine interpolation weights
                 // Could also use higher order polynomial/s-curve here
                float sx = x - (float) x0;
                float sy = y - (float) y0;
 
                 // Interpolate between grid point gradients
                float n0 = DotGridGradient(x0, y0, x, y);
                float n1 = DotGridGradient(x1, y0, x, y);
                float ix0 = Interpolate(n0, n1, sx);
                n0 = DotGridGradient(x0, y1, x, y);
                n1 = DotGridGradient(x1, y1, x, y);
                float ix1 = Interpolate(n0, n1, sx);
                float value = Interpolate(ix0, ix1, sy);
 
                return float4(value, value, value, 1.0f);
            }
            ENDCG
        }
    }
}
