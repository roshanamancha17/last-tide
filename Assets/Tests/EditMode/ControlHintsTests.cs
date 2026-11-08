using NUnit.Framework;

namespace LastTide.Tests
{
    public class ControlHintsTests
    {
        [TestCase("[E] Dig out the buried robot", "Dig out the buried robot")]
        [TestCase("[E] Pick up Wood", "Pick up Wood")]
        [TestCase("[E] Sleep until morning", "Sleep until morning")]
        [TestCase("<b>Crafting</b>   [Tab] close", "<b>Crafting</b>   Tap Craft to close")]
        [TestCase("<color=#8ff08f>[1] Knife: 1 Wood  1 Stone</color>", "<color=#8ff08f>Knife: 1 Wood  1 Stone</color>")]
        [TestCase("Thanks for playing.\n[R] Play again", "Thanks for playing.\nTap Jump to play again")]
        [TestCase("[Tab] Craft    [C] Eat", "")]
        [TestCase("You need a knife to cut this.", "You need a knife to cut this.")]
        public void PhoneWordingReplacesKeyNames(string keyboard, string phone)
        {
            Assert.AreEqual(phone, ControlHints.ForDevice(keyboard, touch: true));
        }

        [TestCase("[E] Pick up Wood")]
        [TestCase("[Tab] Craft    [C] Eat")]
        [TestCase("[R] Play again")]
        public void KeyboardWordingIsLeftAlone(string text)
        {
            Assert.AreEqual(text, ControlHints.ForDevice(text, touch: false));
        }

        [Test]
        public void EmptyTextStaysEmpty()
        {
            Assert.IsNull(ControlHints.ForDevice(null, true));
            Assert.AreEqual(string.Empty, ControlHints.ForDevice(string.Empty, true));
        }
    }
}
