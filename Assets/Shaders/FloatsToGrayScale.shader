Shader "Unlit/FlotsToGrayScale"
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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                int x = int(i.uv.x * _FloatBufferSizeX);
                int y = int(i.uv.y * _FloatBufferSizeX);
                float val = _FloatBuffer[(x + y * _FloatBufferSizeX)];
    
                return float4(val, val, val, 1);

            }
            ENDCG
        }
    }
}
