using UnityEngine;

namespace GraphWarRemake.Logging
{
    /// <summary>
    /// Реализация логгера игры.
    /// Поддерживает динамическое включение/выключение через ToggleLogging.
    /// Регистрируется как Instance в VContainer (один на всё приложение).
    /// </summary>
    public class GameLogger : IGameLogger
    {
        public bool IsLoggingEnabled { get; private set; }

        public GameLogger()
        {
            // По умолчанию логирование включено
            IsLoggingEnabled = true;
        }

        /// <summary>
        /// Переключает состояние логирования.
        /// </summary>
        public void ToggleLogging(bool state)
        {
            IsLoggingEnabled = state;
        }

        public void Log(string message)
        {
            if (!IsLoggingEnabled) return;
            Debug.Log($"[Game] {message}");
        }

        public void LogWarning(string message)
        {
            if (!IsLoggingEnabled) return;
            Debug.LogWarning($"[Game] {message}");
        }

        public void LogError(string message)
        {
            // Ошибки логируются всегда, даже если логирование отключено
            Debug.LogError($"[Game] {message}");
        }
    }
}
