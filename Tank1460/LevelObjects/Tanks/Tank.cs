using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using Tank1460.Audio;
using Tank1460.Common.Extensions;
using Tank1460.Common.Level.Object;
using Tank1460.Common.Level.Object.Tank;
using Tank1460.Extensions;
using Tank1460.LevelObjects.Explosions;
using Tank1460.LevelObjects.Tiles;

namespace Tank1460.LevelObjects.Tanks;

public abstract class Tank : MoveableLevelObject
{
    public TankStatus Status { get; private set; }

    /// <summary>
    /// Запрет на движение.
    /// </summary>
    public bool IsImmobile { get; set; }

    /// <summary>
    /// Запрет на стрельбу.
    /// </summary>
    public bool IsPacifist { get; set; }

    public override CollisionType CollisionType =>
        Status == TankStatus.Normal ? CollisionType.ShootableAndImpassable : CollisionType.None;

    public bool HasShip { get; private set; }

    public int BonusCount { get; private set; }

    public TankType Type { get; private set; }

    protected TankColor Color { get; private set; }

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
    private double _timeTillReload;
    private readonly List<Shell> _shells = new();
    private readonly TankEffects _activeEffects = new();
    private int _maxShells;
    private ShellProperties _shellProperties;
    private ShellSpeed _shellSpeed;
    [CanBeNull] private string _afterExplosionText;

    protected Tank(Level level, TankType type, TankColor color, int bonusCount) : base(level, 0.75f)
    {
        Status = TankStatus.Spawning;
        BonusCount = bonusCount;
        SetTypeAndColor(type, color);
        _activeEffects.EffectAdded += ActiveEffects_EffectAdded;
        _activeEffects.EffectRemoved += ActiveEffects_EffectRemoved; 
    }

    protected Tank(Level level, TankType type, TankColor color) : this(level, type, color, 0)
    {
    }

    public sealed override void Update(GameTime gameTime)
    {
        Debug.Assert(Status != TankStatus.Unknown);
        switch (Status)
        {
            case TankStatus.Spawning:
                if (Sprite.HasAnimationEnded)
                {
                    Status = TankStatus.Normal;
                    TurnTo(Direction);
                    OnSpawn();
                }
                else
                    Sprite.ProcessAnimation(gameTime);

                break;

            case TankStatus.Exploding when _explosion.ToRemove:
                if (!string.IsNullOrEmpty(_afterExplosionText))
                    Level.CreateFloatingText(BoundingRectangle.Center, _afterExplosionText, 12.0 * Tank1460Game.OneFrameSpan);
                
                _explosion = null;
                Status = TankStatus.Destroyed;

                _activeEffects.EffectAdded -= ActiveEffects_EffectAdded;
                _activeEffects.EffectRemoved -= ActiveEffects_EffectRemoved; 

                Remove();
                break;

            case TankStatus.Normal:
                Sprite.ProcessAnimation(gameTime);

                CalcIsFrontTileBlocked();

                // Даём танку подумать и обрабатываем придуманный приказ. Но если всё запрещено, то и думать нечего.
                if (IsImmobile && IsPacifist)
                    break;

                var order = Think(gameTime);

                if (order.HasFlag(TankOrder.Shoot) && !IsPacifist)
                    Shoot(gameTime);

                if (IsImmobile)
                    break;

                var newDirection = order.ToDirection();
                if (newDirection is not null)
                {
                    MoveTo(newDirection.Value);
                    Level.SoundPlayer.Loop(this is PlayerTank ? Sound.MovePlayer : Sound.MoveBot);
                }

                break;
        }

        _activeEffects.Update(gameTime);
        _shells.RemoveAll(s => s.ToRemove);

        base.Update(gameTime);
    }

    public void AddTimedInvulnerability(double invulnerabilityTime)
    {
        _activeEffects.AddExclusive(new Invulnerability(Level, invulnerabilityTime));
    }

    public void AddTimedImmobility()
    {
        // Время не начинается снова и не накапливается.
        if (_activeEffects.HasEffect<Immobility>())
            return;

        _activeEffects.Add(new Immobility(272 * Tank1460Game.OneFrameSpan));
    }

