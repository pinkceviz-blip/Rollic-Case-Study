Shader "BusJam/Station"
{
    Properties { _Color ("Color", Color) = (1,1,1,1) _LampEmission("Lamp emission",Float)=0 }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
 #include "StationLighting.cginc"
 float _BusJamDark;
 half4 ThemeColor(float3 color,float alpha){return half4(color*lerp(float3(1,1,1),float3(.48,.59,.82),_BusJamDark),alpha);}
            struct Input { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct Varying { float4 position : SV_POSITION; float3 normal : TEXCOORD0; float3 view : TEXCOORD1; float3 world:TEXCOORD2; };
            float4 _Color;float _LampEmission;
            Varying vert(Input v)
            {
                Varying o;
                o.position = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                float3 world = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.world=world;o.view = _WorldSpaceCameraPos.xyz - world;
                return o;
            }
            half4 frag(Varying i) : SV_Target
            {
                float3 n = normalize(i.normal);
                float diffuse = saturate(dot(n, normalize(float3(-0.5, 1, -0.6))));
                float highlight = pow(saturate(dot(n, normalize(normalize(i.view) + normalize(float3(-0.5,1,-0.6))))), 40) * 0.16;
                half4 result=ThemeColor(_Color.rgb * (0.65 + diffuse * 0.35) + highlight, 1);result.rgb+=StationLampLight(i.world,n)*_Color.rgb;result.rgb=lerp(result.rgb,float3(1,.95,.65),saturate(_LampEmission*_BusJamLampsOn));return result;
            }
            ENDHLSL
        }
    }
}
