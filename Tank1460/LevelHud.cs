﻿using System;
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
        if (level.IsPlayerInGame(1))
        {
            _font.Draw("│", spriteBatch, currentLocation with { X = currentLocation.X - 1 });
            _font.Draw("P", spriteBatch, currentLocation with { X = currentLocation.X + Tile.DefaultWidth });
            DrawPlayerLives(spriteBatch, currentLocation with { Y = currentLocation.Y + Tile.DefaultHeight },
                level.PlayerLivesRemaining(1));
        }

        currentLocation.Y += 3 * Tile.DefaultHeight;
        if (level.IsPlayerInGame(2))
        {
            _font.Draw("║", spriteBatch, currentLocation with { X = currentLocation.X - 1 });
            _font.Draw("P", spriteBatch, currentLocation with { X = currentLocation.X + Tile.DefaultWidth });
            DrawPlayerLives(spriteBatch, currentLocation with { Y = currentLocation.Y + Tile.DefaultHeight },
                level.PlayerLivesRemaining(2));
        }

        currentLocation.Y += 3 * Tile.DefaultHeight;
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

    private void DrawPlayerLives(SpriteBatch spriteBatch, Vector2 location, int lives)
    {
        if (--lives < 0)
            lives = 0;

        spriteBatch.Draw(_player, location, Color.White);
        _font.Draw($"{lives}", spriteBatch, location with { X = location.X + Tile.DefaultWidth });
    }

    private void DrawLevelIndex(SpriteBatch spriteBatch, Vector2 location, int levelIndex)
    {
        spriteBatch.Draw(_levelFlag, location, Color.White);
        _font.Draw($"{levelIndex,2}", spriteBatch, location with { Y = location.Y + 2 * Tile.DefaultWidth });
    }
}