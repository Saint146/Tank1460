using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Tank1460;

public class GameState
{
    public Dictionary<PlayerIndex, PlayerState> PlayersStates { get; }

    public GameState(IEnumerable<PlayerIndex> playersInGame)
    {
        PlayersStates = playersInGame.ToDictionary(playerIndex => playerIndex,
                                                   _ => new PlayerState
                                                   {
                                                       LivesRemaining = 3,
                                                       TankType = null,
                                                       Score = 0,
                                                       TankHasShip = false
                                                   });
    }
}