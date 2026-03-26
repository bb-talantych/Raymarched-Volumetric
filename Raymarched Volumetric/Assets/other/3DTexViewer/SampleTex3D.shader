Shader "_BB/SampleTex3D"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

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

            sampler3D _VolumeTex;
            float _Slice;

            v2f vert (appdata v)
            {
                v2f o;
                
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float ratio = _ScreenParams.x / _ScreenParams.y;
                float centeredX = i.uv.x * 2 - 1;
                centeredX *= ratio;
                centeredX = centeredX * 0.5 + 0.5;

                float3 uvw;
                uvw.x = centeredX;
                uvw.y = i.uv.y;
                uvw.z = _Slice;

                // borders
                if(uvw.x < 0 || uvw.x > 1)
                {
                    return float4(0.1, 0.5, 0.5, 1);
                }

                float volumeTex = tex3D(_VolumeTex, uvw);
                return float4(volumeTex.xxx, 1);
            }
            ENDCG
        }
    }
}
