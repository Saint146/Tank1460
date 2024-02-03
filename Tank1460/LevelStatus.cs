﻿namespace Tank1460;

internal enum LevelStatus
{
    /// <summary>
    /// Загрузка уровня.
    /// </summary>
    Loading,

    /// <summary>
    /// Вступление, когда уровень уже рисуется, но управления у игрока нет.
    /// </summary>
    Intro,

    /// <summary>
    /// Обычное состояние уровня.
    /// </summary>
    Running,

    /// <summary>
    /// Уровень на паузе.
    /// </summary>
    Paused,

    /// <summary>
    /// Уровень пройден, но игра ещё продолжается фиксированное время.
    /// </summary>
    WinDelay,

    /// <summary>
    /// Уровень проигран, но управление и звук ещё не отключены, игра продолжается небольшое фиксированное время.
    /// </summary>
    LostPreDelay,

    /// <summary>
    /// Уровень проигран, управление игроков отключено, звук отключен, но игра ещё продолжается фиксированное время.
    /// </summary>
    LostDelay,

    // TODO
    WinScoreScreen,

    // TODO
    LostScoreScreen,

    // TODO
    GameOverScreen,

    /// <summary>
    /// Уровень пройден. Игра не идет, но уровень ещё рисуется.
    /// </summary>
    Win,

    /// <summary>
    /// Уровень проигран. Игра не идет, но уровень ещё рисуется.
    /// </summary>
    GameOver
}