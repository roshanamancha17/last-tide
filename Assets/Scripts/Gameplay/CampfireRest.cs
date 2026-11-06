namespace LastTide
{
    /// <summary>Lets the player rest at the campfire, or sleep through the night.</summary>
    public class CampfireRest : Interactable
    {
        public override string GetPrompt(Inventory inventory)
        {
            SurvivalStats stats = inventory.GetComponent<SurvivalStats>();
            if (stats == null)
            {
                return null;
            }

            return stats.IsNight ? "[E] Sleep until morning" : "[E] Rest by the fire";
        }

        public override string Interact(Inventory inventory)
        {
            SurvivalStats stats = inventory.GetComponent<SurvivalStats>();
            if (stats != null)
            {
                stats.Rest();
            }
            return null;
        }
    }
}
