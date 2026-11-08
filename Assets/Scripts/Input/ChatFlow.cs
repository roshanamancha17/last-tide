namespace LastTide
{
    public enum ChatState
    {
        /// <summary>The chat window is hidden.</summary>
        Closed,

        /// <summary>The window shows Marlow's replies; on phones the keyboard is down and the game can be played.</summary>
        Reading,

        /// <summary>The player is typing; gameplay keys are ignored.</summary>
        Typing,
    }

    public enum ChatCommand { None, OpenKeyboard, CloseKeyboard, Send }

    /// <summary>
    /// When the Marlow chat opens, closes and sends, for keyboards and for phones.
    /// Kept apart from the UI so every path can be tested.
    /// </summary>
    public sealed class ChatFlow
    {
        private readonly bool touch;

        public ChatFlow(bool touchMode)
        {
            touch = touchMode;
        }

        public ChatState State { get; private set; } = ChatState.Closed;
        public bool PanelVisible => State != ChatState.Closed;
        public bool BlocksGameplayKeys => State == ChatState.Typing;

        /// <summary>Marlow wakes and greets the player. On phones the keyboard stays down so the greeting can be read.</summary>
        public void Introduce()
        {
            State = touch ? ChatState.Reading : ChatState.Typing;
        }

        public ChatCommand TalkPressed()
        {
            if (State == ChatState.Typing)
            {
                return ChatCommand.None;
            }

            State = ChatState.Typing;
            return touch ? ChatCommand.OpenKeyboard : ChatCommand.None;
        }

        /// <summary>Enter on a keyboard, or Done on the phone keyboard.</summary>
        public ChatCommand SubmitPressed()
        {
            if (State != ChatState.Typing)
            {
                return ChatCommand.None;
            }

            // On a phone the keyboard closes after sending, so the reply can be read.
            if (touch)
            {
                State = ChatState.Reading;
            }

            return ChatCommand.Send;
        }

        /// <summary>The phone keyboard was dismissed without sending.</summary>
        public void KeyboardCanceled()
        {
            if (touch && State == ChatState.Typing)
            {
                State = ChatState.Reading;
            }
        }

        /// <summary>Esc on a keyboard, or the Android back button.</summary>
        public ChatCommand BackPressed()
        {
            if (touch && State == ChatState.Typing)
            {
                State = ChatState.Reading;
                return ChatCommand.CloseKeyboard;
            }

            State = ChatState.Closed;
            return ChatCommand.None;
        }

        /// <summary>Walking away closes a chat that is only being read.</summary>
        public void PlayerMoved()
        {
            if (State == ChatState.Reading)
            {
                State = ChatState.Closed;
            }
        }
    }
}
