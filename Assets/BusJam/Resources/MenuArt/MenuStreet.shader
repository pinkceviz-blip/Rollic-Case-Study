Shader "BusJam/MenuStreet"
{
    Properties { _Color("Color",Color)=(1,1,1,1) _FogColor("Distance haze",Color)=(.48,.67,.72,1) _Gloss("Gloss",Range(0,1))=1 _SurfaceDetail("Surface detail",Float)=0 _LampGlow("Lamp emission",Float)=0 }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
 float _BusJamDark,_BusJamLampsOn;
 half4 ThemeColor(float3 color,float alpha){return half4(color*lerp(float3(1,1,1),float3(.48,.59,.82),_BusJamDark),alpha);}
            struct App {float4 vertex:POSITION;float3 normal:NORMAL;UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Out {float4 pos:SV_POSITION;float3 normal:TEXCOORD0;float3 world:TEXCOORD1;float3 local:TEXCOORD2;};
            CBUFFER_START(UnityPerMaterial)
            float4 _Color,_FogColor;float _Gloss,_SurfaceDetail,_LampGlow;
            CBUFFER_END
            Out vert(App v){UNITY_SETUP_INSTANCE_ID(v);Out o;o.pos=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.local=v.vertex.xyz;return o;}
            half4 frag(Out i):SV_Target
            {
                float3 n=normalize(i.normal),view=normalize(_WorldSpaceCameraPos.xyz-i.world),light=normalize(float3(-.5,1,-.6));
                float diffuse=saturate(dot(n,light));float spec=pow(saturate(dot(n,normalize(view+light))),40)*.16*_Gloss;
                float3 color=_Color.rgb*(.65+diffuse*.35)+spec;
                if(_SurfaceDetail>.5 && _SurfaceDetail<1.5)
                {
                    float horizontal=abs(n.x)>.5?i.local.z:i.local.x;
                    float row=floor(i.local.y*4.3);
                    float2 grid=frac(float2(horizontal*2.4+fmod(row,2)*.5,i.local.y*4.3));
                    float mortar=1-smoothstep(.025,.055,min(grid.x,grid.y));
                    float noise=frac(sin(dot(floor(float2(horizontal*2.4+fmod(row,2)*.5,row)),float2(127.1,311.7)))*43758.5453);
                    color*=.96+noise*.08;color=lerp(color,color*.85,mortar*.45);
                }
                else if(_SurfaceDetail>2.5)
                {
                    float leaves=sin(i.local.x*65+i.local.z*29)*sin(i.local.y*71-i.local.z*41);
                    color*=.96+leaves*.07;
                }
                else if(_SurfaceDetail>1.5)
                {
                    float grain=sin(i.local.x*94+i.local.z*86+sin(i.local.y*7));
                    color*=.94+grain*.06;
                }
                float haze=.42*smoothstep(50,145,distance(i.world,_WorldSpaceCameraPos.xyz));
                half4 result=ThemeColor(lerp(color,_FogColor.rgb,haze),1);
                // Lamps repeat with the baked street chunks; warm light follows each moving pole.
                float dz=abs(frac((i.local.z-1)/12+.5)-.5)*12;
                float dx=abs(abs(i.local.x)-3.47);
                float pool=exp(-dx*dx/2.2-dz*dz/3.0)*saturate(n.y)*step(abs(i.local.x),5.5)*(1-smoothstep(.15,.4,i.local.y));
                result.rgb+=float3(.58,.38,.12)*pool*_BusJamLampsOn;
                result.rgb=lerp(result.rgb,float3(1,.93,.58),saturate(_LampGlow*_BusJamLampsOn));
                return result;
            }
            ENDHLSL
        }
    }
}
