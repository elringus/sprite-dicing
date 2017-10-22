
Shader "SpriteDicing/Default"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        [PerRendererData] _AlphaTex("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha("Enable External Alpha", Float) = 0
        [PerRendererData] _TintColor("Tint Color", Color) = (1,1,1,1)
        [PerRendererData] _Flip("Flip", Vector) = (1,1,1,1)

        [MaterialToggle] PixelSnap("Pixel snap", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM

            #pragma target 2.0
            #pragma vertex ComputeVertex
            #pragma fragment ComputeFragment
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA

            #include "UnityCG.cginc"

            CBUFFER_START(UnityPerDrawSprite)
            float _EnableExternalAlpha;
            CBUFFER_END

            sampler2D _MainTex;
            fixed4 _TintColor;
            float4 _Flip;

            struct VertexInput
            {
                float4 Vertex : POSITION;
                fixed4 Color : COLOR;
                float2 TexCoord : TEXCOORD0;
            };

            struct VertexOutput
            {
                float4 Vertex : SV_POSITION;
                fixed4 Color : COLOR;
                float2 TexCoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            VertexOutput ComputeVertex (VertexInput vertexInput)
            {
                VertexOutput vertexOutput;

                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(vertexOutput);

                vertexInput.Vertex.xy *= _Flip.xy;
                vertexOutput.Vertex = UnityObjectToClipPos(vertexInput.Vertex);
                vertexOutput.TexCoord = vertexInput.TexCoord;
                vertexOutput.Color = vertexInput.Color * _TintColor;

                #ifdef PIXELSNAP_ON
                vertexOutput.Vertex = UnityPixelSnap(vertexOutput.Vertex);
                #endif

                return vertexOutput;
            }

            fixed4 SampleSpriteTexture(float2 uv)
            {
                fixed4 color = tex2D(_MainTex, uv);

                #if ETC1_EXTERNAL_ALPHA
                fixed4 alpha = tex2D(_AlphaTex, uv);
                color.a = lerp(color.a, alpha.r, _EnableExternalAlpha);
                #endif

                return color;
            }

            fixed4 ComputeFragment (VertexOutput vertexOutput) : SV_Target
            {
                fixed4 color = SampleSpriteTexture(vertexOutput.TexCoord); 
                color *= vertexOutput.Color;
                color.rgb *= color.a;

                return color;
            }

            ENDCG
        }
    }
}
