using System;using System.Collections.Generic;using UnityEngine;
namespace Rollic.BusJam
{
 [Serializable] public class EntranceData {public int column;public List<int> colors=new List<int>();}
 public static class EntranceRules
 {
  public static List<int> Initial(LevelData level){var result=new List<int>();foreach(var gate in level.entrances)result.Add(0);return result;}
  public static Vector2Int EntryCell(LevelData level,int column)
  {
   int row=-1;foreach(var p in level.passengers)if(p.x==column)row=Math.Max(row,p.y);
   return new Vector2Int(column,Math.Max(0,row));
  }
  public static List<Vector2Int> EntryPath(LevelData level,List<PassengerData> people,Vector2Int target,PassengerData entering=null)
  {
   var start=new Vector2Int(target.x,level.height-1);var blocked=new HashSet<Vector2Int>(level.obstacles);
   foreach(var p in people)if(p!=entering)blocked.Add(new Vector2Int(p.x,p.y));
   if(blocked.Contains(start)||blocked.Contains(target))return null;
   var queue=new Queue<Vector2Int>();var prev=new Dictionary<Vector2Int,Vector2Int>();queue.Enqueue(start);prev[start]=start;
   while(queue.Count>0)
   {
    var cell=queue.Dequeue();if(cell==target){var path=new List<Vector2Int>{cell};while(cell!=start){cell=prev[cell];path.Add(cell);}path.Reverse();return path;}
    foreach(var dir in new[]{Vector2Int.up,Vector2Int.left,Vector2Int.right,Vector2Int.down}){var next=cell+dir;if(next.x<0||next.x>=level.width||next.y<0||next.y>=level.height||blocked.Contains(next)||prev.ContainsKey(next))continue;prev[next]=cell;queue.Enqueue(next);}
   }return null;
  }
  public static List<PassengerData> Fill(LevelData level,List<PassengerData> people,List<int> offsets)
  {
   var added=new List<PassengerData>();for(int i=0;i<level.entrances.Count;i++)
   {
    var gate=level.entrances[i];var cell=EntryCell(level,gate.column);
    if(offsets[i]>=gate.colors.Count||people.Exists(p=>p.x==gate.column&&p.y>=cell.y)||EntryPath(level,people,cell)==null)continue;
    var passenger=new PassengerData(cell.x,cell.y,gate.colors[offsets[i]++]);people.Add(passenger);added.Add(passenger);
   }return added;
  }
}
}
