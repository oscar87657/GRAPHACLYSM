Shader "GRAPHACLYSM/PolynomiographLines"
{
    SubShader
    {
        Tags { "Queue" = "Overlay" "RenderType" = "Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            struct VertexInput
            {
                float4 position : POSITION;
                float4 color : COLOR;
            };

            struct VertexOutput
            {
                float4 position : SV_POSITION;
                float4 color : COLOR;
            };

            VertexOutput Vert(VertexInput input)
            {
                VertexOutput output;
                output.position = UnityObjectToClipPos(input.position);
                output.color = input.color;
                return output;
            }

            float4 Frag(VertexOutput input) : SV_Target
            {
                return input.color;
            }
            ENDHLSL
        }
    }
}
