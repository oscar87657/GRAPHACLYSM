Shader "Hidden/Graphaclysm/PortraitMedallion"
{
    Properties
    {
        _MainTex ("Portrait", 2D) = "white" {}
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

            fixed4 frag(v2f_img input) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, input.uv);
                color.a *= 1.0 - smoothstep(0.485, 0.505, length(input.uv - 0.5));
                return color;
            }
            ENDCG
        }
    }
}
