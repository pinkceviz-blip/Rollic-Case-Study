using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
namespace Rollic.BusJam
{
    public class BusJamGame:MonoBehaviour
    {
        public LevelData[] levels;
        enum Phase
        {
            Start,Transition,Playing,Paused,Won,Lost
        }
        [Serializable] class SaveData
        {
            public int level,bus,seats;
            public int revision;
            public float time;
            public bool timerStarted;
            public List<int> entrances;
            public List<PassengerData> people;
            public List<int> waiting;
        }
        const string SaveKey="Rollic.BusJam.Save.v1";
        const string CompletedKey="Rollic.BusJam.CompletedLevels";
        public static readonly Color[] Palette=
        {
            new Color(1,.24f,.37f),new Color(.15f,.66f,1),new Color(.19f,.82f,.48f),new Color(1,.71f,.12f),new Color(.66f,.3f,1),new Color(1,.42f,.72f)
        }
        ;
        public static readonly string[] ColorNames=
        {
            "CORAL","BLUE","GREEN","GOLD","VIOLET","PINK"
        }
        ;
        LevelData level;
        SaveData data,transaction;
        sealed class PassengerTransit
        {
            public PassengerData passenger;
            public Transform root;
            public List<Vector2Int> path;
            public int order;
            public bool ready,completed,earlyWaiting;
            public int reservedWaitingSlot=-1;
        }
        readonly Queue<PassengerTransit> selectionQueue=new Queue<PassengerTransit>();
        readonly List<PassengerTransit> inFlight=new List<PassengerTransit>();
        readonly List<PassengerData> acceptedMoves=new List<PassengerData>();
        float nextAutoSave;
        bool awaitingResumeTap;int resumeTapFrame;
        Phase phase;
        bool busy,enteringGameplay;
        Image transitionCurtain;
        Texture2D timerBarTexture;
        Sprite timerBarSprite;
        RawImage pausePlayGraphic;
        RectTransform headerContent;
        Rect lastSafeArea;
        Vector2 lastScreenSize;
        Transform world,bus,queuedBus;
        int queuedBusIndex=-1;
        const float BusQueueX=-5.4f;
        Camera cam;
        Canvas canvas;
        Font font,displayFont;
        Text timerLabel,levelLabel,statusLabel,busLabel,progressLabel;
        GameObject overlay;
        GameObject headerHUD,footerHUD;
        Image timerFill;
        readonly Dictionary<PassengerData,Transform> views=new Dictionary<PassengerData,Transform>();
        readonly List<Transform> waitingViews=new List<Transform>();
        readonly List<Material> materials=new List<Material>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Bootstrap()
        {
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="LevelEditor")return;
            if(FindAnyObjectByType<BusJamGame>()==null)new GameObject("Bus Jam").AddComponent<BusJamGame>();
        }
        RuntimeErrorHandler errorHandler;
        void Awake(){errorHandler=GetComponent<RuntimeErrorHandler>();if(!errorHandler)errorHandler=gameObject.AddComponent<RuntimeErrorHandler>();}
        internal void SuspendForError()
        {
            StopAllCoroutines();busy=false;phase=Phase.Paused;JellyPassenger.AnimationPaused=true;
            foreach(var runner in FindObjectsByType<GameplayTransitionRunner>())Destroy(runner.gameObject);
            enteringGameplay=false;
            if(transitionCurtain){transitionCurtain.gameObject.SetActive(false);Destroy(transitionCurtain.gameObject);transitionCurtain=null;}
        }
        internal void RecoverFromError(bool restart)
        {
            if(!canvas||data==null||levels==null||levels.Length==0)throw new InvalidOperationException("Game initialization is incomplete.");
            int index=Mathf.Clamp(data.level,0,levels.Length-1);NewLevel(index);
            if(restart)Begin();else ShowMainMenu();
        }
        void Start()
        {
            Application.targetFrameRate=60;
            AudioListener.volume=PlayerPrefs.GetInt("Rollic.BusJam.Sound",1)!=0?1f:0f;
            Screen.orientation=ScreenOrientation.Portrait;
            font=Resources.Load<Font>("Fonts/Nunito-ExtraBold")??Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if(levels==null||levels.Length==0)
            {
                levels=Resources.LoadAll<LevelData>("BusJamLevels");
                Array.Sort(levels,(a,b)=>string.CompareOrdinal(a.name,b.name));
                if(levels.Length==0)
                {
                    levels=new LevelData[LevelData.DefaultLevelCount];
                    for(int i=0;i<LevelData.DefaultLevelCount;i++)levels[i]=LevelData.CreateDefault(i);
                }
            }
            displayFont=Resources.Load<Font>("Fonts/Nunito-Black")??font;
            SetupCamera();
            BuildUI();
            if(!GetComponent<GamePreferences>())gameObject.AddComponent<GamePreferences>();
            Load();
            ShowMainMenu();
        }
        void SetupCamera()
        {
            foreach(var c in FindObjectsByType<Camera>())c.gameObject.SetActive(false);
            cam=new GameObject("Game Camera").AddComponent<Camera>();
            cam.gameObject.AddComponent<AudioListener>();
            cam.tag="MainCamera";
            cam.transform.position=new Vector3(0,16,-13);
            cam.transform.LookAt(new Vector3(0,0,1));
            cam.transform.position+=cam.transform.up*1.45f;
            cam.orthographic=true;
            cam.orthographicSize=Mathf.Max(8.3f,4.15f/cam.aspect);
            cam.backgroundColor=new Color(.48f,.76f,.89f);
            cam.clearFlags=CameraClearFlags.SolidColor;
            RenderSettings.ambientLight=new Color(.75f,.78f,.9f);
            var light=new GameObject("Sun").AddComponent<Light>();
            light.type=LightType.Directional;
            light.intensity=1.4f;
            light.transform.rotation=Quaternion.Euler(45,-35,0);
        }
        Material Mat(Color c)
        {
            var m=new Material(Resources.Load<Shader>("BusJam/Station")??Shader.Find("Universal Render Pipeline/Lit")??Shader.Find("Standard"));
            m.color=c;
            materials.Add(m);
            return m;
        }
        Transform Shape(string name,PrimitiveType type,Transform parent,Vector3 pos,Vector3 scale,Color color)
        {
            var g=GameObject.CreatePrimitive(type);
            g.name=name;
            g.transform.SetParent(parent,false);
            g.transform.localPosition=pos;
            g.transform.localScale=scale;
            g.GetComponent<Renderer>().sharedMaterial=Mat(color);
            return g.transform;
        }
        const float BusStopZ=4.9f;
        const float TileSize=.76f;
        const float TileHeight=.07f;
        static Vector3 SquareTileScale=>new Vector3(TileSize,TileHeight,TileSize);
        const float SlotPitch=1.04f;
        const float FrontRowZ=.9f;
        Vector3 Cell(int x,int y)
        {
            float column=x-(level.width-1)*.5f;
            return new Vector3(column*SlotPitch,0,FrontRowZ-y*SlotPitch);
        }
        Transform Person(int color,Vector3 pos)
        {
            var prefab=Resources.Load<GameObject>("JellyPassengers/Jelly_"+ColorNames[color]);
            if(prefab)
            {
                var passenger=Instantiate(prefab,world).transform;
                passenger.position=pos;
                return passenger;
            }
            var root=new GameObject("Passenger "+ColorNames[color]).transform;
            root.SetParent(world);
            root.position=pos;
            var c=Palette[color];
            Shape("Body",PrimitiveType.Capsule,root,new Vector3(0,.46f,0),new Vector3(.36f,.32f,.32f),c);
            Shape("Head",PrimitiveType.Sphere,root,new Vector3(0,.96f,0),Vector3.one*.43f,c);
            for(int side=-1;side<=1;side+=2)
            {
                Shape("Leg",PrimitiveType.Capsule,root,new Vector3(side*.12f,.17f,0),new Vector3(.14f,.18f,.16f),c);
                var arm=Shape("Arm",PrimitiveType.Capsule,root,new Vector3(side*.26f,.49f,0),new Vector3(.13f,.22f,.13f),c);
                arm.localRotation=Quaternion.Euler(0,0,side*18);
                Shape("Eye",PrimitiveType.Sphere,root,new Vector3(side*.075f,1,-.19f),Vector3.one*.052f,new Color(.08f,.1f,.17f));
            }
            var hit=root.gameObject.AddComponent<BoxCollider>();
            hit.center=new Vector3(0,.6f,0);
            hit.size=new Vector3(.75f,1.4f,.75f);
            return root;
        }
        void DrawWorld()
        {
            if(world)Destroy(world.gameObject);
            foreach(var m in materials)Destroy(m);
            materials.Clear();
            views.Clear();rearSlotViews.Clear();arriving.Clear();gateCounters.Clear();gateDoors.Clear();entranceQueue.Clear();
            waitingViews.Clear();
            queuedBus=null;queuedBusIndex=-1;
            world=new GameObject("Station").transform;
            var plaza=Resources.Load<GameObject>("StationArt/StationPlaza");
            if(plaza)Instantiate(plaza,Vector3.zero,Quaternion.identity,world);
            else Shape("Station base",PrimitiveType.Cube,world,new Vector3(0,-.25f,0),new Vector3(8.4f,.4f,13),new Color(.73f,.78f,.75f));
            var passengerSlot=Resources.Load<GameObject>("StationArt/PassengerSlot");
            var waitingSlot=Resources.Load<GameObject>("StationArt/WaitingSlot");
            var road=Resources.Load<GameObject>("StationArt/StationRoad");
            if(road)
            {
                // Road lane boundaries remain at Z=3.8 and Z=6.6; the bus stops closer to the curb.
                Instantiate(road, new Vector3(0,0,6.6f), Quaternion.identity, world);
            }
            else
            {
                Shape("Bus lane",PrimitiveType.Cube,world,new Vector3(0,-.015f,5.2f),new Vector3(16,.1f,2.8f),new Color(.19f,.21f,.24f));
            }
            for(int y=0;y<level.height;y++)for(int x=0;x<level.width;x++)
            {
                bool blocked=level.obstacles.Contains(new Vector2Int(x,y));
                Transform tile;
                if(!blocked&&passengerSlot)tile=Instantiate(passengerSlot,Cell(x,y),Quaternion.identity,world).transform;
                else tile=Shape(blocked?"Planter":"Tile",PrimitiveType.Cube,world,Cell(x,y)+new Vector3(0,blocked?.17f:0,0),blocked?new Vector3(TileSize,.4f,TileSize):SquareTileScale,blocked?new Color(.29f,.42f,.51f):new Color(.94f,.94f,.88f));
                if(!blocked)rearSlotViews[new Vector2Int(x,y)]=tile;
                if(blocked)Shape("Shrub",PrimitiveType.Sphere,world,Cell(x,y)+Vector3.up*.55f,new Vector3(.78f,.6f,.78f),Palette[2]);
            }
            for(int i=0;i<level.waitingSlots;i++)
            {
                if(waitingSlot)Instantiate(waitingSlot,WaitPosition(i),Quaternion.identity,world);
                else Shape("Waiting slot",PrimitiveType.Cube,world,WaitPosition(i),SquareTileScale,new Color(.97f,.97f,1));
            }
            foreach(var p in data.people)views[p]=Person(p.color,Cell(p.x,p.y));
            for(int i=0;i<data.waiting.Count;i++)waitingViews.Add(Person(data.waiting[i],WaitPosition(i)));
            DrawBus();
            DrawEntrances();
            DrawStationLamps();
            DrawPlazaPerimeterLights();
            TrimRearSlots();
        }
        void DrawStationLamps()
        {
            var halo=new Material(Resources.Load<Shader>("MenuArt/MenuLampGlow"));materials.Add(halo);
            foreach(float x in new[]{-3.3f,-1.1f,1.1f,3.3f})
            {
                var lamp=new GameObject("Station evening lamp").transform;lamp.SetParent(world,false);lamp.localPosition=new Vector3(x,0,10.12f);
                Shape("Lamp base",PrimitiveType.Cube,lamp,new Vector3(0,.07f,0),new Vector3(.28f,.14f,.30f),new Color(.21f,.28f,.35f));
                Shape("Lamp pole",PrimitiveType.Cylinder,lamp,new Vector3(0,.80f,0),new Vector3(.065f,.80f,.065f),new Color(.18f,.26f,.34f));
                Shape("Gold lamp cap",PrimitiveType.Cube,lamp,new Vector3(0,1.62f,0),new Vector3(.43f,.09f,.25f),new Color(1,.74f,.24f));
                var bulb=Shape("Warm station lamp",PrimitiveType.Cube,lamp,new Vector3(0,1.52f,0),new Vector3(.31f,.15f,.18f),new Color(1,.94f,.69f));bulb.GetComponent<Renderer>().sharedMaterial.SetFloat("_LampEmission",1);
                var glow=Shape("Station lamp glow",PrimitiveType.Quad,lamp,new Vector3(0,1.52f,0),Vector3.one*.80f,Color.white);glow.rotation=cam.transform.rotation;glow.position-=cam.transform.forward*.12f;glow.GetComponent<Renderer>().sharedMaterial=halo;
                var pool=Shape("Warm pavement light",PrimitiveType.Quad,lamp,new Vector3(0,.071f,0),new Vector3(2.0f,2.2f,1),Color.white);pool.localRotation=Quaternion.Euler(90,0,0);pool.GetComponent<Renderer>().sharedMaterial=halo;
                foreach(var collider in lamp.GetComponentsInChildren<Collider>()){collider.gameObject.layer=2;Destroy(collider);}
            }
        }
        void DrawPlazaPerimeterLights()
        {
            var halo=new Material(Resources.Load<Shader>("MenuArt/MenuLampGlow"));materials.Add(halo);
            foreach(int side in new[]{-1,1})foreach(float z in new[]{2.3f,-.5f,-3.2f,-5.9f})
            {
                var lamp=new GameObject("Plaza perimeter light").transform;lamp.SetParent(world,false);lamp.localPosition=new Vector3(side*3.76f,0,z);
                Shape("Border light base",PrimitiveType.Cube,lamp,new Vector3(0,.08f,0),new Vector3(.23f,.16f,.25f),new Color(.13f,.22f,.32f));
                Shape("Gold border cap",PrimitiveType.Cube,lamp,new Vector3(0,.34f,0),new Vector3(.27f,.065f,.27f),new Color(1,.78f,.23f));
                var bulb=Shape("Warm border lantern",PrimitiveType.Cube,lamp,new Vector3(0,.24f,0),new Vector3(.20f,.17f,.20f),new Color(1,.94f,.73f));bulb.GetComponent<Renderer>().sharedMaterial.SetFloat("_LampEmission",1);
                var glow=Shape("Border lantern glow",PrimitiveType.Quad,lamp,new Vector3(0,.27f,0),Vector3.one*.62f,Color.white);glow.rotation=cam.transform.rotation;glow.position-=cam.transform.forward*.08f;glow.GetComponent<Renderer>().sharedMaterial=halo;
                var pool=Shape("Border light spill",PrimitiveType.Quad,lamp,new Vector3(0,.071f,0),new Vector3(2.4f,3.2f,1),Color.white);pool.localRotation=Quaternion.Euler(90,0,0);pool.GetComponent<Renderer>().sharedMaterial=halo;
                foreach(var collider in lamp.GetComponentsInChildren<Collider>()){collider.gameObject.layer=2;Destroy(collider);}
            }
        }
        readonly Dictionary<Vector2Int,Transform> rearSlotViews=new Dictionary<Vector2Int,Transform>();
        void TrimRearSlots()
        {
            var occupied=new HashSet<Vector2Int>();
            // Reserve the visible tile while its next passenger is queued or walking in.
            foreach(var p in data.people)occupied.Add(new Vector2Int(p.x,p.y));
            for(int i=0;i<level.entrances.Count;i++)
                if(data.entrances[i]<level.entrances[i].colors.Count)
                    occupied.Add(EntranceRules.EntryCell(level,level.entrances[i].column));
            foreach(var tile in rearSlotViews)if(tile.Value)tile.Value.gameObject.SetActive(occupied.Contains(tile.Key));
        }
        readonly Queue<PassengerData> entranceQueue=new Queue<PassengerData>();
        readonly HashSet<PassengerData> arriving=new HashSet<PassengerData>();
        readonly List<TextMesh> gateCounters=new List<TextMesh>();
        readonly List<Transform> gateDoors=new List<Transform>();
        Vector3 EntrancePosition(int column)=>new Vector3(Cell(column,level.height-1).x,0,-6.30f);
        void DrawEntrances()
        {
            for(int i=0;i<level.entrances.Count;i++)
            {
                var gate=level.entrances[i];var position=EntrancePosition(gate.column);
                var root=new GameObject("Passenger entrance "+i).transform;root.SetParent(world,false);root.position=position;
                Shape("Entrance shadow",PrimitiveType.Cube,root,new Vector3(0,.33f,0),new Vector3(.92f,.68f,.22f),new Color(.025f,.12f,.30f));
                foreach(int side in new[]{-1,1})Shape("Golden door frame",PrimitiveType.Cube,root,new Vector3(side*.46f,.38f,-.06f),new Vector3(.11f,.82f,.20f),new Color(1,.76f,.16f));
                Shape("Door lintel",PrimitiveType.Cube,root,new Vector3(0,.80f,-.06f),new Vector3(1.02f,.12f,.22f),new Color(1,.88f,.36f));
                var door=Shape("Sliding entrance door",PrimitiveType.Cube,root,new Vector3(0,.37f,-.14f),new Vector3(.77f,.66f,.08f),new Color(.03f,.43f,.88f));gateDoors.Add(door);
                var badgePosition=new Vector3(0,1.02f,-.07f);
                var badge=Shape("Counter gold rim",PrimitiveType.Cube,root,badgePosition,new Vector3(.58f,.40f,.08f),new Color(1,.80f,.22f));badge.rotation=cam.transform.rotation;
                var face=Shape("Counter blue face",PrimitiveType.Cube,root,badgePosition-cam.transform.forward*.045f,new Vector3(.50f,.32f,.04f),new Color(.025f,.18f,.48f));face.rotation=cam.transform.rotation;
                var counter=new GameObject("Hidden passenger count").AddComponent<TextMesh>();counter.transform.SetParent(root,false);counter.transform.localPosition=new Vector3(0,1.02f,-.07f);counter.transform.rotation=cam.transform.rotation;counter.transform.position-=cam.transform.forward*.085f;counter.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");counter.GetComponent<MeshRenderer>().sharedMaterial=counter.font.material;counter.anchor=TextAnchor.MiddleCenter;counter.alignment=TextAlignment.Center;counter.characterSize=.045f;counter.fontSize=64;counter.color=new Color(1,.94f,.60f);gateCounters.Add(counter);
            }
            RefreshEntrances();
        }
        void RefreshEntrances()
        {
            for(int i=0;i<gateCounters.Count;i++)
            {
                var counter=gateCounters[i];counter.text=(level.entrances[i].colors.Count-data.entrances[i]).ToString();
                StartCoroutine(FitEntranceCounter(counter));
            }
        }
        IEnumerator FitEntranceCounter(TextMesh counter)
        {
            // Wait for the font mesh, then fit its visible glyphs rather than the font's line height.
            yield return null;if(!counter)yield break;
            var bounds=counter.GetComponent<MeshRenderer>().localBounds;
            if(bounds.size.x<=0||bounds.size.y<=0)yield break;
            float scale=Mathf.Min(1,.40f/bounds.size.x,.23f/bounds.size.y);
            counter.transform.localScale=Vector3.one*scale;
            counter.transform.localPosition=new Vector3(0,1.02f,-.07f)-cam.transform.forward*.085f;
            counter.transform.position-=counter.transform.TransformVector(bounds.center);
        }
        void FillEntrances()
        {
            var added=EntranceRules.Fill(level,data.people,data.entrances);
            var routePeople=new List<PassengerData>(data.people);foreach(var passenger in added)routePeople.Remove(passenger);
            foreach(var passenger in added)
            {
                routePeople.Add(passenger);
                var entryPath=EntranceRules.EntryPath(level,routePeople,new Vector2Int(passenger.x,passenger.y),passenger);
                var target=Cell(passenger.x,passenger.y);var root=Person(passenger.color,EntrancePosition(passenger.x)+Vector3.back*2.1f);views[passenger]=root;arriving.Add(passenger);
                foreach(var hit in root.GetComponentsInChildren<Collider>())hit.enabled=false;
                int gate=level.entrances.FindIndex(g=>g.column==passenger.x);entranceQueue.Enqueue(passenger);StartCoroutine(EnterStation(passenger,root,target,gate,entryPath));
            }
            RefreshEntrances();TrimRearSlots();
        }
        IEnumerator AnimateEntranceDoor(Transform door,bool open)
        {
            float from=door.localScale.x,to=open?.10f:.77f,elapsed=0;
            while(elapsed<.12f){if(phase==Phase.Playing&&!awaitingResumeTap){elapsed+=Time.deltaTime;door.localScale=new Vector3(Mathf.Lerp(from,to,Mathf.SmoothStep(0,1,elapsed/.12f)),.66f,.08f);}yield return null;}
            door.localScale=new Vector3(to,.66f,.08f);
        }
        IEnumerator EnterStation(PassengerData passenger,Transform root,Vector3 target,int gate,List<Vector2Int> path)
        {
            // The grid is reserved immediately, while physical movement waits for the departing rider to clear the cell.
            bool blocked=true;
            while(blocked)
            {
                blocked=phase!=Phase.Playing||awaitingResumeTap||entranceQueue.Count==0||entranceQueue.Peek()!=passenger;
                foreach(var other in inFlight)if(other.root&&!other.completed&&((other.root.position-target).sqrMagnitude<.65f*.65f||other.path.Contains(new Vector2Int(passenger.x,passenger.y))))blocked=true;
                if(blocked)yield return null;
            }
            if(path==null)throw new InvalidOperationException("Reserved entrance has no valid path.");
            var door=gateDoors[gate];yield return AnimateEntranceDoor(door,true);
            yield return Move(root,door.parent.position+Vector3.forward*.32f,.30f);
            foreach(var step in path)yield return Move(root,Cell(step.x,step.y),.16f);
            yield return AnimateEntranceDoor(door,false);arriving.Remove(passenger);entranceQueue.Dequeue();TrimRearSlots();
            foreach(var hit in root.GetComponentsInChildren<Collider>())hit.enabled=true;
            Save();
        }
        // Keep a single-person aisle free at X=0 between the two waiting banks.
        const float BoardingCorridorZ=1.65f;
        Vector3 WaitPosition(int i)
        {
            int left=(level.waitingSlots+1)/2;
            float column=i<left?i-left:i-left+1;
            return new Vector3(column*SlotPitch,0,FrontRowZ+2*SlotPitch);
        }
        IEnumerator BoardThroughCenter(Transform passenger)
        {
            if(Mathf.Abs(passenger.position.z-BoardingCorridorZ)>.02f)
                yield return Move(passenger,new Vector3(passenger.position.x,0,BoardingCorridorZ),.18f);
            yield return Move(passenger,new Vector3(0,0,BoardingCorridorZ),.20f);
            yield return Move(passenger,new Vector3(0,0,3.15f),.25f);
            yield return AnimateBusDoors(true);
            yield return Move(passenger,new Vector3(0,0,3.80f),.18f);
            var model=bus?bus.GetComponent<BusModel>():null;
            Vector3 entry=model&&model.boardingPoint?model.boardingPoint.position:new Vector3(0,.40f,BusStopZ-1.04f);
            Vector3 inside=model&&model.interiorPoint?model.interiorPoint.position:new Vector3(0,.50f,BusStopZ-.46f);
            var jelly=passenger.GetComponent<JellyPassenger>();
            if(jelly)
            {
                jelly.ResetPose();jelly.enabled=false;
            }
            yield return Move(passenger,entry,.25f,passenger.localScale*.66f);
            yield return Move(passenger,inside,.25f);
        }
        IEnumerator AnimateBusDoors(bool open)
        {
            var model=bus?bus.GetComponent<BusModel>():null;
            if(!model||!model.HasDoors)yield break;
            float from=model.DoorOpening,target=open?1f:0f;
            if(Mathf.Abs(from-target)<.001f)yield break;
            float elapsed=0,duration=.34f*Mathf.Abs(target-from);
            while(elapsed<duration)
            {
                if(phase!=Phase.Playing||awaitingResumeTap){yield return null;continue;}
                elapsed+=Time.deltaTime;
                float t=Mathf.Clamp01(elapsed/duration);
                model.SetDoorOpening(Mathf.Lerp(from,target,t*t*(3-2*t)));
                yield return null;
            }
            model.SetDoorOpening(target);
        }
        void UpdateBusSeats()
        {
            var model=bus?bus.GetComponent<BusModel>():null;
            if(model)model.SetOccupancy(data.seats,level.busCapacity,level.buses[data.bus]);
            else DrawBus();
        }
        void DrawQueuedBus(float initialX=BusQueueX)
        {
            int next=data.bus+1;
            if(queuedBus&&queuedBusIndex==next)return;
            if(queuedBus)Destroy(queuedBus.gameObject);
            queuedBus=null;queuedBusIndex=-1;
            if(next>=level.buses.Count)return;
            int color=level.buses[next];
            var prefab=Resources.Load<GameObject>("BusFleet/Bus_"+ColorNames[color]);
            if(!prefab)return;
            queuedBus=Instantiate(prefab,world).transform;
            queuedBus.name="Queued bus "+ColorNames[color];
            queuedBus.position=new Vector3(initialX,0,BusStopZ);
            var model=queuedBus.GetComponent<BusModel>();
            if(model){model.SetOccupancy(0,level.busCapacity,color);model.SetDoorOpening(0);}
            queuedBusIndex=next;
        }
        IEnumerator AdvanceBusQueue()
        {
            Vector3 front=bus.position,rear=queuedBus?queuedBus.position:Vector3.zero;
            float elapsed=0;
            while(elapsed<.75f)
            {
                if(phase!=Phase.Playing||awaitingResumeTap){yield return null;continue;}
                elapsed+=Time.deltaTime;float t=Mathf.Clamp01(elapsed/.75f);t=t*t*(3-2*t);
                bus.position=Vector3.Lerp(front,new Vector3(0,0,BusStopZ),t);
                if(queuedBus)queuedBus.position=Vector3.Lerp(rear,new Vector3(BusQueueX,0,BusStopZ),t);
                yield return null;
            }
            bus.position=new Vector3(0,0,BusStopZ);
            if(queuedBus)queuedBus.position=new Vector3(BusQueueX,0,BusStopZ);
        }
        void DrawBus()
        {
            if(bus)Destroy(bus.gameObject);
            if(data.bus>=level.buses.Count)return;
            int color=level.buses[data.bus];
            var prefab=Resources.Load<GameObject>("BusFleet/Bus_"+ColorNames[color]);
            if(prefab)
            {
                bus=Instantiate(prefab,world).transform;
                bus.position=new Vector3(0,0,BusStopZ);
                var visual=bus.GetComponent<BusModel>();
                if(visual)visual.SetOccupancy(data.seats,level.busCapacity,color);
                DrawQueuedBus();
                return;
            }
            bus=new GameObject("Bus "+ColorNames[color]).transform;
            bus.SetParent(world);
            bus.position=new Vector3(0,0,BusStopZ);
            var c=Palette[color];
            Shape("Coach",PrimitiveType.Cube,bus,new Vector3(0,.7f,0),new Vector3(4.4f,1.05f,1.45f),c);
            Shape("Roof",PrimitiveType.Cube,bus,new Vector3(0,1.26f,0),new Vector3(4.45f,.14f,1.5f),Color.white);
            for(int i=0;i<6;i++)Shape("Window",PrimitiveType.Cube,bus,new Vector3(-1.76f+i*.7f,.91f,-.736f),new Vector3(.54f,.38f,.035f),new Color(.08f,.22f,.34f));
            foreach(float x in new[]
            {
                -1.4f,1.4f
            }
            )foreach(float z in new[]
            {
                -.7f,.7f
            }
            )
            {
                var wheel=Shape("Wheel",PrimitiveType.Cylinder,bus,new Vector3(x,.3f,z),new Vector3(.63f,.12f,.63f),new Color(.12f,.15f,.23f));
                wheel.localRotation=Quaternion.Euler(90,0,0);
            }
            for(int i=0;i<level.busCapacity;i++)Shape("Seat indicator",PrimitiveType.Sphere,bus,new Vector3(-.9f+i*.9f,1.4f,0),Vector3.one*.27f,i<data.seats?Color.white:c*.65f);
        }
        void BuildUI()
        {
            var go=new GameObject("UI");
            canvas=go.AddComponent<Canvas>();
            canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(900,1600);
            scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            go.AddComponent<GraphicRaycaster>();
            var events=EventSystem.current?EventSystem.current:FindAnyObjectByType<EventSystem>();
            if(!events)events=new GameObject("Input").AddComponent<EventSystem>();
            var input=events.GetComponent<InputSystemUIInputModule>();
            if(!input)input=events.gameObject.AddComponent<InputSystemUIInputModule>();
            foreach(var module in events.GetComponents<BaseInputModule>())if(module!=input)module.enabled=false;
            if(!input.actionsAsset)input.AssignDefaultActions();
            input.enabled=true;events.enabled=true;EventSystem.current=events;
            var header=Panel(canvas.transform,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(-450,-235),new Vector2(450,0),Color.clear);
            headerHUD=header.gameObject;
            headerContent=new GameObject("Safe area header content",typeof(RectTransform)).GetComponent<RectTransform>();
            headerContent.SetParent(header,false);headerContent.anchorMin=headerContent.anchorMax=new Vector2(0,1);headerContent.pivot=new Vector2(0,1);headerContent.sizeDelta=new Vector2(900,235);
            header=headerContent;
            IllustratedControl(header,"MENÜ",0,new Vector2(92,-62),new Vector2(110,110),ShowMainMenu);
            pausePlayGraphic=IllustratedControl(header,"DURAKLAT",1,new Vector2(808,-62),new Vector2(110,110),TogglePause);
            var timerCard=new GameObject("Illustrated level and timer",typeof(RectTransform),typeof(RawImage)).GetComponent<RectTransform>();
            timerCard.SetParent(header,false);timerCard.anchorMin=timerCard.anchorMax=new Vector2(0,1);timerCard.anchoredPosition=new Vector2(450,-70);timerCard.sizeDelta=new Vector2(440,176);
            var artwork=timerCard.GetComponent<RawImage>();artwork.texture=Resources.Load<Texture2D>("MenuArt/LevelTimerHUD");artwork.raycastTarget=false;
            levelLabel=Label(timerCard,"",24,new Vector2(220,-27),new Vector2(190,40));levelLabel.fontStyle=FontStyle.Normal;
            timerLabel=Label(timerCard,"",42,new Vector2(220,-88),new Vector2(360,65));timerLabel.fontStyle=FontStyle.Normal;
            foreach(var label in new[]{levelLabel,timerLabel}){label.font=displayFont;var outline=label.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.025f,.12f,.26f,.8f);outline.effectDistance=new Vector2(1,-1);}
            var bar=Panel(timerCard,new Vector2(.15f,.17f),new Vector2(.932f,.285f),Vector2.zero,Vector2.zero,Color.clear);
            timerFill=Panel(bar,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,Palette[2]).GetComponent<Image>();
            timerBarTexture=new Texture2D(64,16,TextureFormat.RGBA32,false){name="Glossy timer fill",wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color[64*16];
            for(int y=0;y<16;y++)for(int x=0;x<64;x++)
            {
                var q=new Vector2(Mathf.Abs(x-31.5f)-24,Mathf.Abs(y-7.5f));
                float distance=new Vector2(Mathf.Max(q.x,0),q.y).magnitude-7.5f;
                float shade=.78f+.22f*y/15f;pixels[y*64+x]=new Color(shade,shade,shade,Mathf.Clamp01(.5f-distance));
            }
            timerBarTexture.SetPixels(pixels);timerBarTexture.Apply();
            timerBarSprite=Sprite.Create(timerBarTexture,new Rect(0,0,64,16),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(8,7,8,7));
            timerFill.sprite=timerBarSprite;timerFill.type=Image.Type.Sliced;
            progressLabel=Label(header,"",23,new Vector2(450,-176),new Vector2(850,45));
            var footer=Panel(canvas.transform,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(-450,0),new Vector2(450,165),Color.clear);
            footerHUD=footer.gameObject;
            statusLabel=Label(footer,"",29,new Vector2(450,-42),new Vector2(850,65));
            statusLabel.font=displayFont;statusLabel.fontStyle=FontStyle.Normal;
            busLabel=Label(footer,"",30,new Vector2(450,-110),new Vector2(850,65));
            busLabel.font=displayFont;busLabel.fontStyle=FontStyle.Normal;
            foreach(var label in new[]{progressLabel,statusLabel,busLabel})
            {
                var outline=label.gameObject.AddComponent<Outline>();
                outline.effectColor=new Color(.025f,.12f,.26f,.85f);outline.effectDistance=new Vector2(1,-1);
            }
            UpdateHUDSafeArea();
        }
        void UpdateHUDSafeArea()
        {
            if(!headerContent||!canvas)return;
            var size=new Vector2(Screen.width,Screen.height);var safe=Screen.safeArea;
            if(size==lastScreenSize&&safe==lastSafeArea)return;
            lastScreenSize=size;lastSafeArea=safe;
            Canvas.ForceUpdateCanvases();
            float topInset=(Screen.height-safe.yMax)/Mathf.Max(1,Screen.height)*canvas.GetComponent<RectTransform>().rect.height;
            PositionGameplayHeader(topInset+18);
        }
        void PositionGameplayHeader(float inset)
        {
            headerContent.anchoredPosition=new Vector2(0,-inset);
            var root=headerHUD.GetComponent<RectTransform>();root.offsetMin=new Vector2(-450,-235-inset);root.offsetMax=new Vector2(450,0);
        }
        RawImage IllustratedControl(Transform parent,string name,int icon,Vector2 position,Vector2 size,UnityEngine.Events.UnityAction action)
        {
            var root=new GameObject(name,typeof(RectTransform),typeof(RawImage),typeof(Button));root.transform.SetParent(parent,false);
            var rect=root.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.anchoredPosition=position;rect.sizeDelta=size;
            var image=root.GetComponent<RawImage>();image.texture=Resources.Load<Texture2D>("MenuArt/GameplayControls");image.uvRect=new Rect(icon/3f,0,1f/3,1);image.raycastTarget=true;
            var button=root.GetComponent<Button>();button.targetGraphic=image;button.navigation=new Navigation{mode=Navigation.Mode.None};button.onClick.AddListener(action);return image;
        }
        void TogglePause()
        {
            if(phase==Phase.Paused)Begin();else if(phase==Phase.Playing)Pause();
        }
        void UpdatePauseControl(bool paused)
        {
            if(pausePlayGraphic)pausePlayGraphic.uvRect=new Rect((paused?2f:1f)/3,0,1f/3,1);
        }
        void ShowMainMenu()
        {
            if(enteringGameplay)return;
            phase=Phase.Start;JellyPassenger.AnimationPaused=true;
            Save();
            headerHUD.SetActive(false);footerHUD.SetActive(false);
            if(world)world.gameObject.SetActive(false);
            CloseOverlay();
            var backdrop=Panel(canvas.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,new Color(.08f,.72f,.91f));
            overlay=backdrop.gameObject;overlay.name="Main Menu";backdrop.GetComponent<Image>().raycastTarget=true;
            int completed=Mathf.Clamp(PlayerPrefs.GetInt(CompletedKey,0),0,levels.Length);
            PlayerPrefs.SetInt(CompletedKey,completed);PlayerPrefs.Save();
            bool progressed=data.people.Count!=level.passengers.Count||data.bus>0||data.seats>0||data.waiting.Count>0||data.time<level.seconds-.05f;
            var menu=overlay.AddComponent<CityMainMenu>();
            menu.Initialize(data.level,levels.Length,completed,level.title,data.time,progressed,()=>EnterFromMenu(null),
                RestartLevel,index=>EnterFromMenu(()=>NewLevel(index)));
        }

