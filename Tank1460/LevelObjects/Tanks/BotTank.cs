using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Diagnostics;
using Tank1460.AI;
using Tank1460.Audio;
using Tank1460.Common.Level.Object.Tank;
using Tank1460.Extensions;

namespace Tank1460.LevelObjects.Tanks;

public class BotTank : Tank
{
    public int PeriodIndex { get; set; }

    public int Index { get; }

    internal int Hp { get; private set; }

    protected override int[] SpawnAnimationTimesInFrames() => new[] { 4, 4, 4, 6, 4, 4, 6, 4, 4, 6, 4, 4, 2 };

    private readonly BotTankAi _ai;

    /// <summary>
    /// Цвет танка в зависимости от хп.
    /// </summary>
    private static readonly Dictionary<int, TankColor> HpToColorMap = new()
    {
        { 1, TankColor.Gray },
        { 2, TankColor.Yellow | TankColor.Green },
        { 3, TankColor.Gray | TankColor.Yellow },
        { 4, TankColor.Gray | TankColor.Green },
        { 5, TankColor.Gray | TankColor.Green },
        { 6, TankColor.Gray | TankColor.Green },
        { 7, TankColor.Gray | TankColor.Green },
        { 8, TankColor.Gray | TankColor.Green }
    };

    public BotTank(Level level, TankType type, int hp, int bonusCount, int index, int periodIndex) : base(level, type, HpToTankColor(hp), bonusCount)
    {
        Hp = hp;
        Index = index;
        PeriodIndex = periodIndex;

        _ai = new ClassicBotTankAi(this, level);
    }

    public void GiveArmorPiercingShells()
    {
        SetShellProperties(ShellProperties.ArmorPiercing);
    }

    public void TransformIntoType9()
    {
        // TODO: Тут два раза обновляются текстуры
        SetHp(4);
        SetType(TankType.B9);
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.Draw(gameTime, spriteBatch);
#if DEBUG
        if (!GameRules.ShowBotsPeriods || Status is not TankStatus.Normal)
            return;

        switch (PeriodIndex)
        {
            case 0:
                break;
            case 1:
                var targetPlayer = Level.GetTargetPlayerForBot(Index);
                spriteBatch.DrawDebugArrow(BoundingRectangle, Microsoft.Xna.Framework.Color.Gold, targetPlayer?.BoundingRectangle.Center);
                break;
            default:
                var targetFalcon = Level.GetTargetFalconForBot(Index);
                spriteBatch.DrawDebugArrow(BoundingRectangle, Microsoft.Xna.Framework.Color.Red, targetFalcon?.BoundingRectangle.Center);
                break;
        }
#endif
    }

    protected override TankOrder Think(GameTime gameTime)
    {
        return _ai.Think();
    }

    protected override void HandleDamaged(Tank damagedBy)
    {
        Debug.Assert(damagedBy is PlayerTank, "Боты не могут бить друг друга.");

        if (Hp <= 1)
        {
            Explode(damagedBy);
            return;
        }

        Level.SoundPlayer.Play(Sound.HitHurt);
        SetHp(Hp - 1);
    }

    private static TankColor HpToTankColor(int hp)
    {
        Debug.Assert(hp is > 0 and <= 8);
        return HpToColorMap[hp];
    }

    public void SetHp(int newHp)
    {
        Debug.Assert(newHp is > 0 and <= 8);
        Hp = newHp;

        SetColor(HpToTankColor(newHp));
    }
}