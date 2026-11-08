using UnityEngine;
using UnityEngine.InputSystem;

namespace LastTide
{
    /// <summary>
    /// Runs before gameplay scripts each frame and merges keyboard, mouse and touch into GameInput.Current.
    /// Created automatically, so scenes don't need it.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public class InputCollector : MonoBehaviour
    {
        private TouchControls touch;
        private CraftingSystem crafting;
        private MarlowChat chat;

        /// <summary>True once on-screen touch controls are in use.</summary>
        public static bool TouchActive { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            TouchActive = false;
            if (FindAnyObjectByType<InputCollector>() != null)
            {
                return;
            }

            GameObject host = new GameObject("Game Input");
            DontDestroyOnLoad(host);
            host.AddComponent<InputCollector>();
        }

        private void Update()
        {
            if (touch == null && (Application.isMobilePlatform || Touchscreen.current != null))
            {
                touch = gameObject.AddComponent<TouchControls>();
                TouchActive = true;
            }

            // Gameplay keys step aside while the player types to Marlow.
            InputFrame keys = UiState.IsTyping
                ? default
                : KeyboardMouseInput.Read(Keyboard.current, Mouse.current, Cursor.lockState == CursorLockMode.Locked);
            if (touch != null)
            {
                if (crafting == null)
                {
                    crafting = FindAnyObjectByType<CraftingSystem>();
                }

                touch.SetRecipesVisible(crafting != null && crafting.PanelOpen);
                if (chat == null)
                {
                    chat = FindAnyObjectByType<MarlowChat>();
                }

                touch.SetReportVisible(chat != null && chat.CanReport);
            }

            if (touch != null)
            {
                touch.SetVisible(!UiState.MenuOpen);
            }

            InputFrame fingers = touch != null ? touch.Collect() : default;
            InputFrame combined = InputFrame.Combine(keys, fingers);
            if (UiState.MenuOpen)
            {
                // Menus only listen for Back; nothing reaches the game behind them.
                combined = new InputFrame { Pause = combined.Pause };
            }

            GameInput.Set(combined);
        }
    }
}
