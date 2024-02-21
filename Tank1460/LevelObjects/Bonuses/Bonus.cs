using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Tank1460.Audio;
using Tank1460.Common.Extensions;
using Tank1460.LevelObjects.Tanks;

namespace Tank1460.LevelObjects.Bonuses;

public class Bonus : LevelObject
{
    public readonly BonusType Type;

    private IAnimation _animation;

    private const int ArmorTimeInFrames = 640;
    private const int ShovelTimeInFrames = 1280;
    private const int ClockOnBotsTimeInFrames = 640;

    private const int PointsRewardForBonus = 500;

    public Bonus(Level level, BonusType type) : base(level)
    {
        Type = type;
    }

    protected override IAnimation GetDefaultAnimation() => _animation;

    public void Destroy() => Remove();

    protected override void LoadContent()
    {
        _animation = new Animation(Level.Content.Load<Texture2D>($@"Sprites/Bonus/{Type}"), GameRules.TimeInFrames(8), true);
    }

    private void ApplyEffectOnPlayer(PlayerTank playerTank)
    {
        Level.SoundPlayer.Play(Sound.BonusPickup);
        Level.RewardPlayerWithPoints(playerTank.PlayerIndex, PointsRewardForBonus);
        Level.CreateFloatingText(BoundingRectangle.Center, PointsRewardForBonus.ToString(), GameRules.TimeInFrames(49));

        switch (Type)
        {
            case BonusType.Armor:
                playerTank.AddTimedInvulnerability(GameRules.TimeInFrames(ArmorTimeInFrames));
                break;

            case BonusType.OneUp:
                Level.GetPlayerSpawner(playerTank.PlayerIndex).AddOneUps();
                break;

            case BonusType.Pistol:
                playerTank.UpgradeToPistol();
                break;

            case BonusType.Star:
                playerTank.UpgradeUp();
                break;

            case BonusType.Grenade:
                // Передаём null, чтобы не давать очков игроку за уничтожение (логика оригинала).
                Level.BotManager.ExplodeAll(
#if DEBUG
                    playerTank
#else
                    null
#endif
                );
                break;

            case BonusType.Ship:
                playerTank.AddShip();
                break;

            case BonusType.Shovel:
                Level.RemoveAllEffects<UnprotectedFalconEffect>();
                Level.ArmorFalcons(GameRules.TimeInFrames(ShovelTimeInFrames));
                break;

            case BonusType.Clock:
                Level.BotManager.AddParalyze(GameRules.TimeInFrames(ClockOnBotsTimeInFrames));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void ApplyEffectOnBot(BotTank botTank)
    {
        Level.SoundPlayer.Play(Sound.BonusPickup);
        Level.CreateFloatingText(BoundingRectangle.Center, "BRUH", GameRules.TimeInFrames(49));

        switch (Type)
        {
            case BonusType.Armor:
                //botTank.AddTimedInvulnerability(armorTimeInFrames * Tank1460Game.OneFrameSpan);
                //break;

            case BonusType.OneUp:
                //Level.BotManager.AddOneUp();
                foreach (var tank in Level.BotManager.BotTanks)
                {
                    if (tank.BonusCount > 0)
                    {
                        tank.SetBonusCount(0);
                        // Тут он и перерисуется.
                        tank.SetHp(4);
                    }
                    else
                    {
                        tank.SetBonusCount(4);
                        // Тут он и перерисуется.
                        tank.SetHp(tank.Hp + 3);
                    }
                }
                break;

            case BonusType.Grenade:
                // Взрываем два случайных танка игроков.
                Level.GetAllPlayerTanks().ToArray().GetRandoms(2).ForEach(playerTank => playerTank.Explode(botTank));
                break;

            case BonusType.Pistol:
                botTank.TransformIntoType9();
                break;

            case BonusType.Star:
                Level.BotManager.BotTanks.ForEach(tank => tank.GiveArmorPiercingShells());
                break;

            case BonusType.Ship:
                botTank.AddShip();
                break;

            case BonusType.Shovel:
                Level.RemoveAllEffects<ArmoredFalconEffect>();
                Level.LeaveFalconsUnprotected();
                break;

            case BonusType.Clock:
                Level.GetAllPlayerTanks().ForEach(playerTank => playerTank.AddTimedImmobility());
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public override void Update(GameTime gameTime)
    {
        Sprite.ProcessAnimation(gameTime);

        HandleCollisions();
    }

    private void HandleCollisions()
    {
        // TODO: Тут надо всё оптимизировать, хотя бы сделать отдельный метод, учитывающий только танки.
        var allTankCollisions = Level.GetAllCollisionsSimple(this)
            .Where(levelObject => levelObject is Tank { Status: TankStatus.Normal })
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