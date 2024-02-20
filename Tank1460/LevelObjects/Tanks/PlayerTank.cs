using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using System.Collections.Generic;
using Tank1460.Audio;
using Tank1460.Common.Level.Object.Tank;
using Tank1460.Input;

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

    private PlayerInput _playerInput;

    protected override int[] SpawnAnimationTimesInFrames() => new[] { 2, 3, 3, 4, 2, 3, 4, 3, 2, 4, 3, 3, 1 };

    public PlayerTank(Level level, PlayerIndex playerIndex, TankType type) : base(level, type, PlayerNumberToColorMap[playerIndex])
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

    public void HandleInput(PlayerInput playerInput)
    {
        _playerInput = playerInput;
    }

    protected override void OnSpawn()
    {
        base.OnSpawn();
        AddTimedInvulnerability(GameRules.TimeInFrames(184));
    }

    protected override void HandleDamaged(Tank damagedBy)
    {
#if DEBUG
        if (GodMode) return;
#endif

        if (Type == TankType.Type3 && !Level.ClassicRules)
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

        if (_playerInput.Active.HasFlag(PlayerInputCommands.Left))
            order |= TankOrder.MoveLeft;

        if (_playerInput.Active.HasFlag(PlayerInputCommands.Right))
            order |= TankOrder.MoveRight;

        if (_playerInput.Active.HasFlag(PlayerInputCommands.Up))
            order |= TankOrder.MoveUp;

        if (_playerInput.Active.HasFlag(PlayerInputCommands.Down))
            order |= TankOrder.MoveDown;

        if (_playerInput.Active.HasFlag(PlayerInputCommands.ShootTurbo) || _playerInput.Pressed.HasFlag(PlayerInputCommands.Shoot))
            order |= TankOrder.Shoot;

        return order;
    }
}