    public void SetBonusCount(int newBonusCount)
    {
        BonusCount = newBonusCount;
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (Status != TankStatus.Normal && Status != TankStatus.Spawning)
            return;

        base.Draw(gameTime, spriteBatch);
        _activeEffects.Draw(spriteBatch, Position.ToVector2());
    }

    public void HandleShot(Shell shell)
    {
        if (IsInvulnerable())
            return;

        if (Status != TankStatus.Normal)
            return;

        if (BonusCount > 0)
        {
            BonusCount--;
            Level.BonusManager.Spawn();
        }

        if (HasShip)
        {
            RemoveShip();
            Level.SoundPlayer.Play(Sound.HitHurt);
            return;
        }

        HandleDamaged(shell.ShotBy);
    }

    public void AddShip()
    {
        HasShip = true;
        _activeEffects.AddExclusive(new Ship(Level, Color));
        if (BonusCount > 0)
            BonusCount++;
    }

    protected void RemoveShip()
    {
        HasShip = false;
        _activeEffects.RemoveAll<Ship>();
    }

    protected override void LoadContent()
    {
        // Звезда при респауне.
        var spawnAnimationTimes = SpawnAnimationTimesInFrames().Select(t => t * Tank1460Game.OneFrameSpan).ToArray();
        _spawnAnimation = new Animation(Level.Content.Load<Texture2D>(@"Sprites/Effects/SpawnNova"), spawnAnimationTimes, false);
    }

    private static Dictionary<ObjectDirection, IAnimation> LoadAnimationsForType(ContentManagerEx content,
                                                                                 TankType type,
                                                                                 TankColor color,
                                                                                 TankFlashingType flashingType)
    {
        var oneTypeAnimations = new Dictionary<ObjectDirection, IAnimation>();

        foreach (ObjectDirection direction in Enum.GetValues(typeof(ObjectDirection)))
            oneTypeAnimations[direction] = LoadAnimationsForDirection(content, type, color, flashingType, direction);

        return oneTypeAnimations;
    }

    private static IAnimation LoadAnimationsForDirection(ContentManagerEx content,
                                                         TankType type,
                                                         TankColor color,
                                                         TankFlashingType flashingType,
                                                         ObjectDirection direction)
    {
        // Загружаем обычные текстуры в любом случае.
        var plainTexture = content.LoadRecoloredTexture($"Sprites/Tank/Type{(int)type}/{direction}",
                                                        $"Sprites/_R/Tank/{color}");

        // В случае мигания подгружаем вторые нужные текстуры и подключаем "двумерную" анимацию.
        Texture2D bonusTexture;
        switch(flashingType)
        {
            case TankFlashingType.None:
                return new Animation(plainTexture, true);

            case TankFlashingType.Bonus:
                bonusTexture = content.LoadRecoloredTexture($"Sprites/Tank/Type{(int)type}/{direction}",
                                                            $"Sprites/_R/Tank/Red");
                break;

            case TankFlashingType.Immobile:
                bonusTexture = content.LoadNewSolidColoredTexture(Microsoft.Xna.Framework.Color.Transparent,
                                                          plainTexture.Bounds.Width,
                                                          plainTexture.Bounds.Height);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(flashingType), flashingType, null);
        }
        
