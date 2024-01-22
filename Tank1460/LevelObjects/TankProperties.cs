namespace Tank1460.LevelObjects;

public record TankProperties
(
    double TankSpeed,
    int MaxShells,
    ShellSpeed ShellSpeed,
    ShellProperties ShellProperties
);