Shader"Unlit/FloatsToNormalMap"
{
    Properties
    {

    }
    SubShader
    {

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

            StructuredBuffer<float> _FloatBuffer;
            int _FloatBufferSizeX;


            float2 GetBissectorVector(float valm1, float val0, float val1)
            {
                float2 a = float2(1.0f, (val1 - val0) * _FloatBufferSizeX);
                float2 b = float2(-1.0f, (valm1 - val0) * _FloatBufferSizeX);
                
                float theta_u = atan2(a.x, a.y);
                float theta_v = atan2(b.x, b.y);
                float average_theta = (theta_u + theta_v) / 2;
    
                float2 c = float2(sin(average_theta), cos(average_theta));
                return c;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float3 frag(v2f i) : SV_Target
            {
                int x0 = int(i.uv.x * _FloatBufferSizeX);
                int y0 = int(i.uv.y * _FloatBufferSizeX);
                if (x0 == 0 || x0 == _FloatBufferSizeX - 1 || y0 == 0 || y0 == _FloatBufferSizeX - 1)
                {
                    return float3(0.5f, 0.5f, 1);
                }
    
                float val0 = _FloatBuffer[(x0 + y0 * _FloatBufferSizeX)];
                float valm1 = _FloatBuffer[(x0 - 1 + y0 * _FloatBufferSizeX)];
                float val1 = _FloatBuffer[(x0 + 1 + y0 * _FloatBufferSizeX)];
    
                float2 vecX = GetBissectorVector(valm1, val0, val1);
    
                valm1 = _FloatBuffer[(x0 + (y0 - 1) * _FloatBufferSizeX)];
                val1 = _FloatBuffer[(x0 + (y0 + 1) * _FloatBufferSizeX)];
    
                float2 vecY = GetBissectorVector(valm1, val0, val1);
                
                return (float3(vecX.x, vecY.x, (vecX.y + vecY.y) / 2) + float3(1, 1, 1)) / 2;
            }
            ENDCG
        }
    }
}
