using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace Rollic.BusJam.EditorTools
{
 public static class MenuTrafficAssetBuilder
 {
  const string Root="Assets/BusJam/Resources/MenuTraffic";
  static int meshIndex;
  static Material paint,glass,rubber,metal,white,red,seat,skin,helmet;
  static readonly List<Mesh> temporary=new List<Mesh>();
  [MenuItem("Bus Jam/Build Menu Traffic Assets")]
  public static void Build()
  {
   System.IO.Directory.CreateDirectory(Root);AssetDatabase.Refresh();meshIndex=0;
   glass=Mat("Window",new Color(.045f,.22f,.31f));rubber=Mat("Tire",new Color(.075f,.09f,.13f));metal=Mat("Alloy",new Color(.70f,.82f,.88f));white=Mat("Headlight",new Color(1,.96f,.70f));red=Mat("Taillight",new Color(1,.12f,.18f));seat=Mat("Saddle",new Color(.13f,.12f,.23f));skin=Mat("Rider jelly",new Color(.26f,.83f,.65f));helmet=Mat("Helmet",new Color(1,.79f,.18f));
   Color[] colors={new Color(1,.26f,.34f),new Color(1,.71f,.10f),new Color(.18f,.65f,1)};
   for(int i=0;i<colors.Length;i++){paint=Mat("CarPaint"+i,colors[i]);var root=new GameObject("Traffic Car "+i);Car(root.transform);Save(root,"Car_"+i);}
   paint=Mat("Scooter violet",new Color(.64f,.31f,.98f));var bike=new GameObject("Traffic Scooter");Scooter(bike.transform);Save(bike,"Scooter");
   var bus=new GameObject("Traffic Bus");var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/BusJam/Resources/BusFleet/Bus_GOLD.prefab");if(!source)throw new System.Exception("Gold fleet prefab missing");var model=(GameObject)PrefabUtility.InstantiatePrefab(source);model.transform.SetParent(bus.transform,false);model.transform.localRotation=Quaternion.Euler(0,-90,0);model.transform.localScale=Vector3.one*.88f;PrefabUtility.SaveAsPrefabAsset(bus,Root+"/Bus.prefab");Object.DestroyImmediate(bus);
   AssetDatabase.SaveAssets();AssetDatabase.Refresh();Debug.Log("TRAFFIC ASSETS BUILT: three cars, open deck bus, scooter with helmeted jelly rider.");
  }
  static Material Mat(string n,Color c){string path=Root+"/"+n+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Resources.Load<Shader>("MenuArt/MenuStreet"));AssetDatabase.CreateAsset(m,path);}m.color=c;EditorUtility.SetDirty(m);return m;}
  static Transform Part(Transform p,string n,PrimitiveType type,Vector3 pos,Vector3 size,Material m){var g=GameObject.CreatePrimitive(type);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=size;g.GetComponent<MeshRenderer>().sharedMaterial=m;Object.DestroyImmediate(g.GetComponent<Collider>());return g.transform;}
  static void Box(Transform p,string n,Vector3 pos,Vector3 size,Material m){var t=Part(p,n,PrimitiveType.Cube,pos,size,m);var mesh=Rounded(size,.08f);t.localScale=Vector3.one;t.GetComponent<MeshFilter>().sharedMesh=mesh;temporary.Add(mesh);}
  static void Wheel(Transform p,float z,float x,float radius){var t=Part(p,"Rubber wheel",PrimitiveType.Cylinder,new Vector3(x,radius,z),new Vector3(radius*2,.095f,radius*2),rubber);t.localRotation=Quaternion.Euler(0,0,90);var h=Part(p,"Alloy wheel hub",PrimitiveType.Cylinder,new Vector3(x+Mathf.Sign(x)*.105f,radius,z),new Vector3(radius*1.12f,.015f,radius*1.12f),metal);h.localRotation=t.localRotation;}
  static void Car(Transform p){
   Box(p,"Rounded car body",new Vector3(0,.52f,0),new Vector3(1.48f,.55f,2.85f),paint);
   Box(p,"Glass cabin",new Vector3(0,.94f,-.10f),new Vector3(1.24f,.54f,1.55f),glass);
   Box(p,"Painted roof",new Vector3(0,1.23f,-.15f),new Vector3(1.27f,.10f,1.33f),paint);
   Box(p,"Front hood",new Vector3(0,.78f,1.04f),new Vector3(1.36f,.09f,.60f),paint);
   Box(p,"Rear bumper",new Vector3(0,.37f,-1.45f),new Vector3(1.3f,.12f,.08f),metal);
   Box(p,"Front grille",new Vector3(0,.43f,1.439f),new Vector3(.67f,.16f,.04f),rubber);
   for(int s=-1;s<=1;s+=2){Wheel(p,.92f,s*.70f,.29f);Wheel(p,-.88f,s*.70f,.29f);Box(p,"Headlamp",new Vector3(s*.51f,.61f,1.445f),new Vector3(.32f,.15f,.045f),white);Box(p,"Rear light",new Vector3(s*.54f,.61f,-1.445f),new Vector3(.25f,.14f,.045f),red);Box(p,"Side mirror",new Vector3(s*.77f,.94f,.57f),new Vector3(.20f,.13f,.23f),paint);Box(p,"Window pillar",new Vector3(s*.63f,.98f,-.17f),new Vector3(.035f,.50f,.075f),paint);Box(p,"Door handle",new Vector3(s*.75f,.71f,-.26f),new Vector3(.045f,.05f,.20f),metal);}
  }
  static void Scooter(Transform p){
   Wheel(p,.66f,.04f,.29f);Wheel(p,-.62f,.04f,.29f);
   Box(p,"Footboard",new Vector3(0,.35f,0),new Vector3(.51f,.13f,1.05f),metal);
   Box(p,"Rear rounded body",new Vector3(0,.57f,-.48f),new Vector3(.60f,.46f,.70f),paint);
   Box(p,"Saddle",new Vector3(0,.85f,-.28f),new Vector3(.49f,.14f,.76f),seat);
   Box(p,"Front leg shield",new Vector3(0,.70f,.50f),new Vector3(.53f,.68f,.19f),paint);
   Box(p,"Handlebar",new Vector3(0,1.08f,.58f),new Vector3(.85f,.08f,.10f),metal);
   Part(p,"Headlamp",PrimitiveType.Sphere,new Vector3(0,1.04f,.70f),new Vector3(.29f,.25f,.13f),white);
   Box(p,"Tail lamp",new Vector3(0,.65f,-.85f),new Vector3(.23f,.12f,.04f),red);
   Part(p,"Jelly rider torso",PrimitiveType.Capsule,new Vector3(0,1.13f,-.24f),new Vector3(.42f,.36f,.40f),skin);
   Part(p,"Helmet",PrimitiveType.Sphere,new Vector3(0,1.67f,-.12f),new Vector3(.51f,.52f,.51f),helmet);
   Part(p,"Helmet visor",PrimitiveType.Sphere,new Vector3(0,1.66f,.086f),new Vector3(.40f,.20f,.12f),glass);
   for(int s=-1;s<=1;s+=2){var arm=Part(p,"Rider arm",PrimitiveType.Capsule,new Vector3(s*.24f,1.18f,.22f),new Vector3(.13f,.28f,.13f),skin);arm.localRotation=Quaternion.Euler(65,0,s*15);Box(p,"Rider shoe",new Vector3(s*.28f,.43f,.04f),new Vector3(.19f,.16f,.32f),seat);var leg=Part(p,"Bent jelly leg",PrimitiveType.Capsule,new Vector3(s*.22f,.68f,-.03f),new Vector3(.17f,.24f,.17f),skin);leg.localRotation=Quaternion.Euler(25,0,s*12);}
  }
  static void Save(GameObject root,string name){
   var groups=new Dictionary<Material,List<CombineInstance>>();foreach(var f in root.GetComponentsInChildren<MeshFilter>()){var m=f.GetComponent<Renderer>().sharedMaterial;if(!groups.ContainsKey(m))groups[m]=new List<CombineInstance>();groups[m].Add(new CombineInstance{mesh=f.sharedMesh,transform=root.transform.worldToLocalMatrix*f.transform.localToWorldMatrix});}
   var children=new List<GameObject>();foreach(Transform t in root.transform)children.Add(t.gameObject);foreach(var child in children)Object.DestroyImmediate(child);
   foreach(var pair in groups){var mesh=new Mesh();mesh.CombineMeshes(pair.Value.ToArray());string path=Root+"/"+name+"_"+(meshIndex++)+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing){EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);mesh=existing;EditorUtility.SetDirty(mesh);}else AssetDatabase.CreateAsset(mesh,path);var g=new GameObject(pair.Key.name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(root.transform,false);g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<MeshRenderer>().sharedMaterial=pair.Key;g.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;}
   foreach(var mesh in temporary)Object.DestroyImmediate(mesh);temporary.Clear();PrefabUtility.SaveAsPrefabAsset(root,Root+"/"+name+".prefab");Object.DestroyImmediate(root);
  }
  static Mesh Rounded(Vector3 size,float radius){var vs=new List<Vector3>();var ns=new List<Vector3>();var ts=new List<int>();var h=size*.5f;float r=Mathf.Min(radius,Mathf.Min(h.x,Mathf.Min(h.y,h.z))*.95f);for(int a=0;a<3;a++)for(int sign=-1;sign<=1;sign+=2){int u=(a+1)%3,v=(a+2)%3,offset=vs.Count;for(int y=0;y<5;y++)for(int x=0;x<5;x++){var p=Vector3.zero;p[a]=h[a]*sign;p[u]=Mathf.Lerp(-h[u],h[u],x/4f);p[v]=Mathf.Lerp(-h[v],h[v],y/4f);var inner=new Vector3(Mathf.Clamp(p.x,-h.x+r,h.x-r),Mathf.Clamp(p.y,-h.y+r,h.y-r),Mathf.Clamp(p.z,-h.z+r,h.z-r));var n=(p-inner).normalized;vs.Add(inner+n*r);ns.Add(n);}for(int y=0;y<4;y++)for(int x=0;x<4;x++){int i=offset+y*5+x;if(sign>0)ts.AddRange(new[]{i,i+1,i+6,i,i+6,i+5});else ts.AddRange(new[]{i,i+6,i+1,i,i+5,i+6});}}var m=new Mesh();m.SetVertices(vs);m.SetNormals(ns);m.SetTriangles(ts,0);m.RecalculateBounds();return m;}
 }
}
