using Tank1460;

namespace Tank1460Win;

public partial class FormMain : Form
{
    // Ñì. https://github.com/bangclash/WinformMonoGame

    private Tank1460Game? _gameObject;

    public FormMain()
    {
        InitializeComponent();
    }

    public IntPtr GetDrawSurface()
    {
        return pictureBoxGame.Handle;
    }

    public void SetGameObject(Tank1460Game game)
    {
        _gameObject = game;
    }

    private void FormMain_FormClosed(object sender, FormClosedEventArgs e)
    {
        Application.Exit();
    }
}