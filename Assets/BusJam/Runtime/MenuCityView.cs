using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
namespace Rollic.BusJam
{
    /// <summary>Isolated, low-cost 3D city backdrop for the home menu.</summary>
    public sealed class MenuCityView : MonoBehaviour
    {
        const int Layer=30;
        static MenuCityView cached;
        RectTransform owner;
        bool obscured,forceRender=true;
        int renderedFrames;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetCache(){cached=null;}
        public static MenuCityView Acquire(RectTransform parent)
        {
            if(!cached)cached=new GameObject("Cached menu city").AddComponent<MenuCityView>();
            cached.gameObject.SetActive(true);cached.Initialize(parent);return cached;
        }
        public void Release(RectTransform parent)
        {
            if(!ReferenceEquals(owner,parent))return;
            owner=null;display=null;gameObject.SetActive(false);
        }
        public void SetObscured(bool value){obscured=value;forceRender=true;}

        readonly Vector3 origin=new Vector3(100,0,0);
        readonly List<Material> materials=new List<Material>();
        readonly List<Mesh> meshes=new List<Mesh>();
        readonly Dictionary<string,Material> palette=new Dictionary<string,Material>();
        readonly List<Transform> exhaust=new List<Transform>();
        Transform world,statics,hero;
        const float ChunkLength=24f, StreetSpeed=3.2f, RecycleStart=-48f;
        const int ChunkCount=6;
        readonly List<Transform> streetChunks=new List<Transform>();
        float travel;
        sealed class TrafficVehicle { public Transform root; public float speed; }
        readonly List<TrafficVehicle> traffic=new List<TrafficVehicle>();
        readonly System.Random trafficRandom=new System.Random(842);
        float nextTraffic=1.2f;
        int trafficSequence;
        const float HeroLane=1.65f,OppositeLane=-1.65f;
        public float TravelDistance=>travel;
        Camera preview;
        RawImage display;
        RenderTexture texture;
        Material paint,upper;
        int screenWidth,screenHeight;Vector2 viewportSize;
        int selected;
        public int SelectedColor=>selected;
        public static readonly Color[] Colors={new Color(1,.25f,.35f),new Color(.035f,.73f,.86f),new Color(.20f,.80f,.39f),new Color(1,.72f,.08f),new Color(.61f,.32f,.98f),new Color(1,.39f,.68f)};
        public void Initialize(RectTransform parent)
        {
            owner=parent;obscured=false;forceRender=true;
            var g=new GameObject("City viewport",typeof(RectTransform),typeof(RawImage));g.transform.SetParent(parent,false);var r=g.GetComponent<RectTransform>();r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;display=g.GetComponent<RawImage>();display.raycastTarget=false;
            if(world)
            {
                world.gameObject.SetActive(true);if(texture)display.texture=texture;
                Canvas.ForceUpdateCanvases();Resize();Render();return;
            }
            world=new GameObject("Menu city - isolated layer").transform;world.position=origin;
            statics=new GameObject("Static city").transform;statics.SetParent(world,false);
            BuildCity();BuildHero();BuildTraffic();BakeGroup(statics);ScrollEnvironment(0);
            preview=new GameObject("Menu city camera").AddComponent<Camera>();preview.transform.SetParent(world,false);preview.transform.localPosition=new Vector3(HeroLane,6,-18);preview.transform.LookAt(origin+new Vector3(HeroLane,2,8));preview.fieldOfView=37;preview.clearFlags=CameraClearFlags.SolidColor;preview.backgroundColor=new Color(.08f,.72f,.91f);preview.cullingMask=1<<Layer;preview.nearClipPlane=.1f;preview.farClipPlane=150;preview.enabled=false;preview.allowHDR=false;preview.allowMSAA=false;
            SetLayer(world.gameObject);
            selected=Mathf.Clamp(PlayerPrefs.GetInt("Rollic.BusJam.MenuBusColor",1),0,5);SetColor(selected,false);
            Canvas.ForceUpdateCanvases();Resize();Render();
        }
        Material Mat(string name,Color color)
        {
            if(palette.TryGetValue(name,out var found))return found;
            var m=new Material(Resources.Load<Shader>("MenuArt/MenuStreet")??Resources.Load<Shader>("BusJam/Station")??Shader.Find("Universal Render Pipeline/Lit"));m.name=name;m.color=color;m.enableInstancing=true;palette[name]=m;materials.Add(m);return m;
        }
        Transform Part(string name,PrimitiveType type,Vector3 position,Vector3 size,Material material,Transform parent=null)
        {
            var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent?parent:statics,false);g.transform.localPosition=position;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=material;g.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;g.GetComponent<Renderer>().receiveShadows=false;Destroy(g.GetComponent<Collider>());return g.transform;
        }
        Transform Box(string n,Vector3 p,Vector3 s,Material m,Transform parent=null)=>Part(n,PrimitiveType.Cube,p,s,m,parent);
        static float RoadY(float z)=>0f;
        void BuildCity()
        {
            var backdrop=statics;
            var sky=new Material(Resources.Load<Shader>("MenuArt/MenuSky"));materials.Add(sky);
            Part("Gradient sky",PrimitiveType.Quad,new Vector3(0,24,75),new Vector3(160,90,1),sky);
            Box("City ground",new Vector3(0,-.18f,40),new Vector3(200,.3f,240),Mat("City ground",new Color(.57f,.68f,.67f)));
            BuildHorizonSkyline();
            for(int module=0;module<2;module++)
            {
                var chunk=new GameObject("Street chunk "+module).transform;chunk.SetParent(world,false);statics=chunk;
                BuildStreetChunk(module);BakeGroup(chunk);streetChunks.Add(chunk);
            }
            for(int i=2;i<ChunkCount;i++)
            {
                var chunk=new GameObject("Street chunk "+i).transform;chunk.SetParent(world,false);
                foreach(var filter in streetChunks[i%2].GetComponentsInChildren<MeshFilter>())
                {
                    var g=new GameObject(filter.name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(chunk,false);g.GetComponent<MeshFilter>().sharedMesh=filter.sharedMesh;g.GetComponent<MeshRenderer>().sharedMaterial=filter.GetComponent<MeshRenderer>().sharedMaterial;g.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                }
                streetChunks.Add(chunk);
            }
            statics=backdrop;
        }
        void BuildHorizonSkyline()
        {
            // A stationary city closes the vanishing point, behind the moving street.
            var shader=Resources.Load<Shader>("MenuArt/MenuSkyline");
            Material Silhouette(string name,Color color)
            {
                var material=new Material(shader){name=name,color=color};materials.Add(material);return material;
            }
            var far=Silhouette("Horizon far silhouettes",new Color(.43f,.60f,.65f));
            var near=Silhouette("Horizon city silhouettes",new Color(.32f,.49f,.57f));
            var windows=Silhouette("Horizon soft windows",new Color(.55f,.69f,.72f));
            var random=new System.Random(39);
            for(int layer=0;layer<2;layer++)
            {
                float z=layer==0?112:98;
                for(int i=0;i<29;i++)
                {
                    float x=(i-14)*3.6f,w=2.8f+(float)random.NextDouble()*.9f;
                    float h=5+(float)random.NextDouble()*7;
                    if(Mathf.Abs(x)<13)h+=2;
                    h*=.58f;
                    var material=layer==0?far:near;
                    Box("Horizon tower",new Vector3(x,h*.5f-.35f,z),new Vector3(w,h,2.4f),material);
                    if(i%4==0)Box("Horizon rooftop",new Vector3(x,h+.10f,z),new Vector3(w*.58f,.9f,1.8f),material);
                    if(i%7==0)Box("Horizon antenna",new Vector3(x,h+1.0f,z),new Vector3(.12f,1.6f,.12f),material);
                    if(layer==1&&i%2==0)
                        for(int floor=2;floor<h-1;floor+=2)
                            for(int col=-1;col<=1;col++)Box("Horizon window",new Vector3(x+col*.68f,floor,z-1.22f),new Vector3(.22f,.36f,.025f),windows);
                }
            }
        }
        void BuildStreetChunk(int module)
        {
            var asphalt=Mat("Street asphalt",new Color(.27f,.30f,.36f));var pavement=Mat("Sidewalk stone",new Color(.73f,.79f,.79f));var ivory=Mat("Warm ivory",new Color(.96f,.97f,.96f));var amber=Mat("Safety yellow",new Color(1,.66f,.17f));
            Box("Straight asphalt",new Vector3(0,.01f,ChunkLength*.5f),new Vector3(6.85f,.035f,ChunkLength),asphalt);
            for(float z=1.2f;z<ChunkLength;z+=2.4f)Box("Moving lane stripe",new Vector3(0,.04f,z),new Vector3(.10f,.025f,.95f),ivory);
            var pole=Surface("Streetlamp steel",new Color(.24f,.29f,.31f),.2f);var bulb=Mat("Streetlamp light",new Color(1,.94f,.72f));bulb.SetFloat("_LampGlow",1);
            var lampHalo=new Material(Resources.Load<Shader>("MenuArt/MenuLampGlow"));lampHalo.name="Warm lamp halos";materials.Add(lampHalo);
            for(int side=-1;side<=1;side+=2)
            {
                Box("Flat sidewalk",new Vector3(side*4.54f,.04f,ChunkLength*.5f),new Vector3(1.6f,.10f,ChunkLength),pavement);
                for(int i=0;i<20;i++)Box("Straight curb",new Vector3(side*3.58f,.09f,(i+.5f)*1.2f),new Vector3(.28f,.18f,1.18f),i%2==0?amber:ivory);
                for(int i=0;i<4;i++)
                {
                    int variation=(i+module+(side>0?1:0))%4;
                    float z=3+i*6,x=side*(6.8f+(variation%2)*.35f);
                    DetailedBuilding(side,x,z,variation);
                    NaturalTree(side,6+i*6,100+module*37+i*13+(side>0?9:0));
                }
                for(float z=1;z<ChunkLength;z+=12)
                {
                    Part("Streetlamp pole",PrimitiveType.Cylinder,new Vector3(side*3.95f,1.45f,z),new Vector3(.075f,1.45f,.075f),pole);
                    Box("Lamp arm",new Vector3(side*3.70f,2.88f,z),new Vector3(.55f,.07f,.07f),pole);Box("Lamp head",new Vector3(side*3.47f,2.83f,z),new Vector3(.30f,.13f,.27f),bulb);
                    Part("Warm lamp halo",PrimitiveType.Quad,new Vector3(side*3.47f,2.83f,z-.15f),Vector3.one*.85f,lampHalo);
                }
            }
        }
        Material Surface(string name,Color color,float gloss=0,float detail=0)
        {
            var material=Mat(name,color);material.SetFloat("_Gloss",gloss);material.SetFloat("_SurfaceDetail",detail);return material;
        }
        void DetailedBuilding(int side,float x,float z,int variation)
        {
            float h=new[]{4.1f,5.3f,4.65f,5.9f}[variation];
            Color[] tones={new Color(.65f,.43f,.32f),new Color(.76f,.70f,.58f),new Color(.67f,.69f,.65f),new Color(.76f,.57f,.43f)};
            var wall=Surface("Masonry facade "+variation,tones[variation],.08f,1);
            var stone=Surface("Sandstone window surrounds",new Color(.83f,.81f,.73f),.06f);
            var dark=Surface("Window recesses",new Color(.12f,.18f,.20f));
            var glass=Surface("Reflective city glass",new Color(.27f,.43f,.49f),.7f);
            var rail=Surface("Balcony iron",new Color(.23f,.27f,.27f),.25f);
            var roof=Surface("Slate and flashing",new Color(.28f,.31f,.30f),.15f);
            Box("Masonry apartment",new Vector3(x,h*.5f,z),new Vector3(3.3f,h,3.8f),wall);
            Box("Foundation plinth",new Vector3(x,.2f,z),new Vector3(3.38f,.32f,3.86f),roof);
            Box("Roof cornice",new Vector3(x,h-.05f,z),new Vector3(3.48f,.18f,3.96f),stone);
            // Recess, jamb, glazing, mullions and projecting sill on both visible elevations.
            for(float y=1.18f;y<h-.45f;y+=1.22f)
            {
                if(y>1.3f)Box("Storey cornice",new Vector3(x,y-.61f,z),new Vector3(3.34f,.075f,3.84f),stone);
                for(int col=-1;col<=1;col++)
                {
                    float wx=x+col*.93f,wz=z+col*1.12f,face=x-side*1.65f;
                    Box("Front window recess",new Vector3(wx,y,z-1.915f),new Vector3(.77f,1.02f,.035f),dark);
                    Box("Front stone jamb",new Vector3(wx,y,z-1.94f),new Vector3(.68f,.94f,.04f),stone);
                    Box("Front recessed glass",new Vector3(wx,y,z-1.966f),new Vector3(.55f,.80f,.025f),glass);
                    Box("Front mullion",new Vector3(wx,y,z-1.985f),new Vector3(.035f,.80f,.025f),stone);
                    Box("Front transom",new Vector3(wx,y-.08f,z-1.99f),new Vector3(.55f,.035f,.025f),stone);
                    Box("Projecting front sill",new Vector3(wx,y-.5f,z-2.01f),new Vector3(.81f,.095f,.23f),stone);
                    if(y<1.3f&&col==0)continue;
                    Box("Side window recess",new Vector3(face-side*.015f,y,wz),new Vector3(.035f,1.02f,.77f),dark);
                    Box("Side stone jamb",new Vector3(face-side*.04f,y,wz),new Vector3(.04f,.94f,.68f),stone);
                    Box("Side recessed glass",new Vector3(face-side*.066f,y,wz),new Vector3(.025f,.80f,.55f),glass);
                    Box("Side mullion",new Vector3(face-side*.087f,y,wz),new Vector3(.025f,.80f,.035f),stone);
                    Box("Side transom",new Vector3(face-side*.09f,y-.08f,wz),new Vector3(.025f,.035f,.55f),stone);
                    Box("Projecting side sill",new Vector3(face-side*.10f,y-.5f,wz),new Vector3(.23f,.095f,.81f),stone);
                }
                if(y>2&&variation%2==0)
                {
                    float face=x-side*1.65f,deck=y-.56f;
                    Box("Balcony slab",new Vector3(face-side*.35f,deck,z),new Vector3(.78f,.12f,1.4f),stone);
                    Box("Balcony handrail",new Vector3(face-side*.69f,deck+.52f,z),new Vector3(.045f,.045f,1.36f),rail);
                    for(int post=0;post<6;post++)Box("Balcony baluster",new Vector3(face-side*.69f,deck+.27f,z-.63f+post*.25f),new Vector3(.026f,.49f,.026f),rail);
                    for(int end=-1;end<=1;end+=2)Box("Balcony side rail",new Vector3(face-side*.35f,deck+.52f,z+end*.66f),new Vector3(.70f,.045f,.045f),rail);
                }
            }
            float entry=x-side*1.68f;
            Box("Entrance stone surround",new Vector3(entry,.9f,z),new Vector3(.12f,1.8f,.95f),stone);
            Box("Recessed glazed door",new Vector3(entry-side*.075f,.89f,z),new Vector3(.035f,1.58f,.73f),dark);
            Box("Door glazing",new Vector3(entry-side*.10f,1.02f,z),new Vector3(.025f,1.13f,.60f),glass);
            Box("Door handle",new Vector3(entry-side*.13f,.81f,z-.22f),new Vector3(.035f,.20f,.025f),stone);
            Box("Entrance step",new Vector3(entry-side*.26f,.16f,z),new Vector3(.52f,.13f,1.13f),stone);
            if(variation%2==1)
            {
                var canvas=Surface("Muted awning canvas",new Color(.34f,.44f,.34f));
                for(int stripe=0;stripe<8;stripe++)Box("Striped entry awning",new Vector3(entry-side*.36f,1.95f,z-.75f+stripe*.21f),new Vector3(.84f,.11f,.20f),stripe%2==0?canvas:stone);
                var tile=Surface("Terracotta roof tiles",new Color(.49f,.28f,.20f),.08f,1);
                for(int direction=-1;direction<=1;direction+=2)
                {
                    var slope=Box("Pitched tiled roof",new Vector3(x+direction*.85f,h+.28f,z),new Vector3(1.91f,.17f,4.04f),tile);
                    slope.localRotation=Quaternion.Euler(0,0,-direction*18);
                }
                var gable=new Mesh{name="Closed masonry roof gables"};
                gable.vertices=new[]{new Vector3(x-1.7f,h,z-1.91f),new Vector3(x,h+.58f,z-1.91f),new Vector3(x+1.7f,h,z-1.91f),new Vector3(x-1.7f,h,z+1.91f),new Vector3(x,h+.58f,z+1.91f),new Vector3(x+1.7f,h,z+1.91f)};
                gable.triangles=new[]{0,1,2,3,5,4};gable.RecalculateNormals();gable.RecalculateBounds();meshes.Add(gable);MeshPart("Masonry roof gable",gable,Vector3.zero,Vector3.one,wall);
                Box("Chimney",new Vector3(x+.8f,h+.55f,z+.8f),new Vector3(.38f,1.05f,.43f),wall);
                Box("Chimney cap",new Vector3(x+.8f,h+1.09f,z+.8f),new Vector3(.48f,.09f,.52f),roof);
            }
            else
            {
                Box("Recessed flat roof",new Vector3(x,h+.04f,z),new Vector3(3.15f,.12f,3.66f),roof);
                var unit=Surface("Rooftop equipment",new Color(.66f,.69f,.65f),.15f);
                Box("Ventilation unit",new Vector3(x+.5f,h+.33f,z+.7f),new Vector3(.86f,.48f,.72f),unit);
                for(int grille=0;grille<5;grille++)Box("Ventilation grille",new Vector3(x+.18f+grille*.15f,h+.32f,z+.325f),new Vector3(.045f,.29f,.015f),dark);
                for(int edge=-1;edge<=1;edge+=2)Box("Roof parapet",new Vector3(x+edge*1.6f,h+.16f,z),new Vector3(.12f,.26f,3.8f),wall);
            }
        }
        Mesh canopyMesh,branchMesh;
        Mesh CanopyMesh()
        {
            if(canopyMesh)return canopyMesh;
            var vertices=new List<Vector3>();var triangles=new List<int>();const int rings=10,slices=16;
            for(int y=0;y<=rings;y++)for(int x=0;x<=slices;x++)
            {
                float a=y*Mathf.PI/rings,b=x*Mathf.PI*2/slices;
                var n=new Vector3(Mathf.Sin(a)*Mathf.Cos(b),Mathf.Cos(a),Mathf.Sin(a)*Mathf.Sin(b));
                float irregularity=1+.075f*Mathf.Sin(n.x*7+n.y*4)*Mathf.Cos(n.z*6-n.y*3);
                vertices.Add(n*.5f*irregularity);
            }
            for(int y=0;y<rings;y++)for(int x=0;x<slices;x++){int i=y*(slices+1)+x;triangles.AddRange(new[]{i,i+1,i+slices+2,i,i+slices+2,i+slices+1});}
            canopyMesh=new Mesh{name="Organic foliage cluster"};canopyMesh.SetVertices(vertices);canopyMesh.SetTriangles(triangles,0);canopyMesh.RecalculateNormals();canopyMesh.RecalculateBounds();meshes.Add(canopyMesh);return canopyMesh;
        }
        Transform MeshPart(string name,Mesh mesh,Vector3 p,Vector3 scale,Material material)
        {
            var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(statics,false);g.transform.localPosition=p;g.transform.localScale=scale;g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<MeshRenderer>().sharedMaterial=material;g.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;return g.transform;
        }
        void Branch(Vector3 from,Vector3 to,float radius,Material material)
        {
            if(!branchMesh)
            {
                var vertices=new List<Vector3>();var triangles=new List<int>();const int sides=10;
                for(int y=0;y<2;y++)for(int i=0;i<sides;i++){float a=i*Mathf.PI*2/sides;float r=y==0?1:.58f;vertices.Add(new Vector3(Mathf.Cos(a)*r,y,Mathf.Sin(a)*r));}
                for(int i=0;i<sides;i++){int j=(i+1)%sides;triangles.AddRange(new[]{i,i+sides,j+sides,i,j+sides,j});}
                branchMesh=new Mesh{name="Tapered tree branch"};branchMesh.SetVertices(vertices);branchMesh.SetTriangles(triangles,0);branchMesh.RecalculateNormals();branchMesh.RecalculateBounds();meshes.Add(branchMesh);
            }
            var direction=to-from;var branch=MeshPart("Tapered trunk and branch",branchMesh,from,new Vector3(radius,direction.magnitude,radius),material);branch.localRotation=Quaternion.FromToRotation(Vector3.up,direction);
        }
        void NaturalTree(int side,float z,int seed)
        {
            var random=new System.Random(seed);float x=side*4.55f,height=2.5f+(float)random.NextDouble()*.40f;
            var bark=Surface("Natural bark",new Color(.33f,.26f,.18f),.02f,2);
            var soil=Surface("Tree soil",new Color(.27f,.25f,.19f));var stone=Surface("Tree well stone",new Color(.58f,.60f,.55f));
            Box("Tree planting well",new Vector3(x,.115f,z),new Vector3(.84f,.035f,.84f),stone);
            Box("Tree soil square",new Vector3(x,.14f,z),new Vector3(.70f,.025f,.70f),soil);
            var trunk=new Vector3(x,0,z);var crown=new Vector3(x+side*.07f,height,z+.05f);
            Branch(trunk,crown,.12f,bark);
            Color[] tones={new Color(.23f,.40f,.13f),new Color(.32f,.48f,.18f),new Color(.40f,.55f,.24f),new Color(.19f,.35f,.14f)};
            var leaves=new Material[tones.Length];for(int i=0;i<leaves.Length;i++)leaves[i]=Surface("Natural foliage "+i,tones[i],.03f,3);
            for(int j=0;j<6;j++)
            {
                float angle=j*Mathf.PI*2/6+(float)random.NextDouble()*.3f;
                var reach=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*(.49f+(float)random.NextDouble()*.18f);
                var end=crown+reach+Vector3.up*((j%2)*.25f-.2f);
                Branch(trunk+Vector3.up*(height*.52f+j*.08f),end,.043f,bark);
                var cluster=MeshPart("Irregular foliage crown",CanopyMesh(),end,new Vector3(1.05f,1.12f,1.05f),leaves[j%4]);cluster.localRotation=Quaternion.Euler(j*17,j*43,j*11);
                MeshPart("Foliage outer tuft",CanopyMesh(),end+reach*.46f+Vector3.up*.12f,new Vector3(.65f,.80f,.70f),leaves[(j+1)%4]);
            }
            MeshPart("Upper foliage crown",CanopyMesh(),crown+Vector3.up*.45f,new Vector3(1.05f,1.3f,1.05f),leaves[2]);
        }

        // Recycle complete modules only after they have passed behind the camera.
        void ScrollEnvironment(float delta)
        {
            float span=ChunkLength*ChunkCount;
            travel=Mathf.Repeat(travel+Mathf.Max(0,delta)*StreetSpeed,span);
            for(int i=0;i<streetChunks.Count;i++)streetChunks[i].localPosition=new Vector3(0,0,RecycleStart+Mathf.Repeat(i*ChunkLength-travel,span));
        }
        void BuildHero()
        {
            var prefab=Resources.Load<GameObject>("BusFleet/Bus_BLUE");if(!prefab)return;
            hero=Instantiate(prefab,world).transform;hero.name="City coach hero";hero.localScale=Vector3.one*1.24f;hero.localPosition=new Vector3(HeroLane,.05f,6f);hero.localRotation=Quaternion.Euler(0,-90,0);
            RemoveHeroRails();
            paint=Mat("Hero paint",Colors[1]);upper=Mat("Hero upper paint",Color.Lerp(Colors[1],Color.white,.24f));
            foreach(var r in hero.GetComponentsInChildren<MeshRenderer>())
            {
                string name=r.sharedMaterial.name;
                if(name.EndsWith("_UpperPaint"))r.sharedMaterial=upper;else if(name.EndsWith("_Paint"))r.sharedMaterial=paint;
            }
            // Keep the fleet prefab's open deck and its three upholstered seats visible.
            var glass=Mat("Coach rear glass",new Color(.035f,.28f,.44f));var white=Mat("Coach plate",new Color(.96f,.97f,.92f));
            Box("Rear cap",new Vector3(-2.24f,1.48f,0),new Vector3(.17f,.36f,1.53f),paint,hero);Box("Rear window",new Vector3(-2.335f,1.47f,0),new Vector3(.025f,.28f,1.12f),glass,hero);Box("Rear number plate",new Vector3(-2.365f,.70f,0),new Vector3(.025f,.15f,.48f),white,hero);
            for(int side=-1;side<=1;side+=2)Box("Upper glass band",new Vector3(0,1.52f,side*.806f),new Vector3(4.0f,.25f,.025f),glass,hero);
            for(int i=0;i<5;i++)exhaust.Add(Part("Soft exhaust",PrimitiveType.Sphere,Vector3.zero,Vector3.one*.2f,Mat("Soft exhaust white",new Color(.83f,.88f,.89f)),world));
        }
        void RemoveHeroRails()
        {
            var source=hero.Find("Editable source - enable this and disable Render geometry to edit");
            var trim=hero.Find("Render geometry/Ivory");if(!source||!trim)return;
            var material=trim.GetComponent<Renderer>().sharedMaterial;
            var parts=new List<CombineInstance>();
            foreach(var filter in source.GetComponentsInChildren<MeshFilter>(true))
            {
                if(filter.name=="Upper rail"||filter.name=="Rail support")continue;
                var renderer=filter.GetComponent<Renderer>();if(!renderer||renderer.sharedMaterial!=material)continue;
                parts.Add(new CombineInstance{mesh=filter.sharedMesh,transform=trim.worldToLocalMatrix*filter.transform.localToWorldMatrix});
            }
            if(parts.Count==0)return;
            var mesh=new Mesh{name="Menu bus trim without roof rails",indexFormat=IndexFormat.UInt32};
            mesh.CombineMeshes(parts.ToArray(),true,true);mesh.RecalculateBounds();meshes.Add(mesh);
            trim.GetComponent<MeshFilter>().sharedMesh=mesh;
        }
        void BuildTraffic()
        {
            // Reuse five vehicles; no runtime mesh generation or repeated Instantiate/Destroy.
            foreach(string name in new[]{"Car_0","Scooter","Bus","Car_1","Car_2"})
            {
                var prefab=Resources.Load<GameObject>("MenuTraffic/"+name);if(!prefab)continue;
                var root=Instantiate(prefab,world).transform;root.name="Oncoming "+name;
                root.localRotation=Quaternion.Euler(0,180,0);root.gameObject.SetActive(false);
                foreach(var collider in root.GetComponentsInChildren<Collider>())Destroy(collider);
                foreach(var renderer in root.GetComponentsInChildren<Renderer>())renderer.shadowCastingMode=ShadowCastingMode.Off;
                SetLayer(root.gameObject);traffic.Add(new TrafficVehicle{root=root});
            }
        }
        void UpdateTraffic(float delta)
        {
            delta=Mathf.Max(0,delta);
            foreach(var vehicle in traffic)
            {
                if(!vehicle.root.gameObject.activeSelf)continue;
                vehicle.root.localPosition+=Vector3.back*(vehicle.speed*delta);
                if(vehicle.root.localPosition.z< -25)vehicle.root.gameObject.SetActive(false);
            }
            nextTraffic-=delta;
            if(nextTraffic>0||traffic.Count==0)return;
            var next=traffic[trafficSequence%traffic.Count];
            if(next.root.gameObject.activeSelf)return;
            // Enter beyond the fog, face the camera, and stay across the centre line.
            next.root.localPosition=new Vector3(OppositeLane,.05f,72);
            next.speed=11.5f+(float)trafficRandom.NextDouble()*3.5f;
            next.root.gameObject.SetActive(true);trafficSequence++;
            nextTraffic=3.8f+(float)trafficRandom.NextDouble()*2.8f;
        }
        public void SetColor(int color,bool persist=true)
        {
            selected=Mathf.Clamp(color,0,5);if(paint)paint.color=Colors[selected];if(upper)upper.color=Color.Lerp(Colors[selected],Color.white,.24f);
            if(persist){PlayerPrefs.SetInt("Rollic.BusJam.MenuBusColor",selected);PlayerPrefs.Save();}
            forceRender=true;
        }
        void Update()
        {
            if(!preview||obscured)return;
            ScrollEnvironment(Time.unscaledDeltaTime);
            UpdateTraffic(Time.unscaledDeltaTime);
            float time=Time.unscaledTime,z=6f;
            if(hero)
            {
                hero.localPosition=new Vector3(HeroLane,RoadY(z)+.05f+Mathf.Sin(time*3f)*.012f,z);float pitch=Mathf.Sin(time*2f)*.20f;hero.localRotation=Quaternion.Euler(0,-90,0)*Quaternion.Euler(0,0,pitch);
                for(int i=0;i<exhaust.Count;i++){float age=Mathf.Repeat(time*.6f+i*.2f,1);exhaust[i].localPosition=new Vector3(HeroLane+Mathf.Sin(i*3f+age)*.10f,RoadY(z)+.25f+age*.12f,z-2.9f-age*1.6f);exhaust[i].localScale=Vector3.one*(.09f+age*.32f)*(1-age);}
            }
        }
        void LateUpdate()
        {
            if(!preview||!display||obscured&&!forceRender)return;
            var size=display.rectTransform.rect.size;
            if(!texture||screenWidth!=Screen.width||screenHeight!=Screen.height||viewportSize!=size)Resize();
            Render();forceRender=false;
        }
        void Resize()
        {
            if(!display||!preview)return;
            viewportSize=display.rectTransform.rect.size;screenWidth=Screen.width;screenHeight=Screen.height;
            float aspect=Mathf.Max(.1f,viewportSize.x/Mathf.Max(1,viewportSize.y));
            int width=Mathf.Clamp(screenWidth,360,720),height=Mathf.RoundToInt(width/aspect);
            if(height>1600){height=1600;width=Mathf.Max(1,Mathf.RoundToInt(height*aspect));}
            if(texture&&texture.width==width&&texture.height==height){display.texture=texture;return;}
            if(texture){preview.targetTexture=null;texture.Release();Destroy(texture);}
            texture=new RenderTexture(width,height,16){name="Menu city viewport",antiAliasing=1};texture.Create();display.texture=texture;preview.targetTexture=texture;preview.aspect=aspect;
        }
        public void Render(){if(preview&&texture){preview.Render();renderedFrames++;}}
        void SetLayer(GameObject root){root.layer=Layer;foreach(Transform child in root.transform)SetLayer(child.gameObject);}
        void BakeGroup(Transform group)
        {
            var groups=new Dictionary<Material,List<CombineInstance>>();var filters=group.GetComponentsInChildren<MeshFilter>();
            foreach(var f in filters){var m=f.GetComponent<MeshRenderer>().sharedMaterial;if(!groups.ContainsKey(m))groups[m]=new List<CombineInstance>();groups[m].Add(new CombineInstance{mesh=f.sharedMesh,transform=group.worldToLocalMatrix*f.transform.localToWorldMatrix});}
            foreach(var f in filters){f.gameObject.SetActive(false);Destroy(f.gameObject);}
            foreach(var pair in groups){var mesh=new Mesh(){indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(pair.Value.ToArray(),true,true);meshes.Add(mesh);var g=new GameObject(pair.Key.name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(group,false);g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<MeshRenderer>().sharedMaterial=pair.Key;g.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;}
        }
        Mesh RoundedBox(Vector3 size,float radius)
        {
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var triangles=new List<int>();var half=size*.5f;float r=Mathf.Min(radius,Mathf.Min(half.x,Mathf.Min(half.y,half.z))*.97f);
            for(int axis=0;axis<3;axis++)for(int sign=-1;sign<=1;sign+=2){int u=(axis+1)%3,v=(axis+2)%3;float[] xs={-half[u],-half[u]+r*.3f,-half[u]+r,0,half[u]-r,half[u]-r*.3f,half[u]};float[] ys={-half[v],-half[v]+r*.3f,-half[v]+r,0,half[v]-r,half[v]-r*.3f,half[v]};int offset=vertices.Count;for(int y=0;y<7;y++)for(int x=0;x<7;x++){var p=Vector3.zero;p[axis]=half[axis]*sign;p[u]=xs[x];p[v]=ys[y];var inner=new Vector3(Mathf.Clamp(p.x,-half.x+r,half.x-r),Mathf.Clamp(p.y,-half.y+r,half.y-r),Mathf.Clamp(p.z,-half.z+r,half.z-r));var n=(p-inner).normalized;vertices.Add(inner+n*r);normals.Add(n);}for(int y=0;y<6;y++)for(int x=0;x<6;x++){int a=offset+y*7+x,b=a+1,d=a+7,c=d+1;if(sign>0)triangles.AddRange(new[]{a,b,c,a,c,d});else triangles.AddRange(new[]{a,c,b,a,d,c});}}
            var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();meshes.Add(mesh);return mesh;
        }
        void OnDisable(){if(world)world.gameObject.SetActive(false);}
        void OnDestroy()
        {
            if(cached==this)cached=null;
            if(preview)preview.targetTexture=null;
            if(texture){texture.Release();Destroy(texture);}
            if(world)Destroy(world.gameObject);
            foreach(var material in materials)if(material)Destroy(material);
            foreach(var mesh in meshes)if(mesh)Destroy(mesh);
        }
    }
}
