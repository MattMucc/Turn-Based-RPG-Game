using System;
using System.Collections.Generic;
namespace Game.GridPrototype
{
    public enum EncounterKind { Roaming, OneOff, Story }
    public static class EncounterWeights
    {
        public static int Pick(IReadOnlyList<int> weights,double roll)
        {
            double total=0;for(int i=0;i<weights.Count;i++)total+=Math.Max(0,weights[i]);
            if(total<=0)return -1;double value=Math.Max(0,Math.Min(.999999999,roll))*total;
            for(int i=0;i<weights.Count;i++){value-=Math.Max(0,weights[i]);if(value<0)return i;}return -1;
        }
    }
}
