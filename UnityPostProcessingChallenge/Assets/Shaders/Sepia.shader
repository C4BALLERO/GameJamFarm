Shader "Custom/PostFX/Sepia"
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

                float3 sepia;
                sepia.r = dot(col.rgb, float3(0.393, 0.769, 0.189));
                sepia.g = dot(col.rgb, float3(0.349, 0.686, 0.168));
                sepia.b = dot(col.rgb, float3(0.272, 0.534, 0.131));

                return fixed4(saturate(sepia), col.a);
            }
            ENDCG
        }
    }
}
