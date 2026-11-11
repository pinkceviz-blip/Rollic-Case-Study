using System;using System.Collections.Generic;using UnityEngine;using BigInteger=System.Numerics.BigInteger;
namespace Rollic.BusJam.Editor
{
    // Exact bounded search. Taking a reachable matching rider is always safe;
    // only off-color choices consume buffer slots and require branching.
    public sealed class LevelSolver
    {
        LevelData level;BigInteger full,blocked,top,left,right;readonly BigInteger[] colors=new BigInteger[6];
        readonly HashSet<string> failed=new HashSet<string>();int maxWaiting,budget;
        public int Visited {get;private set;}public bool LimitReached {get;private set;}
        public bool Solve(LevelData source,int waitingLimit,out List<Vector2Int> solution,int nodeBudget=200000)
        {
            if(source.entrances.Count>0)
            {
                var entrances=new EntranceLevelSolver();bool solved=entrances.Solve(source,waitingLimit,out solution,nodeBudget);Visited=entrances.Visited;LimitReached=entrances.LimitReached;return solved;
            }
            level=source;maxWaiting=waitingLimit;budget=nodeBudget;Visited=0;LimitReached=false;failed.Clear();Array.Clear(colors,0,colors.Length);
            int size=level.width*level.height;full=(BigInteger.One<<size)-1;top=(BigInteger.One<<level.width)-1;left=0;blocked=0;
            for(int y=0;y<level.height;y++)left|=BigInteger.One<<(y*level.width);right=left<<(level.width-1);
            foreach(var p in level.obstacles)blocked|=BigInteger.One<<(p.y*level.width+p.x);
            BigInteger mask=0;foreach(var p in level.passengers){var bit=BigInteger.One<<(p.y*level.width+p.x);mask|=bit;colors[p.color]|=bit;}
            try{return Search(mask,0,0,new int[6],out solution);}catch(SearchLimit){LimitReached=true;solution=null;return false;}
        }
        sealed class SearchLimit:Exception{}
        BigInteger Neighbors(BigInteger value)=>(((value&~right)<<1)|((value&~left)>>1)|(value<<level.width)|(value>>level.width))&full;
        BigInteger Accessible(BigInteger mask)
        {
            var empty=full&~(mask|blocked);var reached=empty&top;
            while(true){var next=reached|(Neighbors(reached)&empty);if(next==reached)return (Neighbors(reached)|top)&mask;reached=next;}
        }
        int CellIndex(BigInteger bit){int i=0;while(bit>1){bit>>=1;i++;}return i;}
        struct Choice {public int index,color,nextBus;public BigInteger bit;}
        bool Search(BigInteger mask,int bus,int seats,int[] waiting,out List<Vector2Int> solution)
        {
            if(++Visited>budget)throw new SearchLimit();
            var prefix=new List<Vector2Int>();waiting=(int[])waiting.Clone();
            while(bus<level.buses.Count)
            {
                int color=level.buses[bus],take=Math.Min(level.busCapacity-seats,waiting[color]);seats+=take;waiting[color]-=take;
                if(seats==level.busCapacity){bus++;seats=0;continue;}
                var available=Accessible(mask)&colors[color];if(available.IsZero)break;
                var bit=available&-available;mask^=bit;seats++;int index=CellIndex(bit);prefix.Add(new Vector2Int(index%level.width,index/level.width));
            }
            if(bus==level.buses.Count){solution=prefix;return true;}
            string key=mask.ToString("X")+":"+bus+":"+seats+":"+string.Join(",",waiting);
            solution=null;if(failed.Contains(key))return false;
            int count=0;foreach(int n in waiting)count+=n;if(count>=maxWaiting){failed.Add(key);return false;}
            var candidates=new List<Choice>();var accessible=Accessible(mask);
            while(!accessible.IsZero)
            {
                var bit=accessible&-accessible;accessible^=bit;int index=CellIndex(bit),color=0;while((colors[color]&bit).IsZero)color++;
                int next=level.buses.FindIndex(bus+1,c=>c==color);candidates.Add(new Choice{index=index,color=color,nextBus=next<0?int.MaxValue:next,bit=bit});
            }
            candidates.Sort((a,b)=>a.nextBus!=b.nextBus?a.nextBus.CompareTo(b.nextBus):a.index.CompareTo(b.index));
            foreach(var candidate in candidates)
            {
                var next=(int[])waiting.Clone();next[candidate.color]++;
                if(Search(mask^candidate.bit,bus,seats,next,out var tail))
                {
                    prefix.Add(new Vector2Int(candidate.index%level.width,candidate.index/level.width));prefix.AddRange(tail);solution=prefix;return true;
                }
            }
            failed.Add(key);return false;
        }
    }
}
