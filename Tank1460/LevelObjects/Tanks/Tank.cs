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

    protected TankType Type { get; private set; }

    protected ObjectDirection Direction { get; private set; } = ObjectDirection.Up;

    protected bool IsFrontTileBlocked { get; private set; } = true;

    /// <summary>
    /// У танков игроков и ботов разное время звезды перед спауном. Пока способ это отразить - через такую абстракцию.
    /// TODO: Сделать нову отдельной сущностью.
    /// </summary>
    protected abstract int[] SpawnAnimationTimesInFrames();

    private const double FireDelay = 2 * Tank1460Game.OneFrameSpan;
    private IReadOnlyDictionary<ObjectDirection, IAnimation> _animations;
    private IAnimation _spawnAnimation;
    private Explosion _explosion;
    private double _lastFireTime;
    private readonly List<Shell> _shells = new();
    private readonly TankEffects _activeEffects = new();
    private int _maxShells;
    private ShellDamage _shellDamage;
    private ShellSpeed _shellSpeed;
    private readonly TankColor _color;
    private int _bonusCount;

    protected Tank(Level level, TankType type, TankColor color, int bonusCount) : base(level, 0.75f)
    {
        State = TankState.Spawning;
        _color = color;
        _bonusCount = bonusCount;
        SetType(type);
    }

    protected Tank(Level level, TankType type, TankColor color) : this(level, type, color, 0)
    {
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

    public void HandleShot(Shell shell)
    {
        if (IsInvulnerable())
            return;

        if (State != TankState.Normal)
            return;

        if (_bonusCount > 0)
        {
            _bonusCount--;
            Level.BonusManager.Spawn();
        }

        HandleDamaged(shell.ShotBy);
    }

    protected override void LoadContent()
    {
        // Звезда при респауне.
        var spawnAnimationTimes = SpawnAnimationTimesInFrames().Select(t => t * Tank1460Game.OneFrameSpan).ToArray();
        _spawnAnimation = new Animation(Level.Content.Load<Texture2D>(@"Sprites/Effects/SpawnNova"), spawnAnimationTimes, false);
    }

    private static Dictionary<ObjectDirection, IAnimation> LoadAnimationsForType(ContentManagerEx content,
        TankType type, TankColor color, bool isFlashingBonus)
    {
        var oneTypeAnimations = new Dictionary<ObjectDirection, IAnimation>();

        foreach (ObjectDirection direction in Enum.GetValues(typeof(ObjectDirection)))
            oneTypeAnimations[direction] = LoadAnimationsForDirection(content, type, color, isFlashingBonus, direction);

        return oneTypeAnimations;
    }

    private static IAnimation LoadAnimationsForDirection(ContentManagerEx content, TankType type, TankColor color,
        bool isFlashingBonus, ObjectDirection direction)
    {
        // Загружаем обычные текстуры в любом случае.
        var plainTexture = content.LoadRecoloredTexture(
            $"Sprites/Tank/Type{(int)type}/{direction}",
            $"Sprites/_R/Tank/{color}");

        if (!isFlashingBonus)
            return new Animation(plainTexture, true);

        // Но только в случае бонуса подгружаем нужную текстуру и подключаем "двумерную" анимацию.
        var bonusTexture = content.LoadRecoloredTexture(
            $"Sprites/Tank/Type{(int)type}/{direction}",
            $"Sprites/_R/Tank/Red");

        return new ShiftingAnimation(new[] { bonusTexture, plainTexture }, double.MaxValue, true,
            8 * Tank1460Game.OneFrameSpan);
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
        PlayCurrentAnimation();
    }

    protected void SetType(TankType type)
    {
        Type = type;
        RefreshTankProperties();

        // Каждый раз при смене типа подгружается новая анимация.
        // TODO: Возможно, сделать у самих анимаций возможность подгружать новые текстуры, чтобы не пересоздавать объект? Так можно заодно и не начинать мигание заново.
        _animations = LoadAnimationsForType(Level.Content, Type, _color, _bonusCount > 0);

        PlayCurrentAnimation();
    }

    protected abstract TankOrder Think(GameTime gameTime, KeyboardState keyboardState);

    protected abstract void HandleDamaged(Tank damagedBy);

    protected virtual void OnSpawn()
    {
    }

    private void PlayCurrentAnimation()
    {
        //if (_animations is null)
        //    return;

        Sprite.PlayAnimation(_animations[Direction]);
    }

    private void RefreshTankProperties()
    {
        var properties = TankPropertiesProvider.Get(Type);

        SetMovingSpeed(properties.TankSpeed);
        _maxShells = properties.MaxShells;
        _shellDamage = properties.ShellDamage;
        _shellSpeed = properties.ShellSpeed;
    }

    private void TryShoot(GameTime gameTime)
    {
        if (_shells.Count >= _maxShells)
            return;

        if (gameTime.TotalGameTime.TotalSeconds < _lastFireTime + FireDelay)
            return;

        Shoot(gameTime);
    }

    private void Shoot(GameTime gameTime)
    {
        _lastFireTime = gameTime.TotalGameTime.TotalSeconds;
        var shell = new Shell(Level, Direction, _shellSpeed, this, _shellDamage);
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