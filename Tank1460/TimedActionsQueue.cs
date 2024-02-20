using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace Tank1460;

internal class TimedActionsQueue
{
    public bool IsFinished;

    public IReadOnlyCollection<(Action Action, double ActionDelay)> Actions => _actions;

    private readonly Queue<(Action Action, double ActionDelay)> _actions = new();
    private double _time;

    private Action _nextAction;
    private double _nextActionTime;

    public TimedActionsQueue(IEnumerable<(Action Action, double ActionDelay)> actions)
    {
        EnqueueActions(actions);
        DequeueAction();
    }

    public TimedActionsQueue(params (Action Action, double ActionDelay)[] actions) : this((IEnumerable<(Action Action, double ActionDelay)>)actions)
    {
    }

    public void EnqueueAction(Action action, double actionDelay)
    {
        _actions.Enqueue((action, actionDelay));
        if (IsFinished)
            Reset();
    }

    public void EnqueueActions(IEnumerable<(Action Action, double ActionDelay)> actions)
    {
        foreach (var (action, actionDelay) in actions)
        {
            EnqueueAction(action, actionDelay);
        }
    }

    public void Update(GameTime gameTime)
    {
        if (IsFinished)
            return;

        _time += gameTime.ElapsedGameTime.TotalSeconds;

        while (_time >= _nextActionTime && !IsFinished)
        {
            _time -= _nextActionTime;
            DoNextAction();
            DequeueAction();
        }
    }

    private void Reset()
    {
        _time = 0.0;
        IsFinished = false;
    }

    private void DoNextAction()
    {
        _nextAction.Invoke();
    }

    private void DequeueAction()
    {
        if (!_actions.TryDequeue(out var action))
        {
            IsFinished = true;
            return;
        }

        _nextAction = action.Action;
        _nextActionTime = action.ActionDelay;
    }
}