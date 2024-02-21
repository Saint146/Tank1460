using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Content;
using System.Linq;
using Tank1460.Audio;
using Tank1460.Common.Extensions;
using Tank1460.Input;
using Tank1460.LevelObjects.Tiles;

namespace Tank1460.Forms;

internal class HighscoreScreen : Form
{
    private const string FirstLine = "HISCORE";
    private const string SecondLineFormat = "{0,7}";

    private const int PositionX = (int)(Tile.DefaultWidth * 2.5);
    private const int PositionY = (int)(Tile.DefaultHeight * 5.5);
    private const int InterlineIndent = (int)(Tile.DefaultHeight * 2.5);

    public HighscoreScreen(GameServiceContainer serviceProvider, int highscore) : base(serviceProvider)
    {
        CreateText(highscore);
        SoundPlayer.StopAll();
        SoundPlayer.Play(Sound.Highscore);
    }

    protected override void OnClick(FormItem item)
    {
        if (SoundPlayer.IsPlaying(Sound.Highscore))
            return;

        Exit();
    }

    protected override void OnInputPressed(PlayerIndex playerIndex, PlayerInputCommands input)
    {
        if (SoundPlayer.IsPlaying(Sound.Highscore))
            return;

        if (input.HasOneOfFlags(PlayerInputCommands.Shoot, PlayerInputCommands.ShootTurbo, PlayerInputCommands.Start))
            Exit();
    }

    private void CreateText(int highscore)
    {
        var flashingRecolorTextures = Content.MassLoadContent<Texture2D>(@"Sprites/_R/Pattern/Flashing");

        var flashingFonts = flashingRecolorTextures.Keys.Select(textureId => Content.LoadOrCreateCustomFont($"FlashingFont_{textureId}",
                                                                    () =>
                                                                    {
                                                                        var commonFont = Content.LoadFont(@"Sprites/Font/Pixel8");
                                                                        var titlePatternTexture =
                                                                            Content.LoadRecoloredTexture(@"Sprites/Hud/Pattern2", textureId);
                                                                        return commonFont.CreateFontUsingTextureAsPattern(titlePatternTexture);
                                                                    })).ToArray();

        var fontForCalc = flashingFonts[0];

        var oneFrameSize = new Point(x: fontForCalc.CharWidth * FirstLine.Length,
                                     y: fontForCalc.CharHeight * 2 + InterlineIndent);

        var texture = new Texture2D(Content.GetGraphicsDevice(), oneFrameSize.X * flashingFonts.Length, oneFrameSize.Y);

        for (var i = 0; i < flashingFonts.Length; i++)
        {
            var font = flashingFonts[i];
            var firstLine = font.CreateTexture(FirstLine);
            var secondLine = font.CreateTexture(string.Format(SecondLineFormat, highscore));

            texture.Draw(firstLine, new Point(x: oneFrameSize.X * i, y: 0));
            texture.Draw(secondLine, new Point(x: oneFrameSize.X * i, y: firstLine.Height + InterlineIndent));
        }

        var animation = new Animation(texture, flashingFonts.Select(_ => GameRules.TimeInFrames(1)).ToArray(), true);

        var title = new FormImage(animation);
        AddItem(title, new Point(PositionX, PositionY));
    }
}