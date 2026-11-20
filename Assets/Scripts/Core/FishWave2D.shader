Shader "Custom/FishWave2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Pengaturan Gelombang Seluruh Badan)]
        _WaveSpeed ("Kecepatan Gelombang", Float) = 8.0
        _WaveFrequency ("Kerapatan Gelombang", Float) = 6.0
        _WaveAmplitude ("Tinggi Gelombang", Float) = 0.08
        _HeadFactor ("Intensitas Gerak Kepala (0 - 1)", Range(0, 1)) = 1.0
        
        [Toggle] _FacingRight ("Sprite Menghadap Kanan?", Float) = 1
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
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _WaveSpeed;
            float _WaveFrequency;
            float _WaveAmplitude;
            float _HeadFactor;
            float _FacingRight;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;

                // Menentukan posisi dari kepala (0.0) ke ekor (1.0)
                float bodyPos = (_FacingRight > 0.5) ? (1.0 - IN.texcoord.x) : IN.texcoord.x;
                
                // Jika _HeadFactor = 1.0, seluruh tubuh bergelombang 100% sama besar
                // Jika diturunkan (misal 0.5), kepala ikut bergelombang tapi ekor sedikit lebih lebar
                float flexibility = lerp(_HeadFactor, 1.0, bodyPos);

                // Arah rambat gelombang dari kepala menuju ekor
                float waveDir = (_FacingRight > 0.5) ? 1.0 : -1.0;
                float wave = sin((IN.texcoord.x * _WaveFrequency * waveDir) + (_Time.y * _WaveSpeed));
                
                // Terapkan gelombang ke sumbu Y seluruh sprite
                float4 modifiedVertex = IN.vertex;
                modifiedVertex.y += wave * _WaveAmplitude * flexibility;

                OUT.vertex = UnityObjectToClipPos(modifiedVertex);
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}