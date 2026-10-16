Shader "Alpha/CrabBoss/AttackArea"
{
    Properties
    {
        _Color ("Tint", Color) = (1, 0.15, 0.08, 0.32)
        [HideInInspector] _RevealProgress ("Reveal Progress", Range(0, 1)) = 1
        [HideInInspector] _RevealExtent ("Reveal Extent", Float) = 1
        [HideInInspector] _RevealBox ("Reveal Box", Float) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _RevealProgress;
            float _RevealExtent;
            float _RevealBox;

            struct Attributes
            {
                float4 vertex : POSITION;
            };

            struct Varyings
            {
                float4 vertex : SV_POSITION;
                float2 localXZ : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.localXZ = input.vertex.xz;
                return output;
            }

            fixed4 Frag(Varyings input) : SV_Target
            {
                // 전체 범위 위에 올린 채움만 중심에서 바깥으로 드러낸다.
                if (_RevealProgress < 1)
                {
                    float reach = lerp(length(input.localXZ),
                        max(0, input.localXZ.y), saturate(_RevealBox));
                    clip(_RevealProgress * _RevealExtent + 0.0001 - reach);
                }
                return _Color;
            }
            ENDCG
        }
    }
}
