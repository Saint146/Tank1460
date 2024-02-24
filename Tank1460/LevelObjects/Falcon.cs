using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Tank1460.Audio;
using Tank1460.LevelObjects.Explosions;

namespace Tank1460.LevelObjects;

public class Falcon : LevelObject
{
    private readonly Dictionary<FalconStatus, IAnimation> _animations = new();
    private Explosion _explosion;

    public FalconStatus Status { get; private set; }

    public Falcon(Level level, Point size) : base(level)
    {
        // TODO: Size check.
    }

    public override CollisionType CollisionType => CollisionType.ShootableAndImpassable;

    public bool IsAlive => Status == FalconStatus.Normal;

    protected override void LoadContent()
    {
        foreach (FalconStatus status in Enum.GetValues(typeof(FalconStatus)))
        {
            var animationKey = (status == FalconStatus.Exploding ? FalconStatus.Destroyed : status).ToString();
            var animation = new Animation(Level.Content.Load<Texture2D>($"Sprites/Falcon/{animationKey}"), false);
            _animations.Add(status, animation);
        }

        SetStatus(FalconStatus.Normal);
    }

    protected override IAnimation GetDefaultAnimation() => _animations[FalconStatus.Normal];

    private void SetStatus(FalconStatus status)
    {
        Status = status;
        Sprite.PlayAnimation(_animations[Status]);
    }

    public override void Update(GameTime gameTime)
    {
        switch (Status)
        {
            case FalconStatus.Exploding when _explosion.ToRemove:
                _explosion = null;
                SetStatus(FalconStatus.Destroyed);
                break;
        }
    }

    public void Explode()
    {
        SetStatus(FalconStatus.Exploding);
        _explosion = new BigExplosion(Level);
        _explosion.SpawnViaCenterPosition(BoundingRectangle.Center);

        Level.SoundPlayer.Play(Sound.ExplosionBig);
        Level.SoundPlayer.Play(Sound.Fail);

        Level.HandleFalconDestroyed(this);
    }

    public void HandleShot(Shell shell)
    {
        if (Status == FalconStatus.Normal)
            Explode();
    }
}