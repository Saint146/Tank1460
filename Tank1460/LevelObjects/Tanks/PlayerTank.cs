using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using System.Collections.Generic;
using Tank1460.Audio;
using Tank1460.PlayerInput;

namespace Tank1460.LevelObjects.Tanks;

public class PlayerTank : Tank
{
    public PlayerIndex PlayerIndex { get; }

#if DEBUG
    internal bool GodMode;
#endif

    /// <summary>
    /// Цвет танка в зависимости от номера игрока.
    /// </summary>
    private static readonly Dictionary<PlayerIndex, TankColor> PlayerNumberToColorMap = new()
    {
        { PlayerIndex.One, TankColor.Yellow },
        { PlayerIndex.Two, TankColor.Green }
    };

    private PlayerInputs _inputs;

    protected override int[] SpawnAnimationTimesInFrames() => new[] { 2, 3, 3, 4, 2, 3, 4, 3, 2, 4, 3, 3, 1 };

    public PlayerTank(Level level, PlayerIndex playerIndex) : base(level, TankType.Type0, PlayerNumberToColorMap[playerIndex])
    {
        PlayerIndex = playerIndex;
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.Draw(gameTime, spriteBatch);

#if DEBUG
        if (GodMode)
            spriteBatch.DrawEllipse(BoundingRectangle.Center.ToVector2(), new Vector2(2), 4, Microsoft.Xna.Framework.Color.Black);
#endif
    }

    public void UpgradeUp()
    {
        if (Type == TankType.Type3)
            return;

        SetType((TankType)(((int)Type + 1) % 4));
    }

    public void UpgradeDown()
    {
        if (Type == TankType.Type0)
            return;

        SetType((TankType)(((int)Type + 3) % 4));
    }

    public void UpgradeMax()
    {
        SetType(TankType.Type3);
    }

    public void HandleInput(PlayerInputs inputs)
    {
        _inputs = inputs;

        if (_inputs.HasFlag(PlayerInputs.Start))
        {
            // TODO: level handle player pressed start
        }
    }

    public void AddPointsReward(int points)
    {

    }

    protected override void OnSpawn()
    {
        base.OnSpawn();
        AddTimedInvulnerability(184 * Tank1460Game.OneFrameSpan);
    }

    protected override void HandleDamaged(Tank damagedBy)
    {
#if DEBUG
        if (GodMode) return;
#endif

        if (Type == TankType.Type3)
        {
            Level.SoundPlayer.Play(Sound.HitHurt);
            SetType(TankType.Type2);
            return;
        }

        Explode(damagedBy);
    }

    protected override TankOrder Think(GameTime gameTime)
    {
        var order = TankOrder.None;

        if (_inputs.HasFlag(PlayerInputs.Left))
            order |= TankOrder.MoveLeft;

        if (_inputs.HasFlag(PlayerInputs.Right))
            order |= TankOrder.MoveRight;

        if (_inputs.HasFlag(PlayerInputs.Up))
            order |= TankOrder.MoveUp;

        if (_inputs.HasFlag(PlayerInputs.Down))
            order |= TankOrder.MoveDown;

        if (_inputs.HasFlag(PlayerInputs.Shoot))
            order |= TankOrder.Shoot;

        return order;
    }
}