using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Tank1460.Common.Extensions;
using Tank1460.LevelObjects.Tiles;

namespace Tank1460;

public class LevelHud
{
    public static readonly int HudWidth = 2 * Tile.DefaultWidth;

    public static readonly Dictionary<PlayerIndex, char> PlayerNames = new()
    {
        { PlayerIndex.One, 'Ⅰ' },
        { PlayerIndex.Two, 'Ⅱ' },
        { PlayerIndex.Three, 'Ⅲ' },
        { PlayerIndex.Four, 'Ⅳ' }
    };

    private Font _font;

    private Texture2D _bot, _player, _levelFlag;

    public LevelHud(ContentManagerEx content)
    {
        LoadContent(content);
    }

    private void LoadContent(ContentManagerEx content)
    {
        _font = content.LoadFont(@"Sprites/Font/Pixel8");
        _bot = content.Load<Texture2D>(@"Sprites/Hud/Bot");
        _player = content.Load<Texture2D>(@"Sprites/Hud/Player");
        _levelFlag = content.Load<Texture2D>(@"Sprites/Hud/Flag");
    }

    public void Draw(Level level, SpriteBatch spriteBatch, Point location)
    {
        var currentLocation = location;

        DrawBotSpawnsRemaining(spriteBatch, currentLocation, Math.Min(level.BotSpawnsRemaining, 20));

        if (level.PlayersInGame.Length <= 2)
            currentLocation.Y += 14 * Tile.DefaultHeight;
        else
            currentLocation.Y += 11 * Tile.DefaultHeight;

        foreach (var playerIndex in level.PlayersInGame)
        {
            var spawner = level.GetPlayerSpawner(playerIndex);
            var lives = spawner.HasInfiniteLives ? " " : spawner.LivesRemaining.ToString();

            DrawPlayerLives(spriteBatch, currentLocation, PlayerNames[playerIndex], lives);
            currentLocation.Y += 3 * Tile.DefaultHeight;
        }

        DrawLevelIndex(spriteBatch, currentLocation, level.LevelNumber);
    }

    private void DrawBotSpawnsRemaining(SpriteBatch spriteBatch, Point location, int count)
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

    private void DrawPlayerLives(SpriteBatch spriteBatch, Point location, char playerName, string lives)
    {
        _font.Draw(playerName.ToString(), spriteBatch, location with { X = location.X - 1 });
        _font.Draw("P", spriteBatch, location with { X = location.X + Tile.DefaultWidth });
        spriteBatch.Draw(_player, location with { Y = location.Y + Tile.DefaultHeight }, Color.White);
        _font.Draw(lives, spriteBatch, location with { X = location.X + Tile.DefaultWidth, Y = location.Y + Tile.DefaultHeight });
    }

    private void DrawLevelIndex(SpriteBatch spriteBatch, Point location, int levelIndex)
    {
        spriteBatch.Draw(_levelFlag, location with { X = location.X - 1 }, Color.White);
        _font.Draw($"{levelIndex,2}", spriteBatch, location with { Y = location.Y + 2 * Tile.DefaultWidth });
    }
}