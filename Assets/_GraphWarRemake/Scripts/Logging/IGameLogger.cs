namespace GraphWarRemake.Logging
{
    /// <summary>
    /// Интерфейс для логирования событий игры.
    /// Все компоненты, которым нужно логирование, depend от этого интерфейса.
    /// </summary>
    public interface IGameLogger
    {
        /// <summary>
        /// Включено ли логирование в данный момент.
        /// </summary>
        bool IsLoggingEnabled { get; }

        /// <summary>
        /// Динамически включает или выключает логирование.
        /// Вызывается из UI по нажатию кнопки.
        /// </summary>
        void ToggleLogging(bool state);

        /// <summary>
        /// Логирует информационное сообщение.
        /// </summary>
        void Log(string message);

        /// <summary>
        /// Логирует предупреждение.
        /// </summary>
        void LogWarning(string message);

        /// <summary>
        /// Логирует ошибку.
        /// </summary>
        void LogError(string message);
    }
}
