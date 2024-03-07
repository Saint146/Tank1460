﻿using Microsoft.Xna.Framework;

namespace Tank1460.Common.Level.Object;

public class BotSpawnerModel : LevelObjectModel
{
    public static readonly Point DefaultSize = new(2, 2);

    public override LevelObjectType Type => LevelObjectType.BotSpawner;
}