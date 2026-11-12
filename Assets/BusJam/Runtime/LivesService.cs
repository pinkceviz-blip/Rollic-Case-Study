using System;using System.Globalization;using UnityEngine;
namespace Rollic.BusJam
{
    public static class LivesService
    {
        public const int Maximum=10;
        public const long RechargeSeconds=4*60*60;
        const string CountKey="Rollic.BusJam.Lives.Count",NextKey="Rollic.BusJam.Lives.NextUtc",IntervalKey="Rollic.BusJam.Lives.IntervalSeconds";
        public struct State {public int count;public long nextUtc;}
        public static long UtcNow=>DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        public static State GetState()=>Refresh(UtcNow);
        // Explicit UTC makes offline recharge deterministic and independently testable.
        public static State Refresh(long now)
        {
            if(PlayerPrefs.GetInt("Rollic.BusJam.Lives.ReleaseTopUp.20261112",0)==0)
            {
                PlayerPrefs.SetInt("Rollic.BusJam.Lives.ReleaseTopUp.20261112",1);Store(Maximum,0);
            }
            int stored=PlayerPrefs.GetInt(CountKey,Maximum),count=Mathf.Clamp(stored,0,Maximum);
            long.TryParse(PlayerPrefs.GetString(NextKey,"0"),NumberStyles.Integer,CultureInfo.InvariantCulture,out long next);
            long oldNext=next;
            long previousInterval=PlayerPrefs.GetInt(IntervalKey,5*60*60);
            bool migrate=previousInterval!=RechargeSeconds;
            if(migrate&&next>0)next=Math.Max(1,next+RechargeSeconds-previousInterval);
            if(count==Maximum)next=0;
            else
            {
                if(next<=0)next=now+RechargeSeconds;
                if(now>=next)
                {
                    long grants=(now-next)/RechargeSeconds+1;
                    count=(int)Math.Min(Maximum,count+grants);
                    next=count==Maximum?0:next+grants*RechargeSeconds;
                }
            }
            if(!PlayerPrefs.HasKey(CountKey)||migrate||count!=stored||next!=oldNext)Store(count,next);
            return new State{count=count,nextUtc=next};
        }
        public static bool TrySpend()=>TrySpendAt(UtcNow);
        public static bool TrySpendAt(long now)
        {
            var state=Refresh(now);if(state.count==0)return false;
            Store(state.count-1,state.count==Maximum?now+RechargeSeconds:state.nextUtc);return true;
        }
#if UNITY_EDITOR
        // Editor-only override; preserve an existing partial recharge deadline.
        public static void SetForTesting(int count)
        {
            var now=UtcNow;var state=Refresh(now);count=Mathf.Clamp(count,0,Maximum);
            Store(count,count==Maximum?0:state.nextUtc>now?state.nextUtc:now+RechargeSeconds);
        }
#endif
        static void Store(int count,long next){PlayerPrefs.SetInt(IntervalKey,(int)RechargeSeconds);PlayerPrefs.SetInt(CountKey,count);PlayerPrefs.SetString(NextKey,next.ToString(CultureInfo.InvariantCulture));PlayerPrefs.Save();}
        public static string Remaining(State state,long now)
        {
            var time=TimeSpan.FromSeconds(Math.Max(0,state.nextUtc-now));return ((int)time.TotalHours).ToString("00")+":"+time.Minutes.ToString("00")+":"+time.Seconds.ToString("00");
        }
    }
}
