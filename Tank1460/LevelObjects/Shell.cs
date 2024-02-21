using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tank1460.Audio;
using Tank1460.Common.Extensions;
using Tank1460.Common.Level.Object;
using Tank1460.LevelObjects.Explosions;
using Tank1460.LevelObjects.Tanks;
using Tank1460.LevelObjects.Tiles;

namespace Tank1460.LevelObjects;

public class Shell : MoveableLevelObject
{
    public ShellStatus Status = ShellStatus.Normal;

    // TODO: Это нужно для ai, чтобы он понимал, попадает он или нет. Но ширина по факту сейчас зависит просто от текстуры.
    public const int DefaultShellWidth = 3;

    private IAnimation _animation;
    public readonly Tank ShotBy;
    private Explosion _explosion;
    public readonly ObjectDirection Direction;
    private bool _skipCollisionCheck;
    public readonly ShellProperties Properties;

    public Shell(Level level, ObjectDirection direction, ShellSpeed shellSpeed, Tank shotBy, ShellProperties properties) : base(level, shellSpeed == ShellSpeed.Normal ? 2.0f : 4.0f)
    {
        Level.Shells.Add(this);

        Direction = direction;
        ShotBy = shotBy;
        Properties = properties;
    }

    public override CollisionType CollisionType => Status == ShellStatus.Normal ? CollisionType.Shootable : CollisionType.None;

    protected override IAnimation GetDefaultAnimation() => _animation;

    protected override void LoadContent()
    {
        _animation = new Animation(Level.Content.Load<Texture2D>($"Sprites/Shell/{Direction}"), true);
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (ToRemove)
            return;

        if (Status != ShellStatus.Normal)
            return;

        base.Draw(gameTime, spriteBatch);
    }

    public override void Update(GameTime gameTime)
    {
        if (ToRemove)
            return;

        switch (Status)
        {
            case ShellStatus.Normal:
                // Пуля всегда летит вперёд.
                MovingDirection = Direction;
                break;

            case ShellStatus.Exploding when _explosion.ToRemove:
                _explosion = null;
                Status = ShellStatus.Destroyed;
                Remove();
                break;
        }

        base.Update(gameTime);
    }

    protected override bool CanMove() => Status == ShellStatus.Normal && !ToRemove;

    protected override void HandleMove()
    {
        base.HandleMove();
        HandleCollisions();
    }

    private void HandleCollisions()
    {
        if (Status != ShellStatus.Normal)
            return;

        // Имитация оригинала: пропускаем каждый второй кадр
        _skipCollisionCheck = !_skipCollisionCheck;
        if (_skipCollisionCheck)
            return;

        var collisions = Level.GetAllCollisionsSimple(this, new LevelObject[] { ShotBy });
        if (collisions.Count == 0)
            return;

        var (shellCollisions, nonShellCollisions) = collisions.SplitByCondition(levelObject => levelObject is Shell);

        // Коллизии с другими снарядами в приоритете, чтобы не было случая, когда два танка в упор поражают друг друга.
        foreach (var otherShell in shellCollisions)
        {
            // Две противоборствующие пули самоуничтожаются.
            if (otherShell.ToRemove || !(ShotBy is BotTank ^ ((Shell)otherShell).ShotBy is BotTank)) continue;

            Remove();
            otherShell.Remove();
            return;
        }

        // Обрабатываем остальные коллизии с одинаковым приоритетом и возможностью поразить сразу несколько целей (например, два тайла рядом).
        foreach (var levelObject in nonShellCollisions)
        {
            if (levelObject is null)
            {
                // Граница уровня.
                if (ShotBy is PlayerTank)
                    Level.SoundPlayer.Play(Sound.HitDull);

                Explode();
                return;
            }

            if (!levelObject.CollisionType.HasFlag(CollisionType.Shootable) || levelObject.ToRemove)
                continue;

            switch (levelObject)
            {
                case Tile tile:
                    var shouldExplode = tile.HandleShot(this);
                    if (shouldExplode)
                        Explode();
                    break;

                case PlayerTank playerTank:
                    if (ShotBy is PlayerTank)
                    {
                        // Свой снаряд пропускаем.
                        if (playerTank != ShotBy)
                        {
                            // Другой танк игрока получает дебафф.
                            Explode();
                            playerTank.AddTimedImmobility();
                        }
                        break;
                    }

                    Explode();
                    playerTank.HandleShot(this);
                    break;

                case Falcon falcon:
                    Explode();
                    falcon.HandleShot(this);
                    break;

                case BotTank botTank:
                    if (ShotBy is PlayerTank)
                    {
                        Explode();
                        botTank.HandleShot(this);
                    }
                    // Пули ботов пролетают сквозь других ботов.
                    break;
            }
        }
    }

    private void Explode()
    {
        if (Status != ShellStatus.Normal)
            return;

        Status = ShellStatus.Exploding;
        _explosion = new CommonExplosion(Level);
        _explosion.SpawnViaCenterPosition(BoundingRectangle.Center);
    }
}