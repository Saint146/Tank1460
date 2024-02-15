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
    InMainMenu,

    /// <summary>
    /// В процессе игры на уровне.
    /// </summary>
    InLevel,

    /// <summary>
    /// Уровень загружен, шторка открывается.
    /// </summary>
    CurtainOpening,

    /// <summary>
    /// Уровень закончен или меню закрыто, шторка закрывается.
    /// </summary>
    CurtainClosing,

    /// <summary>
    /// На экране подсчёта очков после пройденного уровня.
    /// </summary>
    InWinScoreScreen,

    /// <summary>
    /// На экране подсчёта очков после проигранной игры.
    /// </summary>
    InLostScoreScreen,

    /// <summary>
    /// На экране конца игры.
    /// </summary>
    GameOverScreen,

    /// <summary>
    /// На экране побитого рекорда.
    /// </summary>
    HighscoreScreen
}