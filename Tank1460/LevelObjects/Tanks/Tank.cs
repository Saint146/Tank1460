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
    public TankState State { get; set; }

    public override CollisionType CollisionType =>
        State == TankState.Normal ? CollisionType.ShootableAndImpassable : CollisionType.None;

    protected ObjectDirection Direction { get; private set; }

    protected bool IsFrontTileBlocked { get; private set; } = true;

    protected abstract int[] SpawnAnimationTimesInFrames();

    protected abstract IReadOnlyDictionary<ObjectDirection, IAnimation> Animations();

    protected abstract ShootingProperties ShootingProperties();

    private const double FireDelay = 2 * Tank1460Game.OneFrameSpan;
    private IAnimation _spawnAnimation;
    private Explosion _explosion;
    private double _lastFireTime;
    private readonly List<Shell> _shells = new();
    private readonly TankEffects _activeEffects = new();

    protected Tank(Level level) : base(level, 0.75f)
    {
        State = TankState.Spawning;
    }

    public sealed override void Update(GameTime gameTime, KeyboardState keyboardState)
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

                // Даём танку подумать и обрабатываем придуманный приказ.
                var order = Think(gameTime, keyboardState);

                if (order.HasFlag(TankOrder.Shoot))
                    TryShoot(gameTime);

                var newDirection = order.ToDirection();
                if (newDirection is not null)
                {
                    MoveTo(newDirection.Value);
                    Level.SoundPlayer.Loop(this is PlayerTank ? Sound.MovePlayer : Sound.MoveBot);
                }

                break;
        }

        base.Update(gameTime, keyboardState);
        _activeEffects.Update(gameTime);
        _shells.RemoveAll(s => s.ToRemove);
    }

    public void AddTimedInvulnerability(double invulnerabilityTime)
    {
        _activeEffects.AddExclusive(new Invulnerability(Level, invulnerabilityTime));
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (State != TankState.Normal && State != TankState.Spawning)
            return;

        base.Draw(gameTime, spriteBatch);
        _activeEffects.Draw(spriteBatch, Position.ToVector2());
    }

    public abstract void HandleShot(Shell shell);

    protected override void LoadContent()
    {
        var spawnAnimationTimes = SpawnAnimationTimesInFrames().Select(t => t * Tank1460Game.OneFrameSpan).ToArray();
        _spawnAnimation = new Animation(Level.Content.Load<Texture2D>(@"Sprites/Effects/SpawnNova"), spawnAnimationTimes, false);
    }

    protected override IAnimation GetDefaultAnimation() => _spawnAnimation;

    protected override bool CanMove() => !IsFrontTileBlocked;

    protected override void HandleTryMove()
    {
        base.HandleTryMove();
        Sprite.AdvanceAnimation();
        Level.SoundPlayer.Loop(this is PlayerTank ? Sound.MovePlayer : Sound.MoveBot);
    }

    protected void Explode(Tank destroyedBy)
    {
        State = TankState.Exploding;
        _explosion = new BigExplosion(Level);
        _explosion.SpawnViaCenterPosition(BoundingRectangle.Center);
        //MovingDirection = null;

        Level.SoundPlayer.Play(this is PlayerTank ? Sound.ExplosionBig : Sound.ExplosionSmall);
    }

    protected bool IsTankCenteredOnTile() => Position.X % Tile.DefaultWidth == 0 && Position.Y % Tile.DefaultHeight == 0;

    protected bool IsInvulnerable() => _activeEffects.HasEffect<Invulnerability>();

    protected void TurnTo(ObjectDirection newDirection)
    {
        // При повороте танка на 90° округляем его координаты до клетки (механика из оригинальной игры для более удобного прохождения между препятствиями)
        if (newDirection.Has90DegreesDifference(Direction))
            Position = new Point((int)Math.Round((double)Position.X / Tile.DefaultWidth) * Tile.DefaultWidth, (int)Math.Round((double)Position.Y / Tile.DefaultHeight) * Tile.DefaultHeight);

        Direction = newDirection;
        Sprite.PlayAnimation(Animations()[newDirection]);
    }

    protected abstract TankOrder Think(GameTime gameTime, KeyboardState keyboardState);

    protected virtual void OnSpawn()
    {
    }

    private void TryShoot(GameTime gameTime)
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

    private void CalcIsFrontTileBlocked()
    {
        if (!IsTankCenteredOnTile())
            IsFrontTileBlocked = false;
        else
            IsFrontTileBlocked = TileRectangle.NearestTiles(Direction).GetAllPoints().Any(point => !Level.IsTileFree(point));
    }

    private void MoveTo(ObjectDirection newDirection)
    {
        Debug.Assert(State == TankState.Normal);

        // Сразу после поворота танк ещё не движется.
        if (newDirection == Direction)
            MovingDirection = newDirection;
        else
        {
            TurnTo(newDirection);
            MovingDirection = null;
        }
    }
}