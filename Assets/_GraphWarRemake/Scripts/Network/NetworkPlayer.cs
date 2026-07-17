using System;
using GraphWarRemake.Game;
using GraphWarRemake.LifetimeScopes;
using GraphWarRemake.Logging;
using Mirror;
using UnityEngine;
using VContainer;

namespace GraphWarRemake.Network
{
    [RequireComponent(typeof(NetworkIdentity))]
    public class NetworkPlayer : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnHPChanged))]
        public int currentHP = 100;

        [SyncVar(hook = nameof(OnNameChanged))]
        public string playerName;

        [SyncVar(hook = nameof(OnColorChanged))]
        public Color playerColor = Color.white;

        [SyncVar]
        public int spawnIndex;

        private const int MaxHP = 100;
        private IGameLogger _logger;

        public event Action<int, int> OnHPChangedEvent;
        public event Action<string> OnNameChangedEvent;
        public event Action OnDeathEvent;

        private static readonly Vector3[] SpawnPositions = new Vector3[]
        {
            new Vector3(-10f, 0.5f, 0f),
            new Vector3(10f, 0.5f, 0f),
            new Vector3(-6f, 0.5f, -3f),
            new Vector3(6f, 0.5f, -3f),
            new Vector3(-6f, 0.5f, 3f),
            new Vector3(6f, 0.5f, 3f),
            new Vector3(-12f, 0.5f, 0f),
            new Vector3(12f, 0.5f, 0f),
            new Vector3(0f, 0.5f, -5f),
            new Vector3(0f, 0.5f, 5f),
        };

        public override void OnStartServer()
        {
            base.OnStartServer();

            var container = GameLifetimeScope.GlobalContainer;
            _logger = container?.Resolve<IGameLogger>();

            currentHP = MaxHP;
            playerName = $"Player {netId}";

            playerColor = new Color(
                UnityEngine.Random.Range(0.3f, 1f),
                UnityEngine.Random.Range(0.3f, 1f),
                UnityEngine.Random.Range(0.3f, 1f));

            var turnManager = FindFirstObjectByType<TurnManager>();
            spawnIndex = turnManager != null ? turnManager.GetComponent<TurnManager>() != null ? 0 : 0 : 0;

            var allPlayers = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
            spawnIndex = allPlayers.Length - 1;

            transform.position = GetSpawnPosition();

            var turnMgr = FindFirstObjectByType<TurnManager>();
            turnMgr?.RegisterPlayer(netId);

            _logger?.Log($"Игрок {netId} ({playerName}) создан. HP={MaxHP}, позиция={transform.position}");
        }

        public override void OnStopServer()
        {
            var turnManager = FindFirstObjectByType<TurnManager>();
            turnManager?.UnregisterPlayer(netId);
            base.OnStopServer();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            var container = GameLifetimeScope.GlobalContainer;
            if (container != null && _logger == null)
                _logger = container.Resolve<IGameLogger>();

            _logger?.Log($"Клиент: игрок {netId} ({playerName}) появился. HP={currentHP}");

            var renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
                renderer.material.color = playerColor;
        }

        public Vector3 GetSpawnPosition()
        {
            int index = Mathf.Clamp(spawnIndex, 0, SpawnPositions.Length - 1);
            return SpawnPositions[index];
        }

        [Server]
        public void TakeDamage(int damage)
        {
            if (!isServer || currentHP <= 0) return;

            int previousHP = currentHP;
            currentHP = Mathf.Max(0, currentHP - damage);

            _logger?.Log($"{netId} получил {damage} урона: {previousHP} -> {currentHP} HP");

            if (currentHP <= 0)
                OnPlayerDeath();
        }

        [Server]
        private void OnPlayerDeath()
        {
            _logger?.Log($"Игрок {netId} уничтожен!");

            var turnManager = FindFirstObjectByType<TurnManager>();
            turnManager?.OnPlayerDied(netId);

            RpcOnDeath();
            gameObject.SetActive(false);
        }

        [ClientRpc]
        private void RpcOnDeath()
        {
            OnDeathEvent?.Invoke();
        }

        private void OnHPChanged(int oldHP, int newHP)
        {
            OnHPChangedEvent?.Invoke(oldHP, newHP);
        }

        private void OnNameChanged(string oldName, string newName)
        {
            OnNameChangedEvent?.Invoke(newName);
        }

        private void OnColorChanged(Color oldColor, Color newColor)
        {
            var renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
                renderer.material.color = newColor;
        }
    }
}
