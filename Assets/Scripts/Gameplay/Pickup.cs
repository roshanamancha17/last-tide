using UnityEngine;

namespace LastTide
{
    /// <summary>A loose item in the world. Vines can require a knife.</summary>
    public class Pickup : Interactable
    {
        [SerializeField] private ItemType item;
        [SerializeField] private bool needsKnife;

        public ItemType Item => item;

        public void Configure(ItemType newItem, bool requiresKnife)
        {
            item = newItem;
            needsKnife = requiresKnife;
        }

        public override string GetPrompt(Inventory inventory)
        {
            if (needsKnife && !inventory.Has(ItemType.Knife))
            {
                return $"{item}: needs a knife to cut";
            }

            return $"[E] {(needsKnife ? "Cut" : "Pick up")} {item}";
        }

        public override string Interact(Inventory inventory)
        {
            if (needsKnife && !inventory.Has(ItemType.Knife))
            {
                return "You need a knife to cut this.";
            }

            inventory.Add(item);
            GameEvents.Log($"Collected {item}");
            Destroy(gameObject);
            return $"+1 {item}";
        }
    }
}
