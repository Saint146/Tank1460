using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Tank1460.Audio;
using Tank1460.Extensions;
using Tank1460.LevelObjects.Explosions;
using Tank1460.LevelObjects.Tiles;

namespace Tank1460.LevelObjects.Tanks;

public abstract class Tank : MoveableLevelObject
{
    public TankState State { get; set; } = TankState.Spawning;

    private Explosion _explosion;

    protected ObjectDirection Direction { get; private set; }

    private IAnimation _spawnAnimation;

    protected abstract int[] SpawnAnimationTimesInFrames();

    protected abstract IReadOnlyDictionary<ObjectDirection, IAnimation> Animations();

    private double _lastFireTime;
    private readonly List<Shell> _shells = new();

    protected abstract ShootingProperties ShootingProperties();

    private const double FireDelay = 2 * Tank1460Game.OneFrameSpan;

    private readonly TankEffects _activeEffects = new();

    public override CollisionType CollisionType =>
        State == TankState.Normal ? CollisionType.ShootableAndImpassable : CollisionType.None;

    protected Tank(Level level) : base(level, 0.75f)
    {
    }

    protected void IssueShootOrder(GameTime gameTime)
    {
        if (_shells.Count >= ShootingProperties().MaxShells)
            return;

        if (gameTime.TotalGameTime.TotalSeconds < _lastFireTime + FireDelay)
            return;

        Shoot(gameTime);
    }

    private void Shoot(GameTime gameTime)
    {
        _lastFireTime = gameTime.TotalGameTime.TotalSeconds;
        var shell = new Shell(Level, Direction, ShootingProperties().ShellSpeed, this, ShootingProperties().ShellDamage);
        shell.SpawnViaCenterPosition(BoundingRectangle.GetEdgeCenter(Direction));
        _shells.Add(shell);

        if (this is PlayerTank)
            Level.SoundPlayer.Play(Sound.Shot);
    }

    protected override void LoadContent()
    {
        var spawnAnimationTimes = SpawnAnimationTimesInFrames().Select(t => t * Tank1460Game.OneFrameSpan).ToArray();
        _spawnAnimation = new Animation(Level.Content.Load<Texture2D>(@"Sprites/Effects/SpawnNova"), spawnAnimationTimes, false);
    }

    public override void Update(GameTime gameTime, KeyboardState keyboardState)
    {
        Debug.Assert(State != TankState.Unknown);
        switch (State)
        {
            case TankState.Spawning:
                if (Sprite.HasAnimationEnded)
                {
                    State = TankState.Normal;
                    TurnTo(Direction);
                    OnSpawn();
                }
                else
                    Sprite.ProcessAnimation(gameTime);

                break;

            case TankState.Exploding when _explosion.ToRemove:
                _explosion = null;
                State = TankState.Destroyed;
                Remove();
                break;

            case TankState.Normal:
                Sprite.ProcessAnimation(gameTime);

                CalcIsFrontTileBlocked();
                Think(gameTime, keyboardState);

                _shells.RemoveAll(s => s.ToRemove);

                if (MovingDirection is not null)
                    Level.SoundPlayer.Loop(this is PlayerTank ? Sound.MovePlayer : Sound.MoveBot);

                break;
        }

        base.Update(gameTime, keyboardState);
        _activeEffects.Update(gameTime);
    }

    protected virtual void OnSpawn()
    {
    }

    protected bool IsFrontTileBlocked { get; private set; } = true;

    protected override IAnimation GetDefaultAnimation() => _spawnAnimation;

    protected override bool CanMove() => !IsFrontTileBlocked;

    protected override void HandleTryMove()
    {
        base.HandleTryMove();
        Sprite.AdvanceAnimation();
        Level.SoundPlayer.Loop(this is PlayerTank ? Sound.MovePlayer : Sound.MoveBot);
    }

    private void CalcIsFrontTileBlocked()
    {
        if (!IsTankCenteredOnTile())
            IsFrontTileBlocked = false;
        else
            IsFrontTileBlocked = TileRectangle.NearestTiles(Direction).GetAllPoints().Any(point => !Level.IsTileFree(point));
    }

    protected bool IsTankCenteredOnTile() => Position.X % Tile.DefaultWidth == 0 && Position.Y % Tile.DefaultHeight == 0;

    protected bool IsInvulnerable() => _activeEffects.HasEffect<Invulnerability>();

    public void AddTimedInvulnerability(double invulnerabilityTime)
    {
        _activeEffects.AddExclusive(new Invulnerability(Level, invulnerabilityTime));
    }

    protected abstract void Think(GameTime gameTime, KeyboardState keyboardState);

    protected void TurnTo(ObjectDirection newDirection)
    {
        // При повороте танка на 90° округляем его координаты до клетки (механика из оригинальной игры для более удобного прохождения между препятствиями)
        if (newDirection.Has90DegreesDifference(Direction))
            TrimPositionToTile();

        Direction = newDirection;
        Sprite.PlayAnimation(Animations()[newDirection]);
    }

    private void TrimPositionToTile()
    {
        Position = new Point((int)Math.Round((double)Position.X / (double)Tile.DefaultWidth) * Tile.DefaultWidth, (int)Math.Round((double)Position.Y / (double)Tile.DefaultHeight) * Tile.DefaultHeight);
    }

    protected void MoveTo(ObjectDirection newDirection)
    {
        if (State != TankState.Normal)
            return;

        TurnTo(newDirection);
        MovingDirection = newDirection;
    }

    public abstract void HandleShot(Shell shell);

    protected void Explode()
    {
        State = TankState.Exploding;
        _explosion = new BigExplosion(Level);
        _explosion.SpawnViaCenterPosition(BoundingRectangle.Center);
        MovingDirection = null;

        Level.SoundPlayer.Play(this is PlayerTank ? Sound.ExplosionBig : Sound.ExplosionSmall);
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (State != TankState.Normal && State != TankState.Spawning)
            return;

        base.Draw(gameTime, spriteBatch);
        _activeEffects.Draw(spriteBatch, Position.ToVector2());
    }
}