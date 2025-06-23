using UnityEngine;

public class EnemyMovementSettings : MonoBehaviour
{
    [Header("AI Movement Settings")]
    [SerializeField] private float _patrolRadius = 10f;
    [SerializeField] private bool _enablePatrol = true;

    public bool IsPatrolEnabled => _enablePatrol;
    public float PatrolRadius => _patrolRadius;
    public Vector3 SpawnPosition { get; private set; }

    private EnemyAIController _controller;
    private EnemyNavMeshMovement _navMeshMovement;

    public void Initialize(EnemyAIController controller)
    {
        _controller = controller;
        _navMeshMovement = GetComponent<EnemyNavMeshMovement>();
        
        if (_navMeshMovement == null)
        {
            Debug.LogError($"EnemyNavMeshMovement component not found on {gameObject.name}");
        }

        SpawnPosition = transform.position;
    }

    public Vector3? GetRandomPatrolPoint()
    {
        return _navMeshMovement.GetRandomPointAround(_patrolRadius);
    }

    public void ResetForPool()
    {
        _navMeshMovement.StopMovement();
        _navMeshMovement.SetAgentEnabled(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(SpawnPosition, _patrolRadius);
    }
}