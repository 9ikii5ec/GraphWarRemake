using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace GraphWarRemake.UI
{
    public class FormulaInput : MonoBehaviour
    {
        private TextField _inputField;

        public event Action<string> OnFormulaSubmitted;

        public void Initialize(VisualElement root)
        {
            _inputField = root.Q<TextField>("FormulaInput");

            if (_inputField != null)
            {
                _inputField.RegisterCallback<KeyDownEvent>(OnKeyDown);
            }
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                SubmitFormula();
                evt.StopPropagation();
            }
        }

        private void SubmitFormula()
        {
            string formula = GetFormula();
            if (!string.IsNullOrWhiteSpace(formula))
            {
                OnFormulaSubmitted?.Invoke(formula);
                _inputField.value = "";
            }
        }

        public string GetFormula()
        {
            return _inputField != null ? _inputField.value.Trim() : "";
        }

        public void SetInteractable(bool interactable)
        {
            if (_inputField != null)
                _inputField.SetEnabled(interactable);
        }

        public void Clear()
        {
            if (_inputField != null)
                _inputField.value = "";
        }

        private void OnDestroy()
        {
            if (_inputField != null)
                _inputField.UnregisterCallback<KeyDownEvent>(OnKeyDown);
        }
    }
}
