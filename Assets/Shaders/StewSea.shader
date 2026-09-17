// Fluid motion for the StewSea sprite: rolling swell, fine ripples, sideways flow, a bobbing horizon
// and glints drifting over the bright swirls. Built-in render pipeline, SpriteRenderer compatible.
// Set the StewSea texture Wrap Mode to Mirror so mirrored neighbour tiles stay seamless while moving.
Shader "Crescentia/StewSea"
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

        [Header(Parallax)]
        _ParallaxFollow ("Follow Fraction (same as ParallaxLayer X, Y)", Vector) = (0.7, 0.8, 0, 0)
        _TimeOffset ("Time Offset (desync copies of the sea)", Float) = 0

        [Header(Swell)]
        _HorizonV ("Horizon Height in Texture (0 bottom, 1 top)", Range(0, 1)) = 0.7
        _WaveAmplitude ("Swell Amplitude", Range(0, 0.04)) = 0.012
        _WaveFrequency ("Swell Frequency", Float) = 0.9
        _WaveSpeed ("Swell Speed", Float) = 0.8
        _RippleAmplitude ("Ripple Amplitude", Range(0, 0.02)) = 0.003
        _HorizonBob ("Horizon Bob", Range(0, 0.03)) = 0.005

        [Header(Flow)]
        _FlowAmplitude ("Flow Amplitude", Range(0, 0.04)) = 0.008
        _FlowSpeed ("Flow Speed", Float) = 0.5

        [Header(Shimmer)]
        _ShimmerColor ("Shimmer Color", Color) = (1, 0.85, 0.55, 1)
        _ShimmerStrength ("Shimmer Strength", Range(0, 1)) = 0.35
        _ShimmerSpeed ("Shimmer Speed", Float) = 0.6
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

            struct v2f_sea
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4 _ParallaxFollow;
            float _TimeOffset;
            float _HorizonV, _WaveAmplitude, _WaveFrequency, _WaveSpeed, _RippleAmplitude, _HorizonBob;
            float _FlowAmplitude, _FlowSpeed;
            fixed4 _ShimmerColor;
            float _ShimmerStrength, _ShimmerSpeed;

            v2f_sea vert(appdata_t IN)
            {
                v2f_sea OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                float4 pos = UnityFlipSprite(IN.vertex.xyz, _Flip);
                OUT.worldPos = mul(unity_ObjectToWorld, pos).xyz;
                OUT.vertex = UnityObjectToClipPos(pos);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color * _RendererColor;
                return OUT;
            }

            fixed4 frag(v2f_sea IN) : SV_Target
            {
                float t = _Time.y + _TimeOffset;
                float2 uv = IN.texcoord;

                // Layer space: cancel the parallax so waves travel with the painted sea, not with the world
                float2 p = IN.worldPos.xy - _WorldSpaceCameraPos.xy * _ParallaxFollow.xy;

                // Mirrored tiles run their texture backwards; flip sideways motion so the flow keeps one direction
                float flip = (ddx(uv.x) * ddx(IN.worldPos.x) + ddy(uv.x) * ddy(IN.worldPos.x)) >= 0.0 ? 1.0 : -1.0;

                float depth = saturate((_HorizonV - uv.y) / max(_HorizonV, 0.001)); // 0 at the horizon, 1 at the bottom edge
                float near = lerp(0.35, 1.0, depth);                                // nearer water moves more

                float swell = sin(p.x * _WaveFrequency + t * _WaveSpeed) * 0.6
                            + sin(p.x * _WaveFrequency * 2.3 - t * _WaveSpeed * 1.37 + 1.7) * 0.4;
                float ripple = sin(uv.y * lerp(90.0, 45.0, depth) + p.x * 0.8 - t * 1.6);
                float bob = sin(p.x * 0.23 + t * 0.55) * (1.0 - depth);

                float dy = (swell * _WaveAmplitude + ripple * _RippleAmplitude) * near + bob * _HorizonBob;
                float dx = sin(uv.y * 28.0 + p.x * 0.35 + t * _FlowSpeed) * _FlowAmplitude * near * flip;

                fixed4 col = SampleSpriteTexture(uv + float2(dx, dy)) * IN.color;

                // Glints drifting across the bright, buttery swirls
                float lum = dot(col.rgb, float3(0.299, 0.587, 0.114));
                float glint = saturate(sin(p.x * 1.9 + uv.y * 41.0 + t * _ShimmerSpeed)
                                     * sin(p.x * 0.73 - uv.y * 17.0 - t * _ShimmerSpeed * 0.8 + 2.1));
                glint = pow(glint, 6.0) * smoothstep(0.35, 0.8, lum);
                col.rgb += _ShimmerColor.rgb * glint * _ShimmerStrength * col.a;

                col.rgb *= col.a;
                return col;
            }
            ENDCG
        }
    }
}
