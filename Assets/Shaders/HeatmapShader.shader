Shader "Unlit/HeatmapShader"
{
      Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _HeatmapAmount ("Heatmap Blend Amount", Range(0,1)) = 1 // blend between original texture and heatmap - can keep it 1
        _LowColor ("Low Color", Color) = (0,0,1,1)
        _MidColor ("Mid Color", Color) = (0,1,0,1)
        _HighColor ("High Color", Color) = (1,0,0,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            // --- ADD THESE TWO LINES ---
            ZWrite On
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                // We get the vertex ID directly from the GPU
                uint vertexId : SV_VertexID;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float heat : TEXCOORD1; // Pass the calculated heat value
            };

            // This buffer will receive the displacement data from our C# script
            StructuredBuffer<float> _DisplacementBuffer;
            // This uniform will receive the current maximum displacement for normalization
            uniform float _MaxDisplacement;

            fixed4 _LowColor;
            fixed4 _MidColor;
            fixed4 _HighColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                // Read the displacement for this specific vertex from the buffer
                float displacement = _DisplacementBuffer[v.vertexId];

                // Normalize the heat value, avoiding division by zero
                o.heat = _MaxDisplacement > 0 ? displacement / _MaxDisplacement : 0;
                
                return o;
            }

            sampler2D _MainTex;
            float _HeatmapAmount;

            fixed4 frag (v2f i) : SV_Target
            {
                // Get the character's original texture color
                fixed4 col = tex2D(_MainTex, i.uv);

                // Calculate the heatmap color based on the heat value from the vertex shader
                fixed4 heatColor = lerp(_LowColor, _MidColor, saturate(i.heat * 2.0)); //low to mid
                heatColor = lerp(heatColor, _HighColor, saturate((i.heat - 0.5) * 2.0)); //mid to high

                // Blend the original texture color with the heatmap color
                return lerp(col, heatColor, _HeatmapAmount);
            }
            ENDCG
        }
    }
}