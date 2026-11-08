using UnityEngine;

namespace LastTide
{
    /// <summary>A palm trunk that gives wood when chopped with the axe.</summary>
    public class ChoppableTree : Interactable
    {
        [SerializeField] private int chopsLeft = 3;
        [SerializeField] private int woodPerChop = 2;

        public override float PriorityPenalty => 1.5f;

        public int ChopsLeft
        {
            get => chopsLeft;
            set => chopsLeft = Mathf.Max(0, value);
        }

        public override string GetPrompt(Inventory inventory)
        {
            if (!inventory.Has(ItemType.Axe) || chopsLeft <= 0)
            {
                return null;
            }

            return "[E] Chop palm";
        }

        public override string Interact(Inventory inventory)
        {
            if (!inventory.Has(ItemType.Axe) || chopsLeft <= 0)
            {
                return null;
            }

            chopsLeft--;
            GameEvents.Log("Chopped a palm");
            inventory.Add(ItemType.Wood, woodPerChop);
            return $"+{woodPerChop} Wood";
        }
    }
}
