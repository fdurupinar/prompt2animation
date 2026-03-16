Shader "Custom/RealisticSkinShader"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _SpecTex ("Specular Map (R)", 2D) = "white" {}
        _SubsurfaceTex ("Subsurface Map (RGB)", 2D) = "white" {}
        _SpecColor ("Specular Color", Color) = (1,1,1,1)
        _SpecIntensity ("Specular Intensity", Range(0,1)) = 0.2
        _Shininess ("Shininess", Range(0.01, 1)) = 0.078125
        _SubsurfaceColor ("Subsurface Color", Color) = (1,0.8,0.6,1)
        _SubsurfaceIntensity ("Subsurface Intensity", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300

        CGPROGRAM
        // Custom surface shader with our own lighting function.
        #pragma surface surf CustomLighting

        sampler2D _MainTex;
        sampler2D _SpecTex;
        sampler2D _SubsurfaceTex;
        float _SpecIntensity;
        float _Shininess;
        fixed4 _SubsurfaceColor;
        float _SubsurfaceIntensity;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_SpecTex;
            float2 uv_SubsurfaceTex;
            float3 viewDir;
        };

        // Custom lighting function that combines diffuse, specular, and a subsurface scattering approximation.
        half4 LightingCustomLighting (SurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
        {
            // Calculate half vector for Blinn-Phong specular.
            half3 halfDir = normalize(lightDir + viewDir);

            // Diffuse using half-Lambert (shifts dark values up for a softer look).
            half NdotL = saturate(dot(s.Normal, lightDir));
            half diff = NdotL * 0.5 + 0.5;

            // Specular component using Blinn-Phong model.
            half NdotH = saturate(dot(s.Normal, halfDir));
            half spec = pow(NdotH, 1.0 / _Shininess);
            // Multiply the specular term by our intensity factor.
            spec *= _SpecIntensity;

            // Subsurface scattering approximation:
            // When light hits from behind, the skin appears to glow slightly.
            half subsurface = saturate(1.0 - dot(s.Normal, lightDir));
            subsurface = pow(subsurface, 2.0); // exponent adjusts the softness

            // Combine all components.
            half3 col = s.Albedo * diff 
                        + _SpecColor.rgb * spec 
                        + _SubsurfaceColor.rgb * subsurface * _SubsurfaceIntensity;
            return half4(col, s.Alpha);
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            // Base texture sample.
            fixed4 texColor = tex2D(_MainTex, IN.uv_MainTex);
            o.Albedo = texColor.rgb;

            // Optionally, modulate the base color with a subsurface variation map.
            fixed4 subTex = tex2D(_SubsurfaceTex, IN.uv_SubsurfaceTex);
            o.Albedo *= subTex.rgb;

            // Adjust the shininess value using a specular map (if provided) to vary the specular highlight.
            fixed4 specMap = tex2D(_SpecTex, IN.uv_SpecTex);
            // Interpolate between two shininess values based on the red channel.
            _Shininess = lerp(0.1, 0.03, specMap.r);

            o.Alpha = texColor.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
