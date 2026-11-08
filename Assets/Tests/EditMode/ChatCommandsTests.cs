using NUnit.Framework;

namespace LastTide.Tests
{
    public class ChatCommandsTests
    {
        [TestCase("/report", true)]
        [TestCase("  /REPORT  ", true)]
        [TestCase("/Report", true)]
        [TestCase("report", false)]
        [TestCase("/reports", false)]
        [TestCase("please /report", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void OnlyTheExactCommandReports(string typed, bool expected)
        {
            Assert.AreEqual(expected, ChatCommands.IsReport(typed));
        }
    }
}
