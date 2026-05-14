Shader "Custom/PostFX/Pixelation"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _PixelSize ("Pixel Size", Float) = 8
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _PixelSize;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 pixelScale = _MainTex_TexelSize.xy * max(_PixelSize, 1.0);
                float2 snappedUV = floor(i.uv / pixelScale) * pixelScale;
                return tex2D(_MainTex, snappedUV);
            }
            ENDCG
        }
    }
}
