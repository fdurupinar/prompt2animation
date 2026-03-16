Shader "Custom/CC4HairShader_Adjusted"
{
    Properties
    {
        _MainTex ("Hair Diffuse (with transparency)", 2D) = "white" {}
        _SpecTex ("Hair Specular Map (R channel for intensity)", 2D) = "white" {}
        _Opacity ("Opacity", Range(0,1)) = 1.0
        _HairTrans ("Hair Translucency Color", Color) = (1,1,1,1)
        _Shininess ("Shininess", Range(0.01, 1)) = 0.1
        _SpecIntensity ("Specular Intensity", Range(0,1)) = 0.1
        _TransIntensity ("Translucency Intensity", Range(0,1)) = 0.1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 300
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        CGPROGRAM
        // Use a custom lighting function with alpha fade.
        #pragma surface surf CustomLighting alpha:fade

        sampler2D _MainTex;
        sampler2D _SpecTex;
        fixed4 _HairTrans;
        float _Opacity;
        float _Shininess;
        float _SpecIntensity;
        float _TransIntensity;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_SpecTex;
            float3 viewDir;
        };

        // Custom lighting function that combines diffuse, specular, and translucency.
        half4 LightingCustomLighting (SurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
        {
            // Diffuse term: using a half-Lambert approach for a softer shading.
            half NdotL = saturate(dot(s.Normal, lightDir));
            half diff = NdotL * 0.5 + 0.5;

            // Blinn-Phong specular term.
            half3 halfDir = normalize(lightDir + viewDir);
            half NdotH = saturate(dot(s.Normal, halfDir));
            half spec = pow(NdotH, 1.0 / _Shininess);
            // Scale specular contribution with the specular map value and intensity factor.
            spec *= s.Emission.r * _SpecIntensity;

            // Hair translucency: simulate light scattering through fine hair strands.
            // This effect is stronger when light comes from behind the hair.
            half translucency = saturate(dot(-s.Normal, lightDir));
            half3 hairTrans = _HairTrans.rgb * translucency * _TransIntensity;

            // Combine diffuse, specular, and translucency.
            half3 col = s.Albedo * diff + spec + hairTrans;
            return half4(col, s.Alpha);
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            // Sample the diffuse (hair) texture.
            fixed4 diffCol = tex2D(_MainTex, IN.uv_MainTex);
            o.Albedo = diffCol.rgb;
            // Use the _Opacity property to modulate the texture's alpha.
            o.Alpha = _Opacity * diffCol.a;
            
            
            // Sample the specular map and store it in the emission channel.
            // The red channel controls the specular intensity.
            fixed4 specSample = tex2D(_SpecTex, IN.uv_SpecTex);
            o.Emission =0;// specSample.rgb;

            
        }
        ENDCG
    }
    FallBack "Transparent/Diffuse"
}
