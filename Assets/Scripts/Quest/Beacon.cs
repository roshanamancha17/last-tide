using UnityEngine;

namespace LastTide
{
    /// <summary>The signal beacon on the peak. Lighting it starts the rescue ending.</summary>
    public class Beacon : Interactable
    {
        [SerializeField] private Renderer lamp;
        [SerializeField] private Material litMaterial;
        [SerializeField] private Light beaconLight;
        [SerializeField] private QuestManager quest;

        private bool lit;

        public void Configure(Renderer lampRenderer, Material whenLit, Light light, QuestManager manager)
        {
            lamp = lampRenderer;
            litMaterial = whenLit;
            beaconLight = light;
            quest = manager;
        }

        public override string GetPrompt(Inventory inventory)
        {
            if (lit)
            {
                return null;
            }

            int found = quest.PartsFound(inventory);
            return found < QuestManager.PartsNeeded
                ? $"The beacon is broken: {found} of {QuestManager.PartsNeeded} parts found"
                : "[E] Repair and light the beacon";
        }

        public override string Interact(Inventory inventory)
        {
            if (lit || quest.PartsFound(inventory) < QuestManager.PartsNeeded)
            {
                return "The beacon still needs parts.";
            }

            lit = true;
            inventory.Remove(ItemType.Lens);
            inventory.Remove(ItemType.Battery);
            inventory.Remove(ItemType.Antenna);
            lamp.sharedMaterial = litMaterial;
            beaconLight.enabled = true;
            GameEvents.Log("Lit the signal beacon");
            quest.BeginEnding(transform);
            return "The beacon is lit!";
        }
    }
}
