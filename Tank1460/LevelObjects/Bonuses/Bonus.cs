using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tank1460.Audio;
using Tank1460.Extensions;
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
                Level.GetPlayerSpawner(playerTank.PlayerIndex).AddOneUp();
                break;

            case BonusType.Pistol:
                playerTank.UpgradeMax();
                break;

            case BonusType.Star:
                playerTank.UpgradeUp();
                break;

            case BonusType.Grenade:
                Level.BotManager.ExplodeAll(playerTank);
                break;

            case BonusType.Ship:
                playerTank.AddShip();
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
            case BonusType.Grenade:
                Level.PlayerTanks.ForEach(playerTank => playerTank.Explode(botTank));
                break;

            case BonusType.Pistol:
                botTank.TransformIntoType3();
                break;

            case BonusType.Star:
                Level.BotManager.BotTanks.ForEach(tank => tank.GiveArmorPiercingShells());
                break;

            case BonusType.Ship:
                botTank.AddShip();
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        Sprite.ProcessAnimation(gameTime);

        HandleCollisions();
    }

    private void HandleCollisions()
    {
        // TODO: Тут надо всё оптимизировать, хотя бы сделать отдельный метод, учитывающий только танки.
        var allTankCollisions = Level.GetAllCollisionsSimple(this)
            .Where(levelObject => levelObject is Tank { State: TankState.Normal })
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

        if (Level.BotsCanGrabBonuses)
        {
            var bot = allTankCollisions.OfType<BotTank>().FirstOrDefault();
            if (bot is not null)
            {
                ApplyEffectOnBot(bot);
                Remove();
                return;
            }
        }

        throw new Exception("Unknown tank type.");
    }
}