        void EnterFromMenu(Action prepare)
        {
            if(enteringGameplay)return;
            enteringGameplay=true;phase=Phase.Transition;JellyPassenger.AnimationPaused=true;
            var host=new GameObject("Menu to gameplay sequence");host.transform.SetParent(canvas.transform,false);
            host.AddComponent<GameplayTransitionRunner>().Play(GameplayEntrance(prepare));
        }
        IEnumerator FadeEntrance(Image shade,float from,float to,float duration)
        {
            float elapsed=0;
            while(elapsed<duration)
            {
                elapsed+=Mathf.Min(Time.unscaledDeltaTime,.05f);float t=Mathf.Clamp01(elapsed/duration);t=t*t*(3-2*t);
                shade.color=new Color(0,0,0,Mathf.Lerp(from,to,t));yield return null;
            }
            shade.color=new Color(0,0,0,to);
        }
        IEnumerator GameplayEntrance(Action prepare)
        {
            var curtain=Panel(canvas.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,Color.clear).GetComponent<Image>();
            transitionCurtain=curtain;
            curtain.name="Gameplay transition curtain";curtain.raycastTarget=true;curtain.transform.SetAsLastSibling();
            yield return FadeEntrance(curtain,0,1,.38f);
            CloseOverlay();
            prepare?.Invoke();
            CloseOverlay();
            phase=Phase.Transition;JellyPassenger.AnimationPaused=true;
            if(world)world.gameObject.SetActive(true);
            headerHUD.SetActive(true);footerHUD.SetActive(true);SetHUDInteraction(false);Refresh();
            var front=bus;var rear=queuedBus;
            Vector3 destination=front?front.position:Vector3.zero,rearDestination=rear?rear.position:Vector3.zero;
            var model=front?front.GetComponent<BusModel>():null;float door=model?model.DoorOpening:0;
            if(model)model.SetDoorOpening(0);
            if(front)front.position=destination+Vector3.left*10;
            if(rear)rear.position=rearDestination+Vector3.left*10;
            curtain.transform.SetAsLastSibling();
            yield return FadeEntrance(curtain,1,0,.42f);
            curtain.raycastTarget=false;
            float elapsed=0;
            while(elapsed<1.05f)
            {
                elapsed+=Mathf.Min(Time.unscaledDeltaTime,.05f);float t=Mathf.Clamp01(elapsed/1.05f);t=1-Mathf.Pow(1-t,3);
                if(front)front.position=destination+Vector3.left*(10*(1-t));
                if(rear)rear.position=rearDestination+Vector3.left*(10*(1-t));
                yield return null;
            }
            if(front)front.position=destination;if(rear)rear.position=rearDestination;
            if(model)model.SetDoorOpening(door);
            curtain.raycastTarget=false;curtain.gameObject.SetActive(false);Destroy(curtain.gameObject);transitionCurtain=null;enteringGameplay=false;Begin();
        }

