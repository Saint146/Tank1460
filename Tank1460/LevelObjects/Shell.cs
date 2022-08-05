using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using Tank1460.Audio;
using Tank1460.Extensions;
using Tank1460.LevelObjects.Explosions;
using Tank1460.LevelObjects.Tanks;
using Tank1460.LevelObjects.Tiles;

namespace Tank1460.LevelObjects;

public class Shell : MoveableLevelObject
{
    public ShellState State = ShellState.Normal;

    private IAnimation _animation;
    public readonly Tank ShotBy;
    private Explosion _explosion;
    public readonly ObjectDirection Direction;
    private bool _skipCollisionCheck = false;
    public readonly ShellDamage Damage;

    public Shell(Level level, ObjectDirection direction, ShellSpeed shellSpeed, Tank shotBy, ShellDamage damage) : base(level, shellSpeed == ShellSpeed.Normal ? 2.0f : 4.0f)
    {
        Level.Shells.Add(this);

        Direction = direction;
        ShotBy = shotBy;
        Damage = damage;
    }

    public override CollisionType CollisionType => State == ShellState.Normal ? CollisionType.Shootable : CollisionType.None;

    protected override IAnimation GetDefaultAnimation() => _animation;

    protected override void LoadContent()
    {
        _animation = new Animation(Level.Content.Load<Texture2D>($"Sprites/Shell/{Enum.GetName(Direction)}"), true);
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (ToRemove)
            return;

        if (State != ShellState.Normal)
            return;

        base.Draw(gameTime, spriteBatch);
    }

    public override void Update(GameTime gameTime, KeyboardState keyboardState)
    {
        if (ToRemove)
            return;

        switch (State)
        {
            case ShellState.Normal:
                MovingDirection = Direction;
                HandleCollisions();
                break;

            case ShellState.Exploding when _explosion.ToRemove:
                _explosion = null;
                State = ShellState.Destroyed;
                Remove();
                break;
        }

        base.Update(gameTime, keyboardState);
    }

    protected override bool CanMove() => State == ShellState.Normal && !ToRemove;

    private void HandleCollisions()
    {
        // Имитация оригинала: пропускаем каждый второй кадр
        _skipCollisionCheck = !_skipCollisionCheck;
        if (_skipCollisionCheck)
            return;

        var collisions = Level.GetAllCollisionsSimple(this, new LevelObject[] { ShotBy });
        if (collisions.Count == 0)
            return;

        foreach (var levelObject in collisions)
        {
            if (levelObject is null)
            {
                // Граница уровня.
                if (ShotBy is PlayerTank)
                    Level.SoundPlayer.Play(Sound.HitDull);

                Explode();
                break;
            }

            if (!levelObject.CollisionType.HasFlag(CollisionType.Shootable) || levelObject.ToRemove)
                continue;

            if (levelObject is Shell shell)
            {
                // Две противоборствующие пули самоуничтожаются.
                if (ShotBy is EnemyTank ^ shell.ShotBy is EnemyTank)
                {
                    Remove();
                    shell.Remove();
                    break;
                }
            }

            if (levelObject is Tile tile)
            {
                // Одной пулей можно покоцать сразу несколько (читай: два) тайла.
                if (levelObject.CollisionType.HasFlag(CollisionType.Impassable))
                    Explode();

                tile.HandleShot(this);
            }
            else if (levelObject is PlayerTank player)
            {
                Explode();
                player.HandleShot(this);
                break;
            }
            else if (levelObject is Falcon falcon)
            {
                Explode();
                falcon.HandleShot(this);
                break;
            }
            else if (levelObject is EnemyTank enemy)
            {
                if (ShotBy is PlayerTank)
                {
                    Explode();
                    enemy.HandleShot(this);
                    break;
                }
            }
        }
    }
    
    private void Explode()
    {
        if (State == ShellState.Exploding)
            return;

        State = ShellState.Exploding;
        _explosion = new CommonExplosion(Level);
        _explosion.SpawnViaCenterPosition(BoundingRectangle.Center);

        //// TODO: Переделать на умный учет коллизий
        //Level.HandleObjectRemoved(this);
    }
}