using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Tank1460.Common.Level.Object;

namespace Tank1460.LevelObjects;

public abstract class MoveableLevelObject : LevelObject
{
    protected ObjectDirection? MovingDirection { get; set; }

    protected double MovingSpeed { get; private set; }

    // Вместо перемещения на нецелый путь каждый такт - объект перемещается на целый путь не каждый такт.
    // Главное при этом - разделять возможные действия объекта на те, что случаются:
    // - каждый такт независимо от попыток движения; (Update)
    // - каждый такт при том, что команда движения дана; (сейчас тоже в Update, объект сам думает об этом)
    // - только при реальной попытке сдвинуться; (HandleTryMove)
    // - когда объект действительно сдвинулся. (HandleMove)
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

    public override void Update(GameTime gameTime)
    {
        // Совершаем движение (бывает, что и не одно), если можно и нужно.
        _time += gameTime.ElapsedGameTime.TotalSeconds;
        while (_time > _timeToMove)
        {
            _time -= _timeToMove;
            TryMove();
        }

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
    /// </summary>
    protected virtual void HandleTryMove()
    {
    }

    /// <summary>
    /// Для действий, которые происходят после реального передвижения.
    /// Например, для проверки коллизий (которые влияют НЕ на запрет продвижения, а как например для пули)
    /// </summary>
    protected virtual void HandleMove()
    {
    }

    /// <summary>
    /// Проверка на то, можно ли сдвинуться вперёд.
    /// TODO: Возможно, в будущем сделать не абстрактным, потому что правила для всех по сути одинаковые? Хотя для снарядов это не так.
    /// </summary>
    protected abstract bool CanMove();

    /// <summary>
    /// Попытка продвинуться вперёд.
    /// </summary>
    private void TryMove()
    {
        if (MovingDirection is null)
            return;

        HandleTryMove();

        if (CanMove())
            Move();
    }

    /// <summary>
    /// Реальное движение вперёд после всех проверок.
    /// </summary>
    private void Move()
    {
        // ReSharper disable once PossibleInvalidOperationException
        Position += Velocities[MovingDirection.Value];
        HandleMove();
    }
}