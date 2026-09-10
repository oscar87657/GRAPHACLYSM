Shader "Hidden/Graphaclysm/PortraitMedallion"
{
    Properties
    {
        _MainTex ("Portrait", 2D) = "white" {}
        _Crop ("Crop", Vector) = (0,0,1,1)
        _Focus ("Focus", Range(0,1)) = 1
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
            float4 _Crop;
            float _Focus;

            fixed4 frag(v2f_img input) : SV_Target
            {
                float2 uv = _Crop.xy + input.uv * _Crop.zw;
                float2 stepSize = _MainTex_TexelSize.xy * lerp(5.0, 0.0, _Focus);
                fixed4 color = tex2D(_MainTex, uv) * 0.24;
                color += tex2D(_MainTex, uv + float2(stepSize.x, 0)) * 0.12;
                color += tex2D(_MainTex, uv - float2(stepSize.x, 0)) * 0.12;
                color += tex2D(_MainTex, uv + float2(0, stepSize.y)) * 0.12;
                color += tex2D(_MainTex, uv - float2(0, stepSize.y)) * 0.12;
                color += tex2D(_MainTex, uv + stepSize) * 0.07;
                color += tex2D(_MainTex, uv - stepSize) * 0.07;
                color += tex2D(_MainTex, uv + float2(stepSize.x, -stepSize.y)) * 0.07;
                color += tex2D(_MainTex, uv + float2(-stepSize.x, stepSize.y)) * 0.07;
                float luminance = dot(color.rgb, float3(0.299, 0.587, 0.114));
                color.rgb = lerp(lerp(luminance.xxx, color.rgb, 0.22) * 0.58, color.rgb, _Focus);
                color.a *= 1.0 - smoothstep(0.485, 0.505, length(input.uv - 0.5));
                return color;
            }
            ENDCG
        }
    }
}
