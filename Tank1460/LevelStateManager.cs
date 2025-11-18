using System;

namespace Tank1460
{
    public class LevelStateManager
    {
        private string currentState;
        private string[] levelStates;
        private int delay;

        public LevelStateManager(string[] states, int initialDelay)
        {
            levelStates = states;
            currentState = levelStates[0]; // Start with the first state
            delay = initialDelay;
        }

        public string GetCurrentState() => currentState;

        public void TransitionToState(string newState)
        {
            if (Array.Exists(levelStates, state => state == newState))
            {
                currentState = newState;
            }
            else
            {
                throw new ArgumentException("Invalid state transition");
            }
        }

        public void DelayTransition(int milliseconds)
        {
            delay = milliseconds;
            System.Threading.Thread.Sleep(delay);
        }
    }
}