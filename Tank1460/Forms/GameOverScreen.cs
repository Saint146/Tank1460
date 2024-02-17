using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tank1460.Common.Extensions;
using Tank1460.Input;
using Tank1460.LevelObjects.Tiles;

namespace Tank1460.Forms;

internal class GameOverScreen : Form
{
    private const string Text = "GAME\nOVER";

    private const int CenterX = 14 * Tile.DefaultWidth;
    private const int CenterY = 14 * Tile.DefaultHeight;

    public GameOverScreen(ContentManagerEx content) : base(content)
    {
        CreateTitle();
    }

    protected override void OnClick(FormItem item)
    {
        Exit();
    }

    protected override void OnPress(PlayerIndex playerIndex, PlayerInputCommands input)
    {
        if (input.HasOneOfFlags(PlayerInputCommands.Shoot, PlayerInputCommands.ShootTurbo, PlayerInputCommands.Start))
            Exit();
    }

    private void CreateTitle()
    {
        var commonFont = Content.LoadFont(@"Sprites/Font/Pixel8");
        var titlePatternTexture = Content.Load<Texture2D>(@"Sprites/Hud/Pattern2");
        var titleFont = Content.LoadOrCreateCustomFont("GameOverFont", () => commonFont.CreateFontUsingTextureAsPattern(titlePatternTexture));

        var title = CreateTextLabel(Text, titleFont, Point.Zero);
        AddItem(title);
        title.Position = new Point(x: CenterX - title.Bounds.Width / 2,
                                   y: CenterY - title.Bounds.Height / 2);
    }
}