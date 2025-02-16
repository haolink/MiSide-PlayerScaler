using System;
using UnityEngine;
using UObject = UnityEngine.Object;

public class UnityObjectDictionary<TKey, TObject> : Dictionary<TKey, TObject> where TKey : UObject
{    
    private class UnityObjectCompare : IEqualityComparer<TKey>
    {
        public bool Equals(TKey? x, TKey? y)
        {
            if (x == null || y == null) return false;
            return x.GetInstanceID() == y.GetInstanceID();
        }

        public int GetHashCode(TKey obj)
        {
            return obj.GetInstanceID();
        }
    }
    public UnityObjectDictionary() : base(new UnityObjectCompare()) {         
    }

    public void RemoveDestroyed()
    {
        foreach (TKey key in this.Keys.ToList())
        {
            if (key.WasCollected)
            {
                Remove(key);
            }
        }
    }    
}

