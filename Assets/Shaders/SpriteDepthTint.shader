// Sprite / Tilemap shader that fades the sprite toward a dark color the lower it sits in the world.
// Keep Top Y / Bottom Y in step with the DepthDarkness material so tiles and background darken together.
Shader "Crescentia/SpriteDepthTint"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        // Defaults for renderers that do not set these (e.g. TilemapRenderer); SpriteRenderer overrides them
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
        _Color ("Tint", Color) = (1,1,1,1)
        _DarkColor ("Dark Color", Color) = (0.05, 0.02, 0.015, 1)
        _TopY ("Top Y (no darkening)", Float) = 0
        _BottomY ("Bottom Y (full darkening)", Float) = -19
        _Curve ("Curve", Range(0.3, 4)) = 1.3
        _Strength ("Strength", Range(0, 1)) = 0.7
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #include "UnitySprites.cginc"

            struct v2f_tint
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float  worldY   : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _DarkColor;
            float _TopY, _BottomY, _Curve, _Strength;

            v2f_tint vert(appdata_t IN)
            {
                v2f_tint OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                float4 pos = UnityFlipSprite(IN.vertex.xyz, _Flip);
                OUT.worldY = mul(unity_ObjectToWorld, pos).y;
                OUT.vertex = UnityObjectToClipPos(pos);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color * _RendererColor;
                return OUT;
            }

            fixed4 frag(v2f_tint IN) : SV_Target
            {
                fixed4 col = SampleSpriteTexture(IN.texcoord) * IN.color;
                float h = saturate((_TopY - IN.worldY) / max(_TopY - _BottomY, 0.001));
                float k = pow(h, _Curve) * _Strength;
                #ifndef UNITY_COLORSPACE_GAMMA
                k = 1.0 - pow(1.0 - k, 2.2); // keep Strength perceptual in linear color space
                #endif
                col.rgb = lerp(col.rgb, _DarkColor.rgb, k);
                col.rgb *= col.a;
                return col;
            }
            ENDCG
        }
    }
}
