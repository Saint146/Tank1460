using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace Tank1460.LevelObjects;

internal class TimedActionsQueue
{
    public bool ToRemove;

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
        if (ToRemove)
            return;

        _time += gameTime.ElapsedGameTime.TotalSeconds;

        while (_time >= _nextActionTime && !ToRemove)
        {
            _time -= _nextActionTime;
            DoNextAction();
            DequeueAction();
        }
    }

    private void DoNextAction()
    {
        _nextAction.Invoke();
    }

    private void DequeueAction()
    {
        if (!_actions.TryDequeue(out var action))
        {
            Remove();
            return;
        }

        _nextAction = action.Action;
        _nextActionTime = action.ActionDelay;
    }

    private void Remove()
    {
        ToRemove = true;
    }
}