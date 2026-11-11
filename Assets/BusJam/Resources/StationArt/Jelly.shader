Shader "BusJam/Jelly"
{
    Properties { _Color ("Candy color", Color) = (0.1,0.7,1,0.92) }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForwardOnly" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
 #include "../BusJam/StationLighting.cginc"
 float _BusJamDark;
 half4 ThemeColor(float3 color,float alpha){return half4(color*lerp(float3(1,1,1),float3(.48,.59,.82),_BusJamDark),alpha);}
            struct App { float4 vertex:POSITION; float3 normal:NORMAL; };
            struct Out { float4 position:SV_POSITION; float3 normal:TEXCOORD0; float3 world:TEXCOORD1; };
            float4 _Color;
            Out vert(App v) { Out o; o.position=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o; }
            half4 frag(Out i):SV_Target
            {
                float3 n=normalize(i.normal),v=normalize(_WorldSpaceCameraPos.xyz-i.world),l=normalize(float3(-.5,1,-.65));
                float nv=saturate(dot(n,v)),nl=saturate(dot(n,l));
                float3 h=normalize(v+l);
                float gloss=pow(saturate(dot(n,h)),65)*.85+pow(saturate(dot(n,h)),14)*.17;
                float rim=pow(1-nv,2.7);
                float scatter=pow(saturate(dot(-n,l)),1.5)*.22;
                float3 color=_Color.rgb*(.54+nl*.43+scatter)+lerp(_Color.rgb,float3(1,1,1),.52)*rim*.55+gloss;
                half4 result=ThemeColor(color,saturate(_Color.a+rim*.06));result.rgb+=StationLampLight(i.world,n)*_Color.rgb;return result;
            }
            ENDHLSL
        }
    }
}
