Shader "Custom/PostFX/GrayscaleLuma"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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

            fixed4 frag(v2f_img i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                fixed luma = dot(col.rgb, fixed3(0.299, 0.587, 0.114));
                return fixed4(luma, luma, luma, col.a);
            }
            ENDCG
        }
    }
}
