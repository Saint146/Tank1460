using Microsoft.Xna.Framework;

namespace Tank1460.Common.Level.Object;

public class PlayerSpawnerModel : LevelObjectModel
{
    public static readonly Point DefaultSize = new(2, 2);

    public override LevelObjectType Type => LevelObjectType.PlayerSpawner;

    public PlayerIndex Player { get; }

    public PlayerSpawnerModel(PlayerIndex player)
    {
        Player = player;
    }
}