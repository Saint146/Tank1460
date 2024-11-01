using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Tank1460.AI;
using Tank1460.Audio;
using Tank1460.Common.Level.Object.Tank;
using Tank1460.Extensions;
using Tank1460.Globals;
using Tank1460.Input;

namespace Tank1460.LevelObjects.Tanks;

public class PlayerTank : Tank
{
    public PlayerIndex PlayerIndex { get; }

    public bool IsControlledByAi { get; }

#if DEBUG
    internal bool GodMode;
#endif

    /// <summary>
    /// Приказ игрока в предыдущий такт, актуально только для танков реальных игроков.
    /// </summary>
    private TankOrder previousOrder = TankOrder.None;
    private TankSlidingState slidingState = TankSlidingState.None;
    /// <summary>
    /// Если танк находится в процессе скольжения, это время, когда он перестанет скользить.
    /// </summary>
    private double timeToFinishSliding;

    /// <summary>
    /// Цвет танка в зависимости от номера игрока.
    /// </summary>
    private static readonly Dictionary<PlayerIndex, TankColor> PlayerNumberToColorMap = new()
    {
        { PlayerIndex.One, TankColor.Yellow },
        { PlayerIndex.Two, TankColor.Green },
        { PlayerIndex.Three, TankColor.Blue },
        { PlayerIndex.Four, TankColor.Blue | TankColor.Red }
    };

    private PlayerInput _playerInput;

    private readonly PlayerTankAi _ai;

    protected override int[] SpawnAnimationTimesInFrames() => new[] { 2, 3, 3, 4, 2, 3, 4, 3, 2, 4, 3, 3, 1 };

    public PlayerTank(Level level, PlayerIndex playerIndex, TankType type, bool controlledByAi) : base(level, type, PlayerNumberToColorMap[playerIndex])
    {
        PlayerIndex = playerIndex;
        IsControlledByAi = controlledByAi;

        if (controlledByAi)
            _ai = new CommonPlayerTankAi(this, level);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        switch (slidingState)
        {
            case TankSlidingState.None:
                break;

            case TankSlidingState.InProgress:
                if (gameTime.TotalGameTime.TotalSeconds >= timeToFinishSliding)
                {
                    slidingState = TankSlidingState.JustStopped;
                }
                break;

            case TankSlidingState.Started:
                slidingState = TankSlidingState.InProgress;
                timeToFinishSliding = gameTime.TotalGameTime.TotalSeconds + GameRules.TimeInFrames(28);
                break;

            case TankSlidingState.JustStopped:
                slidingState = TankSlidingState.None;
                break;
        }
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
        if (Type == TankType.P4)
            return;

        var newType = (int)Type + 1;
        if (!Enum.IsDefined(typeof(TankType), newType))
            newType = (int)TankType.P4;

        SetType((TankType)newType);
    }

    public void UpgradeDown()
    {
        if (Type == TankType.P0)
            return;

        var newType = (int)Type - 1;
        if (!Enum.IsDefined(typeof(TankType), newType))
            newType = (int)TankType.P0;

        SetType((TankType)newType);
    }

    public void UpgradeToPistol()
    {
        if (Type >= TankType.P3)
            return;

        SetType(TankType.P3);
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

        if (Type >= TankType.P3 && !Level.ClassicRules)
        {
            Level.SoundPlayer.Play(Sound.HitHurt);
            SetType(TankType.P2);
            return;
        }

        Explode(damagedBy);
    }

    protected override TankOrder Think(GameTime gameTime)
    {
        if (IsControlledByAi)
            return _ai.Think();

        var order = TankOrder.None;

        if (slidingState == TankSlidingState.InProgress)
        {
            // TODO: Проверить оригинал, возможно, отвернуть можно всегда, и тогда это надо убрать ниже.
            order = previousOrder.GetMovementOnly();
        }
        else
        {
            if (_playerInput.Active.HasFlag(PlayerInputCommands.Left))
                order |= TankOrder.MoveLeft;

            if (_playerInput.Active.HasFlag(PlayerInputCommands.Right))
                order |= TankOrder.MoveRight;

            if (_playerInput.Active.HasFlag(PlayerInputCommands.Up))
                order |= TankOrder.MoveUp;

            if (_playerInput.Active.HasFlag(PlayerInputCommands.Down))
                order |= TankOrder.MoveDown;

            if (order == TankOrder.None && previousOrder.GetMovementOnly() != TankOrder.None && slidingState != TankSlidingState.JustStopped && Level.IsObjectOnIce(this))
            {
                order = previousOrder.GetMovementOnly();
                slidingState = TankSlidingState.Started;
                //Level.SoundPlayer.Play(Sound.Slide);
            }
        }

        if (_playerInput.Active.HasFlag(PlayerInputCommands.ShootTurbo) || _playerInput.Pressed.HasFlag(PlayerInputCommands.Shoot))
            order |= TankOrder.Shoot;

        previousOrder = order;

        Debug.WriteLine(order.ToString());

        return order;
    }
}