using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace Tank1460;

internal class Curtain
{
    public bool IsFinished { get; private set; }

    private readonly Color _color;

    private readonly double _tickTime = GameRules.TimeInFrames(1);
    private const int OpenedPosition = 0;
    private const int ClosedPosition = 24;
    private const int ClosedDelay = 5;

    private readonly int _targetPosition;
    private readonly int _step;
    private int _position;
    private double _time;

    public Curtain(Color color, CurtainAction action)
    {
        _color = color;
        switch (action)
        {
            case CurtainAction.Open:
                _position = ClosedPosition;
                _targetPosition = OpenedPosition;
                break;
            case CurtainAction.Close:
                _position = OpenedPosition;
                _targetPosition = ClosedPosition + ClosedDelay;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }

        _step = Math.Sign(_targetPosition - _position);
        IsFinished = false;
    }

    public void Update(GameTime gameTime)
    {
        _time += gameTime.ElapsedGameTime.TotalSeconds;

        while (_time > _tickTime)
        {
            _time -= _tickTime;
            _position += _step;

            if (_step > 0 && _position <= _targetPosition || _step < 0 && _position >= _targetPosition)
                continue;

            Finish();
            return;
        }
    }

    public void Draw(SpriteBatch spriteBatch, Rectangle curtainBounds)
    {
        var curtainHeight = curtainBounds.Height * (_position - OpenedPosition) / (ClosedPosition - OpenedPosition) / 2;
        var curtainWidth = curtainBounds.Width;

        spriteBatch.FillRectangle(curtainBounds.X, curtainBounds.Y, curtainWidth, curtainHeight, _color);
        spriteBatch.FillRectangle(curtainBounds.X, curtainBounds.Y + curtainBounds.Height - curtainHeight, curtainWidth, curtainHeight, _color);
    }

    internal void Finish()
    {
        if (IsFinished)
            return;

        IsFinished = true;
    }
}