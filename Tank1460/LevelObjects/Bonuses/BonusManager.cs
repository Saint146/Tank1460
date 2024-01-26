using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tank1460.Audio;

namespace Tank1460.LevelObjects.Bonuses;

public class BonusManager
{
    private readonly Level _level;
    private readonly int _maxBonusesOnScreen;
    private readonly List<Bonus> _bonuses = new();
    private readonly BonusType[] _allowedTypes;

    public BonusManager(Level level, int maxBonusesOnScreen)
    {
        _level = level;
        _maxBonusesOnScreen = maxBonusesOnScreen;
        _allowedTypes = Enum.GetValues<BonusType>();
    }

    public Bonus Spawn(BonusType type, int x, int y)
    {
        if (_bonuses.Count >= _maxBonusesOnScreen)
        {
            _bonuses[0].Destroy();
            _bonuses.RemoveAt(0);
        }

        var newBonus = new Bonus(_level, type);
        _bonuses.Add(newBonus);

        var bounds = Level.GetTileBounds(x, y);
        newBonus.Spawn(new Point(bounds.Left, bounds.Top));

        _level.SoundPlayer.Play(Sound.BonusSpawn);

        return newBonus;
    }

    public Bonus Spawn(BonusType type)
    {
        var (x, y) = GetRandomBonusSpot();
        return Spawn(type, x, y);
    }

    public Bonus Spawn(int x, int y)
    {
        var type = GetRandomBonusType();
        return Spawn(type, x, y);
    }

    public Bonus Spawn()
    {
        var type = GetRandomBonusType();
        var (x, y) = GetRandomBonusSpot();
        return Spawn(type, x, y);
    }

    private (int x, int y) GetRandomBonusSpot()
    {
        // TODO: Проверить
        var x = Rng.NextEven(_level.TileBounds.Left + 1, _level.TileBounds.Right) - 1;
        var y = Rng.NextEven(_level.TileBounds.Top + 1, _level.TileBounds.Bottom) - 1;
        return (x, y);
    }

    private BonusType GetRandomBonusType() => _allowedTypes[Rng.Next(_allowedTypes.Length)];

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        foreach (var bonus in _bonuses)
            bonus.Draw(gameTime, spriteBatch);
    }

    public void Update(GameTime gameTime)
    {
#if DEBUG
        if (KeyboardEx.IsPressed(Keys.LeftControl))
        {
            if (KeyboardEx.HasBeenPressed(Keys.B) || (KeyboardEx.IsPressed(Keys.LeftShift) && KeyboardEx.IsPressed(Keys.B)))
            {
                Spawn(BonusType.Shovel);
            }
        }
#endif

        _bonuses.RemoveAll(bonus => bonus.ToRemove);

        foreach (var bonus in _bonuses)
            bonus.Update(gameTime);
    }
}