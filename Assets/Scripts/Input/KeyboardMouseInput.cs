using UnityEngine;
using UnityEngine.InputSystem;

namespace LastTide
{
    /// <summary>Reads the keyboard and mouse into an InputFrame, using the game's original key bindings.</summary>
    public static class KeyboardMouseInput
    {
        public static InputFrame Read(Keyboard keyboard, Mouse mouse, bool lookWithMouse)
        {
            InputFrame frame = new InputFrame();
            if (keyboard != null)
            {
                Vector2 move = Vector2.zero;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) move.y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) move.y -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move.x += 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move.x -= 1f;
                frame.Move = Vector2.ClampMagnitude(move, 1f);
                frame.Sprint = keyboard.leftShiftKey.isPressed;
                frame.Jump = keyboard.spaceKey.wasPressedThisFrame;
                frame.Interact = keyboard.eKey.wasPressedThisFrame;
                frame.Eat = keyboard.cKey.wasPressedThisFrame;
                frame.Craft = keyboard.tabKey.wasPressedThisFrame;
                frame.Talk = keyboard.tKey.wasPressedThisFrame;
            }

            if (mouse != null && lookWithMouse)
            {
                frame.Look = mouse.delta.ReadValue();
            }

            return frame;
        }
    }
}
