Shader "BusJam/MenuSkyline"
{
    Properties { _Color("Silhouette color",Color)=(.13,.47,.66,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
 float _BusJamDark;
 half4 ThemeColor(float3 color,float alpha){return half4(color*lerp(float3(1,1,1),float3(.48,.59,.82),_BusJamDark),alpha);}
            struct App { float4 vertex:POSITION; };
            struct Out { float4 pos:SV_POSITION;float height:TEXCOORD0; };
            float4 _Color;
            Out vert(App v){Out o;o.pos=UnityObjectToClipPos(v.vertex);o.height=mul(unity_ObjectToWorld,v.vertex).y;return o;}
            half4 frag(Out i):SV_Target
            {
                float mist=.08*(1-saturate(i.height/12));
                return ThemeColor(lerp(_Color.rgb,float3(.48,.67,.72),mist),1);
            }
            ENDHLSL
        }
    }
}
