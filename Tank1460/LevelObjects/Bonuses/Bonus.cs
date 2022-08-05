using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tank1460.Audio;
using Tank1460.LevelObjects.Tanks;

namespace Tank1460.LevelObjects.Bonuses;

public class Bonus : LevelObject
{
    public readonly BonusType Type;

    private IAnimation _animation;

    public Bonus(Level level, BonusType type) : base(level)
    {
        Type = type;
    }

    protected override IAnimation GetDefaultAnimation() => _animation;

    public void Destroy() => Remove();

    protected override void LoadContent()
    {
        _animation = new Animation(Level.Content.Load<Texture2D>($@"Sprites/Bonus/{Type}"), 8.0 * Tank1460Game.OneFrameSpan, true);
    }

    private void ApplyEffectOnPlayer(PlayerTank playerTank)
    {
        Level.SoundPlayer.Play(Sound.BonusPickup);

        switch (Type)
        {
            case BonusType.Armor:
                playerTank.AddTimedInvulnerability(600 * Tank1460Game.OneFrameSpan);
                break;

            case BonusType.OneUp:
                Level.GetPlayerSpawner(playerTank.PlayerNumber).AddOneUp();
                break;

            case BonusType.Pistol:
                playerTank.UpgradeMax();
                break;

            case BonusType.Star:
                playerTank.UpgradeUp();
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void ApplyEffectOnBot(BotTank botTank)
    {
        Level.SoundPlayer.Play(Sound.BonusPickup);

        switch (Type)
        {
            case BonusType.Armor:
                botTank.AddTimedInvulnerability(600 * Tank1460Game.OneFrameSpan);
                break;

            case BonusType.OneUp:
                Level.BotManager.AddOneUp();
                break;
        }
    }

    public override void Update(GameTime gameTime, KeyboardState keyboardState)
    {
        base.Update(gameTime, keyboardState);
        Sprite.ProcessAnimation(gameTime);

        HandleCollisions();
    }

    private void HandleCollisions()
    {
        // Огромное поле для оптимизации, самое простое - сделать отдельный метод, учитывающий только танки и не подсчитывающий глубину коллизии.
        var allTankCollisions = Level.GetAllCollisionsSimple(this)
            .Where(levelObject => levelObject is Tank)
            .ToArray();

        if (allTankCollisions.Length == 0)
            return;

        // Приоритет на игроке.
        var player = allTankCollisions.OfType<PlayerTank>().FirstOrDefault();
        if (player is not null)
        {
            ApplyEffectOnPlayer(player);
            Remove();
            return;
        }

        if (!Level.BotsCanGrabBonuses)
            return;

        var bot = allTankCollisions.OfType<BotTank>().FirstOrDefault();
        if (bot is not null)
        {
            ApplyEffectOnBot(bot);
            Remove();
            return;
        }

        throw new Exception("Unknown tank type.");
    }
}