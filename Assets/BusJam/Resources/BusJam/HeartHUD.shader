Shader "BusJam/HeartHUD"
{
 Properties { [PerRendererData] _MainTex("HUD",2D)="white" {} _HeartGreen("Green heart",Range(0,1))=0 }
 SubShader
 {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
  Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct appdata { float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR; };
   struct v2f { float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR; };
   sampler2D _MainTex;float _HeartGreen;
   v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
   fixed4 frag(v2f i):SV_Target
   {
    fixed4 c=tex2D(_MainTex,i.uv);
    float area=step(.71,i.uv.x)*step(i.uv.x,.84)*step(.34,i.uv.y)*step(i.uv.y,.68);
    float red=smoothstep(.015,.08,c.r-c.g)*step(c.b,c.r+.03);
    c.rgb=lerp(c.rgb,c.grb,area*red*_HeartGreen);
    return c*i.color;
   }
   ENDCG
  }
 }
}
