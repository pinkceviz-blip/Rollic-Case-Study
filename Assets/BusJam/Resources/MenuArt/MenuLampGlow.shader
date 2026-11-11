Shader "BusJam/MenuLampGlow"
{
 SubShader
 {
  Tags {"Queue"="Transparent" "RenderType"="Transparent"}
  Pass
  {
   Tags {"LightMode"="UniversalForwardOnly"}
   Blend SrcAlpha One
   ZWrite Off
   Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   float _BusJamLampsOn;
   struct App {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
   struct Out {float4 position:SV_POSITION;float2 uv:TEXCOORD0;};
   Out vert(App v){Out o;o.position=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
   half4 frag(Out i):SV_Target {float glow=pow(saturate(1-length((i.uv-.5)*2)),2);return half4(1,.78,.30,glow*.7*_BusJamLampsOn);}
   ENDHLSL
  }
 }
}
