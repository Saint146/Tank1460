using Microsoft.Xna.Framework;
using System;
using System.Runtime.InteropServices;

namespace Tank1460.Extensions;

public static class GameWindowExtensions
{
    [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern void SDL_MaximizeWindow(IntPtr window);

    [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern void SDL_RestoreWindow(IntPtr window);

    [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern int SDL_GetWindowFlags(IntPtr window);
    private const int SDL_WINDOW_MAXIMIZED = 0x00000080;

    public static void Maximize(this GameWindow window)
    {
        SDL_MaximizeWindow(window.Handle);
    }

    public static void Restore(this GameWindow window)
    {
        SDL_RestoreWindow(window.Handle);
    }

    public static bool IsMaximized(this GameWindow window)
    {
        var flags = SDL_GetWindowFlags(window.Handle);
        return (flags & SDL_WINDOW_MAXIMIZED) != 0;
    }
}