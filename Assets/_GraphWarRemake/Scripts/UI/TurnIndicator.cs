using UnityEngine;
using UnityEngine.UIElements;

namespace GraphWarRemake.UI
{
    public class TurnIndicator : MonoBehaviour
    {
        private Label _turnText;
        private Label _timerText;

        private const string MyTurnClass = "my-turn";
        private const string EnemyTurnClass = "enemy-turn";
        private const string NormalClass = "timer-normal";
        private const string WarningClass = "timer-warning";
        private const string CriticalClass = "timer-critical";

        public void Initialize(VisualElement root)
        {
            _turnText = root.Q<Label>("TurnText");
            _timerText = root.Q<Label>("TimerText");
        }

        public void UpdateTimer(float timeRemaining, bool isMyTurn)
        {
            if (_turnText != null)
            {
                _turnText.text = isMyTurn ? "ВАШ ХОД" : "ХОД ПРОТИВНИКА";
                _turnText.RemoveFromClassList(MyTurnClass);
                _turnText.RemoveFromClassList(EnemyTurnClass);
                _turnText.AddToClassList(isMyTurn ? MyTurnClass : EnemyTurnClass);
            }

            if (_timerText != null)
            {
                int seconds = Mathf.CeilToInt(Mathf.Max(0f, timeRemaining));
                _timerText.text = $"{seconds}";

                _timerText.RemoveFromClassList(NormalClass);
                _timerText.RemoveFromClassList(WarningClass);
                _timerText.RemoveFromClassList(CriticalClass);

                if (timeRemaining <= 10f)
                    _timerText.AddToClassList(CriticalClass);
                else if (timeRemaining <= 20f)
                    _timerText.AddToClassList(WarningClass);
                else
                    _timerText.AddToClassList(NormalClass);
            }
        }

        public void SetText(string text)
        {
            if (_turnText != null)
                _turnText.text = text;
        }
    }
}
