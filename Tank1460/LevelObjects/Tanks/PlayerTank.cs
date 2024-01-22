using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using System.Collections.Generic;
using Tank1460.Audio;

namespace Tank1460.LevelObjects.Tanks;

public class PlayerTank : Tank
{
    public int PlayerNumber { get; }

#if DEBUG
    private bool _godMode;
#endif

    /// <summary>
    /// Цвет танка в зависимости от номера игрока.
    /// </summary>
    private static readonly Dictionary<int, TankColor> PlayerNumberToColorMap = new()
    {
        { 1, TankColor.Yellow },
        { 2, TankColor.Green }
    };

    protected override int[] SpawnAnimationTimesInFrames() => new[] { 2, 3, 3, 4, 2, 3, 4, 3, 2, 4, 3, 3, 1 };

    public PlayerTank(Level level, int playerNumber) : base(level, TankType.Type0, PlayerNumberToColorMap[playerNumber])
    {
        PlayerNumber = playerNumber;
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.Draw(gameTime, spriteBatch);

#if DEBUG
        if (_godMode)
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

    protected override void OnSpawn()
    {
        base.OnSpawn();
        AddTimedInvulnerability(184 * Tank1460Game.OneFrameSpan);
    }

    protected override void HandleDamaged(Tank damagedBy)
    {
#if DEBUG
        if (_godMode) return;
#endif

        if (Type == TankType.Type3)
        {
            Level.SoundPlayer.Play(Sound.HitHurt);
            SetType(TankType.Type2);
            return;
        }

        Explode(damagedBy);
    }

    protected override TankOrder Think(GameTime gameTime, KeyboardState keyboardState)
    {
        var order = TankOrder.None;

        // TODO: придумать как передавать управление, пока супер-костыль
        if (keyboardState.IsKeyDown(PlayerNumber == 1 ? Keys.A : Keys.Left))
            order |= TankOrder.MoveLeft;

        if (keyboardState.IsKeyDown(PlayerNumber == 1 ? Keys.D : Keys.Right))
            order |= TankOrder.MoveRight;

        if (keyboardState.IsKeyDown(PlayerNumber == 1 ? Keys.W : Keys.Up))
            order |= TankOrder.MoveUp;

        if (keyboardState.IsKeyDown(PlayerNumber == 1 ? Keys.S : Keys.Down))
            order |= TankOrder.MoveDown;

        if (keyboardState.IsKeyDown(PlayerNumber == 1 ? Keys.K : Keys.NumPad2))
            order |= TankOrder.Shoot;

        if (KeyboardEx.HasBeenPressed(PlayerNumber == 1 ? Keys.L : Keys.NumPad3))
            order |= TankOrder.Shoot;

#if DEBUG
        if (KeyboardEx.HasBeenPressed(Keys.F10))
            _godMode = !_godMode;

        if (KeyboardEx.HasBeenPressed(Keys.PageUp))
            UpgradeUp();

        if (KeyboardEx.HasBeenPressed(Keys.PageDown))
            UpgradeDown();

        if (KeyboardEx.HasBeenPressed(Keys.Enter))
            Explode(this);
#endif

        return order;
    }
}