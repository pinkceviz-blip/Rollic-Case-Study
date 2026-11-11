#ifndef BUSJAM_STATION_LIGHTING
#define BUSJAM_STATION_LIGHTING
float _BusJamLampsOn;
float3 StationLampLight(float3 world,float3 normal)
{
 float facing=.35+.65*saturate(normal.y);
 // The road's lamps stay on the opposite pavement.
 float4 dx=world.x-float4(-3.3,-1.1,1.1,3.3);
 float dz=world.z-10.12;
 float4 roadPool=exp(-dx*dx/1.35-dz*dz/2.1);
 float3 light=float3(.34,.25,.12)*dot(roadPool,float4(1,1,1,1))*facing;
 // Overlapping perimeter lights and their bounced fill illuminate the entire passenger plaza.
 float edge=abs(world.x)-3.76;
 float4 rowDistance=world.z-float4(2.3,-.5,-3.2,-5.9);
 float4 areaPool=exp(-edge*edge/12-rowDistance*rowDistance/7);
 float areaMask=(1-smoothstep(4.05,4.45,abs(world.x)))*smoothstep(-6.7,-6.3,world.z)*(1-smoothstep(3.15,3.65,world.z));
 float spread=saturate(dot(areaPool,float4(1,1,1,1))*.75);
 light+=(float3(.16,.12,.055)+float3(.24,.18,.08)*spread)*areaMask*(.6+.4*saturate(normal.y));
 return light*_BusJamLampsOn;
}
#endif
