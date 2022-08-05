﻿using System.Collections.Generic;

namespace Tank1460.Extensions;

public static class ListExtensions
{
    public static T GetRandom<T>(this IList<T> list) => list[Rng.Next(list.Count)];
}