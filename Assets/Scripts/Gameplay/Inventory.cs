using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastTide
{
    /// <summary>Counts of the items the player is carrying.</summary>
    public class Inventory : MonoBehaviour
    {
        private readonly Dictionary<ItemType, int> counts = new Dictionary<ItemType, int>();

        public event Action Changed;

        public int Count(ItemType item)
        {
            return counts.TryGetValue(item, out int count) ? count : 0;
        }

        public bool Has(ItemType item, int amount = 1)
        {
            return Count(item) >= amount;
        }

        public void Add(ItemType item, int amount = 1)
        {
            counts[item] = Count(item) + amount;
            Changed?.Invoke();
        }

        public bool Remove(ItemType item, int amount = 1)
        {
            if (!Has(item, amount))
            {
                return false;
            }

            counts[item] = Count(item) - amount;
            Changed?.Invoke();
            return true;
        }
    }
}
