using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Tank1460.LevelObjects.Tiles;

namespace Tank1460;

public class LevelHud
{
    public static readonly int HudWidth = 3 * Tile.DefaultWidth;

    private Font _font, _redFont;

    private Texture2D _bot, _player, _levelFlag;

    private static readonly Dictionary<PlayerIndex, char> PlayerNames = new()
    {
        { PlayerIndex.One, '│' },
        { PlayerIndex.Two, '║' }
    };

    public LevelHud(ContentManager content)
    {
        LoadContent(content);
    }

    private void LoadContent(ContentManager content)
    {
        _font = new Font(content);
        _redFont = new Font(content, new Color(0x0027d1));
        _bot = content.Load<Texture2D>(@"Sprites/Hud/Bot");
        _player = content.Load<Texture2D>(@"Sprites/Hud/Player");
        _levelFlag = content.Load<Texture2D>(@"Sprites/Hud/Flag");
    }

    public void Update(GameTime gameTime)
    {

    }

    public void Draw(Level level, SpriteBatch spriteBatch, Point location)
    {
        var currentLocation = location.ToVector2();

        DrawBotSpawnsRemaining(spriteBatch, currentLocation, Math.Min(level.BotSpawnsRemaining, 20));

        currentLocation.Y += 14 * Tile.DefaultHeight;
        foreach (var playerIndex in level.PlayersInGame)
        {
            DrawPlayerLives(spriteBatch, currentLocation, PlayerNames[playerIndex], level.PlayerLivesRemaining(playerIndex));
            currentLocation.Y += 3 * Tile.DefaultHeight;
        }

        DrawLevelIndex(spriteBatch, currentLocation, level.LevelNumber);
    }

    private void DrawBotSpawnsRemaining(SpriteBatch spriteBatch, Vector2 location, int count)
    {
        if (count > 20)
            count = 20;

        var currentLocation = location;

        for (var i = 0; i < count / 2; i++)
        {
            spriteBatch.Draw(_bot, currentLocation, Color.White);
            spriteBatch.Draw(_bot, currentLocation with { X = currentLocation.X + Tile.DefaultWidth }, Color.White);
            currentLocation.Y += Tile.DefaultHeight;
        }

        if (count % 2 > 0)
            spriteBatch.Draw(_bot, currentLocation, Color.White);
    }

    private void DrawPlayerLives(SpriteBatch spriteBatch, Vector2 location, char playerName, int lives)
    {
        if (--lives < 0)
            lives = 0;

        _font.Draw(playerName.ToString(), spriteBatch, location with { X = location.X - 1 });
        _font.Draw("P", spriteBatch, location with { X = location.X + Tile.DefaultWidth });
        spriteBatch.Draw(_player, location with { Y = location.Y + Tile.DefaultHeight }, Color.White);
        _font.Draw($"{lives}", spriteBatch, location with { X = location.X + Tile.DefaultWidth, Y = location.Y + Tile.DefaultHeight });
    }

    private void DrawLevelIndex(SpriteBatch spriteBatch, Vector2 location, int levelIndex)
    {
        spriteBatch.Draw(_levelFlag, location with { X = location.X - 1 }, Color.White);
        _font.Draw($"{levelIndex,2}", spriteBatch, location with { Y = location.Y + 2 * Tile.DefaultWidth });
    }
}