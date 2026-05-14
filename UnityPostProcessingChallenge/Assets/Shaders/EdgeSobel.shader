Shader "Custom/PostFX/EdgeSobel"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _EdgeThreshold ("Edge Threshold", Range(0, 2)) = 0.2
        _EdgeIntensity ("Edge Intensity", Range(0, 5)) = 1.0
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
            float _EdgeThreshold;
            float _EdgeIntensity;

            float Luma(float3 c)
            {
                return dot(c, float3(0.299, 0.587, 0.114));
            }

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 texel = _MainTex_TexelSize.xy;

                float tl = Luma(tex2D(_MainTex, i.uv + texel * float2(-1,  1)).rgb);
                float  l = Luma(tex2D(_MainTex, i.uv + texel * float2(-1,  0)).rgb);
                float bl = Luma(tex2D(_MainTex, i.uv + texel * float2(-1, -1)).rgb);
                float  t = Luma(tex2D(_MainTex, i.uv + texel * float2( 0,  1)).rgb);
                float  b = Luma(tex2D(_MainTex, i.uv + texel * float2( 0, -1)).rgb);
                float tr = Luma(tex2D(_MainTex, i.uv + texel * float2( 1,  1)).rgb);
                float  r = Luma(tex2D(_MainTex, i.uv + texel * float2( 1,  0)).rgb);
                float br = Luma(tex2D(_MainTex, i.uv + texel * float2( 1, -1)).rgb);

                float gx = -tl - 2.0 * l - bl + tr + 2.0 * r + br;
                float gy =  tl + 2.0 * t + tr - bl - 2.0 * b - br;

                float edge = length(float2(gx, gy)) * _EdgeIntensity;
                float mask = step(_EdgeThreshold, edge);

                return fixed4(mask, mask, mask, 1.0);
            }
            ENDCG
        }
    }
}
