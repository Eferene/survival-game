Shader "Custom/TerrainSplatmapURP"
{
    Properties
    {
        [MainTexture] _Control ("Control (Splat) Map (RGBA)", 2D) = "white" {}
        _Texture1 ("Texture 1 (R)", 2D) = "white" {}
        _Texture2 ("Texture 2 (G)", 2D) = "white" {}
        _Texture3 ("Texture 3 (B)", 2D) = "white" {}
        _Texture4 ("Texture 4 (A)", 2D) = "white" {}

        _Tile1("Texture 1 Tiling", Float) = 1
        _Tile2("Texture 2 Tiling", Float) = 1
        _Tile3("Texture 3 Tiling", Float) = 1
        _Tile4("Texture 4 Tiling", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
            };

            TEXTURE2D(_Control);        SAMPLER(sampler_Control);
            TEXTURE2D(_Texture1);       SAMPLER(sampler_Texture1);
            TEXTURE2D(_Texture2);       SAMPLER(sampler_Texture2);
            TEXTURE2D(_Texture3);       SAMPLER(sampler_Texture3);
            TEXTURE2D(_Texture4);       SAMPLER(sampler_Texture4);

            CBUFFER_START(UnityPerMaterial)
                float4 _Control_ST;
                float4 _Texture1_ST;
                float4 _Texture2_ST;
                float4 _Texture3_ST;
                float4 _Texture4_ST;
                float _Tile1, _Tile2, _Tile3, _Tile4;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float4 splat = SAMPLE_TEXTURE2D(_Control, sampler_Control, IN.uv);
                
                float2 uv1 = IN.uv * _Tile1;
                float2 uv2 = IN.uv * _Tile2;
                float2 uv3 = IN.uv * _Tile3;
                float2 uv4 = IN.uv * _Tile4;

                half4 color1 = SAMPLE_TEXTURE2D(_Texture1, sampler_Texture1, uv1);
                half4 color2 = SAMPLE_TEXTURE2D(_Texture2, sampler_Texture2, uv2);
                half4 color3 = SAMPLE_TEXTURE2D(_Texture3, sampler_Texture3, uv3);
                half4 color4 = SAMPLE_TEXTURE2D(_Texture4, sampler_Texture4, uv4);

                half4 finalColor = color1 * splat.r + color2 * splat.g + color3 * splat.b + color4 * splat.a;
                
                Light mainLight = GetMainLight();
                float3 normalWS = normalize(IN.normalWS);
                half dotNL = saturate(dot(normalWS, mainLight.direction));
                half3 lighting = dotNL * mainLight.color;
                finalColor.rgb *= lighting + UNITY_LIGHTMODEL_AMBIENT.rgb;

                return finalColor;
            }
            ENDHLSL
        }
    }
}