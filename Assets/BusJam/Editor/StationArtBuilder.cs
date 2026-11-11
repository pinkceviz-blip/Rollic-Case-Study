using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace Rollic.BusJam.Editor
{
    public static class StationArtBuilder
    {
        const string Art="Assets/BusJam/Art/StationArt";
        const string Res="Assets/BusJam/Resources";
        static readonly string[] Names={"CORAL","BLUE","GREEN","GOLD","VIOLET","PINK"};
        static readonly Color[] Colors={new Color(1,.19f,.32f,.89f),new Color(.06f,.58f,.98f,.89f),new Color(.12f,.78f,.43f,.89f),new Color(1,.69f,.055f,.89f),new Color(.61f,.25f,.98f,.89f),new Color(1,.35f,.67f,.89f)};
        static Mesh sphere,cube;
        static Material Mat(string name,Color color,string shader="BusJam/Station")
        {
            string path=Art+"/Materials/"+name+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!mat){mat=new Material(Shader.Find(shader));AssetDatabase.CreateAsset(mat,path);}
            mat.color=color;EditorUtility.SetDirty(mat);return mat;
        }
        static Mesh SaveMesh(Mesh mesh,string name)
        {
            mesh.name=name;string path=Art+"/Meshes/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(old){EditorUtility.CopySerialized(mesh,old);EditorUtility.SetDirty(old);UnityEngine.Object.DestroyImmediate(mesh);return old;}
            AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        static GameObject Part(Transform parent,string name,Mesh mesh,Material material,Vector3 pos,Vector3 scale)
        {
            var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=scale;g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<MeshRenderer>().sharedMaterial=material;return g;
        }
        static Mesh Primitive(PrimitiveType type,string name)
        {
            var g=GameObject.CreatePrimitive(type);var mesh=SaveMesh(UnityEngine.Object.Instantiate(g.GetComponent<MeshFilter>().sharedMesh),name);UnityEngine.Object.DestroyImmediate(g);return mesh;
        }
        [MenuItem("Bus Jam/Art/Build road and jelly passengers")]
        public static void Build()
        {
            Directory.CreateDirectory(Art+"/Meshes");Directory.CreateDirectory(Art+"/Materials");Directory.CreateDirectory(Res+"/JellyPassengers");Directory.CreateDirectory(Res+"/StationArt");
            sphere=Primitive(PrimitiveType.Sphere,"DetailSphere");cube=Primitive(PrimitiveType.Cube,"RoadCube");
            var jelly=SaveMesh(CreateJellyMesh(),"UnifiedJellyBody");var seatedMesh=SaveMesh(CreateJellyMesh(true),"SeatedJellyBody");var eye=Mat("LicoriceEyes",new Color(.025f,.035f,.075f));var glint=Mat("EyeSparkles",Color.white);var shadow=Mat("SoftContact",new Color(.05f,.07f,.12f,.24f),"BusJam/ContactShadow");
            for(int c=0;c<6;c++)
            {
                var root=new GameObject("Jelly_"+Names[c]);var visuals=new GameObject("Squishy visuals").transform;visuals.SetParent(root.transform,false);visuals.localPosition=Vector3.up*.055f;
                Part(visuals,"Unified gummy body",jelly,Mat(Names[c]+"_Jelly",Colors[c],"BusJam/Jelly"),Vector3.zero,Vector3.one);
                var face=new GameObject("Face").transform;face.SetParent(visuals,false);
                var sparkles=new GameObject("Sparkles").transform;sparkles.SetParent(visuals,false);
                for(int side=-1;side<=1;side+=2)
                {
                    Part(face,"Candy eye",sphere,eye,new Vector3(side*.095f,1.10f,-.247f),new Vector3(.075f,.095f,.040f));
                    Part(sparkles,"Eye glint",sphere,glint,new Vector3(side*.095f-.011f,1.12f,-.269f),Vector3.one*.019f);
                }
                Part(face,"Friendly smile",Smile(),eye,Vector3.zero,Vector3.one);
                Combine(face,"JellyFace",eye);Combine(sparkles,"EyeGlints",glint);
                var q=new Mesh();q.vertices=new[]{new Vector3(-.42f,0,-.32f),new Vector3(.42f,0,-.32f),new Vector3(.42f,0,.32f),new Vector3(-.42f,0,.32f)};q.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};q.triangles=new[]{0,2,1,0,3,2};q.RecalculateNormals();
                var shadowMesh=c==0?SaveMesh(q,"JellyShadow"):AssetDatabase.LoadAssetAtPath<Mesh>(Art+"/Meshes/JellyShadow.asset");if(c!=0)UnityEngine.Object.DestroyImmediate(q);
                Part(root.transform,"Grounding shadow",shadowMesh,shadow,new Vector3(0,.067f,0),Vector3.one);
                var hit=root.AddComponent<BoxCollider>();hit.center=new Vector3(0,.70f,0);hit.size=new Vector3(.91f,1.43f,.76f);
                root.AddComponent<JellyPassenger>().visuals=visuals;
                PrefabUtility.SaveAsPrefabAsset(root,Res+"/JellyPassengers/Jelly_"+Names[c]+".prefab");
                var seated=UnityEngine.Object.Instantiate(root);seated.name="SeatedJelly_"+Names[c];
                UnityEngine.Object.DestroyImmediate(seated.GetComponent<JellyPassenger>());
                foreach(var collider in seated.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(collider);
                UnityEngine.Object.DestroyImmediate(seated.transform.Find("Grounding shadow").gameObject);
                var seatedVisuals=seated.transform.Find("Squishy visuals");seatedVisuals.localPosition=Vector3.zero;
                seatedVisuals.Find("Unified gummy body").GetComponent<MeshFilter>().sharedMesh=seatedMesh;
                PrefabUtility.SaveAsPrefabAsset(seated,Res+"/JellyPassengers/SeatedJelly_"+Names[c]+".prefab");
                UnityEngine.Object.DestroyImmediate(seated);UnityEngine.Object.DestroyImmediate(root);
            }
            BuildRoad();AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            Debug.Log("STATION ART BUILT: six unified jelly prefabs; bus lane center=5.2, edge=3.8, divider=6.6. Body triangles="+jelly.triangles.Length/3);
        }
        static void Combine(Transform root,string meshName,Material material)
        {
            var list=new List<CombineInstance>();var filters=root.GetComponentsInChildren<MeshFilter>();foreach(var f in filters)list.Add(new CombineInstance{mesh=f.sharedMesh,transform=root.worldToLocalMatrix*f.transform.localToWorldMatrix});
            var m=new Mesh();m.CombineMeshes(list.ToArray(),true,true);m=SaveMesh(m,meshName);
            foreach(var f in filters)UnityEngine.Object.DestroyImmediate(f.gameObject);
            Part(root,"Combined face",m,material,Vector3.zero,Vector3.one);
        }
        static Mesh Smile()
        {
            const int rings=14,sides=6;var verts=new List<Vector3>();var normals=new List<Vector3>();var triangles=new List<int>();
            for(int i=0;i<=rings;i++){float a=Mathf.PI*i/rings;var center=new Vector3(.057f*Mathf.Cos(a),1.018f-.026f*Mathf.Sin(a),-.252f);var tangent=new Vector3(-.057f*Mathf.Sin(a),-.026f*Mathf.Cos(a),0).normalized;var bitangent=Vector3.Cross(tangent,Vector3.forward).normalized;for(int j=0;j<=sides;j++){float b=2*Mathf.PI*j/sides;var n=bitangent*Mathf.Cos(b)+Vector3.forward*Mathf.Sin(b);verts.Add(center+n*.008f);normals.Add(n);}}
            for(int i=0;i<rings;i++)for(int j=0;j<sides;j++){int a=i*(sides+1)+j,b=a+sides+1;triangles.AddRange(new[]{a,b,b+1,a,b+1,a+1});}
            var mesh=new Mesh();mesh.SetVertices(verts);mesh.SetNormals(normals);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();return mesh;
        }
        [MenuItem("Bus Jam/Art/Build station road only")]
        public static void BuildStationRoad()
        {
            Directory.CreateDirectory(Art+"/Meshes");Directory.CreateDirectory(Art+"/Materials");Directory.CreateDirectory(Res+"/StationArt");
            cube=Primitive(PrimitiveType.Cube,"RoadCube");BuildRoad();AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            Debug.Log("STATION ROAD BUILT: alternating amber/ivory curbs, stone paving, centered 1.4m boarding gap.");
        }
        static void BuildRoad()
        {
            const float boardingGapHalfWidth=.70f;
            var root=new GameObject("StationRoad");var asphalt=Mat("Asphalt",new Color(.15f,.165f,.185f),"BusJam/Asphalt");var white=Mat("RoadPaint",new Color(.89f,.89f,.84f));var concrete=Mat("Concrete",new Color(.63f,.67f,.71f));var gutter=Mat("Gutter",new Color(.29f,.33f,.36f));var metal=Mat("DrainGrates",new Color(.12f,.15f,.18f));
            var amber=Mat("CurbAmber",new Color(1,.66f,.17f));var ivory=Mat("CurbIvory",new Color(.96f,.97f,.96f));
            var paving=Mat("PavementStone",new Color(.73f,.79f,.79f));var pavingAlternate=Mat("PavementStoneAlternate",new Color(.70f,.76f,.76f));
            var nearPaving=Mat("NearPavingWarm",new Color(1,.80f,.55f),"BusJam/Cartoon");
            var nearPavingLight=Mat("NearPavingCream",new Color(1,.93f,.72f),"BusJam/Cartoon");
            var farPaving=Mat("FarPavingGreen",new Color(.41f,.76f,.26f),"BusJam/Cartoon");
            var farPavingLight=Mat("FarPavingCream",new Color(.93f,.94f,.79f),"BusJam/Cartoon");
            Part(root.transform,"Asphalt",cube,asphalt,new Vector3(0,-.025f,0),new Vector3(16,.10f,5.8f));
            // With root at Z=6.6, the near lane lies exactly halfway between 3.8 and 6.6: Z=5.2.
            foreach(int side in new[]{-1,1})
            {
                Part(root.transform,side<0?"Near edge marking":"Far edge marking",cube,white,new Vector3(0,.031f,side*2.8f),new Vector3(16,.008f,.07f));
                if(side>0)Part(root.transform,"Drainage gutter",cube,gutter,new Vector3(0,.027f,side*2.96f),new Vector3(16,.045f,.19f));
                else foreach(int bank in new[]{-1,1})
                    Part(root.transform,"Drainage gutter",cube,gutter,new Vector3(bank*(8f+boardingGapHalfWidth)*.5f,.027f,side*2.96f),new Vector3(8f-boardingGapHalfWidth,.045f,.19f));
                for(int i=-10;i<10;i++)
                {
                    float start=i*.8f,end=(i+1)*.8f;
                    if(side<0)
                    {
                        if(start<0)end=Mathf.Min(end,-boardingGapHalfWidth);
                        else start=Mathf.Max(start,boardingGapHalfWidth);
                    }
                    if(end<=start)continue;
                    float x=(start+end)*.5f;
                    Part(root.transform,"Striped curb",cube,(i%2==0?amber:ivory),new Vector3(x,.065f,side*3.08f),new Vector3(end-start-.015f,.20f,.20f));
                }
                if(side>0)
                    Part(root.transform,"Plain far sidewalk",cube,farPavingLight,new Vector3(0,-.015f,3.52f),new Vector3(16,.09f,.60f));
                else foreach(int bank in new[]{-1,1})
                    Part(root.transform,"Plain warm sidewalk",cube,nearPavingLight,new Vector3(bank*(8f+boardingGapHalfWidth)*.5f,-.015f,-3.52f),new Vector3(8f-boardingGapHalfWidth,.09f,.60f));
                for(int x=-6;x<=6;x+=4)
                {
                    Part(root.transform,"Drain frame",cube,metal,new Vector3(x,.058f,side*2.98f),new Vector3(.44f,.012f,.13f));
                    for(int slot=0;slot<6;slot++)Part(root.transform,"Grate rib",cube,concrete,new Vector3(x-.18f+slot*.07f,.066f,side*2.98f),new Vector3(.018f,.009f,.11f));
                }
            }
            for(int i=-5;i<=5;i++)Part(root.transform,"Dashed lane divider",cube,white,new Vector3(i*1.45f,.032f,0),new Vector3(.83f,.009f,.075f));
            var farMint=Mat("FarPavementMint",new Color(.41f,.76f,.26f),"BusJam/Cartoon");
            var farCream=Mat("FarPavementCream",new Color(.93f,.94f,.79f),"BusJam/Cartoon");
            var farBlue=Mat("FarPavementBlue",new Color(.48f,.81f,.31f),"BusJam/Cartoon");
            var farJoint=Mat("FarPavementJoints",new Color(.30f,.62f,.21f),"BusJam/Cartoon");
            Part(root.transform,"Far sidewalk foundation",cube,farJoint,new Vector3(0,-.035f,13.35f),new Vector3(24,.10f,19.2f));
            for(int row=0;row<24;row++)for(int col=-15;col<15;col++)
            {
                var tile=row<2||Mathf.Abs(col)<2?farCream:((col+row)%5==0?farBlue:farMint);
                Part(root.transform,"Colorful far sidewalk tile",cube,tile,new Vector3((col+.5f)*.8f,.018f,4.15f+row*.8f),new Vector3(.785f,.024f,.785f));
            }
            var parkSphere=Primitive(PrimitiveType.Sphere,"ParkFoliageSphere");
            var trunk=Mat("ParkTreeBark",new Color(.43f,.25f,.13f),"BusJam/Cartoon");
            var leaf=Mat("ParkTreeCanopy",new Color(.23f,.61f,.15f),"BusJam/Cartoon");
            var leafLight=Mat("ParkTreeLeafLight",new Color(.48f,.80f,.20f),"BusJam/Cartoon");
            var flowerPink=Mat("ParkPinkFlowers",new Color(1,.36f,.60f),"BusJam/Cartoon");
            var flowerGold=Mat("ParkGoldFlowers",new Color(1,.79f,.16f),"BusJam/Cartoon");
            for(int side=-1;side<=1;side+=2)for(int row=0;row<4;row++)
            {
                float x=side*(3.4f+(row%2)*.7f),z=5.4f+row*2.3f;
                Part(root.transform,"Park tree trunk",cube,trunk,new Vector3(x,.50f,z),new Vector3(.20f,1.0f,.20f));
                Part(root.transform,"Rounded tree canopy",parkSphere,leaf,new Vector3(x,1.38f,z),new Vector3(1.35f,1.45f,1.20f));
                Part(root.transform,"Sunlit tree canopy",parkSphere,leafLight,new Vector3(x-.30f,1.60f,z-.18f),new Vector3(.88f,1.10f,.95f));
            }
            for(int side=-1;side<=1;side+=2)
            {
                Part(root.transform,"Park flower bed",cube,farJoint,new Vector3(side*2.7f,.09f,4.45f),new Vector3(2.45f,.18f,.66f));
                for(int i=0;i<9;i++)Part(root.transform,"Colorful flower cluster",parkSphere,i%2==0?flowerPink:flowerGold,new Vector3(side*2.7f-1.05f+i*.26f,.23f,4.45f+(i%2)*.13f),Vector3.one*.24f);
                Part(root.transform,"Garden bench seat",cube,trunk,new Vector3(side*2.1f,.32f,6.3f),new Vector3(1.4f,.10f,.40f));
                Part(root.transform,"Garden bench back",cube,trunk,new Vector3(side*2.1f,.62f,6.48f),new Vector3(1.4f,.45f,.09f));
                for(int leg=-1;leg<=1;leg+=2)Part(root.transform,"Bench leg",cube,metal,new Vector3(side*2.1f+leg*.5f,.15f,6.3f),new Vector3(.08f,.30f,.30f));
            }
            // Bake by material to keep the station road inexpensive to render.
            var groups=new Dictionary<Material,List<CombineInstance>>();var filters=root.GetComponentsInChildren<MeshFilter>();foreach(var f in filters){var mat=f.GetComponent<MeshRenderer>().sharedMaterial;if(!groups.ContainsKey(mat))groups[mat]=new List<CombineInstance>();groups[mat].Add(new CombineInstance{mesh=f.sharedMesh,transform=root.transform.worldToLocalMatrix*f.transform.localToWorldMatrix});}
            foreach(var f in filters)UnityEngine.Object.DestroyImmediate(f.gameObject);
            foreach(var pair in groups){var mesh=new Mesh();mesh.CombineMeshes(pair.Value.ToArray(),true,true);mesh=SaveMesh(mesh,"Road_"+pair.Key.name);Part(root.transform,pair.Key.name,mesh,pair.Key,Vector3.zero,Vector3.one);}
            PrefabUtility.SaveAsPrefabAsset(root,Res+"/StationArt/StationRoad.prefab");UnityEngine.Object.DestroyImmediate(root);
        }
        static float Smooth(float a,float b,float k){float h=Mathf.Clamp01(.5f+.5f*(b-a)/k);return Mathf.Lerp(b,a,h)-k*h*(1-h);}
        static float Ellipsoid(Vector3 p,Vector3 center,Vector3 radii){var q=p-center;return (new Vector3(q.x/radii.x,q.y/radii.y,q.z/radii.z).magnitude-1)*Mathf.Min(radii.x,Mathf.Min(radii.y,radii.z));}
        static float Capsule(Vector3 p,Vector3 a,Vector3 b,float radius){var pa=p-a;var ba=b-a;return (pa-ba*Mathf.Clamp01(Vector3.Dot(pa,ba)/Vector3.Dot(ba,ba))).magnitude-radius;}
        static float Field(Vector3 p)
        {
            float d=Ellipsoid(p,new Vector3(0,.57f,0),new Vector3(.235f,.315f,.18f));d=Smooth(d,Ellipsoid(p,new Vector3(0,1.055f,0),new Vector3(.295f,.295f,.265f)),.11f);
            for(int s=-1;s<=1;s+=2){d=Smooth(d,Capsule(p,new Vector3(s*.21f,.69f,0),new Vector3(s*.34f,.41f,0),.105f),.095f);d=Smooth(d,Ellipsoid(p,new Vector3(s*.34f,.40f,-.008f),new Vector3(.12f,.12f,.12f)),.05f);d=Smooth(d,Capsule(p,new Vector3(s*.125f,.10f,0),new Vector3(s*.125f,.32f,0),.112f),.07f);d=Smooth(d,Ellipsoid(p,new Vector3(s*.125f,.072f,-.055f),new Vector3(.135f,.10f,.17f)),.045f);}return d;
        }
        static float SeatedField(Vector3 p)
        {
            float d=Ellipsoid(p,new Vector3(0,.64f,0),new Vector3(.235f,.28f,.18f));
            d=Smooth(d,Ellipsoid(p,new Vector3(0,1.055f,0),new Vector3(.295f,.295f,.265f)),.11f);
            d=Smooth(d,Ellipsoid(p,new Vector3(0,.40f,.02f),new Vector3(.25f,.20f,.21f)),.10f);
            for(int side=-1;side<=1;side+=2)
            {
                d=Smooth(d,Capsule(p,new Vector3(side*.125f,.41f,0),new Vector3(side*.125f,.40f,-.26f),.12f),.07f);
                d=Smooth(d,Capsule(p,new Vector3(side*.125f,.37f,-.28f),new Vector3(side*.125f,.14f,-.30f),.105f),.055f);
                d=Smooth(d,Ellipsoid(p,new Vector3(side*.125f,.10f,-.36f),new Vector3(.135f,.10f,.17f)),.05f);
                d=Smooth(d,Capsule(p,new Vector3(side*.21f,.73f,0),new Vector3(side*.31f,.49f,-.18f),.105f),.085f);
                d=Smooth(d,Ellipsoid(p,new Vector3(side*.31f,.46f,-.20f),new Vector3(.115f,.11f,.12f)),.05f);
            }
            return d;
        }
        static float Sample(Vector3 p,bool seated)=>seated?SeatedField(p):Field(p);
        static Vector3 Normal(Vector3 p,bool seated=false)
        {
            const float e=.001f;
            return new Vector3(Sample(p+Vector3.right*e,seated)-Sample(p-Vector3.right*e,seated),Sample(p+Vector3.up*e,seated)-Sample(p-Vector3.up*e,seated),Sample(p+Vector3.forward*e,seated)-Sample(p-Vector3.forward*e,seated)).normalized;
        }
        static Mesh CreateJellyMesh(bool seated=false)
        {
            const float step=.045f;var min=new Vector3(-.55f,-.08f,seated?-.61f:-.44f);int nx=25,ny=33,nz=seated?24:20;var samples=new float[nx+1,ny+1,nz+1];for(int x=0;x<=nx;x++)for(int y=0;y<=ny;y++)for(int z=0;z<=nz;z++)samples[x,y,z]=Sample(min+new Vector3(x,y,z)*step,seated);
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var triangles=new List<int>();var map=new Dictionary<Vector3Int,int>();
            int[,] corners={{0,0,0},{1,0,0},{1,1,0},{0,1,0},{0,0,1},{1,0,1},{1,1,1},{0,1,1}};int[,] tetra={{0,5,1,6},{0,1,2,6},{0,2,3,6},{0,3,7,6},{0,7,4,6},{0,4,5,6}};int[,] edges={{0,1},{0,2},{0,3},{1,2},{1,3},{2,3}};
            for(int x=0;x<nx;x++)for(int y=0;y<ny;y++)for(int z=0;z<nz;z++)
            {
                var positions=new Vector3[8];var values=new float[8];for(int c=0;c<8;c++){int xx=x+corners[c,0],yy=y+corners[c,1],zz=z+corners[c,2];positions[c]=min+new Vector3(xx,yy,zz)*step;values[c]=samples[xx,yy,zz];}
                for(int t=0;t<6;t++)
                {
                    var polygon=new List<Vector3>();for(int e=0;e<6;e++){int a=tetra[t,edges[e,0]],b=tetra[t,edges[e,1]];if((values[a]<0)==(values[b]<0))continue;polygon.Add(Vector3.Lerp(positions[a],positions[b],values[a]/(values[a]-values[b])));}
                    if(polygon.Count<3)continue;Vector3 center=Vector3.zero;foreach(var p in polygon)center+=p;center/=polygon.Count;Vector3 normal=Normal(center,seated),u=Vector3.Cross(normal,Mathf.Abs(normal.y)<.9f?Vector3.up:Vector3.right).normalized,v=Vector3.Cross(normal,u);
                    polygon.Sort((a,b)=>Mathf.Atan2(Vector3.Dot(a-center,v),Vector3.Dot(a-center,u)).CompareTo(Mathf.Atan2(Vector3.Dot(b-center,v),Vector3.Dot(b-center,u))));
                    var ids=new List<int>();foreach(var p in polygon){var key=new Vector3Int(Mathf.RoundToInt(p.x*100000),Mathf.RoundToInt(p.y*100000),Mathf.RoundToInt(p.z*100000));if(!map.TryGetValue(key,out int id)){id=vertices.Count;map[key]=id;vertices.Add(p);normals.Add(Normal(p,seated));}ids.Add(id);}for(int n=1;n<ids.Count-1;n++)triangles.AddRange(new[]{ids[0],ids[n],ids[n+1]});
                }
            }
            var mesh=new Mesh();mesh.indexFormat=IndexFormat.UInt32;mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();return mesh;
        }
    }
}
