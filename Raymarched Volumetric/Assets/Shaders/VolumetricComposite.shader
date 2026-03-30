Shader "_BB/VolumetricComposite"
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

            sampler2D _MainTex;
            sampler2D _VolumetricColor, _VolumetricTransmittance;
            

            v2f vert (appdata v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
 
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 src = tex2D(_MainTex, i.uv);
                float3 volumetricColor = tex2D(_VolumetricColor, i.uv);
                float3 transmittance = tex2D(_VolumetricTransmittance, i.uv);

                float3 finalOutput = src * transmittance + volumetricColor;
                return float4(finalOutput, 1);
            }
            ENDCG
        }
    }
}
