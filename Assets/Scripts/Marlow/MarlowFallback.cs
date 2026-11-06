using System.Text.RegularExpressions;

namespace LastTide
{
    /// <summary>Pre-written lines, so Marlow still helps when the AI can't be reached.</summary>
    public static class MarlowFallback
    {
        private static readonly string[] IdleLines =
        {
            "Scanning. Scanning. Still an island. I'll keep you posted.",
            "My logs say we crashed. My logs are very proud of that deduction.",
            "My long-range sensors are full of sand. We'll have to look around the old way.",
        };

        private static int idleIndex;

        public static MarlowResponse Reply(string message, Inventory inventory, SurvivalStats stats, QuestManager quest)
        {
            string text = message.ToLowerInvariant();
            if (Has(text, "shipwreck|wreck|ship|battery|crate")) return Line(inventory.Has(ItemType.Axe)
                ? "The wreck is on the far shore, and the battery is in a crate. I've marked it with a beam."
                : "The battery is in a crate by the wreck. You'll need an axe, but I've marked it with a beam.", "point_wreck");
            if (Has(text, "cave|shelter|lens")) return Line(inventory.Has(ItemType.Knife)
                ? "The lens is in the rock shelter on the hillside. I've marked it with a beam."
                : "The lens is in the rock shelter, tangled in vines. Bring a knife. I've marked it with a beam.", "point_shelter");
            if (Has(text, "antenna|peak")) return Line("The antenna fell beside the dirt trail, near the top. I've marked it with a beam.", "point_peak");
            if (Has(text, "beacon|rescue|signal|help|goal|objective|parts")) return QuestHint(inventory, quest);
            if (Has(text, "wood|stick|sticks|branch|logs")) return Line("Wood is lying about all over. Follow me, I'll hover over the closest piece.", "point_wood");
            if (Has(text, "stone|stones|rock|rocks")) return Line("Loose stones, coming up. I'll hover over the closest one.", "point_stone");
            if (Has(text, "vine|vines")) return Line(inventory.Has(ItemType.Knife)
                ? "Vines grow near the bushes. I'll show you the closest one."
                : "Vines grow near the bushes, but you'll need a knife to cut them. I'll show you one.", "point_vine");
            if (Has(text, "coconut|coconuts")) return Line("Coconuts drop near the palms. I'll hover over the closest one.", "point_coconut");
            if (Has(text, "hungry|hunger|food|eat|starving")) return Line(inventory.Has(ItemType.Coconut)
                ? "You're carrying a coconut. Press C and eat it before you start eyeing my circuits."
                : "Coconuts fall near the palms. I'll find you one.", inventory.Has(ItemType.Coconut) ? "none" : "point_coconut");
            if (Has(text, "tired|sleep|rest|energy|exhausted")) return Line(GameEvents.Happened("Built a campfire")
                ? "Go back to the campfire and press E to rest. At night you'll sleep till morning."
                : "A campfire lets you rest. Six wood and three stone should do it.", "none");
            if (Has(text, "knife")) return Line("One wood, one stone. Press Tab to craft it. Very advanced technology.", "none");
            if (Has(text, "axe")) return Line("An axe takes two wood, two stone and a vine. Then those palms will give you more wood.", "none");
            if (Has(text, "campfire|fire")) return Line("Six wood and three stone, on open flat ground. Press Tab when you're ready.", "none");
            if (Has(text, "who|name|what are you")) return Line("I'm Marlow, ship's robot. Some of me is still on the ship.", "none");
            if (Has(text, "hello|hi|hey")) return Line("Hello. All my systems are working, except the ones that aren't.", "none");
            return NextStep(inventory, stats, quest);
        }

        private static MarlowResponse QuestHint(Inventory inventory, QuestManager quest)
        {
            if (quest == null) return Line("The beacon needs three parts: a lens, a battery and an antenna.", "none");
            if (quest.PartsFound(inventory) >= QuestManager.PartsNeeded) return Line("You have all three parts. Follow the trail to the top and light the beacon.", "point_beacon");
            if (!inventory.Has(ItemType.Antenna)) return Line("The beacon needs a lens, a battery and an antenna. The dirt trail leads up to it, and the antenna is near the top. I've marked it.", "point_peak");
            if (!inventory.Has(ItemType.Lens)) return Line("The lens is in the rock shelter beside the trail. Bring a knife for the vines. I've marked it.", "point_shelter");
            return Line("The last part is the battery, in a crate by the shipwreck. You'll need an axe. I've marked it.", "point_wreck");
        }

        private static MarlowResponse NextStep(Inventory inventory, SurvivalStats stats, QuestManager quest)
        {
            if (stats != null && stats.Hunger < 30f)
            {
                return inventory.Has(ItemType.Coconut)
                    ? Line("Your hunger is low. Press C and eat a coconut.", "none")
                    : Line("Your hunger is low. Let's find a coconut.", "point_coconut");
            }

            if (stats != null && stats.Energy < 25f) return Line("You're running out of energy. Rest by a campfire soon.", "none");
            if (!inventory.Has(ItemType.Knife)) return Line("Start with a knife. Find one wood and one stone, then press Tab.", "none");
            if (!inventory.Has(ItemType.Axe)) return Line("Next, an axe: two wood, two stone and a vine.", "none");
            if (!GameEvents.Happened("Built a campfire")) return Line("We should make camp. Six wood and three stone will build a campfire.", "none");

            if (quest != null)
            {
                return QuestHint(inventory, quest);
            }

            string line = IdleLines[idleIndex];
            idleIndex = (idleIndex + 1) % IdleLines.Length;
            return Line(line, "none");
        }

        private static bool Has(string text, string words)
        {
            return Regex.IsMatch(text, $@"\b({words})\b");
        }

        private static MarlowResponse Line(string reply, string action)
        {
            return new MarlowResponse { reply = reply, mood = "neutral", action = action };
        }
    }
}
