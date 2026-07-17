using System;
using System.Collections.Generic;
using GraphWarRemake.LifetimeScopes;
using GraphWarRemake.Logging;
using Mirror;
using UnityEngine;
using VContainer;

namespace GraphWarRemake.Game
{
    [RequireComponent(typeof(NetworkIdentity))]
    public class TurnManager : NetworkBehaviour
    {
        [Header("Настройки")]
        [SerializeField] private float _turnTimeLimit = 60f;

        [SyncVar(hook = nameof(OnStateChanged))]
        public GameState currentState = GameState.WaitingToStart;

        [SyncVar(hook = nameof(OnCurrentPlayerChanged))]
        public uint currentPlayerId;

        [SyncVar(hook = nameof(OnTimeRemainingChanged))]
        public float timeRemaining;

        [SyncVar(hook = nameof(OnGameWinnerChanged))]
        public uint winnerId;

        private readonly List<uint> _playerOrder = new List<uint>();
        private int _currentTurnIndex;
        private readonly HashSet<uint> _alivePlayers = new HashSet<uint>();

        private IGameLogger _logger;

        public event Action<GameState> OnGameStateChanged;
        public event Action<uint> OnPlayerTurnStarted;
        public event Action<float> OnTimerUpdated;
        public event Action<uint> OnGameOver;

        public override void OnStartServer()
        {
            base.OnStartServer();
            _logger = GameLifetimeScope.GlobalContainer?.Resolve<IGameLogger>();
            _logger?.Log("TurnManager инициализирован на сервере");
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            OnGameStateChanged?.Invoke(currentState);
            OnPlayerTurnStarted?.Invoke(currentPlayerId);
            if (currentState == GameState.GameOver)
                OnGameOver?.Invoke(winnerId);
        }

        [Server]
        public void RegisterPlayer(uint netId)
        {
            if (_playerOrder.Contains(netId)) return;

            _playerOrder.Add(netId);
            _alivePlayers.Add(netId);
            _logger?.Log($"Игрок {netId} зарегистрирован. Всего: {_playerOrder.Count}");

            if (_playerOrder.Count >= 2)
                StartGame();
        }

        [Server]
        public void UnregisterPlayer(uint netId)
        {
            _playerOrder.Remove(netId);
            _alivePlayers.Remove(netId);
            _logger?.Log($"Игрок {netId} отключён. Осталось: {_playerOrder.Count}");

            if (_alivePlayers.Count <= 1 && currentState == GameState.WaitingForInput)
                EndGame();
        }

        [Server]
        public void StartGame()
        {
            if (currentState != GameState.WaitingToStart) return;

            for (int i = _playerOrder.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (_playerOrder[i], _playerOrder[j]) = (_playerOrder[j], _playerOrder[i]);
            }

            _currentTurnIndex = 0;
            _logger?.Log($"Игра началась! Порядок: {string.Join(", ", _playerOrder)}");

            currentState = GameState.WaitingForInput;
            BeginTurn();
        }

        [Server]
        private void BeginTurn()
        {
            if (_playerOrder.Count == 0) return;

            currentPlayerId = _playerOrder[_currentTurnIndex];
            timeRemaining = _turnTimeLimit;
            _logger?.Log($"Ход игрока {currentPlayerId}. Осталось {_turnTimeLimit}с.");
        }

        [Command(requiresAuthority = false)]
        public void CmdFireFormula(uint playerId, string formula)
        {
            if (!isServer) return;
            if (currentPlayerId != playerId)
            {
                _logger?.LogWarning($"Игрок {playerId} стреляет не в свой ход!");
                return;
            }
            if (currentState != GameState.WaitingForInput)
            {
                _logger?.LogWarning($"Невозможно стрелять в состоянии {currentState}");
                return;
            }

            currentState = GameState.Simulating;
            _logger?.Log($"Формула от {playerId}: \"{formula}\"");

            RpcNotifyProjectileFired(playerId, formula);

            SpawnProjectileOnServer(playerId, formula);
        }

