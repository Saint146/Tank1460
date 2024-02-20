namespace Tank1460;

internal enum GameStatus
{
    /// <summary>
    /// Инициализация продолжается, пока не загрузится нужный для старта игры контент.
    /// </summary>
    Initializing,

    /// <summary>
    /// Игра готова показывать главное меню.
    /// </summary>
    Ready,

    /// <summary>
    /// В главном меню.
    /// </summary>
    MainMenu,

    /// <summary>
    /// В процессе игры на уровне.
    /// </summary>
    Level,

    /// <summary>
    /// Уровень закончен или меню закрыто, шторка закрывается.
    /// </summary>
    CurtainClosing,

    /// <summary>
    /// На экране выбора уровня.
    /// </summary>
    LevelSelectScreen,

    /// <summary>
    /// Уровень загружен, шторка открывается.
    /// </summary>
    CurtainOpening,

    /// <summary>
    /// На экране подсчёта очков после пройденного уровня.
    /// </summary>
    WinScoreScreen,

    /// <summary>
    /// На экране подсчёта очков после проигранной игры.
    /// </summary>
    LostScoreScreen,

    /// <summary>
    /// На экране конца игры.
    /// </summary>
    GameOverScreen,

    /// <summary>
    /// На экране побитого рекорда.
    /// </summary>
    HighscoreScreen
}