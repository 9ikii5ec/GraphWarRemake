using GraphWarRemake.Game;
using GraphWarRemake.LifetimeScopes;
using GraphWarRemake.Logging;
using Mirror;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace GraphWarRemake.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class GameUI : MonoBehaviour
    {
        private UIDocument _uidoc;

        private VisualElement _gamePanel;
        private VisualElement _resultPanel;
        private Label _resultTitle;

        private FormulaInput _formulaInput;
        private FireButton _fireButton;
        private TurnIndicator _turnIndicator;
        private HealthBar _healthBar;

        private Network.NetworkPlayer _localPlayer;
        private TurnManager _turnManager;
        private IGameLogger _logger;

        private void OnEnable()
        {
            _uidoc = GetComponent<UIDocument>();
            if (_uidoc == null || _uidoc.visualTreeAsset == null) return;

            var root = _uidoc.visualTreeAsset.Instantiate();
            _uidoc.rootVisualElement.Add(root);

            var uss = Resources.Load<StyleSheet>("UI/GameUI");
            if (uss != null)
                _uidoc.rootVisualElement.styleSheets.Add(uss);

            _gamePanel = root.Q("GamePanel");
            _resultPanel = root.Q("ResultPanel");
            _resultTitle = root.Q<Label>("ResultTitle");

            _formulaInput = gameObject.AddComponent<FormulaInput>();
            _formulaInput.Initialize(root);

            _fireButton = gameObject.AddComponent<FireButton>();
            _fireButton.Initialize(root);

            _turnIndicator = gameObject.AddComponent<TurnIndicator>();
            _turnIndicator.Initialize(root);

            _healthBar = gameObject.AddComponent<HealthBar>();
            var healthContainer = root.Q("HealthPanel");
            _healthBar.Initialize(healthContainer);

            _formulaInput.OnFormulaSubmitted += HandleFormulaSubmitted;
            _fireButton.OnFireClicked += HandleFireClicked;

            var logToggle = root.Q<Toggle>("LogToggle");
            if (logToggle != null)
                logToggle.RegisterValueChangedCallback(evt => _logger?.ToggleLogging(evt.newValue));

            if (_resultPanel != null)
                _resultPanel.style.display = DisplayStyle.None;

            _logger = GameLifetimeScope.GlobalContainer?.Resolve<IGameLogger>();
        }

        private void Update()
        {
            if (_localPlayer == null)
            {
                FindLocalPlayer();
                return;
            }
            UpdateUI();
        }

        private void FindLocalPlayer()
        {
            if (!NetworkClient.active) return;

            foreach (var identity in FindObjectsByType<NetworkIdentity>(FindObjectsSortMode.None))
            {
                if (identity.isLocalPlayer)
                {
                    _localPlayer = identity.GetComponent<Network.NetworkPlayer>();
                    _turnManager = FindFirstObjectByType<TurnManager>();

                    if (_localPlayer != null)
                    {
                        _localPlayer.OnHPChangedEvent += HandleHPChanged;
                        _localPlayer.OnDeathEvent += HandleLocalPlayerDeath;
                        _logger?.Log("GameUI: найден локальный игрок");
                    }

                    if (_turnManager != null)
                    {
                        _turnManager.OnGameOver += HandleGameOver;
                    }
                    break;
                }
            }
        }

        private void UpdateUI()
        {
            if (_turnManager == null) return;

            bool isMyTurn = _turnManager.IsMyTurn(_localPlayer.netId);
            bool isGameOver = _turnManager.currentState == GameState.GameOver;

            _formulaInput?.SetInteractable(isMyTurn);
            _fireButton?.SetInteractable(isMyTurn);
            _turnIndicator?.UpdateTimer(_turnManager.timeRemaining, isMyTurn);

            if (_gamePanel != null)
                _gamePanel.style.display = isGameOver ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void HandleFormulaSubmitted(string formula) => FireFormula(formula);

        private void HandleFireClicked()
        {
            string formula = _formulaInput?.GetFormula();
            if (string.IsNullOrWhiteSpace(formula)) return;
            FireFormula(formula);
        }

        private void FireFormula(string formula)
        {
            if (_localPlayer == null || _turnManager == null) return;

            _logger?.Log($"Отправляем формулу: \"{formula}\"");

            _formulaInput?.SetInteractable(false);
            _fireButton?.SetInteractable(false);

            _turnManager.CmdFireFormula(_localPlayer.netId, formula);
        }

        private void HandleHPChanged(int oldHP, int newHP)
        {
            _healthBar?.UpdateHP(newHP, 100);
        }

        private void HandleLocalPlayerDeath()
        {
            _formulaInput?.SetInteractable(false);
            _fireButton?.SetInteractable(false);
            _turnIndicator?.SetText("ВЫ УНИЧТОЖЕНЫ");
        }

        private void HandleGameOver(uint winnerNetId)
        {
            if (_resultPanel != null)
                _resultPanel.style.display = DisplayStyle.Flex;

            if (_resultTitle != null)
            {
                bool isWinner = winnerNetId == _localPlayer.netId;
                _resultTitle.text = isWinner ? "ПОБЕДА!" : "ПОРАЖЕНИЕ";
                _resultTitle.RemoveFromClassList("win");
                _resultTitle.RemoveFromClassList("lose");
                _resultTitle.AddToClassList(isWinner ? "win" : "lose");
            }

            if (_gamePanel != null)
                _gamePanel.style.display = DisplayStyle.None;
        }

        private void OnDisable()
        {
            if (_formulaInput != null)
                _formulaInput.OnFormulaSubmitted -= HandleFormulaSubmitted;
            if (_fireButton != null)
                _fireButton.OnFireClicked -= HandleFireClicked;
            if (_localPlayer != null)
            {
                _localPlayer.OnHPChangedEvent -= HandleHPChanged;
                _localPlayer.OnDeathEvent -= HandleLocalPlayerDeath;
            }
            if (_turnManager != null)
                _turnManager.OnGameOver -= HandleGameOver;
        }
    }
}
