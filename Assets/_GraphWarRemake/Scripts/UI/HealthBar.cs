using UnityEngine;
using UnityEngine.UIElements;

namespace GraphWarRemake.UI
{
    public class HealthBar : MonoBehaviour
    {
        private VisualElement _fillBar;
        private Label _hpText;
        private Label _nameLabel;

        private Color _fullColor = new Color(0f, 0.8f, 0.27f);
        private Color _lowColor = new Color(1f, 0.27f, 0.27f);
        private Color _mediumColor = new Color(1f, 0.67f, 0f);

        public void Initialize(VisualElement root)
        {
            _fillBar = root.Q<VisualElement>("HealthBarFill");
            _hpText = root.Q<Label>("HealthBarHP");
            _nameLabel = root.Q<Label>("PlayerName");
        }

        public void UpdateHP(int currentHP, int maxHP)
        {
            float normalized = maxHP > 0 ? (float)currentHP / maxHP : 0f;

            if (_fillBar != null)
            {
                _fillBar.style.width = Length.Percent(normalized * 100f);

                Color color = normalized > 0.5f ? _fullColor
                            : normalized > 0.25f ? _mediumColor
                            : _lowColor;
                _fillBar.style.backgroundColor = color;
            }

            if (_hpText != null)
            {
                _hpText.text = $"{currentHP}/{maxHP}";
            }
        }

        public void SetPlayerName(string name)
        {
            if (_nameLabel != null)
                _nameLabel.text = name;
        }
    }
}
