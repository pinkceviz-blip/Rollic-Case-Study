using UnityEngine;
using System.Collections.Generic;
using System.Collections;
namespace Rollic.BusJam
{
    /// <summary>Shared visual controls for the six bus prefabs. Origin is ground level; front is +X.</summary>
    public sealed class BusModel : MonoBehaviour
    {
        public Renderer[] seatIndicators;
        public Material emptySeatMaterial;
        public Material occupiedSeatMaterial;

        public Transform doorRoot, leftDoor, rightDoor, boardingPoint, interiorPoint;
        public Vector3 leftDoorClosed, rightDoorClosed;
        public float doorTravel=.59f;
        public float DoorOpening { get; private set; }
        public bool HasDoors=>leftDoor&&rightDoor;

        void Awake() { SetDoorOpening(0); }
        public void SetDoorOpening(float value)
        {
            DoorOpening=Mathf.Clamp01(value);
            if(!HasDoors)return;
            float outward=-.055f*Mathf.Sin(DoorOpening*Mathf.PI*.5f);
            leftDoor.localPosition=leftDoorClosed+new Vector3(-doorTravel*DoorOpening,0,outward);
            rightDoor.localPosition=rightDoorClosed+new Vector3(doorTravel*DoorOpening,0,outward);
        }

        readonly List<Transform> seatedPassengers=new List<Transform>();
        int passengerColor=-1;
        bool occupancyInitialized;
        Material sparkleMaterial;
        public int VisiblePassengerCount=>seatedPassengers.Count;
        public void SetOccupancy(int passengers, int capacity, int color=-1)
        {
            if(capacity<=0)return;
            if(color>=0&&color!=passengerColor)
            {
                foreach(var seated in seatedPassengers)if(seated)Destroy(seated.gameObject);
                seatedPassengers.Clear();passengerColor=color;
            }
            if(passengerColor<0)
            {
                string[] names={"CORAL","BLUE","GREEN","GOLD","VIOLET","PINK"};
                for(int i=0;i<names.Length;i++)if(name.Contains(names[i])){passengerColor=i;break;}
            }
            int count=Mathf.Clamp(passengers,0,capacity);
            while(seatedPassengers.Count>count)
            {
                int last=seatedPassengers.Count-1;if(seatedPassengers[last])Destroy(seatedPassengers[last].gameObject);seatedPassengers.RemoveAt(last);
            }
            string[] colors={"CORAL","BLUE","GREEN","GOLD","VIOLET","PINK"};
            var prefab=Resources.Load<GameObject>("JellyPassengers/SeatedJelly_"+colors[Mathf.Clamp(passengerColor,0,5)]);
            while(prefab&&seatedPassengers.Count<count)
            {
                int index=seatedPassengers.Count,column=index%3,row=index/3,rows=Mathf.CeilToInt(capacity/3f);
                float scale=capacity<=3?.82f:.58f;
                var rider=Instantiate(prefab,transform).transform;rider.name="Seated passenger "+(index+1);
                rider.localPosition=new Vector3(-1.42f+column*.98f,1.615f-.4f*scale,(row-(rows-1)*.5f)*.36f);
                rider.localRotation=Quaternion.Euler(0,-50,0);rider.localScale=Vector3.one*scale;
                seatedPassengers.Add(rider);
                if(occupancyInitialized&&Application.isPlaying)StartCoroutine(SeatCelebration(rider,scale));
            }
            occupancyInitialized=true;
            if (seatIndicators == null) return;
            for (int i = 0; i < seatIndicators.Length; i++)
            {
                bool occupied = passengers >= Mathf.CeilToInt((i + 1f) * capacity / seatIndicators.Length);
                if (seatIndicators[i]) seatIndicators[i].sharedMaterial = occupied ? occupiedSeatMaterial : emptySeatMaterial;
            }
        }
        IEnumerator SeatCelebration(Transform rider,float size)
        {
            if(!sparkleMaterial)
            {
                sparkleMaterial=new Material(Resources.Load<Shader>("BusJam/Cartoon")??Resources.Load<Shader>("BusJam/Station"));
                sparkleMaterial.color=new Color(1,.92f,.45f);
            }
            var burst=new GameObject("Seat celebration blink").transform;burst.SetParent(transform,false);
            burst.localPosition=rider.localPosition+Vector3.up*(size*.75f);
            var sparks=new Transform[9];
            for(int i=0;i<sparks.Length;i++)
            {
                var spark=GameObject.CreatePrimitive(PrimitiveType.Sphere);spark.name=i==8?"Seat flash":"Celebration sparkle";spark.layer=2;
                Destroy(spark.GetComponent<Collider>());spark.transform.SetParent(burst,false);
                spark.GetComponent<Renderer>().sharedMaterial=sparkleMaterial;sparks[i]=spark.transform;
            }
            float elapsed=0;const float duration=.42f;
            while(rider&&burst&&elapsed<duration)
            {
                if(JellyPassenger.AnimationPaused){yield return null;continue;}
                elapsed+=Time.deltaTime;float t=Mathf.Clamp01(elapsed/duration);
                float pop=t<.4f?Mathf.Lerp(.78f,1.16f,t/.4f):Mathf.Lerp(1.16f,1,(t-.4f)/.6f);
                rider.localScale=Vector3.one*(size*pop);
                for(int i=0;i<8;i++)
                {
                    float angle=i*Mathf.PI*.25f;
                    sparks[i].localPosition=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle)*.7f+.2f,-.28f)*(size*(.15f+t*.85f));
                    sparks[i].localScale=Vector3.one*(size*.16f*(1-t));
                }
                sparks[8].localScale=Vector3.one*(size*.36f*Mathf.Max(0,1-t*3));
                yield return null;
            }
            if(rider)rider.localScale=Vector3.one*size;
            if(burst)Destroy(burst.gameObject);
        }
        void OnDestroy(){if(sparkleMaterial)Destroy(sparkleMaterial);}
    }
}
