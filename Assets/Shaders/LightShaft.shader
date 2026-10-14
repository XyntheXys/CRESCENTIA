// Soft additive light beam with drifting dust streaks and a slow pulse.
// Use on a sprite quad: the top edge of the sprite is the light source, the beam fades toward the bottom edge.
// Rotate the object to slant the beam.
Shader "Crescentia/LightShaft"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        // Defaults for renderers that do not set these (e.g. TilemapRenderer); SpriteRenderer overrides them
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
        _Color ("Light Color", Color) = (1, 0.8, 0.55, 1)
        _Intensity ("Intensity", Range(0, 2)) = 0.25
        _TopWidth ("Width at Source", Range(0.05, 1)) = 0.45
        _BottomWidth ("Width at Tip", Range(0.05, 1)) = 1
        _EdgeSoftness ("Edge Softness", Range(0.01, 1)) = 0.6
        _FadeCurve ("Length Fade Curve", Range(0.2, 4)) = 1.4

        [Header(Motion)]
        _StreakDensity ("Streak Density", Float) = 5
        _StreakSpeed ("Streak Speed", Float) = 0.05
        _PulseSpeed ("Pulse Speed", Float) = 0.06
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.2
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
        Blend One One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #include "UnitySprites.cginc"

            struct v2f_shaft
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float  worldX   : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float _Intensity, _TopWidth, _BottomWidth, _EdgeSoftness, _FadeCurve;
            float _StreakDensity, _StreakSpeed, _PulseSpeed, _PulseAmount;

            v2f_shaft vert(appdata_t IN)
            {
                v2f_shaft OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                float4 pos = UnityFlipSprite(IN.vertex.xyz, _Flip);
                OUT.worldX = mul(unity_ObjectToWorld, pos).x; // per vertex, so batched shafts still pulse out of step
                OUT.vertex = UnityObjectToClipPos(pos);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color * _RendererColor;
                return OUT;
            }

            fixed4 frag(v2f_shaft IN) : SV_Target
            {
                const float TAU = 6.2831853;
                float t = _Time.y;
                float along = IN.texcoord.y; // 1 at the source, 0 at the tip

                float halfWidth = lerp(_BottomWidth, _TopWidth, along) * 0.5;
                float x = abs(IN.texcoord.x - 0.5) / max(halfWidth, 0.001);
                float edge = 1.0 - smoothstep(1.0 - _EdgeSoftness, 1.0, x);
                float fade = pow(along, _FadeCurve) * smoothstep(1.0, 0.9, along);

                float u = IN.texcoord.x;
                float streaks = 0.6 + 0.4 * sin(u * _StreakDensity * TAU + t * _StreakSpeed * TAU)
                                          * sin(u * _StreakDensity * 2.71 * TAU - t * _StreakSpeed * 1.9 * TAU + 1.3);
                float pulse = 1.0 + sin(t * _PulseSpeed * TAU + IN.worldX * 0.12) * _PulseAmount;

                float a = edge * fade * streaks * pulse * _Intensity * IN.color.a;
                return fixed4(IN.color.rgb * a, 0);
            }
            ENDCG
        }
    }
}
