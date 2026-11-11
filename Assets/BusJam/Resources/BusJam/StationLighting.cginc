#ifndef BUSJAM_STATION_LIGHTING
#define BUSJAM_STATION_LIGHTING
float _BusJamLampsOn;
float3 StationLampLight(float3 world,float3 normal)
{
 float dx=abs(world.x)-3.70;
 float3 dz=world.z-float3(-3.8,1.1,6.9);
 float3 falloff=rcp(1+dx*dx*.48+dz*dz*.38);
 float energy=dot(falloff,float3(1,1,1))*(.38+.62*saturate(normal.y));
 energy*=1-smoothstep(5,9,abs(world.x));
 return float3(.42,.31,.14)*energy*_BusJamLampsOn;
}
#endif
