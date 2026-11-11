using System;using System.Collections.Generic;using System.Linq;using UnityEngine;
namespace Rollic.BusJam.Editor
{
 public sealed class EntranceLevelSolver
 {
  LevelData level;int limit,budget;public int Visited;public bool LimitReached;HashSet<string> failed=new HashSet<string>();
  public bool Solve(LevelData source,int waitingLimit,out List<Vector2Int> solution,int nodeBudget)
  {
   level=source;limit=waitingLimit;budget=nodeBudget;Visited=0;LimitReached=false;failed.Clear();
   return Search(new List<PassengerData>(level.passengers),EntranceRules.Initial(level),0,0,new int[6],out solution);
  }
  bool Search(List<PassengerData> people,List<int> gates,int bus,int seats,int[] waiting,out List<Vector2Int> solution)
  {
   solution=null;if(++Visited>budget){LimitReached=true;return false;}
   var prefix=new List<Vector2Int>();
   while(bus<level.buses.Count)
   {
    int color=level.buses[bus],take=Math.Min(level.busCapacity-seats,waiting[color]);waiting[color]-=take;seats+=take;
    if(seats==level.busCapacity){bus++;seats=0;continue;}
    var matching=people.Find(p=>p.color==color&&PuzzleRules.Path(level,people,p)!=null);
    if(matching==null)break;
    people.Remove(matching);prefix.Add(new Vector2Int(matching.x,matching.y));seats++;EntranceRules.Fill(level,people,gates);
   }
   if(bus==level.buses.Count){solution=prefix;return people.Count==0&&level.entrances.Select((g,i)=>g.colors.Count==gates[i]).All(v=>v);}
   string key=bus+":"+seats+":"+string.Join(",",waiting)+":"+string.Join(",",gates)+":"+string.Join(";",people.OrderBy(p=>p.y*level.width+p.x).Select(p=>p.x+","+p.y+","+p.color));
   if(failed.Contains(key)||waiting.Sum()>=limit)return false;
   var choices=people.FindAll(p=>PuzzleRules.Path(level,people,p)!=null);choices.Sort((a,b)=>level.buses.FindIndex(bus+1,c=>c==a.color).CompareTo(level.buses.FindIndex(bus+1,c=>c==b.color)));
   foreach(var p in choices)
   {
    var nextPeople=new List<PassengerData>(people);nextPeople.Remove(p);var nextGates=new List<int>(gates);EntranceRules.Fill(level,nextPeople,nextGates);var nextWaiting=(int[])waiting.Clone();nextWaiting[p.color]++;
    if(Search(nextPeople,nextGates,bus,seats,nextWaiting,out var tail)){prefix.Add(new Vector2Int(p.x,p.y));prefix.AddRange(tail);solution=prefix;return true;}
    if(LimitReached)return false;
   }
   failed.Add(key);return false;
  }
 }
}
