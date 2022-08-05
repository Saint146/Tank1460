using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace Tank1460.LevelObjects;

public abstract class MoveableLevelObject : LevelObject
{
    protected ObjectDirection? MovingDirection { get; set; }

    protected double MovingSpeed { get; private set; }

    // Вместо перемещения на нецелый путь каждый такт - объект перемещается на целый путь не каждый такт.
    private double _time;
    private double _timeToMove;

    private static readonly IReadOnlyDictionary<ObjectDirection, Point> Velocities = new Dictionary<ObjectDirection, Point>
    {
        { ObjectDirection.Up, new Point(0, -1) },
        { ObjectDirection.Down, new Point(0, 1) },
        { ObjectDirection.Left, new Point(-1, 0) },
        { ObjectDirection.Right, new Point(1, 0) }
    };

    protected MoveableLevelObject(Level level, double movingSpeed) : base(level)
    {
        MovingDirection = null;
        SetMovingSpeed(movingSpeed);
    }

    public override void Update(GameTime gameTime, KeyboardState keyboardState)
    {
        // Совершаем движение, если можно и нужно.
        _time += gameTime.ElapsedGameTime.TotalSeconds;
        while (_time > _timeToMove)
        {
            _time -= _timeToMove;
            TryMove();
        }

        base.Update(gameTime, keyboardState);

        // Очищаем движение перед следующим тактом.
        MovingDirection = null;
    }

    protected void SetMovingSpeed(double newMovingSpeed)
    {
        MovingSpeed = newMovingSpeed;
        _timeToMove = Tank1460Game.OneFrameSpan / MovingSpeed;
        _time = 0.0;
    }

    /// <summary>
    /// Для действий, которые происходят при попытке передвижения независимо от того, получится ли сдвинуться.
    /// Например, для продвижения анимаций.
    /// </summary>
    protected virtual void HandleTryMove()
    {
    }

    /// <summary>
    /// Проверка на то, можно ли сдвинуться вперёд.
    /// TODO: Возможно, в будущем сделать не абстрактным, потому что правила для всех по сути одинаковые? Хотя для снарядов это не так.
    /// </summary>
    protected abstract bool CanMove();

    private void TryMove()
    {
        if (MovingDirection is null)
            return;

        HandleTryMove();

        if (CanMove())
            Move();
    }

    private void Move()
    {
        // ReSharper disable once PossibleInvalidOperationException
        Position += Velocities[MovingDirection.Value];
    }
}