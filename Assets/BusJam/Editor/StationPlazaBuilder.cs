using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace Rollic.BusJam.Editor
{
 public static class StationPlazaBuilder
 {
  const string Art="Assets/BusJam/Art/StationPlaza",Res="Assets/BusJam/Resources/StationArt";
  static Mesh cube;
  [MenuItem("Bus Jam/Art/Build boarding plaza")]
  public static void Build()
  {
   Directory.CreateDirectory(Art);Directory.CreateDirectory(Res);AssetDatabase.Refresh();
   var primitive=GameObject.CreatePrimitive(PrimitiveType.Cube);cube=Save(Object.Instantiate(primitive.GetComponent<MeshFilter>().sharedMesh),"PlazaCube");Object.DestroyImmediate(primitive);
   var foundation=Mat("Deep teal foundation",new Color(.06f,.27f,.58f));var rim=Mat("Ivory plaza edging",new Color(1,.85f,.40f));var grout=Mat("Paving joints",new Color(.68f,.48f,.38f));
   var stoneA=Mat("Warm paving A",new Color(1,.80f,.55f));var stoneB=Mat("Warm paving B",new Color(1,.93f,.72f));var stoneC=Mat("Warm paving C",new Color(.93f,.67f,.54f));var teal=Mat("Teal wayfinding",new Color(.04f,.49f,.80f));var gold=Mat("Golden wayfinding",new Color(1,.70f,.20f));
   var root=new GameObject("StationPlaza");
   Part(root.transform,"Raised station foundation",cube,foundation,new Vector3(0,-.245f,-1.54f),new Vector3(8.4f,.38f,9.92f));
   Part(root.transform,"Paving bed",cube,grout,new Vector3(0,-.065f,-1.54f),new Vector3(8.16f,.045f,9.72f));
   Part(root.transform,"Smooth station floor",cube,stoneB,new Vector3(0,-.037f,-1.54f),new Vector3(8.16f,.024f,9.72f));
   const float waitingBack=.9f+2*1.04f-.76f/2;
   const float inlayBack=waitingBack+.06f; // Meet the raised slot rim in the tilted camera view; avoid an antialiasing gap.
   for(int side=-1;side<=1;side+=2)
   {
    Part(root.transform,"Ivory side edge",cube,rim,new Vector3(side*4.13f,-.035f,(waitingBack-6.5f)/2),new Vector3(.12f,.07f,waitingBack+6.5f));
    Part(root.transform,"Teal side inlay",cube,teal,new Vector3(side*3.98f,-.017f,(inlayBack-6.385f)/2),new Vector3(.045f,.012f,inlayBack+6.385f));
   }
   Part(root.transform,"Ivory lower edge",cube,rim,new Vector3(0,-.035f,-6.44f),new Vector3(8.25f,.07f,.12f));
   Part(root.transform,"Lower teal inlay",cube,teal,new Vector3(0,-.015f,-6.29f),new Vector3(7.96f,.012f,.045f));
   Part(root.transform,"Boarding lane inset",cube,Mat("Pale aqua entry",new Color(1,.93f,.72f)),new Vector3(0,-.012f,2.38f),new Vector3(1.25f,.018f,2.04f));
   Part(root.transform,"Flat boarding bridge",cube,Mat("Pale aqua entry",new Color(1,.93f,.72f)),new Vector3(0,-.022f,3.55f),new Vector3(1.25f,.04f,.50f));
   for(int side=-1;side<=1;side+=2)Part(root.transform,"Boarding lane boundary",cube,teal,new Vector3(side*.64f,-.001f,2.38f),new Vector3(.035f,.009f,2.04f));
   var arrow=new Mesh();arrow.vertices=new[]{new Vector3(-.18f,0,-.09f),new Vector3(0,0,.12f),new Vector3(.18f,0,-.09f),new Vector3(.12f,0,-.15f),new Vector3(0,0,-.01f),new Vector3(-.12f,0,-.15f)};arrow.triangles=new[]{0,1,4,1,2,4,0,4,5,2,3,4};arrow.RecalculateNormals();arrow.RecalculateBounds();arrow=Save(arrow,"BoardingChevron");
   for(int i=0;i<3;i++)Part(root.transform,"Boarding direction chevron",arrow,gold,new Vector3(0,.004f,2.42f+i*.28f),Vector3.one);
   Bake(root);PrefabUtility.SaveAsPrefabAsset(root,Res+"/StationPlaza.prefab");Object.DestroyImmediate(root);
   var outside=Mat("Slot bevel teal",new Color(.10f,.42f,.73f));var face=Mat("Slot warm ivory",new Color(1,.98f,.85f));
   var bevel=Save(Rounded(new Vector3(.76f,.12f,.76f),.058f),"SquareSlotBevel");var top=Save(Rounded(new Vector3(.65f,.055f,.65f),.026f),"SquareSlotInset");
   for(int waiting=0;waiting<2;waiting++)
   {
    var slot=new GameObject(waiting==0?"PassengerSlot":"WaitingSlot");Part(slot.transform,"Square teal rim",bevel,outside,Vector3.zero,Vector3.one);Part(slot.transform,"Ivory standing inset",top,face,new Vector3(0,.052f,0),Vector3.one);
    if(waiting==1)Part(slot.transform,"Waiting bay gold marker",cube,gold,new Vector3(0,.083f,.265f),new Vector3(.46f,.007f,.026f));
    Bake(slot);PrefabUtility.SaveAsPrefabAsset(slot,Res+"/"+slot.name+".prefab");Object.DestroyImmediate(slot);
   }
   AssetDatabase.SaveAssets();AssetDatabase.Refresh();Debug.Log("BOARDING PLAZA BUILT: stone paving, teal/ivory border, open central wayfinding lane, uniform .76m square passenger and waiting slots.");
  }
  static Material Mat(string name,Color color){string path=Art+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Resources.Load<Shader>("BusJam/Cartoon"));AssetDatabase.CreateAsset(m,path);}m.shader=Resources.Load<Shader>("BusJam/Cartoon");m.color=color;m.SetFloat("_Gloss",.16f);EditorUtility.SetDirty(m);return m;}
  static Mesh Save(Mesh mesh,string name){mesh.name=name;string path=Art+"/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(mesh,old);EditorUtility.SetDirty(old);Object.DestroyImmediate(mesh);return old;}AssetDatabase.CreateAsset(mesh,path);return mesh;}
  static void Part(Transform parent,string name,Mesh mesh,Material material,Vector3 pos,Vector3 scale){var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=scale;g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<MeshRenderer>().sharedMaterial=material;g.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;}
  static void Bake(GameObject root){var groups=new Dictionary<Material,List<CombineInstance>>();var filters=root.GetComponentsInChildren<MeshFilter>();foreach(var f in filters){var mat=f.GetComponent<Renderer>().sharedMaterial;if(!groups.ContainsKey(mat))groups[mat]=new List<CombineInstance>();groups[mat].Add(new CombineInstance{mesh=f.sharedMesh,transform=root.transform.worldToLocalMatrix*f.transform.localToWorldMatrix});}foreach(var f in filters)Object.DestroyImmediate(f.gameObject);foreach(var pair in groups){var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(pair.Value.ToArray());mesh=Save(mesh,root.name+"_"+pair.Key.name);Part(root.transform,pair.Key.name,mesh,pair.Key,Vector3.zero,Vector3.one);}}
  static Mesh Rounded(Vector3 size,float radius){var vertices=new List<Vector3>();var normals=new List<Vector3>();var triangles=new List<int>();var h=size*.5f;float r=Mathf.Min(radius,Mathf.Min(h.x,Mathf.Min(h.y,h.z))*.97f);for(int axis=0;axis<3;axis++)for(int sign=-1;sign<=1;sign+=2){int u=(axis+1)%3,v=(axis+2)%3,offset=vertices.Count;float[] xs={-h[u],-h[u]+r,0,h[u]-r,h[u]};float[] ys={-h[v],-h[v]+r,0,h[v]-r,h[v]};for(int y=0;y<5;y++)for(int x=0;x<5;x++){var p=Vector3.zero;p[axis]=h[axis]*sign;p[u]=xs[x];p[v]=ys[y];var q=new Vector3(Mathf.Clamp(p.x,-h.x+r,h.x-r),Mathf.Clamp(p.y,-h.y+r,h.y-r),Mathf.Clamp(p.z,-h.z+r,h.z-r));var n=(p-q).normalized;vertices.Add(q+n*r);normals.Add(n);}for(int y=0;y<4;y++)for(int x=0;x<4;x++){int a=offset+y*5+x,b=a+1,c=a+6,d=a+5;if(sign>0)triangles.AddRange(new[]{a,b,c,a,c,d});else triangles.AddRange(new[]{a,c,b,a,d,c});}}var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();return mesh;}
 }
}
