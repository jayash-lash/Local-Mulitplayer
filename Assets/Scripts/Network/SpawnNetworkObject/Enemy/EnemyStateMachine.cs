using UnityEngine;

public class EnemyStateMachine : MonoBehaviour
{
    [Header("State Machine Settings")]
    [SerializeField] private float _movementUpdateInterval = 0.1f;

    public AIState CurrentState { get; private set; } = AIState.Patrol;

    private EnemyAIController _controller;
    private float _movementUpdateTimer;
    private Vector3? _patrolTarget;
    
    public void Initialize(EnemyAIController controller)
    {
        _controller = controller;
        
        if (_controller.NavMeshMovement != null)
        {
            _controller.NavMeshMovement.OnDestinationReached += OnDestinationReached;
        }
        
        CurrentState = AIState.Patrol;
    }

    public void UpdateStateMachine()
    {
        _movementUpdateTimer += Time.deltaTime;

        if (_movementUpdateTimer >= _movementUpdateInterval)
        {
            _movementUpdateTimer = 0f;
            ProcessCurrentState();
        }
    }

    private void ProcessCurrentState()
    {
        switch (CurrentState)
        {
            case AIState.Patrol:
                HandlePatrolState();
                break;
            case AIState.Chasing:
                HandleChasingState();
                break;
            case AIState.Attacking:
                HandleAttackingState();
                break;
            case AIState.Returning:
                HandleReturningState();
                break;
        }
    }

    private void HandlePatrolState()
    {
        var target = _controller.Detection.CurrentTarget;
        if (target != null && target.IsActive)
        {
            float distanceToTarget = Vector3.Distance(transform.position, target.LastKnownPosition);
            if (distanceToTarget <= _controller.Detection.DetectionRange)
            {
                CurrentState = AIState.Chasing;
                return;
            }
        }

        if (_controller.MovementSettings.IsPatrolEnabled && !_controller.NavMeshMovement.IsMoving)
        {
            SetRandomPatrolTarget();
        }
    }

    private void HandleChasingState()
    {
        var target = _controller.Detection.CurrentTarget;
        if (target == null || !target.IsActive)
        {
            CurrentState = AIState.Returning;
            _controller.NavMeshMovement.StopLookAt();
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, target.LastKnownPosition);

        if (distanceToTarget > _controller.Detection.DetectionRange)
        {
            CurrentState = AIState.Returning;
            _controller.NavMeshMovement.StopLookAt();
            return;
        }

        if (distanceToTarget <= _controller.Combat.AttackRange)
        {
            CurrentState = AIState.Attacking;
            _controller.NavMeshMovement.StopMovement();
            return;
        }

        _controller.NavMeshMovement.SetDestination(target.LastKnownPosition);
        _controller.NavMeshMovement.SetStoppingDistance(_controller.Combat.AttackRange);
        _controller.NavMeshMovement.SetLookAtTarget(target.LastKnownPosition);
    }

    private void HandleAttackingState()
    {
        var target = _controller.Detection.CurrentTarget;
        if (target == null || !target.IsActive)
        {
            CurrentState = AIState.Returning;
            _controller.NavMeshMovement.StopLookAt();
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, target.LastKnownPosition);

        if (distanceToTarget > _controller.Combat.AttackRange)
        {
            CurrentState = AIState.Chasing;
            return;
        }

        if (!_controller.Detection.HasLineOfSight(target.LastKnownPosition))
        {
            CurrentState = AIState.Chasing;
            return;
        }

        _controller.NavMeshMovement.SetLookAtTarget(target.LastKnownPosition, true);
    }

    private void HandleReturningState()
    {
        float distanceToSpawn = Vector3.Distance(transform.position, _controller.MovementSettings.SpawnPosition);

        if (distanceToSpawn <= 2f)
        {
            CurrentState = AIState.Patrol;
            _controller.NavMeshMovement.StopLookAt();
            return;
        }

        // Проверить новые цели
        var target = _controller.Detection.CurrentTarget;
        if (target != null && target.IsActive)
        {
            float distanceToTarget = Vector3.Distance(transform.position, target.LastKnownPosition);
            if (distanceToTarget <= _controller.Detection.DetectionRange)
            {
                CurrentState = AIState.Chasing;
                return;
            }
        }

        _controller.NavMeshMovement.SetDestination(_controller.MovementSettings.SpawnPosition);
        _controller.NavMeshMovement.SetStoppingDistance(2f);
        _controller.NavMeshMovement.StopLookAt();
    }

    private void SetRandomPatrolTarget()
    {
        Vector3? randomPoint = _controller.MovementSettings.GetRandomPatrolPoint();
        
        if (randomPoint.HasValue)
        {
            _patrolTarget = randomPoint.Value;
            _controller.NavMeshMovement.SetDestination(_patrolTarget.Value);
            _controller.NavMeshMovement.SetStoppingDistance(1f);
        }
    }

    private void OnDestinationReached()
    {
        if (CurrentState == AIState.Patrol)
        {
            // Ждем перед следующим патрулированием
            Invoke(nameof(SetRandomPatrolTarget), Random.Range(1f, 3f));
        }
    }

    public void ResetForPool()
    {
        CurrentState = AIState.Patrol;
        _movementUpdateTimer = 0f;
        _patrolTarget = null;
    }

    private void OnDrawGizmosSelected()
    {
        // Показать текущую цель патрулирования
        if (_patrolTarget.HasValue)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_patrolTarget.Value, 1f);
            Gizmos.DrawLine(transform.position, _patrolTarget.Value);
        }
    }

    private void OnDestroy()
    {
        if (_controller?.NavMeshMovement != null)
        {
            _controller.NavMeshMovement.OnDestinationReached -= OnDestinationReached;
        }
    }
}