using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Tank1460.Audio;
using Tank1460.LevelObjects.Explosions;

namespace Tank1460.LevelObjects;

public class Falcon : LevelObject
{
    private readonly Dictionary<FalconState, IAnimation> _animations = new();
    private Explosion _explosion;

    public FalconState State { get; private set; }

    public Falcon(Level level) : base(level)
    {
    }

    public override CollisionType CollisionType => CollisionType.ShootableAndImpassable;

    public bool IsAlive => State == FalconState.Normal;

    protected override void LoadContent()
    {
        foreach (FalconState state in Enum.GetValues(typeof(FalconState)))
        {
            var animationKey = (state == FalconState.Exploding ? FalconState.Destroyed : state).ToString();
            var animation = new Animation(Level.Content.Load<Texture2D>($"Sprites/Falcon/{animationKey}"), false);
            _animations.Add(state, animation);
        }

        SetState(FalconState.Normal);
    }

    protected override IAnimation GetDefaultAnimation() => _animations[FalconState.Normal];

    private void SetState(FalconState state)
    {
        State = state;
        Sprite.PlayAnimation(_animations[State]);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        switch (State)
        {
            case FalconState.Exploding when _explosion.ToRemove:
                _explosion = null;
                SetState(FalconState.Destroyed);
                break;
        }
    }

    public void Explode()
    {
        SetState(FalconState.Exploding);
        _explosion = new BigExplosion(Level);
        _explosion.SpawnViaCenterPosition(BoundingRectangle.Center);
        Level.HandleFalconDestroyed(this);

        Level.SoundPlayer.Play(Sound.ExplosionBig);
        Level.SoundPlayer.Play(Sound.Fail);
    }

    public void HandleShot(Shell shell)
    {
        if (State == FalconState.Normal)
            Explode();
    }
}