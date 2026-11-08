using NUnit.Framework;

namespace LastTide.Tests
{
    public class PlayerIdentityTests
    {
        [Test]
        public void TheSameIdIsUsedEverySession()
        {
            string stored = null;
            string first = PlayerIdentity.LoadOrCreate(() => stored, id => stored = id);
            string second = PlayerIdentity.LoadOrCreate(() => stored, id => stored = id);
            Assert.AreEqual(first, second);
            Assert.AreEqual(32, first.Length);
        }

        [TestCase("")]
        [TestCase("not-an-id")]
        [TestCase("zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz")]
        public void ADamagedIdIsReplaced(string damaged)
        {
            string stored = damaged;
            string id = PlayerIdentity.LoadOrCreate(() => stored, value => stored = value);
            Assert.AreNotEqual(damaged, id);
            Assert.AreEqual(id, stored);
        }

        [Test]
        public void TheFirstChatWaitsLongerWhileTheServerWakesUp()
        {
            Assert.Greater(ServerTimeouts.ForRequest(false), 30);
            Assert.AreEqual(12, ServerTimeouts.ForRequest(true));
        }
    }
}
