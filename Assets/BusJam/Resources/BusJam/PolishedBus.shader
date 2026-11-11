Shader "BusJam/PolishedBus"
{
 Properties { _Color("Color",Color)=(1,1,1,1) _Gloss("Clear coat",Range(0,1))=.55 _Metal("Metal finish",Range(0,1))=0 }
 SubShader { Tags {"RenderType"="Opaque" "Queue"="Geometry"} Pass {Tags {"LightMode"="UniversalForwardOnly"}
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 #include "StationLighting.cginc"
 float _BusJamDark;
 half4 ThemeColor(float3 color,float alpha){return half4(color*lerp(float3(1,1,1),float3(.48,.59,.82),_BusJamDark),alpha);}
 struct Input {float4 vertex:POSITION;float3 normal:NORMAL;};
 struct Varying {float4 position:SV_POSITION;float3 normal:TEXCOORD0;float3 view:TEXCOORD1;float3 world:TEXCOORD2;};
 float4 _Color;float _Gloss,_Metal;
 Varying vert(Input v){Varying o;o.position=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.view=_WorldSpaceCameraPos.xyz-o.world;return o;}
 half4 frag(Varying i):SV_Target
 {
  float3 n=normalize(i.normal),v=normalize(i.view),l=normalize(float3(-.45,1,-.65));
  float diffuse=.60+.40*saturate(dot(n,l));float fresnel=pow(1-saturate(dot(n,v)),4);
  float3 r=reflect(-v,n);float sky=smoothstep(-.2,.7,r.y);
  float3 reflection=lerp(float3(.10,.20,.32),float3(.68,.89,1),sky);
  float broad=pow(saturate(dot(n,normalize(v+l))),14)*.15;
  float spec=pow(saturate(dot(n,normalize(v+l))),90)*.65;
  float3 color=_Color.rgb*diffuse+_Gloss*(broad+spec);
  color=lerp(color,reflection,(_Metal*.45+fresnel*.24)*_Gloss);
  half4 result=ThemeColor(color,1);result.rgb+=StationLampLight(i.world,n)*_Color.rgb;return result;
 }
 ENDHLSL
 } }
}
