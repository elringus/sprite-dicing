
Shader "Sprites/Foggy"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        [PerRendererData] _AlphaTex("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha("Enable External Alpha", Float) = 0
        _Color("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip("Flip", Vector) = (1,1,1,1)
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

        ZWrite Off
        Cull Off
        Lighting Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM

            #pragma target 2.0
            #pragma vertex ComputeVertex
            #pragma fragment ComputeFragment
            #pragma multi_compile_fog  
            #pragma multi_compile_instancing
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA

            #include "UnityCG.cginc"

            #ifdef UNITY_INSTANCING_ENABLED
            UNITY_INSTANCING_CBUFFER_START(PerDrawSprite)
            fixed4 unity_SpriteRendererColorArray[UNITY_INSTANCED_ARRAY_SIZE];
            float4 unity_SpriteFlipArray[UNITY_INSTANCED_ARRAY_SIZE];
            UNITY_INSTANCING_CBUFFER_END
            #define _RendererColor unity_SpriteRendererColorArray[unity_InstanceID]
            #define _Flip unity_SpriteFlipArray[unity_InstanceID]
            #endif 

            CBUFFER_START(UnityPerDrawSprite)
            #ifndef UNITY_INSTANCING_ENABLED
            fixed4 _RendererColor;
            float4 _Flip;
            #endif
            float _EnableExternalAlpha;
            CBUFFER_END

            fixed4 _Color;
            sampler2D _MainTex;
            sampler2D _AlphaTex;

            struct VertexInput
            {
                float4 Vertex : POSITION;
                fixed4 Color : COLOR;
                float2 TexCoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VertexOutput
            {
                float4 Vertex : SV_POSITION;
                fixed4 Color : COLOR;
                float2 TexCoord : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                UNITY_VERTEX_OUTPUT_STEREO
            };

            VertexOutput ComputeVertex (VertexInput vertexInput)
            {
                VertexOutput vertexOutput;

                UNITY_SETUP_INSTANCE_ID(vertexInput);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(vertexOutput);

                #ifdef UNITY_INSTANCING_ENABLED
                vertexInput.Vertex.xy *= _Flip.xy;
                #endif

                vertexOutput.Vertex = UnityObjectToClipPos(vertexInput.Vertex);
                vertexOutput.TexCoord = vertexInput.TexCoord;
                vertexOutput.Color = vertexInput.Color * _Color;

                #ifdef PIXELSNAP_ON
                vertexOutput.Vertex = UnityPixelSnap(vertexOutput.Vertex);
                #endif

                UNITY_TRANSFER_FOG(vertexOutput, vertexOutput.Vertex);

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

                UNITY_APPLY_FOG(vertexOutput.fogCoord, color);

                color.rgb *= color.a;

                return color;
            }

            ENDCG
        }
    }
}
