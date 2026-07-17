using GraphWarRemake.Game;
using GraphWarRemake.Logging;
using GraphWarRemake.Math;
using Mirror;
using UnityEngine;

namespace GraphWarRemake.Network
{
    [RequireComponent(typeof(NetworkIdentity))]
    [RequireComponent(typeof(NetworkTransformReliable))]
    public class NetworkProjectile : NetworkBehaviour
    {
        [Header("Настройки")]
        [SerializeField] private float _moveSpeed = 20f;
        [SerializeField] private int _damage = 50;
        [SerializeField] private float _explosionRadius = 5f;

        [Header("Слои")]
        [SerializeField] private LayerMask _obstacleLayer;
        [SerializeField] private LayerMask _playerLayer;

        private const float MaxFlightDistance = 800f;
        private const float TrajectoryStep = 0.1f;
        private const float CollisionRadius = 0.3f;

        private string _formula;
        private Vector3 _startPosition;
        private uint _ownerNetId;
        private MathEngine _mathEngine;
        private IGameLogger _logger;

        private float _currentT;
        private Vector3 _previousPosition;
        private bool _isActive;

        public void Initialize(string formula, Vector3 startPosition, uint ownerNetId,
            MathEngine mathEngine, IGameLogger logger)
        {
            _formula = formula;
            _startPosition = startPosition;
            _ownerNetId = ownerNetId;
            _mathEngine = mathEngine;
            _logger = logger;
            _currentT = 0f;
            _previousPosition = startPosition;
            _isActive = true;

            _logger?.Log($"Снаряд инициализирован: owner={ownerNetId}, formula=\"{formula}\"");
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            if (_obstacleLayer == 0)
                _obstacleLayer = LayerMask.GetMask("Obstacle");
            if (_playerLayer == 0)
                _playerLayer = LayerMask.GetMask("Player");
        }

        [ServerCallback]
        private void Update()
        {
            if (!_isActive || _mathEngine == null) return;

            _currentT += TrajectoryStep * _moveSpeed * Time.deltaTime;

            if (_currentT >= MaxFlightDistance)
            {
                _logger?.Log($"Снаряд достиг максимальной дальности ({MaxFlightDistance})");
                Explode();
                return;
            }

            Vector3 newPosition = _mathEngine.CalculateNextPosition(_formula, _currentT, _startPosition);

            if (CheckCollision(_previousPosition, newPosition))
            {
                _logger?.Log("Снаряд столкнулся с препятствием");
                Explode();
                return;
            }

            _previousPosition = transform.position;
            transform.position = newPosition;
        }

        private bool CheckCollision(Vector3 from, Vector3 to)
        {
            Vector3 direction = to - from;
            float distance = direction.magnitude;

            if (distance < 0.01f) return false;

            if (Physics.SphereCast(from, CollisionRadius, direction, out RaycastHit hit, distance, _obstacleLayer))
            {
                transform.position = hit.point;
                return true;
            }

            Collider[] hits = Physics.OverlapSphere(to, CollisionRadius, _playerLayer);
            foreach (var hitCollider in hits)
            {
                var hitPlayer = hitCollider.GetComponent<NetworkPlayer>();
                if (hitPlayer != null && hitPlayer.netId != _ownerNetId)
                {
                    _logger?.Log($"Снаряд столкнулся с игроком {hitPlayer.netId}");
                    return true;
                }
            }

            return false;
        }

        [Server]
        private void Explode()
        {
            _isActive = false;
            _logger?.Log($"Взрыв в {transform.position}, радиус={_explosionRadius}");

            Collider[] affectedColliders = Physics.OverlapSphere(
                transform.position, _explosionRadius, _playerLayer);

            foreach (var hitCollider in affectedColliders)
            {
                var targetPlayer = hitCollider.GetComponent<NetworkPlayer>();
                if (targetPlayer == null || targetPlayer.netId == _ownerNetId) continue;

                float distance = Vector3.Distance(transform.position, hitCollider.transform.position);
                int damage = CalculateDamage(distance);

                _logger?.Log($"Урон игроку {targetPlayer.netId}: {damage} (dist={distance:F2})");
                targetPlayer.TakeDamage(damage);
            }

            RpcShowExplosion(transform.position, _explosionRadius);

            var turnManager = FindFirstObjectByType<TurnManager>();
            turnManager?.OnProjectileResolved();

            NetworkServer.Destroy(gameObject);
        }

        private int CalculateDamage(float distance)
        {
            if (distance <= 0f) return _damage;
            float multiplier = 1f / (1f + distance * distance / (_explosionRadius * _explosionRadius));
            return Mathf.Max(1, Mathf.RoundToInt(_damage * multiplier));
        }

        [ClientRpc]
        private void RpcShowExplosion(Vector3 position, float radius)
        {
            _logger?.Log($"Взрыв отображён в {position}");
        }
    }
}
