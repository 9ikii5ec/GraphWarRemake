using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace GraphWarRemake.UI
{
    public class FireButton : MonoBehaviour
    {
        private Button _button;

        public event Action OnFireClicked;

        public void Initialize(VisualElement root)
        {
            _button = root.Q<Button>("FireButton");

            if (_button != null)
                _button.RegisterCallback<ClickEvent>(OnClick);
        }

        private void OnClick(ClickEvent evt)
        {
            OnFireClicked?.Invoke();
        }

        public void SetInteractable(bool interactable)
        {
            if (_button != null)
                _button.SetEnabled(interactable);
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.UnregisterCallback<ClickEvent>(OnClick);
        }
    }
}
