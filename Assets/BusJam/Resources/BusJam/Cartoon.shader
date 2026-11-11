Shader "BusJam/Cartoon"
{
 Properties { _Color("Color",Color)=(1,1,1,1) _Gloss("Gloss",Range(0,1))=.4 }
 SubShader { Tags {"RenderType"="Opaque" "Queue"="Geometry"} Pass { Tags {"LightMode"="UniversalForwardOnly"}
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 #include "StationLighting.cginc"
 float _BusJamDark;
 half4 ThemeColor(float3 color,float alpha){return half4(color*lerp(float3(1,1,1),float3(.48,.59,.82),_BusJamDark),alpha);}
 struct Input {float4 vertex:POSITION;float3 normal:NORMAL;};
 struct Varying {float4 position:SV_POSITION;float3 normal:TEXCOORD0;float3 view:TEXCOORD1;float3 world:TEXCOORD2;};
 float4 _Color;float _Gloss;
 Varying vert(Input v){Varying o;o.position=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);float3 w=mul(unity_ObjectToWorld,v.vertex).xyz;o.world=w;o.view=_WorldSpaceCameraPos.xyz-w;return o;}
 half4 frag(Varying i):SV_Target {float3 n=normalize(i.normal),v=normalize(i.view),l=normalize(float3(-.4,1,-.5));float d=dot(n,l);float light=.70+.16*smoothstep(-.2,.05,d)+.14*smoothstep(.4,.55,d);float shine=pow(saturate(dot(n,normalize(v+l))),34)*_Gloss;float rim=pow(1-saturate(dot(n,v)),3)*.10;half4 result=ThemeColor(_Color.rgb*light+shine+rim*_Color.rgb,1);result.rgb+=StationLampLight(i.world,n)*_Color.rgb;return result;}
 ENDHLSL
 } }
}
