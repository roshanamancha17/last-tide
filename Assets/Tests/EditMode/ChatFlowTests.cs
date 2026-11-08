using NUnit.Framework;

namespace LastTide.Tests
{
    public class ChatFlowTests
    {
        // Keyboard and mouse: the original behaviour must not change.

        [Test]
        public void KeyboardMarlowsGreetingOpensTheChatReadyToType()
        {
            var chat = new ChatFlow(touchMode: false);
            chat.Introduce();
            Assert.AreEqual(ChatState.Typing, chat.State);
            Assert.IsTrue(chat.BlocksGameplayKeys);
        }

        [Test]
        public void KeyboardEnterSendsAndKeepsTyping()
        {
            var chat = new ChatFlow(false);
            chat.TalkPressed();
            Assert.AreEqual(ChatCommand.Send, chat.SubmitPressed());
            Assert.AreEqual(ChatState.Typing, chat.State);
        }

        [Test]
        public void KeyboardEscCloses()
        {
            var chat = new ChatFlow(false);
            chat.TalkPressed();
            chat.BackPressed();
            Assert.AreEqual(ChatState.Closed, chat.State);
            Assert.IsFalse(chat.PanelVisible);
        }

        [Test]
        public void KeyboardWalkingDoesNotCloseWhileTyping()
        {
            var chat = new ChatFlow(false);
            chat.TalkPressed();
            chat.PlayerMoved();
            Assert.AreEqual(ChatState.Typing, chat.State);
        }

        // Phones.

        [Test]
        public void PhoneGreetingShowsWithoutTheKeyboardAndLeavesTheGamePlayable()
        {
            var chat = new ChatFlow(touchMode: true);
            chat.Introduce();
            Assert.AreEqual(ChatState.Reading, chat.State);
            Assert.IsTrue(chat.PanelVisible);
            Assert.IsFalse(chat.BlocksGameplayKeys);
        }

        [Test]
        public void PhoneTalkOpensTheKeyboardFromClosedOrReading()
        {
            var closed = new ChatFlow(true);
            Assert.AreEqual(ChatCommand.OpenKeyboard, closed.TalkPressed());
            Assert.AreEqual(ChatState.Typing, closed.State);

            var reading = new ChatFlow(true);
            reading.Introduce();
            Assert.AreEqual(ChatCommand.OpenKeyboard, reading.TalkPressed());
        }

        [Test]
        public void PhoneTalkWhileTypingDoesNothing()
        {
            var chat = new ChatFlow(true);
            chat.TalkPressed();
            Assert.AreEqual(ChatCommand.None, chat.TalkPressed());
            Assert.AreEqual(ChatState.Typing, chat.State);
        }

        [Test]
        public void PhoneDoneSendsAndLowersTheKeyboardToShowTheReply()
        {
            var chat = new ChatFlow(true);
            chat.TalkPressed();
            Assert.AreEqual(ChatCommand.Send, chat.SubmitPressed());
            Assert.AreEqual(ChatState.Reading, chat.State);
        }

        [Test]
        public void PhoneBackLowersTheKeyboardThenClosesTheChat()
        {
            var chat = new ChatFlow(true);
            chat.TalkPressed();
            Assert.AreEqual(ChatCommand.CloseKeyboard, chat.BackPressed());
            Assert.AreEqual(ChatState.Reading, chat.State);
            Assert.AreEqual(ChatCommand.None, chat.BackPressed());
            Assert.AreEqual(ChatState.Closed, chat.State);
        }

        [Test]
        public void PhoneDismissingTheKeyboardKeepsTheChatVisible()
        {
            var chat = new ChatFlow(true);
            chat.TalkPressed();
            chat.KeyboardCanceled();
            Assert.AreEqual(ChatState.Reading, chat.State);
        }

        [Test]
        public void PhoneWalkingAwayClosesAChatThatIsOnlyBeingRead()
        {
            var chat = new ChatFlow(true);
            chat.Introduce();
            chat.PlayerMoved();
            Assert.AreEqual(ChatState.Closed, chat.State);
        }

        [Test]
        public void PhoneWalkingDoesNotCloseWhileTyping()
        {
            var chat = new ChatFlow(true);
            chat.TalkPressed();
            chat.PlayerMoved();
            Assert.AreEqual(ChatState.Typing, chat.State);
        }

        [Test]
        public void SendingIsIgnoredWhenNotTyping()
        {
            var chat = new ChatFlow(true);
            chat.Introduce();
            Assert.AreEqual(ChatCommand.None, chat.SubmitPressed());
        }
    }
}
