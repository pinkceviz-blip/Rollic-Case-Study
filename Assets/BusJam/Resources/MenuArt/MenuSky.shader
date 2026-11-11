Shader "BusJam/MenuSky"
{
    Properties { _Top("Top",Color)=(.14,.56,.86,1) _Bottom("Bottom",Color)=(.03,.83,.94,1) }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="UniversalForwardOnly" }
            Cull Off
            ZWrite Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct App {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
            struct Out {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
            float4 _Top,_Bottom;float _BusJamDark;
            Out vert(App v){Out o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
            half4 frag(Out i):SV_Target{return lerp(lerp(_Bottom,_Top,saturate(i.uv.y)),lerp(float4(.055,.10,.22,1),float4(.015,.025,.08,1),saturate(i.uv.y)),_BusJamDark);}
            ENDHLSL
        }
    }
}
