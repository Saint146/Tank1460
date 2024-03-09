using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using Tank1460.AI;
using Tank1460.Audio;
using Tank1460.Common.Extensions;
using Tank1460.Common.Level.Object.Tank;
using Tank1460.Extensions;
using Tank1460.Globals;
using Tank1460.Input;
using Tank1460.LevelObjects.Tiles;

namespace Tank1460.LevelObjects.Tanks;

public class PlayerTank : Tank
{
    public PlayerIndex PlayerIndex { get; }

    public bool IsControlledByAi { get; }

#if DEBUG
    internal bool GodMode;
    private static readonly Color AiPathColor = new(240, 207, 200, 30);
#endif

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
            _ai = new AggressivePlayerTankAi(this, level);
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.Draw(gameTime, spriteBatch);

#if DEBUG
        if (GodMode)
            spriteBatch.DrawEllipse(BoundingRectangle.Center.ToVector2(), new Vector2(2), 4, Microsoft.Xna.Framework.Color.Black);

        if (GameRules.ShowAiPaths && _ai is AggressivePlayerTankAi aggressiveAi)
        {
            var path = aggressiveAi.LastCalculatedPath;
            if (!path.IsNullOrEmpty())
            {
                var halfTileSize = Tile.DefaultSize.Divide(2);
                var previousPoint = path[0] * Tile.DefaultSize + halfTileSize;
                for (var i = 1; i < path.Count; i++)
                {
                    var currentPoint = path[i] * Tile.DefaultSize + halfTileSize;
                    spriteBatch.DrawLine(previousPoint.ToVector2(), currentPoint.ToVector2(), AiPathColor);
                    previousPoint = currentPoint;
                }
            }

            if (aggressiveAi.Target is not null)
            {
                var targetPosition = aggressiveAi.Target.BoundingRectangle.Center;
                spriteBatch.DrawReticle(AiPathColor, targetPosition);
            }
        }
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