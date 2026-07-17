namespace GraphWarRemake.Game
{
    /// <summary>
    /// Конечный автомат состояний игры.
    /// Определяет текущую фазу игрового цикла.
    /// </summary>
    public enum GameState
    {
        /// <summary>Ожидание начала игры (все игроки подключились).</summary>
        WaitingToStart,

        /// <summary>Ожидание хода активного игрока. UI ввода формулы активен.</summary>
        WaitingForInput,

        /// <summary>Снаряд летит. Симуляция траектории на сервере.</summary>
        Simulating,

        /// <summary>Разрешение хода: взрыв, урон, проверка смертей.</summary>
        Resolving,

        /// <summary>Игра окончена. Остался один игрок.</summary>
        GameOver
    }
}