        return new ShiftingAnimation(new[] { bonusTexture, plainTexture },
                                     double.MaxValue,
                                     true,
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

    public void Explode(Tank destroyedBy)
    {
        if (Status != TankStatus.Normal)
            return;

        var isBotTank = this is BotTank;

        if (isBotTank && destroyedBy is PlayerTank playerTank)
        {
            Level.AddPlayerStatsForDefeatingTank(playerTank.PlayerIndex, this);
            _afterExplosionText = GameRules.TankScoreByType[Type].ToString();
        }

        Status = TankStatus.Exploding;
        _explosion = new BigExplosion(Level);
        _explosion.SpawnViaCenterPosition(BoundingRectangle.Center);

        Level.SoundPlayer.Play(isBotTank ? Sound.ExplosionSmall : Sound.ExplosionBig);
    }

    protected bool IsTankCenteredOnTile() => Position.X % Tile.DefaultWidth == 0 && Position.Y % Tile.DefaultHeight == 0;

    protected bool IsInvulnerable() => _activeEffects.HasEffect<Invulnerability>();

    protected void SetType(TankType type)
    {
        Type = type;
        RefreshTankProperties();

        ReloadAnimations();
        PlayCurrentAnimation();
    }

    protected void SetColor(TankColor color)
    {
        Color = color;

        ReloadAnimations();
        PlayCurrentAnimation();
    }

    protected void SetTypeAndColor(TankType type, TankColor color)
    {
        Type = type;
        Color = color;

        RefreshTankProperties();

        ReloadAnimations();
        PlayCurrentAnimation();
    }

    protected void SetShellProperties(ShellProperties newShellProperties)
    {
        _shellProperties = newShellProperties;
    }

    protected abstract TankOrder Think(GameTime gameTime);

    protected abstract void HandleDamaged(Tank damagedBy);

    protected virtual void OnSpawn()
    {
    }

    private void PlayCurrentAnimation()
    {
        if (Status != TankStatus.Normal)
            return;

        Sprite.PlayAnimation(_animations[Direction]);
    }

    private void ReloadAnimations()
    {
        // Каждый раз при смене подгружается новая анимация.
        // TODO: Возможно, сделать у самих анимаций возможность подгружать новые текстуры, чтобы не пересоздавать объект? Так можно заодно и не начинать мигание заново.
        // TODO: Каждый раз искать по всем эффектам это перебор.
        var flashingType = _activeEffects.HasEffect<Immobility>()
            ? TankFlashingType.Immobile
            : BonusCount > 0 ? TankFlashingType.Bonus : TankFlashingType.None;
        _animations = LoadAnimationsForType(Level.Content, Type, Color, flashingType);
    }

    private void RefreshTankProperties()
    {
        var properties = TankPropertiesProvider.Get(Type);

        SetMovingSpeed(properties.TankSpeed);
        _maxShells = properties.MaxShells;
        _shellProperties = properties.ShellProperties;
        _shellSpeed = properties.ShellSpeed;
    }

    private void TurnTo(ObjectDirection newDirection)
    {
        // При повороте танка на 90° округляем его координаты до клетки (механика из оригинальной игры для более удобного прохождения между препятствиями)
        if (newDirection.Has90DegreesDifference(Direction))
            Position = new Point((int)Math.Round((double)Position.X / Tile.DefaultWidth) * Tile.DefaultWidth,
                                 (int)Math.Round((double)Position.Y / Tile.DefaultHeight) * Tile.DefaultHeight);

        Direction = newDirection;
        PlayCurrentAnimation();
    }

    private void Shoot(GameTime gameTime)
    {
        if (_shells.Count >= _maxShells)
            return;

        if (gameTime.TotalGameTime.TotalSeconds < _timeTillReload)
            return;

        _timeTillReload = gameTime.TotalGameTime.TotalSeconds + FireDelay;
        var shell = new Shell(Level, Direction, _shellSpeed, this, _shellProperties);
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
            IsFrontTileBlocked = TileRectangle.NearestTiles(Direction)
                                              .GetAllPoints()
                                              .Any(point => !Level.CanTankPassThroughTile(this, point));
    }

    private void MoveTo(ObjectDirection newDirection)
    {
        Debug.Assert(Status == TankStatus.Normal);

        // Сразу после поворота танк ещё не движется.
        if (newDirection == Direction)
            MovingDirection = newDirection;
        else
        {
            TurnTo(newDirection);
            MovingDirection = null;
        }
    }

    private void ActiveEffects_EffectAdded(TankEffect effect)
    {
        switch (effect)
        {
            case Immobility:
                IsImmobile = true;
                ReloadAnimations();
                PlayCurrentAnimation();
                break;
        }
    }

    private void ActiveEffects_EffectRemoved(TankEffect effect)
    {
        switch (effect)
        {
            case Immobility:
                IsImmobile = false;
                ReloadAnimations();
                PlayCurrentAnimation();
                break;
        }
    }

    private enum TankFlashingType
    {
        None,
        Bonus,
        Immobile
    }
}