using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
// ReSharper disable InconsistentNaming

namespace Tank1460.Common.Extensions;

public static class GameWindowExtensions
{
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

    public static (int DisplayIndex, Rectangle DisplayBounds) GetContainingDisplay(this GameWindow window)
    {
        var displayCount = SDL_GetNumVideoDisplays();
        if (displayCount <= 1)
            return (0, Rectangle.Empty);

        var position = window.Position;
        for (var i = 0; i < displayCount; i++)
        {
            if (SDL_GetDisplayBounds(i, out var sdlRect) != 0)
                continue;

            var displayRect = sdlRect.ToRect();
            if (!displayRect.Contains(position))
                continue;

            return (i, displayRect);
        }

        return (0, Rectangle.Empty);
    }

    public static void SetDisplayRelativePosition(this GameWindow window, int displayIndex, Point relativePosition)
    {
        if (displayIndex != 0 && SDL_GetDisplayBounds(displayIndex, out var sdlRect) == 0)
        {
            var displayRect = sdlRect.ToRect();
            window.Position = displayRect.Location + relativePosition;
        }
        else
        {
            window.Position = relativePosition;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct SDL_Rect
    {
        private readonly int x;
        private readonly int y;
        private readonly int w;
        private readonly int h;

        public Rectangle ToRect() => new(x, y, w, h);
    }

    [DllImport(libName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void SDL_MaximizeWindow(IntPtr window);

    [DllImport(libName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void SDL_RestoreWindow(IntPtr window);

    [DllImport(libName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int SDL_GetWindowFlags(IntPtr window);

    [DllImport(libName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int SDL_GetNumVideoDisplays();

    [DllImport(libName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int SDL_GetDisplayBounds(int displayIndex, out SDL_Rect rect);

    private const int SDL_WINDOW_MAXIMIZED = 0x00000080;
    private const string libName = "SDL2.dll";
}