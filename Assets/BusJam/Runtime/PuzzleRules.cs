using System.Collections.Generic;
using UnityEngine;
namespace Rollic.BusJam
{
    public static class PuzzleRules
    {
        // Row zero borders the bus lane. Paths use only vacant orthogonal cells.
        public static List<Vector2Int> Path(LevelData level,List<PassengerData> people,PassengerData person)
        {
            var start=new Vector2Int(person.x,person.y);
            var queue=new Queue<Vector2Int>();
            var prev=new Dictionary<Vector2Int,Vector2Int>();
            var blocked=new HashSet<Vector2Int>(level.obstacles);
            foreach(var p in people)if(p!=person)blocked.Add(new Vector2Int(p.x,p.y));
            queue.Enqueue(start);
            prev[start]=start;
            while(queue.Count>0)
            {
                var v=queue.Dequeue();
                if(v.y==0)
                {
                    var path=new List<Vector2Int>();
                    while(v!=start)
                    {
                        path.Add(v);
                        v=prev[v];
                    }
                    path.Reverse();
                    return path;
                }
                foreach(var d in new[]
                {
                    Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right
                }
                )
                {
                    var next=v+d;
                    if(next.x<0||next.x>=level.width||next.y<0||next.y>=level.height||blocked.Contains(next)||prev.ContainsKey(next))continue;
                    prev[next]=v;
                    queue.Enqueue(next);
                }
            }
            return null;
        }
    }
}
