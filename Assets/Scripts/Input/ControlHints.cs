using System.Text.RegularExpressions;

namespace LastTide
{
    /// <summary>Rewrites keyboard hints like "[E] Pick up Wood" for players on a touch screen.</summary>
    public static class ControlHints
    {
        private static readonly Regex KeyPrefix = new Regex(@"\[(E|1|2|3)\]\s*");

        public static string ForDevice(string text, bool touch)
        {
            if (!touch || string.IsNullOrEmpty(text))
            {
                return text;
            }

            // The corner hint only lists keys; on phones the buttons already say the same.
            if (text.Contains("[Tab] Craft") && text.Contains("[C] Eat"))
            {
                return string.Empty;
            }

            text = text.Replace("[Tab] close", "Tap Craft to close")
                       .Replace("[R] Play again", "Tap Jump to play again");
            return KeyPrefix.Replace(text, string.Empty);
        }
    }
}
