using UnityEngine;
namespace Rollic.BusJam
{
    public sealed class JellyPassenger : MonoBehaviour
    {
        public Transform visuals;
        public static bool AnimationPaused;
        Vector3 previousPosition,restPosition;
        float movement,animationTime;
        bool initialized;
        void OnEnable(){previousPosition=transform.position;movement=0;if(visuals&&!initialized){restPosition=visuals.localPosition;initialized=true;}}
        public void ResetPose()
        {
            if(!visuals)return;
            visuals.localPosition=restPosition;visuals.localScale=Vector3.one;visuals.localRotation=Quaternion.identity;
        }
        void LateUpdate()
        {
            if(!visuals)return;
            float speed=(transform.position-previousPosition).magnitude/Mathf.Max(Time.deltaTime,.001f);
            previousPosition=transform.position;
            if(AnimationPaused)return;
            animationTime+=Time.deltaTime;
            movement=Mathf.Lerp(movement,Mathf.Clamp01(speed/3f),1-Mathf.Exp(-Time.deltaTime*12));
            float phase=animationTime*(movement>.1f?12f:2.6f)+transform.position.x*3+transform.position.z;
            float squish=Mathf.Sin(phase)*Mathf.Lerp(.040f,.075f,movement);
            // Deform only the artwork: the passenger's grid cell and collider stay put.
            visuals.localScale=new Vector3(1+squish,1-squish*.9f,1+squish*.45f);
            visuals.localRotation=Quaternion.Euler(0,0,Mathf.Sin(phase*.75f)*Mathf.Lerp(2.2f,4f,movement));
            visuals.localPosition=restPosition+Vector3.up*(Mathf.Sin(phase)*Mathf.Lerp(.015f,.035f,movement));
        }
    }
}
