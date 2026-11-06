using UnityEngine;

namespace LastTide
{
    /// <summary>One of the three parts needed to repair the beacon. Some need a tool to reach.</summary>
    public class BeaconPart : Interactable
    {
        [SerializeField] private ItemType item;
        [SerializeField] private bool needsTool;
        [SerializeField] private ItemType tool;
        [SerializeField] private string lockedPrompt;
        [SerializeField] private string takePrompt;

        public void Configure(ItemType part, bool requiresTool, ItemType requiredTool, string whenLocked, string whenUnlocked)
        {
            item = part;
            needsTool = requiresTool;
            tool = requiredTool;
            lockedPrompt = whenLocked;
            takePrompt = whenUnlocked;
        }

        public override string GetPrompt(Inventory inventory)
        {
            return IsLocked(inventory) ? lockedPrompt : $"[E] {takePrompt}";
        }

        public override string Interact(Inventory inventory)
        {
            if (IsLocked(inventory))
            {
                return lockedPrompt;
            }

            inventory.Add(item);
            GameEvents.Log($"Found the beacon {item.ToString().ToLowerInvariant()}");
            Destroy(gameObject);
            return $"Beacon part found: {item}";
        }

        private bool IsLocked(Inventory inventory)
        {
            return needsTool && !inventory.Has(tool);
        }
    }
}