        void CloseOverlay()
        {
            if(!overlay){overlay=null;return;}
            var old=overlay;overlay=null;restartPromptOpen=false;
            var events=EventSystem.current;
            if(events&&events.currentSelectedGameObject&&events.currentSelectedGameObject.transform.IsChildOf(old.transform))events.SetSelectedGameObject(null);
            // Remove it from hit testing immediately, before Unity's deferred destruction.
            old.SetActive(false);Destroy(old);
        }
        void SetHUDInteraction(bool enabled)
        {
            foreach(var root in new[]{headerHUD,footerHUD})
            {
                var group=root.GetComponent<CanvasGroup>();if(!group)group=root.AddComponent<CanvasGroup>();
                group.interactable=enabled;group.blocksRaycasts=enabled;group.alpha=enabled?1:.6f;
            }
        }

        RectTransform Panel(Transform parent,Vector2 min,Vector2 max,Vector2 low,Vector2 high,Color color)
        {
            var g=new GameObject("Panel",typeof(RectTransform),typeof(Image));
            g.transform.SetParent(parent,false);
            var r=g.GetComponent<RectTransform>();
            r.anchorMin=min;
            r.anchorMax=max;
            r.offsetMin=low;
            r.offsetMax=high;
            g.GetComponent<Image>().color=color;g.GetComponent<Image>().raycastTarget=false;
            return r;
        }
        Text Label(Transform parent,string value,int size,Vector2 pos,Vector2 dimensions)
        {
            var g=new GameObject("Text",typeof(RectTransform),typeof(Text));
            g.transform.SetParent(parent,false);
            var r=g.GetComponent<RectTransform>();
            r.anchorMin=r.anchorMax=new Vector2(0,1);
            r.anchoredPosition=pos;
            r.sizeDelta=dimensions;
            var t=g.GetComponent<Text>();
            t.font=font;
            t.text=value;
            t.fontSize=size;
            t.alignment=TextAnchor.MiddleCenter;
            t.color=Color.white;
            t.raycastTarget=false;
            return t;
        }
        void Button(Transform parent,string text,Vector2 pos,Vector2 size,UnityEngine.Events.UnityAction action)
        {
            var g=new GameObject(text,typeof(RectTransform),typeof(Image),typeof(Button));
            g.transform.SetParent(parent,false);
            var r=g.GetComponent<RectTransform>();
            r.anchorMin=r.anchorMax=new Vector2(0,1);
            r.anchoredPosition=pos;
            r.sizeDelta=size;
            g.GetComponent<Image>().color=new Color(.35f,.32f,.85f);
            var image=g.GetComponent<Image>();image.raycastTarget=true;
            var button=g.GetComponent<Button>();button.targetGraphic=image;
            button.navigation=new Navigation{mode=Navigation.Mode.None};button.onClick.AddListener(action);
            var t=Label(g.transform,text,28,new Vector2(size.x*.5f,-size.y*.5f),size);
        }
        void Modal(string heading,string detail,string primary,UnityEngine.Events.UnityAction action,string secondary=null,UnityEngine.Events.UnityAction second=null)
        {
            CloseOverlay();
            if(heading=="TRY AGAIN"&&Resources.Load<Texture2D>("MenuArt/RetryPopup")){RetryPopup(action);return;}
            if(heading=="PAUSED"&&Resources.Load<Texture2D>("MenuArt/PausePopup")){PausePopup(action,second);return;}
            if(heading=="ALL ABOARD!"&&Resources.Load<Texture2D>("MenuArt/VictoryPopup")){CompletionPopup(primary,action);return;}
            var shade=Panel(canvas.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,new Color(.06f,.07f,.17f,.88f));
            overlay=shade.gameObject;shade.GetComponent<Image>().raycastTarget=true;
            var box=Panel(shade,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-360,-280),new Vector2(360,280),new Color(.17f,.18f,.33f));
            Label(box,heading,48,new Vector2(360,-100),new Vector2(670,110));
            Label(box,detail,25,new Vector2(360,-230),new Vector2(630,140));
            if(primary=="RESUME")IllustratedControl(box,primary,2,new Vector2(360,-370),new Vector2(112,112),action);
            else Button(box,primary,new Vector2(360,-370),new Vector2(590,85),action);
            if(secondary!=null)Button(box,secondary,new Vector2(360,-475),new Vector2(590,70),second);
        }
        IEnumerator PopupAppear(RectTransform card)
        {
            var group=card.gameObject.AddComponent<CanvasGroup>();
            card.localScale=Vector3.one*.78f;group.alpha=0;
            float elapsed=0;const float duration=.34f;
            while(card&&elapsed<duration)
            {
                elapsed+=Mathf.Min(Time.unscaledDeltaTime,.05f);
                float t=Mathf.Clamp01(elapsed/duration),back=t-1;
                float ease=1+2.70158f*back*back*back+1.70158f*back*back;
                card.localScale=Vector3.one*Mathf.LerpUnclamped(.78f,1,ease);
                group.alpha=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.65f));
                yield return null;
            }
            if(card){card.localScale=Vector3.one;group.alpha=1;}
        }
        void PausePopup(UnityEngine.Events.UnityAction resume,UnityEngine.Events.UnityAction restart)
        {
            var shade=Panel(canvas.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,new Color(.025f,.045f,.12f,.78f));
            overlay=shade.gameObject;overlay.name="Illustrated pause popup";shade.GetComponent<Image>().raycastTarget=true;
            var texture=Resources.Load<Texture2D>("MenuArt/PausePopup");float width=720,height=width*texture.height/texture.width;
            var card=new GameObject("Pause card",typeof(RectTransform),typeof(RawImage)).GetComponent<RectTransform>();
            card.SetParent(shade,false);card.anchorMin=card.anchorMax=new Vector2(.5f,.5f);card.sizeDelta=new Vector2(width,height);
            var artwork=card.GetComponent<RawImage>();artwork.texture=texture;artwork.raycastTarget=false;
            void Copy(string text,int size,float y,Color color)
            {
                var label=Label(card,text,size,new Vector2(width*.5f,-height*y),new Vector2(width*.82f,85));label.font=displayFont;label.fontStyle=FontStyle.Normal;label.color=color;
                var outline=label.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.025f,.12f,.26f,.8f);outline.effectDistance=new Vector2(1,-1);
            }
            RectTransform ActionArea(string name,float y,Vector2 size,UnityEngine.Events.UnityAction action,int style=-1)
            {
                var root=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button));root.transform.SetParent(card,false);
                var rect=root.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(0,-height*y);rect.sizeDelta=size;
                var image=root.GetComponent<Image>();image.color=Color.clear;image.raycastTarget=true;
                var button=root.GetComponent<Button>();button.targetGraphic=image;button.transition=Selectable.Transition.None;button.navigation=new Navigation{mode=Navigation.Mode.None};button.onClick.AddListener(action);
                if(style>=0)
                {
                    var face=new GameObject("Illustrated button face",typeof(RectTransform),typeof(RawImage));face.transform.SetParent(rect,false);
                    var faceRect=face.GetComponent<RectTransform>();faceRect.anchorMin=Vector2.zero;faceRect.anchorMax=Vector2.one;faceRect.offsetMin=faceRect.offsetMax=Vector2.zero;
                    var art=face.GetComponent<RawImage>();art.texture=Resources.Load<Texture2D>("MenuArt/MenuPopupButtons");art.raycastTarget=false;
                    art.uvRect=style==1?new Rect(56f/1536,332f/1024,1420f/1536,322f/1024):new Rect(56f/1536,12f/1024,1420f/1536,318f/1024);
                }
                return rect;
            }
            Copy("DURAKLATILDI",52,.37f,Color.white);
            void ButtonCaption(RectTransform button,string text,int size)
            {
                var label=Label(button,text,size,Vector2.zero,button.sizeDelta);
                var rect=label.rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);
                // The atlas includes a lower drop shadow; center the letters on the pill face.
                rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(0,button.sizeDelta.y*.015f);label.font=displayFont;label.fontStyle=FontStyle.Normal;
                label.alignment=TextAnchor.MiddleCenter;label.alignByGeometry=true;
                var outline=label.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.025f,.12f,.26f,.8f);outline.effectDistance=new Vector2(1,-1);
            }
            ActionArea("PAUSE BADGE RESUME",.139f,new Vector2(width*.30f,height*.23f),resume);
            ButtonCaption(ActionArea("RESUME",.59f,new Vector2(width*.64f,height*.136f),resume,1),"DEVAM ET",30);
            ButtonCaption(ActionArea("RESTART LEVEL",.76f,new Vector2(width*.64f,height*.136f),restart,2),"YENİDEN BAŞLAT",30);
            StartCoroutine(PopupAppear(card));
        }
        void RetryPopup(UnityEngine.Events.UnityAction retry)
        {
            var shade=Panel(canvas.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,new Color(.025f,.045f,.12f,.78f));
            overlay=shade.gameObject;overlay.name="Illustrated retry popup";shade.GetComponent<Image>().raycastTarget=true;
            var texture=Resources.Load<Texture2D>("MenuArt/RetryPopup");float width=720,height=width*texture.height/texture.width;
            var card=new GameObject("Retry card",typeof(RectTransform),typeof(RawImage)).GetComponent<RectTransform>();
            card.SetParent(shade,false);card.anchorMin=card.anchorMax=new Vector2(.5f,.5f);card.sizeDelta=new Vector2(width,height);
            var artwork=card.GetComponent<RawImage>();artwork.texture=texture;artwork.raycastTarget=false;
            void Art(string name,string resource,Vector2 center,Vector2 size,Rect uv)
            {
                var rect=new GameObject(name,typeof(RectTransform),typeof(RawImage)).GetComponent<RectTransform>();rect.SetParent(card,false);
                rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.anchoredPosition=center;rect.sizeDelta=size;
                var image=rect.GetComponent<RawImage>();image.texture=Resources.Load<Texture2D>("MenuArt/"+resource);image.uvRect=uv;image.raycastTarget=false;
            }
            void Copy(string text,int size,Vector2 center,Vector2 area)
            {
                var label=Label(card,text,size,center,area);label.font=displayFont;label.fontStyle=FontStyle.Normal;label.alignByGeometry=true;
                var outline=label.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.025f,.12f,.26f,.85f);outline.effectDistance=new Vector2(1,-1);
            }
            Copy(data.time<=0?"SÜRE DOLDU!":"BEKLEME ALANI DOLDU!",36,new Vector2(width*.5f,-height*.39f),new Vector2(width*.80f,95));
            Copy(data.time<=0?"Bir kez daha dene,\nbütün yolcuları otobüse bindir!":"Otobüsün rengine uygun yolcuları\nseçerek bekleme alanını boş tut.",27,new Vector2(width*.5f,-height*.53f),new Vector2(width*.80f,125));
            var buttonSize=new Vector2(width*.64f,height*.136f);var center=new Vector2(width*.5f,-height*.76f);
            var hit=new GameObject("RETRY",typeof(RectTransform),typeof(Image),typeof(Button));hit.transform.SetParent(card,false);
            var hitRect=hit.GetComponent<RectTransform>();hitRect.anchorMin=hitRect.anchorMax=new Vector2(.5f,1);hitRect.pivot=new Vector2(.5f,.5f);hitRect.anchoredPosition=new Vector2(0,center.y);hitRect.sizeDelta=buttonSize;
            var face=new GameObject("Retry button face",typeof(RectTransform),typeof(RawImage)).GetComponent<RectTransform>();face.SetParent(hitRect,false);
            face.anchorMin=Vector2.zero;face.anchorMax=Vector2.one;face.offsetMin=face.offsetMax=Vector2.zero;
            var faceArt=face.GetComponent<RawImage>();faceArt.texture=Resources.Load<Texture2D>("MenuArt/MenuPopupButtons");faceArt.uvRect=new Rect(56f/1536,12f/1024,1420f/1536,318f/1024);faceArt.raycastTarget=false;
            var target=hit.GetComponent<Image>();target.color=Color.clear;target.raycastTarget=true;
            var button=hit.GetComponent<Button>();button.targetGraphic=target;button.transition=Selectable.Transition.None;button.navigation=new Navigation{mode=Navigation.Mode.None};button.onClick.AddListener(retry);
            var caption=Label(hitRect,"YENİDEN DENE",30,Vector2.zero,buttonSize);
            var captionRect=caption.rectTransform;captionRect.anchorMin=captionRect.anchorMax=captionRect.pivot=new Vector2(.5f,.5f);
            captionRect.anchoredPosition=new Vector2(0,buttonSize.y*.04f);caption.font=displayFont;caption.fontStyle=FontStyle.Normal;
            caption.alignment=TextAnchor.MiddleCenter;caption.alignByGeometry=true;
            var captionOutline=caption.gameObject.AddComponent<Outline>();captionOutline.effectColor=new Color(.025f,.12f,.26f,.85f);captionOutline.effectDistance=new Vector2(1,-1);
            StartCoroutine(PopupAppear(card));
        }
        void CompletionPopup(string primary,UnityEngine.Events.UnityAction action)
        {
            var shade=Panel(canvas.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,new Color(.025f,.045f,.12f,.78f));
            overlay=shade.gameObject;overlay.name="Completion popup";shade.GetComponent<Image>().raycastTarget=true;
            var texture=Resources.Load<Texture2D>("MenuArt/VictoryPopup");float width=720,height=width*texture.height/texture.width;
            var card=new GameObject("Illustrated completion card",typeof(RectTransform),typeof(RawImage)).GetComponent<RectTransform>();
            card.SetParent(shade,false);card.anchorMin=card.anchorMax=new Vector2(.5f,.5f);card.sizeDelta=new Vector2(width,height);
            var artwork=card.GetComponent<RawImage>();artwork.texture=texture;artwork.raycastTarget=false;
            Text PopupText(string value,int size,float y,Color color)
            {
                var label=Label(card,value,size,new Vector2(width*.5f,-height*y),new Vector2(width*.80f,100));label.font=displayFont;label.fontStyle=FontStyle.Normal;label.color=color;
                var outline=label.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.025f,.12f,.26f,.8f);outline.effectDistance=new Vector2(1,-1);return label;
            }
            PopupText("HARİKA!",58,.385f,Color.white);
            PopupText("Tüm yolcular yola çıktı.",28,.485f,new Color(.92f,.98f,1));
            PopupText("BÖLÜM "+(data.level+1)+" TAMAMLANDI",32,.635f,new Color(1,.89f,.37f));
            var oval=new GameObject("Oval completion button",typeof(RectTransform),typeof(RawImage)).GetComponent<RectTransform>();
            oval.SetParent(card,false);oval.anchorMin=oval.anchorMax=new Vector2(0,1);oval.anchoredPosition=new Vector2(width*.5f,-height*.825f);oval.sizeDelta=new Vector2(width*.80f,height*.16f);
            var ovalArt=oval.GetComponent<RawImage>();ovalArt.texture=Resources.Load<Texture2D>("MenuArt/MenuPopupButtons");ovalArt.uvRect=new Rect(56f/1536,332f/1024,1420f/1536,322f/1024);ovalArt.raycastTarget=false;
            var hit=new GameObject(primary,typeof(RectTransform),typeof(Image),typeof(Button));hit.transform.SetParent(card,false);
            var rect=hit.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.anchoredPosition=new Vector2(width*.5f,-height*.825f);rect.sizeDelta=new Vector2(width*.80f,height*.16f);
            var image=hit.GetComponent<Image>();image.color=Color.clear;image.raycastTarget=true;
            var button=hit.GetComponent<Button>();button.targetGraphic=image;button.transition=Selectable.Transition.None;button.navigation=new Navigation{mode=Navigation.Mode.None};button.onClick.AddListener(action);
            var caption=PopupText(primary=="NEXT LEVEL"?"SONRAKİ BÖLÜM":"YENİDEN OYNA",31,.825f,Color.white);
            var captionRect=caption.rectTransform;captionRect.SetParent(hit.transform,false);
            captionRect.anchorMin=captionRect.anchorMax=captionRect.pivot=new Vector2(.5f,.5f);
            captionRect.sizeDelta=rect.sizeDelta;captionRect.anchoredPosition=new Vector2(0,rect.sizeDelta.y*.035f);
            caption.alignment=TextAnchor.MiddleCenter;caption.alignByGeometry=true;
            StartCoroutine(PopupAppear(card));
        }
        void Load()
        {
            StopAllCoroutines();
            busy=false;
            transaction=null;
            ClearSelections();
            data=null;
            try
            {
                if(PlayerPrefs.HasKey(SaveKey))data=JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey));
            }
            catch
            {
                data=null;
            }
            if(data==null||data.level<0||data.level>=levels.Length)
            {
                NewLevel(0);
                return;
            }
            if(data.time==-1||(data.revision!=2&&data.revision!=3&&data.revision!=4))
            {
                NewLevel(data.level);
                return;
            }
            level=levels[data.level];
            if(data.revision<4)
            {
                int total=level.passengers.Count;foreach(var gate in level.entrances)total+=gate.colors.Count;
                if(level.entrances.Count>0&&data.people!=null&&data.waiting!=null&&data.people.Count==total&&data.bus==0&&data.seats==0&&data.waiting.Count==0&&!data.timerStarted&&data.time>=level.seconds-.05f){NewLevel(data.level);return;}
                data.entrances=new List<int>();foreach(var gate in level.entrances)data.entrances.Add(gate.colors.Count);
            }
            if(!ValidSnapshot(data,level))
            {
                NewLevel(data.level);
                return;
            }
            if(data.revision==2)
            {
                data.timerStarted=data.bus>0||data.seats>0||data.waiting.Count>0||data.people.Count<level.passengers.Count||data.time<level.seconds-.05f;
                data.revision=3;
            }
            data.revision=4;
            DrawWorld();
            phase=Phase.Start;JellyPassenger.AnimationPaused=true;
            Refresh();
            Modal("BUS JAM","Match passengers to their bus.\nOnly passengers with a clear path can move.","CONTINUE",Begin);
        }
        void NewLevel(int index)
        {
            StopAllCoroutines();
            busy=false;
            transaction=null;
            ClearSelections();
            level=levels[index];awaitingResumeTap=false;
            data=new SaveData
            {
                revision=4,entrances=EntranceRules.Initial(level),timerStarted=false,level=index,time=level.seconds,people=new List<PassengerData>(),waiting=new List<int>()
            }
            ;
            foreach(var p in level.passengers)data.people.Add(new PassengerData(p.x,p.y,p.color));
            DrawWorld();
            phase=Phase.Start;JellyPassenger.AnimationPaused=true;
            Save();
            Refresh();
            Modal("BUS JAM",level.title+"\nTap passengers • Match colors • Fill every bus","TAP TO START",Begin);
        }
        bool restartPromptOpen;
        void RestartLevel()
        {
            if(restartPromptOpen||enteringGameplay)return;
            var origin=phase;var state=LivesService.GetState();
            Action restore=()=>
            {
                restartPromptOpen=false;
                if(origin==Phase.Start)ShowMainMenu();
                else if(origin==Phase.Lost)Modal("TRY AGAIN","","RETRY",RestartLevel);
                else Modal("PAUSED","","RESUME",Begin,"RESTART LEVEL",RestartLevel);
            };
            if(phase==Phase.Playing){phase=Phase.Paused;JellyPassenger.AnimationPaused=true;Save();}
            if(state.count==0)
            {
                LifePopup("HAKKIN KALMADI","Yeni hak için kalan süre:\n"+LivesService.Remaining(state,LivesService.UtcNow),"TAMAM",restore,"ANA MENÜ",()=>{restartPromptOpen=false;ShowMainMenu();});
                restartPromptOpen=true;return;
            }
            LifePopup("YENİDEN BAŞLAT?","Bu işlem 1 hak harcar.\nKalan hak: "+state.count+" → "+(state.count-1),"YENİDEN BAŞLAT",()=>
            {
                restartPromptOpen=false;
                if(!LivesService.TrySpend()){RestartLevel();return;}
                int index=data.level;
                if(origin==Phase.Start)EnterFromMenu(()=>NewLevel(index));
                else{NewLevel(index);Begin();}
            },"VAZGEÇ",restore);
            restartPromptOpen=true;
        }
        void LifePopup(string heading,string detail,string primary,Action accept,string secondary,Action cancel)
        {
            CloseOverlay();
            var shade=Panel(canvas.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,new Color(.025f,.045f,.12f,.80f));
            overlay=shade.gameObject;overlay.name="Restart life confirmation";shade.GetComponent<Image>().raycastTarget=true;
            var card=new GameObject("Life card",typeof(RectTransform),typeof(RawImage)).GetComponent<RectTransform>();card.SetParent(shade,false);card.anchorMin=card.anchorMax=new Vector2(.5f,.5f);card.sizeDelta=new Vector2(720,900);
            var frame=card.GetComponent<RawImage>();frame.texture=Resources.Load<Texture2D>("MenuArt/MenuCategoryPopup");frame.raycastTarget=false;
            Text Copy(string text,int size,float y,Vector2 area,Color color)
            {
                var label=Label(card,text,size,new Vector2(360,-y),area);label.font=displayFont;label.color=color;label.alignByGeometry=true;
                var line=label.gameObject.AddComponent<Outline>();line.effectColor=new Color(.025f,.12f,.26f,.9f);line.effectDistance=new Vector2(1,-1);return label;
            }
            Copy("!",90,126,new Vector2(170,180),new Color(1,.83f,.12f));
            Copy(heading,40,310,new Vector2(630,95),Color.white);Copy(detail,29,440,new Vector2(630,125),Color.white);
            void ActionButton(string text,float y,int style,Action action)
            {
                var rect=new GameObject(text,typeof(RectTransform),typeof(RawImage),typeof(Button)).GetComponent<RectTransform>();rect.SetParent(card,false);rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.anchoredPosition=new Vector2(360,-y);rect.sizeDelta=new Vector2(510,105);
                var art=rect.GetComponent<RawImage>();art.texture=Resources.Load<Texture2D>("MenuArt/MenuPopupButtons");art.uvRect=style==2?new Rect(56f/1536,12f/1024,1420f/1536,318f/1024):new Rect(56f/1536,660f/1024,1420f/1536,319f/1024);
                var button=rect.GetComponent<Button>();button.targetGraphic=art;button.onClick.AddListener(()=>{if(!button.interactable)return;button.interactable=false;action();});
                Copy(text,29,y-2,new Vector2(470,80),Color.white);
            }
            ActionButton(primary,610,2,accept);ActionButton(secondary,755,0,cancel);StartCoroutine(PopupAppear(card));
        }
        void Begin()
        {
            if(enteringGameplay||phase==Phase.Playing||phase==Phase.Won||phase==Phase.Lost)return;
            if(world)world.gameObject.SetActive(true);
            CloseOverlay();
            headerHUD.SetActive(true);
            footerHUD.SetActive(true);SetHUDInteraction(true);
            if(transitionCurtain){transitionCurtain.raycastTarget=false;transitionCurtain.gameObject.SetActive(false);Destroy(transitionCurtain.gameObject);transitionCurtain=null;}
            awaitingResumeTap=data.timerStarted;resumeTapFrame=Time.frameCount+1;
            phase=Phase.Playing;JellyPassenger.AnimationPaused=awaitingResumeTap;UpdatePauseControl(false);Refresh();
            if(data.bus>=level.buses.Count)Finish(true);
            else if(data.time<=0||data.waiting.Count>=level.waitingSlots)Finish(false);
        }
        void Update()
        {
            if(errorHandler&&errorHandler.IsShowing)return;
            UpdateHUDSafeArea();
            JellyPassenger.AnimationPaused=phase!=Phase.Playing||awaitingResumeTap;
            if(data==null)return;
            if(phase==Phase.Playing)
            {
                if(awaitingResumeTap)
                {
                    bool pressed=Pointer.current!=null&&Pointer.current.press.wasPressedThisFrame;
                    bool overUI=false;
                    if(pressed&&EventSystem.current!=null)
                    {
                        var hits=new List<RaycastResult>();
                        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=Pointer.current.position.ReadValue()},hits);overUI=hits.Count>0;
                    }
                    if(Time.frameCount<=resumeTapFrame||!pressed||overUI){Refresh();return;}
                    awaitingResumeTap=false;JellyPassenger.AnimationPaused=false;
                }
                if(data.timerStarted)data.time=Mathf.Max(0,data.time-Time.deltaTime);
                if(Time.unscaledTime>=nextAutoSave)
                {
                    Save();
                    nextAutoSave=Time.unscaledTime+2f;
                }
                Refresh();
                if(data.time<=0)
                {
                    Finish(false);
                    return;
                }
                if(Pointer.current!=null&&Pointer.current.press.wasPressedThisFrame)
                {
                    if(EventSystem.current!=null&&EventSystem.current.IsPointerOverGameObject())return;
                    Ray ray=cam.ScreenPointToRay(Pointer.current.position.ReadValue());
                    if(Physics.Raycast(ray,out var hit))
                    {
                        foreach(var pair in views)if(hit.transform==pair.Value||hit.transform.IsChildOf(pair.Value))
                        {
                            Select(pair.Key);
                            break;
                        }
                    }
                }
            }
        }
        void ClearSelections()
        {
            selectionQueue.Clear();inFlight.Clear();acceptedMoves.Clear();
        }
        void Select(PassengerData p)
        {
            if(phase!=Phase.Playing||awaitingResumeTap||arriving.Contains(p)||!data.people.Contains(p)||!views.TryGetValue(p,out var root))return;
            var projected=Checkpoint();
            if(projected.time<0||projected.bus>=level.buses.Count||projected.waiting.Count>=level.waitingSlots)return;
            var path=PuzzleRules.Path(level,data.people,p);
            if(path==null){statusLabel.text="That passenger needs a clear path.";return;}
            data.timerStarted=true;
            bool startWorker=!busy;
            if(startWorker)
            {
                transaction=JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(data));
                ClearSelections();busy=true;
            }
            var accepted=new PassengerData(p.x,p.y,p.color);
            acceptedMoves.Add(accepted);
            var transit=new PassengerTransit {passenger=accepted,root=root,path=path,order=acceptedMoves.Count-1};
            if(accepted.color!=level.buses[projected.bus])
            {
                for(int slot=0;slot<level.waitingSlots;slot++)
                {
                    if(slot<data.waiting.Count||inFlight.Exists(other=>!other.completed&&other.reservedWaitingSlot==slot))continue;
                    transit.earlyWaiting=true;transit.reservedWaitingSlot=slot;break;
                }
            }
            // Reserve the cell immediately; subsequent taps can use the newly opened route.
            data.people.Remove(p);views.Remove(p);
            foreach(var hit in root.GetComponentsInChildren<Collider>())hit.enabled=false;
            selectionQueue.Enqueue(transit);inFlight.Add(transit);
            StartCoroutine(WalkToBoardingLine(transit));
            if(startWorker)StartCoroutine(ProcessSelections());
            FillEntrances();
            Save();
        }
        bool LeaderBlocks(PassengerTransit transit,Vector3 target)
        {
            foreach(var other in inFlight)
            {
                if(other==transit||other.completed||other.order>=transit.order||!other.root)continue;
                var delta=other.root.position-target;delta.y=0;
                if(delta.sqrMagnitude<.65f*.65f)return true;
            }
            return false;
        }
        IEnumerator WalkStep(PassengerTransit transit,Vector3 target,float duration)
        {
            while(phase!=Phase.Playing||awaitingResumeTap||LeaderBlocks(transit,target))yield return null;
            yield return Move(transit.root,target,duration);
        }
        IEnumerator WalkToBoardingLine(PassengerTransit transit)
        {
            foreach(var cell in transit.path)yield return WalkStep(transit,Cell(cell.x,cell.y),.18f);
            // A small step out of the grid gives immediate visual feedback for every tap.
            yield return WalkStep(transit,new Vector3(transit.root.position.x,0,1.12f),.18f);
            if(transit.earlyWaiting)
            {
                var slot=WaitPosition(transit.reservedWaitingSlot);
                yield return Move(transit.root,new Vector3(transit.root.position.x,0,BoardingCorridorZ),.2f);
                yield return Move(transit.root,new Vector3(slot.x,0,BoardingCorridorZ),.2f);
                yield return Move(transit.root,slot,.2f);
            }
            transit.ready=true;
        }
        IEnumerator ProcessSelections()
        {
            while(selectionQueue.Count>0)
            {
                var transit=selectionQueue.Peek();
                while(!transit.ready||phase!=Phase.Playing||awaitingResumeTap)yield return null;
                var t=transit.root;
                if(!transit.earlyWaiting)yield return Move(t,new Vector3(t.position.x,0,BoardingCorridorZ),.2f);
                if(transit.passenger.color==level.buses[data.bus])
                {
                    yield return BoardThroughCenter(t);Destroy(t.gameObject);data.seats++;
                }
                else
                {
                    data.waiting.Add(transit.passenger.color);waitingViews.Add(t);
                    var slot=WaitPosition(data.waiting.Count-1);
                    if(!transit.earlyWaiting)yield return Move(t,new Vector3(slot.x,0,BoardingCorridorZ),.2f);
                    if((t.position-slot).sqrMagnitude>.001f)yield return Move(t,slot,.2f);
                }
                yield return ResolveBuses();
                transit.completed=true;selectionQueue.Dequeue();inFlight.Remove(transit);
                Save();Refresh();
                if(phase==Phase.Playing&&data.waiting.Count>=level.waitingSlots){Finish(false);yield break;}
                if(phase==Phase.Won||phase==Phase.Lost)break;
            }
            busy=false;transaction=null;ClearSelections();Save();Refresh();
        }
        IEnumerator Move(Transform t,Vector3 target,float duration,Vector3? targetScale=null)
        {
            Vector3 from=t.position;
            Vector3 fromScale=t.localScale;
            float elapsed=0;
            while(elapsed<duration)
            {
                if(phase!=Phase.Playing||awaitingResumeTap)
                {
                    yield return null;
                    continue;
                }
                elapsed+=Time.deltaTime;
                float n=Mathf.Clamp01(elapsed/duration);
                t.position=Vector3.Lerp(from,target,n*n*(3-2*n))+Vector3.up*(Mathf.Sin(n*Mathf.PI)*.08f);
                if(targetScale.HasValue)t.localScale=Vector3.Lerp(fromScale,targetScale.Value,n*n*(3-2*n));
                yield return null;
            }
            t.position=target;
            if(targetScale.HasValue)t.localScale=targetScale.Value;
        }
        IEnumerator ResolveBuses()
        {
            while(data.bus<level.buses.Count)
            {
                for(int i=data.waiting.Count-1;i>=0&&data.seats<level.busCapacity;i--)
                {
                    if(data.waiting[i]!=level.buses[data.bus])continue;
                    var t=waitingViews[i];
                    data.waiting.RemoveAt(i);
                    waitingViews.RemoveAt(i);
                    yield return BoardThroughCenter(t);
                    Destroy(t.gameObject);
                    data.seats++;
                    UpdateBusSeats();
                }
                for(int i=0;i<waitingViews.Count;i++)waitingViews[i].position=WaitPosition(i);
                UpdateBusSeats();
                yield return AnimateBusDoors(false);
                if(data.seats<level.busCapacity)yield break;
                yield return Move(bus,new Vector3(8,0,BusStopZ),.65f);
                Destroy(bus.gameObject);bus=null;
                data.bus++;
                data.seats=0;
                if(data.bus>=level.buses.Count)
                {
                    Finish(true);
                    yield break;
                }
                if(queuedBus&&queuedBusIndex==data.bus)
                {
                    bus=queuedBus;bus.name="Bus "+ColorNames[level.buses[data.bus]];
                    queuedBus=null;queuedBusIndex=-1;
                    UpdateBusSeats();
                    DrawQueuedBus(BusQueueX*2);
                }
                else
                {
                    DrawBus();bus.position=new Vector3(BusQueueX,0,BusStopZ);
                    if(queuedBus)queuedBus.position=new Vector3(BusQueueX*2,0,BusStopZ);
                }
                yield return AdvanceBusQueue();
            }
        }
        void Refresh()
        {
            levelLabel.text=GamePreferences.Translate("BÖLÜM "+(data.level+1));
            timerLabel.text=GamePreferences.Translate("SÜRE  "+TimeSpan.FromSeconds(Mathf.Ceil(data.time)).ToString(@"mm\:ss"));
            timerLabel.color=data.time<20?Palette[0]:Color.white;
            timerFill.rectTransform.anchorMax=new Vector2(Mathf.Clamp01(data.time/level.seconds),1);
            timerFill.color=data.time<20?Palette[0]:Palette[2];
            progressLabel.text="";
            statusLabel.text=GamePreferences.Translate(awaitingResumeTap?(GamePreferences.English?"Tap the screen to continue.":"Devam etmek için ekrana dokun."):selectionQueue.Count>1?"Seçilen yolcular: "+selectionQueue.Count+" • Seçmeye devam edebilirsin":"Tap a passenger with a clear route to the road");
            busLabel.text=GamePreferences.Translate(data.bus<level.buses.Count?ColorNames[level.buses[data.bus]]+" BUS  "+data.seats+"/"+level.busCapacity+"     •     WAITING  "+data.waiting.Count+"/"+level.waitingSlots:"ALL ABOARD!");
        }
        void Pause()
        {
            if(phase!=Phase.Playing)return;
            phase=Phase.Paused;JellyPassenger.AnimationPaused=true;
            Save();
            Modal("PAUSED","Your progress is saved.","RESUME",Begin,"RESTART LEVEL",RestartLevel);
            UpdatePauseControl(true);headerHUD.transform.SetAsLastSibling();
        }
        void Finish(bool won)
        {
            if(phase==Phase.Won||phase==Phase.Lost)return;
            phase=won?Phase.Won:Phase.Lost;JellyPassenger.AnimationPaused=true;
            if(!won)
            {
                StopAllCoroutines();
                busy=false;
            }
            if(won)
            {
                PlayerPrefs.SetInt(CompletedKey,Mathf.Max(PlayerPrefs.GetInt(CompletedKey,0),data.level+1));
                int next=data.level+1;
                PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(new SaveData
                {
                    level=next<levels.Length?next:0,time=-1
                }
                ));
                PlayerPrefs.Save();
                Modal("ALL ABOARD!",next<levels.Length?"Every passenger is on their way.":"All stations completed. Great work!",next<levels.Length?"NEXT LEVEL":"PLAY AGAIN",()=>{NewLevel(next%levels.Length);Begin();});
            }
            else
            {
                PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(new SaveData { level=data.level,time=-1 }));
                PlayerPrefs.Save();
                Modal("TRY AGAIN",data.time<=0?"Time is up. Plan your route and try again.":"The waiting area is full. Match the current bus first.","RETRY",RestartLevel);
            }
        }
        // Persist every accepted tap, including passengers still walking or waiting for the door.
        SaveData Checkpoint()
        {
            if(!busy||transaction==null||acceptedMoves.Count==0)return data;
            var snapshot=JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(transaction));
            snapshot.time=data.time;
            snapshot.timerStarted=data.timerStarted;
            foreach(var passenger in acceptedMoves)
            {
                int index=snapshot.people.FindIndex(p=>p.x==passenger.x&&p.y==passenger.y&&p.color==passenger.color);
                if(index<0)throw new InvalidOperationException("Accepted passenger missing from checkpoint.");
                snapshot.people.RemoveAt(index);
                EntranceRules.Fill(level,snapshot.people,snapshot.entrances);
                if(passenger.color==level.buses[snapshot.bus])snapshot.seats++;
                else snapshot.waiting.Add(passenger.color);
                while(snapshot.bus<level.buses.Count)
                {
                    int color=level.buses[snapshot.bus];
                    while(snapshot.seats<level.busCapacity)
                    {
                        int waiting=snapshot.waiting.FindLastIndex(c=>c==color);
                        if(waiting<0)break;
                        snapshot.waiting.RemoveAt(waiting);snapshot.seats++;
                    }
                    if(snapshot.seats<level.busCapacity)break;
                    snapshot.bus++;snapshot.seats=0;
                }
            }
            if(snapshot.bus>=level.buses.Count)return new SaveData {revision=4,level=(snapshot.level+1)%levels.Length,time=-1};
            return snapshot;
        }
        static bool ValidSnapshot(SaveData save,LevelData definition)
        {
            if(save.people==null||save.waiting==null||float.IsNaN(save.time)||float.IsInfinity(save.time)||save.time<0||
               save.bus<0||save.bus>definition.buses.Count||save.seats<0||save.seats>=definition.busCapacity||save.waiting.Count>definition.waitingSlots)return false;
            if(save.entrances==null||save.entrances.Count!=definition.entrances.Count)return false;
            var counts=new int[Palette.Length];
            foreach(var p in definition.passengers)counts[p.color]++;
            for(int i=0;i<definition.entrances.Count;i++)
            {
                var gate=definition.entrances[i];if(save.entrances[i]<0||save.entrances[i]>gate.colors.Count)return false;
                foreach(int color in gate.colors)counts[color]++;
                for(int j=save.entrances[i];j<gate.colors.Count;j++)counts[gate.colors[j]]--;
            }
            var cells=new HashSet<Vector2Int>(definition.obstacles);
            foreach(var p in save.people)
            {
                if(p==null||p.color<0||p.color>=counts.Length||p.x<0||p.x>=definition.width||p.y<0||p.y>=definition.height||!cells.Add(new Vector2Int(p.x,p.y)))return false;
                if(definition.entrances.Count==0&&!definition.passengers.Exists(original=>original.x==p.x&&original.y==p.y&&original.color==p.color))return false;
                counts[p.color]--;
            }
            foreach(int color in save.waiting){if(color<0||color>=counts.Length)return false;counts[color]--;}
            for(int i=0;i<save.bus;i++)counts[definition.buses[i]]-=definition.busCapacity;
            if(save.bus<definition.buses.Count)counts[definition.buses[save.bus]]-=save.seats;
            else if(save.seats!=0)return false;
            foreach(int count in counts)if(count!=0)return false;
            return true;
        }
        void Save()
        {
            if(data==null||phase==Phase.Won||phase==Phase.Lost)return;
            var snapshot=Checkpoint();
            PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(snapshot));
            PlayerPrefs.Save();
        }
        void OnApplicationFocus(bool focused)
        {
            if(focused||data==null)return;
            if(phase==Phase.Playing)Pause();
            else Save();
        }
        void OnDisable()
        {
            Save();
        }
        void OnApplicationPause(bool paused)
        {
            if(paused)
            {
                Save();
                if(phase==Phase.Playing)Pause();
            }
        }
        void OnApplicationQuit()
        {
            Save();
        }
        void OnDestroy()
        {
            if(timerBarSprite)Destroy(timerBarSprite);if(timerBarTexture)Destroy(timerBarTexture);
            foreach(var m in materials)if(m)Destroy(m);
        }
    }
}
