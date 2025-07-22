using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Text;

namespace Proximity.Content
{
    public class InputManager
    {
        public KeyboardState currentKeyboardState;
        public KeyboardState previousKeyboardState;
        public MouseState currentMouseState;
        public MouseState previousMouseState;
        public Vector2 mousePosition;

        private static InputManager instance;

        public static InputManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new InputManager();
                }
                return instance;
            }
        }

        private InputManager()
        { }

        public void PreUpdate()
        {
            previousKeyboardState = currentKeyboardState;
            previousMouseState = currentMouseState;
        }

        public void PostUpdate(GameTime gameTime)
        {
            currentKeyboardState = Keyboard.GetState();
            currentMouseState = Mouse.GetState();

            mousePosition = new Vector2(currentMouseState.X, currentMouseState.Y) * (float)gameTime.ElapsedGameTime.TotalSeconds * 60f;
        }

        public bool IsKeyDown(Keys key)
        {
            return currentKeyboardState.IsKeyDown(key);
        }

        public bool IsKeySinglePress(Keys key)
        {
            return currentKeyboardState.IsKeyDown(key) && !previousKeyboardState.IsKeyDown(key);
        }

        public bool IsKeyReleased(Keys key)
        {
            return !currentKeyboardState.IsKeyDown(key) && previousKeyboardState.IsKeyDown(key);
        }

        public bool IsButtonReleased(bool leftClick)
        {
            return leftClick
                ? currentMouseState.LeftButton == ButtonState.Released
                : currentMouseState.RightButton == ButtonState.Released;
        }

        public bool IsButtonPressed(bool leftClick)
        {
            return leftClick
                ? currentMouseState.LeftButton == ButtonState.Pressed
                : currentMouseState.RightButton == ButtonState.Pressed;
        }

        public bool IsButtonSingleClick(bool leftClick)
        {
            return leftClick
                ? currentMouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released
                : currentMouseState.RightButton == ButtonState.Pressed && previousMouseState.RightButton == ButtonState.Released;
        }
        public string GetPressedKeys()
        {
            Keys[] pressedKeys = currentKeyboardState.GetPressedKeys();

            if (pressedKeys.Length == 0)
            {
                return "";
            }

            StringBuilder keysStringBuilder = new StringBuilder();

            foreach (Keys key in pressedKeys)
            {
                if (!IsModifierKey(key) && !IsSpecialKey(key) && IsKeySinglePress(key))
                {
                    if (key == Keys.LeftShift && keysStringBuilder.Length > 0)
                    {
                        keysStringBuilder.Length--;
                    }
                    else
                    {
                        string keyString = GetKeyString(key);
                        if (!string.IsNullOrEmpty(keyString))
                        {
                            keysStringBuilder.Append(keyString);
                        }
                    }
                }
            }

            return keysStringBuilder.ToString();
        }

        private string GetKeyString(Keys key)
        {
            if (key >= Keys.D0 && key <= Keys.D9)
            {
                return (key - Keys.D0).ToString();
            }

            if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
            {
                return (key - Keys.NumPad0).ToString();
            }

            switch (key)
            {
                case Keys.Space:
                    return " ";
                case Keys.Enter:
                    return Environment.NewLine;
                case Keys.Tab:
                    return "\t";
            }
            return key.ToString();
        }



        private bool IsSpecialKey(Keys key)
        {
            return key == Keys.Enter || key == Keys.Back || key == Keys.Tab || key == Keys.CapsLock || key == Keys.Up || key == Keys.Down || key == Keys.None;
        }

        private bool IsModifierKey(Keys key)
        {
            return key == Keys.LeftShift || key == Keys.RightShift || key == Keys.LeftControl || key == Keys.RightControl || key == Keys.LeftAlt || key == Keys.RightAlt;
        }
    }
}