using Microsoft.Xna.Framework.Input;

namespace Tank1460;

public static class KeyboardEx
{
    static KeyboardState _currentKeyState;
    static KeyboardState _previousKeyState;

    public static KeyboardState GetState()
    {
        _previousKeyState = _currentKeyState;
        _currentKeyState = Keyboard.GetState();
        return _currentKeyState;
    }

    public static bool IsPressed(Keys key)
    {
        return _currentKeyState.IsKeyDown(key);
    }

    public static bool HasBeenPressed(Keys key)
    {
        return _currentKeyState.IsKeyDown(key) && !_previousKeyState.IsKeyDown(key);
    }
}