Shader "BusJam/ContactShadow"
{
    Properties { _Color("Shadow",Color)=(.05,.07,.12,.23) }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent-10" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForwardOnly" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct App { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct Out { float4 position:SV_POSITION; float2 uv:TEXCOORD0; };
            float4 _Color;
            Out vert(App v) { Out o;o.position=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o; }
            half4 frag(Out i):SV_Target { float radius=length((i.uv-.5)*2);return half4(_Color.rgb,_Color.a*pow(saturate(1-radius),1.6)); }
            ENDHLSL
        }
    }
}
