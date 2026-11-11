Shader "BusJam/Asphalt"
{
    Properties { _Color("Asphalt",Color)=(.16,.18,.20,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
 #include "../BusJam/StationLighting.cginc"
 float _BusJamDark;
 half4 ThemeColor(float3 color,float alpha){return half4(color*lerp(float3(1,1,1),float3(.48,.59,.82),_BusJamDark),alpha);}
            struct App { float4 vertex:POSITION; };
            struct Out { float4 position:SV_POSITION; float3 world:TEXCOORD0; };
            float4 _Color;
            float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float noise(float2 p) { float2 a=floor(p),b=frac(p);b=b*b*(3-2*b);return lerp(lerp(hash(a),hash(a+float2(1,0)),b.x),lerp(hash(a+float2(0,1)),hash(a+1),b.x),b.y); }
            Out vert(App v) { Out o;o.position=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o; }
            half4 frag(Out i):SV_Target
            {
                float2 p=i.world.xz;
                float grain=noise(p*90)-.5;
                float aggregate=noise(p*39)-.5;
                float patches=noise(p*.75)-.5;
                float nearTrack=(p.y-5.2)/.8,farTrack=(p.y-8.0)/.8;
                float wear=exp(-nearTrack*nearTrack)*.012+exp(-farTrack*farTrack)*.012;
                half4 result=ThemeColor(_Color.rgb+grain*.028+aggregate*.020+patches*.016+wear,1);result.rgb+=StationLampLight(i.world,float3(0,1,0));return result;
            }
            ENDHLSL
        }
    }
}
