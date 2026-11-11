using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace Rollic.BusJam.Editor
{
    public static class BusAssetBuilder
    {
        const string Root = "Assets/BusJam/Art/BusFleet";
        const string Prefabs = "Assets/BusJam/Resources/BusFleet";
        static readonly string[] Names = { "CORAL", "BLUE", "GREEN", "GOLD", "VIOLET", "PINK" };
        static readonly Color[] Colors = {
            new Color(1f,.19f,.32f), new Color(.06f,.58f,.98f), new Color(.12f,.78f,.43f),
            new Color(1f,.65f,.055f), new Color(.57f,.25f,.96f), new Color(1f,.35f,.65f)
        };
        static readonly Dictionary<string, Mesh> Meshes = new Dictionary<string, Mesh>();
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        static Material body, highlight, trim, glass, glassShine, rubber, hub, seat, seatInset, lights, red, dark;

        [MenuItem("Bus Jam/Art/Build six bus assets")]
        public static void Build()
        {
            Directory.CreateDirectory(Root + "/Meshes");
            Directory.CreateDirectory(Root + "/Materials");
            Directory.CreateDirectory(Prefabs);
            Meshes.Clear();
            trim = Material("Ivory", new Color(.94f,.97f,1));
            glass = Material("DeepTealGlass", new Color(.035f,.19f,.27f));
            glassShine = Material("WindowReflection", new Color(.26f,.64f,.75f));
            rubber = Material("TireRubber", new Color(.10f,.13f,.19f));
            hub = Material("WheelAlloy", new Color(.65f,.84f,.90f));
            seat = Material("SeatUpholstery", new Color(.57f,.65f,.86f));
            seatInset = Material("SeatCushion", new Color(.72f,.79f,.96f));
            lights = Material("WarmHeadlights", new Color(1,.93f,.64f));
            red = Material("TailLights", new Color(.89f,.07f,.15f));
            dark = Material("Undercarriage", new Color(.15f,.20f,.27f));
            var full = Material("OccupiedSeat", new Color(.31f,1,.65f));
            for (int c = 0; c < Names.Length; c++)
            {
                body = Material(Names[c] + "_Paint", Colors[c]);
                highlight = Material(Names[c] + "_UpperPaint", Color.Lerp(Colors[c], Color.white, .27f));
                var root = new GameObject("Bus_" + Names[c]);
                var model = root.AddComponent<BusModel>();
                model.emptySeatMaterial = dark;
                model.occupiedSeatMaterial = full;
                Box(root.transform,"Chassis",new Vector3(0,.40f,0),new Vector3(4.18f,.25f,1.40f),.11f,dark);
                // Split the shell around a real doorway; no solid body remains behind the moving leaves.
                foreach(int side in new[]{-1,1})
                {
                    Box(root.transform,"Coach body section",new Vector3(side*1.4375f,.86f,0),new Vector3(1.775f,.96f,1.66f),.22f,body);
                    Box(root.transform,"Lower accent section",new Vector3(side*1.4175f,.49f,0),new Vector3(1.735f,.12f,1.70f),.05f,highlight);
                }
                Box(root.transform,"Back wall behind entrance",new Vector3(0,.86f,.72f),new Vector3(1.10f,.96f,.22f),.07f,body);
                Box(root.transform,"Rear cabin floor",new Vector3(-1.375f,1.35f,0),new Vector3(1.6f,.11f,1.40f),.05f,highlight);
                Box(root.transform,"Front cabin floor",new Vector3(1.1f,1.35f,0),new Vector3(1.1f,.11f,1.40f),.05f,highlight);
                Box(root.transform,"Center cabin floor",new Vector3(0,1.35f,.35f),new Vector3(1.1f,.11f,.7f),.05f,highlight);
                // White rails frame an open roof. Seats remain readable from the gameplay camera.
                for (int side = -1; side <= 1; side += 2)
                {
                    float z = side * .835f;
                    Box(root.transform,"Upper rail",new Vector3(-.20f,1.64f,z),new Vector3(4.18f,.10f,.075f),.035f,trim);
                    if(side>0)Box(root.transform,"Window belt",new Vector3(-.20f,1.03f,z),new Vector3(4.13f,.055f,.035f),.016f,highlight);
                    else
                    {
                        Box(root.transform,"Rear window belt",new Vector3(-1.385f,1.03f,z),new Vector3(1.67f,.055f,.035f),.016f,highlight);
                        Box(root.transform,"Front window belt",new Vector3(1.21f,1.03f,z),new Vector3(1.32f,.055f,.035f),.016f,highlight);
                    }
                    for (int i = 0; i < 5; i++)
                    {
                        float x = -1.79f + i * .71f;
                        if(side<0&&Mathf.Abs(x)<.65f)continue;
                        Box(root.transform,"Side window",new Vector3(x,1.27f,z),new Vector3(.58f,.37f,.045f),.075f,glass);
                        var shine = Box(root.transform,"Glass glint",new Vector3(x-.08f,1.32f,z+side*.028f),new Vector3(.22f,.035f,.012f),.013f,glassShine);
                        shine.transform.localRotation = Quaternion.Euler(0,0,32);
                    }
                    // Reflections and finely separated painted panels give the coach a toy-model finish.
                    for(int i=0;i<5;i++)
                    {
                        float x=-1.79f+i*.71f;if(side<0&&Mathf.Abs(x)<.65f)continue;
                        Box(root.transform,"Window silver surround",new Vector3(x,1.27f,z-side*.018f),new Vector3(.625f,.415f,.033f),.075f,hub);
                        Box(root.transform,"Soft sky reflection",new Vector3(x+.065f,1.365f,z+side*.029f),new Vector3(.36f,.065f,.009f),.028f,Material("SoftSkyReflection",new Color(.36f,.74f,.85f)));
                    }
                    foreach(float center in new[]{-1.43f,1.41f})
                    {
                        Box(root.transform,"Satin lower molding",new Vector3(center,.61f,side*.849f),new Vector3(1.63f,.055f,.035f),.022f,hub);
                        Box(root.transform,"Paint highlight line",new Vector3(center,.94f,side*.844f),new Vector3(1.60f,.026f,.018f),.010f,highlight);
                    }
                    for(int vent=0;vent<4;vent++)Box(root.transform,"Rear engine cooling vent",new Vector3(-2.03f+vent*.095f,.77f,side*.843f),new Vector3(.034f,.17f,.016f),.009f,dark);
                    Box(root.transform,"Amber side marker",new Vector3(-1.93f,.57f,side*.877f),new Vector3(.13f,.055f,.035f),.02f,Material("AmberMarker",new Color(1,.58f,.08f)));
                    foreach (float x in new[] {-2.20f,1.83f})
                        Box(root.transform,"Rail support",new Vector3(x,1.50f,z),new Vector3(.075f,.29f,.075f),.03f,trim);
                    if(side<0)BuildDoors(root,model);
                    foreach (float x in new[] {-1.48f,1.48f}) Wheel(root.transform,x,side);
                }
                // A compact rounded driver cabin gives the coach a distinctive front silhouette.
                Box(root.transform,"Driver cabin",new Vector3(1.94f,1.31f,0),new Vector3(.77f,.61f,1.61f),.23f,body);
                Box(root.transform,"Driver roof cap",new Vector3(1.94f,1.65f,0),new Vector3(.83f,.10f,1.65f),.05f,trim);
                Box(root.transform,"Windshield",new Vector3(2.326f,1.32f,0),new Vector3(.045f,.40f,1.26f),.09f,glass);
                Box(root.transform,"Windshield reflection",new Vector3(2.352f,1.42f,-.18f),new Vector3(.012f,.04f,.63f),.014f,glassShine);
                Box(root.transform,"Front bumper",new Vector3(2.30f,.52f,0),new Vector3(.15f,.16f,1.42f),.065f,trim);
                Box(root.transform,"Rear bumper",new Vector3(-2.30f,.52f,0),new Vector3(.15f,.16f,1.42f),.065f,trim);
                Box(root.transform,"Front grille",new Vector3(2.345f,.77f,0),new Vector3(.035f,.18f,.55f),.045f,dark);
                for (int side=-1; side<=1; side+=2)
                {
                    Box(root.transform,"Headlamp",new Vector3(2.345f,.91f,side*.55f),new Vector3(.07f,.19f,.27f),.075f,lights);
                    Box(root.transform,"Tail lamp",new Vector3(-2.345f,.91f,side*.59f),new Vector3(.055f,.25f,.15f),.055f,red);
                    Box(root.transform,"Mirror arm",new Vector3(2.03f,1.40f,side*.91f),new Vector3(.08f,.06f,.25f),.025f,dark);
                    Box(root.transform,"Mirror",new Vector3(2.05f,1.43f,side*1.03f),new Vector3(.19f,.25f,.09f),.04f,body);
                    Box(root.transform,"Driver side glass",new Vector3(1.97f,1.37f,side*.811f),new Vector3(.42f,.30f,.035f),.065f,glass);
                }
                for(int side=-1;side<=1;side+=2)
                {
                    Box(root.transform,"Headlight chrome housing",new Vector3(2.343f,.91f,side*.55f),new Vector3(.065f,.25f,.34f),.10f,hub);
                    Box(root.transform,"LED headlight signature",new Vector3(2.388f,.96f,side*.55f),new Vector3(.025f,.036f,.21f),.014f,lights);
                    Box(root.transform,"Rear red light signature",new Vector3(-2.38f,.91f,side*.59f),new Vector3(.025f,.16f,.075f),.03f,red);
                }
                for(int vent=0;vent<5;vent++)Box(root.transform,"Front grille slat",new Vector3(2.37f,.71f+vent*.026f,0),new Vector3(.022f,.012f,.40f),.005f,hub);
                var indicators = new List<Renderer>();
                for (int i = 0; i < 3; i++)
                {
                    float x = -1.42f + i * .98f;
                    var seatRoot = new GameObject("Seat " + (i+1)).transform;seatRoot.SetParent(root.transform,false);
                    Box(seatRoot,"Seat base",new Vector3(x,1.46f,0),new Vector3(.66f,.19f,1.00f),.075f,seat);
                    Box(seatRoot,"Soft cushion",new Vector3(x+.02f,1.56f,0),new Vector3(.60f,.11f,.89f),.05f,seatInset);
                    Box(seatRoot,"Backrest",new Vector3(x-.24f,1.74f,0),new Vector3(.16f,.58f,1.00f),.075f,seat);
                    Box(seatRoot,"Backrest inset",new Vector3(x-.145f,1.77f,0),new Vector3(.035f,.38f,.77f),.016f,seatInset);
                    for (int side=-1; side<=1; side+=2)
                        Box(seatRoot,"Armrest",new Vector3(x,1.69f,side*.48f),new Vector3(.52f,.09f,.095f),.04f,trim);
                    indicators.Add(Box(seatRoot,"Seat status",new Vector3(x+.19f,1.63f,-.32f),new Vector3(.11f,.045f,.11f),.02f,dark).GetComponent<Renderer>());
                }
                model.seatIndicators=indicators.ToArray();
                var collider=root.AddComponent<BoxCollider>();collider.center=new Vector3(0,1,0);collider.size=new Vector3(4.85f,2.05f,2.15f);
                Optimize(root);
                PrefabUtility.SaveAsPrefabAsset(root,Prefabs+"/Bus_"+Names[c]+".prefab");
                UnityEngine.Object.DestroyImmediate(root);
            }
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            Debug.Log("BUS ASSETS BUILT: six open-roof coach prefabs, reusable meshes and shared materials.");
        }
        static Material Material(string name, Color color)
        {
            string path=Root+"/Materials/"+name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Resources.Load<Shader>("BusJam/Station")??Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.name=name;m.color=color;m.shader=Resources.Load<Shader>("BusJam/PolishedBus");
            bool metal=name=="WheelAlloy"||name=="Ivory";bool glassFinish=name.Contains("Glass")||name.Contains("Reflection");
            m.SetFloat("_Gloss",name=="TireRubber"?.10f:name.Contains("Seat")?.24f:glassFinish?.80f:.55f);m.SetFloat("_Metal",metal?.75f:glassFinish?.35f:0);
            EditorUtility.SetDirty(m);return m;
        }
        static GameObject Box(Transform parent,string name,Vector3 position,Vector3 size,float radius,Material material)
        {
            string key="Rounded_"+size.x.ToString("F3",Inv)+"_"+size.y.ToString("F3",Inv)+"_"+size.z.ToString("F3",Inv)+"_"+radius.ToString("F3",Inv);
            if(!Meshes.TryGetValue(key,out var mesh))
            {
                string path=Root+"/Meshes/"+key+".asset";mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                var generated=RoundedBox(size,radius);generated.name=key;if(!mesh){mesh=generated;AssetDatabase.CreateAsset(mesh,path);}else{EditorUtility.CopySerialized(generated,mesh);EditorUtility.SetDirty(mesh);UnityEngine.Object.DestroyImmediate(generated);}Meshes[key]=mesh;
            }
            return Part(parent,name,position,mesh,material);
        }
        static GameObject Part(Transform parent,string name,Vector3 pos,Mesh mesh,Material mat)
        {
            var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<MeshRenderer>().sharedMaterial=mat;return g;
        }
        static Mesh RoundedBox(Vector3 size,float requestedRadius)
        {
            Vector3 half=size*.5f;float r=Mathf.Min(requestedRadius,Mathf.Min(half.x,Mathf.Min(half.y,half.z))*.97f);
            var verts=new List<Vector3>();var norms=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            for(int axis=0;axis<3;axis++)for(int sign=-1;sign<=1;sign+=2)
            {
                int u=(axis+1)%3,v=(axis+2)%3;float[] xs=Coordinates(half[u],r),ys=Coordinates(half[v],r);int start=verts.Count;
                for(int j=0;j<ys.Length;j++)for(int i=0;i<xs.Length;i++)
                {
                    var point=Vector3.zero;point[axis]=half[axis]*sign;point[u]=xs[i];point[v]=ys[j];
                    var inner=new Vector3(Mathf.Clamp(point.x,-half.x+r,half.x-r),Mathf.Clamp(point.y,-half.y+r,half.y-r),Mathf.Clamp(point.z,-half.z+r,half.z-r));
                    Vector3 normal=(point-inner).normalized;verts.Add(inner+normal*r);norms.Add(normal);uv.Add(new Vector2((float)i/(xs.Length-1),(float)j/(ys.Length-1)));
                }
                for(int j=0;j<ys.Length-1;j++)for(int i=0;i<xs.Length-1;i++)
                {
                    int a=start+j*xs.Length+i,b=a+1,d=a+xs.Length,c=d+1;
                    if(sign>0){triangles.AddRange(new[]{a,b,c,a,c,d});}else{triangles.AddRange(new[]{a,c,b,a,d,c});}
                }
            }
            var m=new Mesh();m.SetVertices(verts);m.SetNormals(norms);m.SetUVs(0,uv);m.SetTriangles(triangles,0);m.RecalculateBounds();return m;
        }
        static float[] Coordinates(float h,float r)=>new[]{-h,-h+r*.3f,-h+r,0,h-r,h-r*.3f,h};
        static void Wheel(Transform root,float x,int side)
        {
            string key="TireTorus";
            if(!Meshes.TryGetValue(key,out var tire))
            {
                string path=Root+"/Meshes/TireTorus.asset";tire=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(!tire){tire=Torus(.285f,.105f);tire.name=key;AssetDatabase.CreateAsset(tire,path);}Meshes[key]=tire;
            }
            var wheel=new GameObject("Wheel assembly").transform;wheel.SetParent(root,false);wheel.localPosition=new Vector3(x,.39f,side*.82f);
            Part(wheel,"Rounded tire",Vector3.zero,tire,rubber);
            // Rounded hub disks use a shallow beveled box with a circular silhouette supplied by the cylinder mesh.
            Disk(wheel,"Alloy hub",side*.095f,.24f,.07f,hub);
            Disk(wheel,"Ivory hub inset",side*.14f,.15f,.035f,trim);
            Disk(wheel,"Painted hub cap",side*.165f,.075f,.025f,body);
            for(int bolt=0;bolt<6;bolt++)
            {
                float a=bolt*Mathf.PI/3;var pivot=new GameObject("Alloy wheel bolt").transform;pivot.SetParent(wheel,false);pivot.localPosition=new Vector3(Mathf.Cos(a)*.112f,Mathf.Sin(a)*.112f,0);
                Disk(pivot,"Chrome lug nut",side*.165f,.018f,.022f,hub);
            }
        }
        static void Disk(Transform parent,string name,float z,float radius,float depth,Material mat)
        {
            string key="Disk_"+radius.ToString("F3",Inv)+"_"+depth.ToString("F3",Inv);
            if(!Meshes.TryGetValue(key,out var mesh)){string path=Root+"/Meshes/"+key+".asset";mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(!mesh){mesh=Cylinder(radius,depth);mesh.name=key;AssetDatabase.CreateAsset(mesh,path);}Meshes[key]=mesh;}
            Part(parent,name,new Vector3(0,0,z),mesh,mat);
        }
        static Mesh Cylinder(float radius,float depth)
        {
            var v=new List<Vector3>();var n=new List<Vector3>();var t=new List<int>();int steps=32;
            for(int face=-1;face<=1;face+=2){int start=v.Count;v.Add(new Vector3(0,0,face*depth*.5f));n.Add(Vector3.forward*face);for(int i=0;i<=steps;i++){float a=2*Mathf.PI*i/steps;v.Add(new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,face*depth*.5f));n.Add(Vector3.forward*face);}for(int i=0;i<steps;i++){if(face>0)t.AddRange(new[]{start,start+i+1,start+i+2});else t.AddRange(new[]{start,start+i+2,start+i+1});}}
            int offset=v.Count;for(int i=0;i<=steps;i++){float a=2*Mathf.PI*i/steps;var normal=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);v.Add(normal*radius-Vector3.forward*depth*.5f);v.Add(normal*radius+Vector3.forward*depth*.5f);n.Add(normal);n.Add(normal);}for(int i=0;i<steps;i++){int a=offset+i*2;t.AddRange(new[]{a,a+2,a+3,a,a+3,a+1});}
            var m=new Mesh();m.SetVertices(v);m.SetNormals(n);m.SetTriangles(t,0);m.RecalculateBounds();return m;
        }
        static Mesh Torus(float major,float minor)
        {
            var v=new List<Vector3>();var n=new List<Vector3>();var t=new List<int>();const int rings=32,sides=10;
            for(int i=0;i<=rings;i++)for(int j=0;j<=sides;j++){float a=2*Mathf.PI*i/rings,b=2*Mathf.PI*j/sides;var radial=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);var normal=radial*Mathf.Cos(b)+Vector3.forward*Mathf.Sin(b);v.Add(radial*major+normal*minor);n.Add(normal);}
            for(int i=0;i<rings;i++)for(int j=0;j<sides;j++){int a=i*(sides+1)+j,b=a+sides+1;t.AddRange(new[]{a,b,b+1,a,b+1,a+1});}
            var m=new Mesh();m.SetVertices(v);m.SetNormals(n);m.SetTriangles(t,0);m.RecalculateBounds();return m;
        }
        static void BuildDoors(GameObject root,BusModel model)
        {
            model.doorRoot=new GameObject("Sliding door mechanism").transform;
            model.doorRoot.SetParent(root.transform,false);
            model.doorRoot.localPosition=new Vector3(0,0,-.89f);
            for(int side=-1;side<=1;side+=2)
            {
                var leaf=new GameObject(side<0?"Left sliding leaf":"Right sliding leaf").transform;
                leaf.SetParent(model.doorRoot,false);leaf.localPosition=new Vector3(side*.275f,0,0);
                Box(leaf,"Door paint",new Vector3(0,.76f,0),new Vector3(.54f,.70f,.055f),.04f,highlight);
                Box(leaf,"Door window",new Vector3(0,1.25f,0),new Vector3(.48f,.26f,.055f),.045f,Material("DoorGlass",new Color(.30f,.72f,.82f)));
                Box(leaf,"Door glass reflection",new Vector3(-.05f,1.29f,-.033f),new Vector3(.22f,.025f,.012f),.01f,glassShine);
                Box(leaf,"Door handle",new Vector3(-side*.17f,.96f,-.045f),new Vector3(.025f,.16f,.035f),.012f,trim);
                if(side<0){model.leftDoor=leaf;model.leftDoorClosed=leaf.localPosition;}
                else{model.rightDoor=leaf;model.rightDoorClosed=leaf.localPosition;}
                Box(root.transform,"Door jamb",new Vector3(side*.595f,.93f,-.88f),new Vector3(.07f,1.06f,.08f),.025f,trim);
            }
            Box(root.transform,"Door header",new Vector3(0,1.455f,-.88f),new Vector3(1.26f,.09f,.08f),.035f,trim);
            Box(root.transform,"Boarding step",new Vector3(0,.35f,-.95f),new Vector3(1.20f,.12f,.30f),.045f,trim);
            model.boardingPoint=new GameObject("Boarding entry aligned X0").transform;
            model.boardingPoint.SetParent(root.transform,false);model.boardingPoint.localPosition=new Vector3(0,.40f,-1.04f);
            model.interiorPoint=new GameObject("Boarding interior").transform;
            model.interiorPoint.SetParent(root.transform,false);model.interiorPoint.localPosition=new Vector3(0,.50f,-.46f);
            model.SetDoorOpening(0);
        }
        static void Optimize(GameObject root)
        {
            var groups=new Dictionary<Material,List<CombineInstance>>();
            var model=root.GetComponent<BusModel>();
            var filters=root.GetComponentsInChildren<MeshFilter>();
            foreach(var f in filters)
            {
                if(f.name=="Seat status"||(model.doorRoot&&f.transform.IsChildOf(model.doorRoot)))continue;
                var mat=f.GetComponent<MeshRenderer>().sharedMaterial;
                if(!groups.ContainsKey(mat))groups[mat]=new List<CombineInstance>();
                groups[mat].Add(new CombineInstance {mesh=f.sharedMesh,transform=root.transform.worldToLocalMatrix*f.transform.localToWorldMatrix});
            }
            var source=new GameObject("Editable source - enable this and disable Render geometry to edit").transform;source.SetParent(root.transform,false);
            var indicators=new GameObject("Seat indicators").transform;indicators.SetParent(root.transform,false);
            foreach(var r in root.GetComponent<BusModel>().seatIndicators)r.transform.SetParent(indicators,true);
            var children=new List<Transform>();foreach(Transform child in root.transform)if(child!=source&&child!=indicators&&child!=model.doorRoot&&child!=model.boardingPoint&&child!=model.interiorPoint)children.Add(child);
            foreach(var child in children)child.SetParent(source,true);
            source.gameObject.SetActive(false);
            var render=new GameObject("Render geometry").transform;render.SetParent(root.transform,false);
            foreach(var pair in groups)
            {
                var mesh=new Mesh {name=root.name+"_"+pair.Key.name,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(pair.Value.ToArray(),true,true);mesh.RecalculateBounds();
                string path=Root+"/Meshes/"+mesh.name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(existing){EditorUtility.CopySerialized(mesh,existing);EditorUtility.SetDirty(existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;}else AssetDatabase.CreateAsset(mesh,path);
                Part(render,pair.Key.name,Vector3.zero,mesh,pair.Key);
            }
        }
        public static void BuildAndExport()
        {
            Build();
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../Builds/BusFleetPreviews"));
            Directory.CreateDirectory(output);
            foreach(string name in Names)
            {
                var root=PrefabUtility.LoadPrefabContents(Prefabs+"/Bus_"+name+".prefab");
                ExportObj(root,output,"Bus_"+name);
                int triangles=0;foreach(var f in root.GetComponentsInChildren<MeshFilter>())triangles+=f.sharedMesh.triangles.Length/3;
                if(root.GetComponent<BusModel>().seatIndicators.Length!=3)throw new Exception("Expected three seats");
                Debug.Log("BUS MODEL VERIFIED "+name+": "+triangles+" triangles, 3 seat indicators.");
                PrefabUtility.UnloadPrefabContents(root);
            }
            AssetDatabase.ExportPackage(new[]{Root,Prefabs,"Assets/BusJam/Runtime/BusModel.cs","Assets/BusJam/Resources/BusJam/Station.shader"},output+"/BusFleet.unitypackage",ExportPackageOptions.Recurse);
            Preview(output);
        }
        static void ExportObj(GameObject root,string output,string name)
        {
            var obj=new StringBuilder("# Bus Jam open-top coach; meters; Y up; front +X\nmtllib "+name+".mtl\n");
            var mtl=new StringBuilder();var used=new HashSet<string>();int offset=1;
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                var mesh=filter.sharedMesh;var renderer=filter.GetComponent<MeshRenderer>();var material=renderer.sharedMaterial;string mat=material.name.Replace(' ','_');
                if(used.Add(mat)){Color c=material.color;mtl.AppendLine("newmtl "+mat);mtl.AppendLine("Kd "+Number(c.r)+" "+Number(c.g)+" "+Number(c.b));mtl.AppendLine("Ks 0.18 0.18 0.18\nNs 35\n");}
                obj.AppendLine("g "+filter.name.Replace(' ','_'));obj.AppendLine("usemtl "+mat);
                Matrix4x4 matrix=root.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
                foreach(var vertex in mesh.vertices){var v=matrix.MultiplyPoint3x4(vertex);obj.AppendLine("v "+Number(v.x)+" "+Number(v.y)+" "+Number(v.z));}
                foreach(var normal in mesh.normals){var n=matrix.inverse.transpose.MultiplyVector(normal).normalized;obj.AppendLine("vn "+Number(n.x)+" "+Number(n.y)+" "+Number(n.z));}
                var indices=mesh.triangles;for(int i=0;i<indices.Length;i+=3)obj.AppendLine("f "+Index(indices[i]+offset)+" "+Index(indices[i+1]+offset)+" "+Index(indices[i+2]+offset));offset+=mesh.vertexCount;
            }
            File.WriteAllText(output+"/"+name+".obj",obj.ToString());File.WriteAllText(output+"/"+name+".mtl",mtl.ToString());
        }
        static string Number(float value)=>value.ToString("0.######",Inv);
        static string Index(int index)=>index+"//"+index;
        static void Preview(string output)
        {
            var scene=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);
            var cam=new GameObject("Asset preview camera").AddComponent<Camera>();cam.transform.position=new Vector3(6,5.4f,-7.4f);cam.transform.LookAt(new Vector3(0,.95f,0));cam.orthographic=true;cam.orthographicSize=2.80f;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.92f,.95f,.99f);
            ShaderUtil.allowAsyncCompilation=false;
            var sheet=new Texture2D(2400,1200,TextureFormat.RGB24,false);
            var shadow=GameObject.CreatePrimitive(PrimitiveType.Cylinder);shadow.name="Studio contact shadow";shadow.transform.position=new Vector3(0,.015f,0);shadow.transform.localScale=new Vector3(4.9f,.008f,1.95f);shadow.GetComponent<Renderer>().sharedMaterial=Material("PreviewShadow",new Color(.78f,.83f,.90f));
            var canvas=new GameObject("Preview labels",typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler));var cv=canvas.GetComponent<Canvas>();cv.renderMode=RenderMode.ScreenSpaceCamera;cv.worldCamera=cam;cv.planeDistance=1;
            var scaler=canvas.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(800,600);
            var title=PreviewText(canvas.transform,"OPEN-TOP CITY COACH",21,new Vector2(400,-42),new Vector2(740,45));title.color=new Color(.35f,.43f,.55f);
            var caption=PreviewText(canvas.transform,"",31,new Vector2(400,-535),new Vector2(740,60));
            var names=new[]{"01 / CORAL RED","02 / SKY BLUE","03 / MINT GREEN","04 / SUNSHINE GOLD","05 / ELECTRIC VIOLET","06 / CANDY PINK"};
            for(int i=0;i<Names.Length;i++)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"/Bus_"+Names[i]+".prefab");var model=(GameObject)PrefabUtility.InstantiatePrefab(prefab);caption.text=names[i];caption.color=Colors[i]*.75f;caption.color=new Color(caption.color.r,caption.color.g,caption.color.b,1);
                var rt=new RenderTexture(800,600,24);rt.antiAliasing=4;rt.Create();cam.targetTexture=rt;cam.aspect=4f/3;Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=rt;
                var image=new Texture2D(800,600,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,800,600),0,0);image.Apply();File.WriteAllBytes(output+"/Bus_"+Names[i]+".png",image.EncodeToPNG());sheet.SetPixels((i%3)*800,(1-i/3)*600,800,600,image.GetPixels());
                RenderTexture.active=null;cam.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(model);
            }
            sheet.Apply();File.WriteAllBytes(output+"/BusFleet-Preview.png",sheet.EncodeToPNG());UnityEngine.Object.DestroyImmediate(sheet);Debug.Log("BUS FLEET PREVIEW EXPORTED");
        }
        static UnityEngine.UI.Text PreviewText(Transform parent,string text,int fontSize,Vector2 position,Vector2 dimensions)
        {
            var go=new GameObject("Caption",typeof(RectTransform),typeof(UnityEngine.UI.Text));go.transform.SetParent(parent,false);var rect=go.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.anchoredPosition=position;rect.sizeDelta=dimensions;var t=go.GetComponent<UnityEngine.UI.Text>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=fontSize;t.text=text;t.alignment=TextAnchor.MiddleCenter;return t;
        }
    }
}
