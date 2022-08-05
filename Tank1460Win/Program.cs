using System.Windows.Forms;
using Tank1460;

namespace Tank1460Win;

internal static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        var formMain = new FormMain();
        formMain.Show();

        using var game = new Tank1460Game(formMain.GetDrawSurface());
        formMain.SetGameObject(game);
        game.Run();
    }
}