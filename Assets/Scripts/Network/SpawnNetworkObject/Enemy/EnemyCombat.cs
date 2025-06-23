using Unity.Netcode;
using UnityEngine;

public class EnemyCombat : NetworkBehaviour
{
    [Header("Combat Settings")]
    [SerializeField] private float _fireInterval = 3f;
    [SerializeField] private float _attackRange = 10f;
    [SerializeField] private NetworkObjectType _projectileType = NetworkObjectType.BasicProjectile;

    public float AttackRange => _attackRange;

    private EnemyAIController _aiController;
    private NetworkObjectFactory _factory;
    private float _fireTimer;

    public void Initialize(EnemyAIController aiController)
    {
        _aiController = aiController;
        _factory = FindObjectOfType<NetworkObjectFactory>();
    }

    public void UpdateCombat()
    {
        _fireTimer += Time.deltaTime;

        if (_fireTimer >= _fireInterval && CanAttack())
        {
            _fireTimer = 0f;
            Fire(_aiController.Detection.CurrentTarget.LastKnownPosition);
        }
    }

    private bool CanAttack()
    {
        var target = _aiController.Detection.CurrentTarget;
        if (target == null || !target.IsActive) return false;

        float distanceToTarget = Vector3.Distance(transform.position, target.LastKnownPosition);
        if (distanceToTarget > _attackRange) return false;

        return _aiController.Detection.HasLineOfSight(target.LastKnownPosition);
    }

    private void Fire(Vector3 targetPosition)
    {
        Vector3 spawnPos = transform.position + transform.forward * 1.5f;
        Vector3 direction = (targetPosition - transform.position).normalized;

        NetworkObject projectile = _factory.CreateObject(_projectileType, spawnPos, Quaternion.identity);

        if (projectile != null)
        {
            projectile.Spawn();

            if (projectile.TryGetComponent<Projectile>(out var proj))
            {
                proj.Initialize(direction, _factory.GetTypeForPrefab(_factory.GetPrefabForType(_projectileType)));
            }
        }
    }

    public void ResetForPool()
    {
        _fireTimer = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        // Радиус атаки
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackRange);
    }
}