        [Server]
        private void SpawnProjectileOnServer(uint ownerNetId, string formula)
        {
            var owner = FindPlayerById(ownerNetId);
            if (owner == null)
            {
                _logger?.LogError($"Игрок {ownerNetId} не найден для спавна снаряда!");
                currentState = GameState.WaitingForInput;
                return;
            }

            Vector3 spawnPos = owner.GetSpawnPosition();
            GameObject prefab = NetworkManager.singleton.spawnPrefabs.Find(p => p.name == "NetworkProjectile");
            if (prefab == null)
            {
                _logger?.LogError("Префаб NetworkProjectile не найден в spawnPrefabs!");
                currentState = GameState.WaitingForInput;
                return;
            }

            var mathEngine = GameLifetimeScope.GlobalContainer?.Resolve<Math.MathEngine>();
            var logger = GameLifetimeScope.GlobalContainer?.Resolve<IGameLogger>();

            GameObject projectileObj = Instantiate(prefab, spawnPos, Quaternion.identity);
            NetworkServer.Spawn(projectileObj);

            var projectile = projectileObj.GetComponent<Network.NetworkProjectile>();
            projectile.Initialize(formula, spawnPos, ownerNetId, mathEngine, logger);
        }

        [Server]
        private Network.NetworkPlayer FindPlayerById(uint netId)
        {
            foreach (var identity in FindObjectsByType<NetworkIdentity>(FindObjectsSortMode.None))
            {
                if (identity.netId == netId)
                    return identity.GetComponent<Network.NetworkPlayer>();
            }
            return null;
        }

        [Server]
        public void OnProjectileResolved()
        {
            if (currentState != GameState.Simulating) return;

            _logger?.Log("Снаряд завершил полёт.");

            if (_alivePlayers.Count <= 1)
            {
                EndGame();
                return;
            }

            currentState = GameState.WaitingForInput;
            NextTurn();
        }

        [Server]
        public void NextTurn()
        {
            int startIndex = _currentTurnIndex;
            do
            {
                _currentTurnIndex = (_currentTurnIndex + 1) % _playerOrder.Count;
            }
            while (!_alivePlayers.Contains(_playerOrder[_currentTurnIndex])
                   && _currentTurnIndex != startIndex);

            BeginTurn();
        }

        [Server]
        public void OnPlayerDied(uint playerId)
        {
            _alivePlayers.Remove(playerId);
            _logger?.Log($"Игрок {playerId} умер. Осталось: {_alivePlayers.Count}");

            if (_alivePlayers.Count <= 1)
                EndGame();
        }

        [Server]
        private void EndGame()
        {
            currentState = GameState.GameOver;
            winnerId = 0;
            foreach (var id in _alivePlayers)
            {
                winnerId = id;
                break;
            }
            _logger?.Log($"Игра окончена! Победитель: {winnerId}");
        }

        [ServerCallback]
        private void Update()
        {
            if (currentState != GameState.WaitingForInput) return;

            timeRemaining -= Time.deltaTime;
            if (timeRemaining <= 0f)
            {
                _logger?.Log($"Время хода игрока {currentPlayerId} вышло!");
                OnProjectileResolved();
            }
        }

        public bool IsMyTurn(uint myNetId)
        {
            return currentState == GameState.WaitingForInput && currentPlayerId == myNetId;
        }

        [ClientRpc]
        private void RpcNotifyProjectileFired(uint playerId, string formula)
        {
            _logger?.Log($"Клиент: снаряд игрока {playerId} полетел по \"{formula}\"");
        }

        private void OnStateChanged(GameState oldState, GameState newState)
        {
            OnGameStateChanged?.Invoke(newState);
        }

        private void OnCurrentPlayerChanged(uint oldId, uint newId)
        {
            OnPlayerTurnStarted?.Invoke(newId);
        }

        private void OnTimeRemainingChanged(float oldTime, float newTime)
        {
            OnTimerUpdated?.Invoke(newTime);
        }

        private void OnGameWinnerChanged(uint oldWinner, uint newWinner)
        {
            if (currentState == GameState.GameOver)
                OnGameOver?.Invoke(newWinner);
        }
    }
}
