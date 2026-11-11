using System;
using System.Collections.Generic;
using UnityEngine;
namespace Rollic.BusJam
{
    [Serializable] public class PassengerData
    {
        public int x,y,color;
        public PassengerData(int x,int y,int c)
        {
            this.x=x;
            this.y=y;
            color=c;
        }
    }
    [CreateAssetMenu(menuName="Bus Jam/Level")] public class LevelData:ScriptableObject
    {
        public const int DefaultLevelCount=10;
        public string title="Morning commute";
        public int width=5,height=5;
        public float seconds=120;
        public int waitingSlots=6;
        public int busCapacity=3;
        public List<PassengerData> passengers=new List<PassengerData>();
        public List<EntranceData> entrances=new List<EntranceData>();
        public List<int> buses=new List<int>();
        public List<Vector2Int> obstacles=new List<Vector2Int>();
        public string ValidateLevel()
        {
            if(width<2||width>8||height<2||height>9||seconds<=0||waitingSlots<1||busCapacity<1)return "Invalid dimensions, timer or capacity.";
            var cells=new HashSet<Vector2Int>();
            var counts=new int[6];
            foreach(var o in obstacles)if(o.x<0||o.x>=width||o.y<0||o.y>=height||!cells.Add(o))return "Invalid obstacle.";
            foreach(var p in passengers)
            {
                if(p.x<0||p.x>=width||p.y<0||p.y>=height||p.color<0||p.color>=6||!cells.Add(new Vector2Int(p.x,p.y)))return "Invalid or overlapping passenger.";
                counts[p.color]++;
            }
            var gateColumns=new HashSet<int>();
            foreach(var gate in entrances)
            {
                if(gate==null||gate.column<0||gate.column>=width||!gateColumns.Add(gate.column)||obstacles.Contains(new Vector2Int(gate.column,height-1)))return "Invalid entrance column.";
                foreach(int color in gate.colors){if(color<0||color>=6)return "Invalid entrance color.";counts[color]++;}
            }
            foreach(int c in buses)
            {
                if(c<0||c>=6)return "Invalid bus color.";
                counts[c]-=busCapacity;
            }
            foreach(int n in counts)if(n!=0)return "Passenger counts must match bus capacities by color.";
            return passengers.Count<15?"Each level must start with at least 15 passengers.":null;
        }
        public static LevelData CreateDefault(int index)
        {
            index=((index%DefaultLevelCount)+DefaultLevelCount)%DefaultLevelCount;
            var level=CreateInstance<LevelData>();
            string[] titles={"First departure","Color shuffle","The courtyard","Rush hour","Narrow passage","Last connection","Garden crossing","Twin corridors","City junction","Grand terminal"};
            int[] widths={5,5,5,6,6,6,6,7,7,7},heights={4,4,5,5,6,6,6,6,6,6};
            float[] timers={110f,120f,135f,145f,155f,165f,175f,185f,195f,205f};
            string[] layouts={"210021010001211.....","011201212220010102..","032021022303121013311032.","120110100003130233323122020...","443404230430232#213221300110141.....","343013341142120#4201#23403122040....","353421313312411#2504#2100#2000325...","51343132102122344#52004#31050#041#243.....","33204240435132202#54150#42015#533#105411..","24552354000332145#04221#40313#015#151342.."};
            string[] busOrders={"01201","012012","01230123","012301230","0123401234","0123401234","0123450123","01234501234","012345012345","012345012345"};
            level.title=titles[index];level.width=widths[index];level.height=heights[index];level.seconds=timers[index];
            level.waitingSlots=6;level.busCapacity=3;
            for(int i=0;i<layouts[index].Length;i++)
            {
                char cell=layouts[index][i];int x=i%level.width,y=i/level.width;
                if(cell=='#')level.obstacles.Add(new Vector2Int(x,y));
                else if(cell>='0'&&cell<='5')level.passengers.Add(new PassengerData(x,y,cell-'0'));
            }
            if(index>=4)
            {
                foreach(int column in new[]{0,level.width/2,level.width-1})
                {
                    var hidden=level.passengers.FindAll(p=>p.x==column);hidden.Sort((a,b)=>b.y.CompareTo(a.y));
                    if(hidden.Count<2)continue;
                    var gate=new EntranceData{column=column};
                    for(int i=0;i<2;i++){gate.colors.Add(hidden[i].color);level.passengers.Remove(hidden[i]);}
                    level.entrances.Add(gate);
                }
            }
            foreach(char color in busOrders[index])level.buses.Add(color-'0');
            return level;
        }
    }
}
