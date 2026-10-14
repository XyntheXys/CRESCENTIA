// World-locked darkness that thickens toward the bottom of the level, with slow drifting murk.
// Put it on a large sprite behind the gameplay layer (Order in Layer between the midground and the tiles).
Shader "Crescentia/DepthDarkness"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        // Defaults for renderers that do not set these (e.g. TilemapRenderer); SpriteRenderer overrides them
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
        _Color ("Dark Color", Color) = (0.05, 0.02, 0.015, 1)
        _TopY ("Top Y (no darkness)", Float) = 0
        _BottomY ("Bottom Y (full darkness)", Float) = -19
        _Curve ("Curve", Range(0.3, 4)) = 1.3
        _MaxAlpha ("Max Darkness", Range(0, 1)) = 0.85

        [Header(Murk)]
        _NoiseStrength ("Murk Strength", Range(0, 0.5)) = 0.15
        _NoiseScale ("Murk Scale", Float) = 0.18
        _NoiseSpeed ("Murk Speed", Float) = 0.06
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="False"
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
            #pragma target 3.0
            #pragma multi_compile_instancing
            #include "UnitySprites.cginc"

            struct v2f_dark
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float3 worldPos : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float _TopY, _BottomY, _Curve, _MaxAlpha;
            float _NoiseStrength, _NoiseScale, _NoiseSpeed;

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            v2f_dark vert(appdata_t IN)
            {
                v2f_dark OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                float4 pos = UnityFlipSprite(IN.vertex.xyz, _Flip);
                OUT.worldPos = mul(unity_ObjectToWorld, pos).xyz;
                OUT.vertex = UnityObjectToClipPos(pos);
                OUT.color = IN.color * _Color * _RendererColor;
                return OUT;
            }

            fixed4 frag(v2f_dark IN) : SV_Target
            {
                float h = saturate((_TopY - IN.worldPos.y) / max(_TopY - _BottomY, 0.001)); // 0 at the top, 1 at the bottom
                float a = pow(h, _Curve) * _MaxAlpha;

                float t = _Time.y * _NoiseSpeed;
                float2 q = IN.worldPos.xy * _NoiseScale;
                float n = ValueNoise(q + float2(t, t * 0.35)) * 0.65 + ValueNoise(q * 2.3 - float2(t * 1.3, 0.0)) * 0.35;
                a = saturate(a * (1.0 + (n - 0.5) * 2.0 * _NoiseStrength)) * IN.color.a;

                #ifndef UNITY_COLORSPACE_GAMMA
                // Blending happens in linear space, which looks much lighter than the value suggests; keep Max Darkness perceptual
                a = 1.0 - pow(1.0 - a, 2.2);
                #endif

                return fixed4(IN.color.rgb * a, a);
            }
            ENDCG
        }
    }
}
