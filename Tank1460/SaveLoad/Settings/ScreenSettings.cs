﻿namespace Tank1460.SaveLoad.Settings;

public class ScreenSettings
{
    public int? DisplayIndex { get; set; }

    public ScreenMode? Mode { get; set; }

    public ScreenPoint? Position { get; set; }

    public ScreenPoint? Size { get; set; }

    public bool? IsMaximized { get; set; }
}