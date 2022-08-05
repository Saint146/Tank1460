using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using Tank1460.Audio;
using Tank1460.Extensions;

namespace Tank1460.LevelObjects.Tanks;

public class PlayerTank : Tank
{
    public int PlayerNumber { get; }

#if DEBUG
    private bool _godMode = false;
#endif

    protected UpgradeLevel UpgradeLevel
    {
        get => _upgradeLevel;
        private set
        {
            _upgradeLevel = value;
            TurnTo(Direction);
        }
    }

    private readonly Dictionary<int, string> PlayerNumberToColorMap = new()
    {
        { 1, "Yellow" },
        { 2, "Green" }
    };

    private UpgradeLevel _upgradeLevel = UpgradeLevel.Basic;

    private readonly Dictionary<UpgradeLevel, Dictionary<ObjectDirection, IAnimation>> _animations = new();

    protected override int[] SpawnAnimationTimesInFrames() => new[] { 2, 3, 3, 4, 2, 3, 4, 3, 2, 4, 3, 3, 1 };

    protected override IReadOnlyDictionary<ObjectDirection, IAnimation> Animations() => _animations[_upgradeLevel];

    protected override ShootingProperties ShootingProperties() => ShootingPropertiesProvider.GetPlayerProperties(_upgradeLevel);

    private readonly Dictionary<Color, Color> Player2RecolorMap = new();

    public PlayerTank(Level level, int playerNumber) : base(level)
    {
        PlayerNumber = playerNumber;
    }

    protected override void LoadContent()
    {
        //Player2RecolorMap.Add(new Color(0, 0, 0, 0), new Color(0, 0, 0, 0));
        Player2RecolorMap.Add(new Color(255, 240, 144), new Color(181, 247, 206));
        Player2RecolorMap.Add(new Color(255, 160, 0), new Color(0, 140, 49));
        Player2RecolorMap.Add(new Color(136, 136, 0), new Color(0, 82, 0));

        base.LoadContent();

        foreach (UpgradeLevel level in Enum.GetValues(typeof(UpgradeLevel)))
        {
            var levelAnimations = new Dictionary<ObjectDirection, IAnimation>();
            _animations[level] = levelAnimations;

            foreach (ObjectDirection direction in Enum.GetValues(typeof(ObjectDirection)))
            {
                var texture = Level.Content.LoadRecoloredTexture($"Sprites/Tank/Type{(int)level}/{direction}",
                                                                 $"Sprites/_R/Tank/{PlayerNumberToColorMap[PlayerNumber]}");

                var animation = new Animation(texture, true);
                levelAnimations.Add(direction, animation);
            }
        }

        TurnTo(ObjectDirection.Up);
    }

    protected override void OnSpawn()
    {
        base.OnSpawn();
        AddTimedInvulnerability(184 * Tank1460Game.OneFrameSpan);
    }

    protected override void Think(GameTime gameTime, KeyboardState keyboardState)
    {
        if (State != TankState.Normal)
            return;

        // TODO: придумать как передавать управление, пока супер-костыль
        if (keyboardState.IsKeyDown(PlayerNumber == 1 ? Keys.A : Keys.Left))
            IssueMoveOrderTo(ObjectDirection.Left);
        else if (keyboardState.IsKeyDown(PlayerNumber == 1 ? Keys.D : Keys.Right))
            IssueMoveOrderTo(ObjectDirection.Right);
        else if (keyboardState.IsKeyDown(PlayerNumber == 1 ? Keys.W : Keys.Up))
            IssueMoveOrderTo(ObjectDirection.Up);
        else if (keyboardState.IsKeyDown(PlayerNumber == 1 ? Keys.S : Keys.Down))
            IssueMoveOrderTo(ObjectDirection.Down);

        if (keyboardState.IsKeyDown(PlayerNumber == 1 ? Keys.K : Keys.NumPad2))
            IssueShootOrder(gameTime);

        if (KeyboardEx.HasBeenPressed(PlayerNumber == 1 ? Keys.L : Keys.NumPad3))
            IssueShootOrder(gameTime);

#if DEBUG
        if (KeyboardEx.HasBeenPressed(Keys.F10))
            _godMode = !_godMode;

        if (KeyboardEx.HasBeenPressed(Keys.PageUp))
            UpgradeLevel = UpgradeLevel.LevelUp();

        if (KeyboardEx.HasBeenPressed(Keys.PageDown))
            UpgradeLevel = UpgradeLevel.LevelDown();

        if (KeyboardEx.HasBeenPressed(Keys.Enter))
            Explode();
#endif
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.Draw(gameTime, spriteBatch);

#if DEBUG
        if (_godMode)
            spriteBatch.DrawEllipse(BoundingRectangle.Center.ToVector2(), new Vector2(2), 4, Color.Black);
#endif
    }

    protected void IssueMoveOrderTo(ObjectDirection newDirection)
    {

        // Сразу после поворота танк игрока ещё не движется.
        if (newDirection == Direction)
            MoveTo(newDirection);
        else
            TurnTo(newDirection);
    }

    public void UpgradeUp()
    {
        UpgradeLevel = UpgradeLevel.LevelUp();
    }

    public void UpgradeMax()
    {
        UpgradeLevel = UpgradeLevel.ArmorPiercing;
    }

    public override void HandleShot(Shell shell)
    {
#if DEBUG
        if (_godMode) return;
#endif

        if (IsInvulnerable())
            return;

        if (State != TankState.Normal)
            return;

        if (UpgradeLevel == UpgradeLevel.ArmorPiercing)
        {
            Level.SoundPlayer.Play(Sound.HitHurt);
            UpgradeLevel = UpgradeLevel.LevelDown();
            return;
        }

        Explode();
    }
}