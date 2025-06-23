using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyNavMeshMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 3f;
    [SerializeField] private float _rotationSpeed = 360f;
    [SerializeField] private float _stoppingDistance = 2f;
    [SerializeField] private float _destinationThreshold = 0.5f;
    [SerializeField] private float _lookAtSpeed = 5f;

    private NavMeshAgent _agent;
    private Vector3 _currentDestination;
    private bool _hasDestination;
    private Vector3? _lookAtTarget;
    private bool _shouldLookAtTarget;

    public bool IsMoving => _agent.hasPath && _agent.remainingDistance > _destinationThreshold;
    public bool HasReachedDestination => _hasDestination && _agent.remainingDistance <= _destinationThreshold;
    public float RemainingDistance => _agent.remainingDistance;
    
    public event Action OnDestinationReached;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        InitializeAgent();
    }

    private void InitializeAgent()
    {
        _agent = GetComponent<NavMeshAgent>();
        
        if (_agent == null)
        {
            Debug.LogError($"NavMeshAgent not found on {gameObject.name}");
            return;
        }
        
        _agent.speed = _moveSpeed;
        _agent.angularSpeed = _rotationSpeed;
        _agent.stoppingDistance = _stoppingDistance;
        _agent.autoBraking = true;
        _agent.autoRepath = true;
    }

    private void Update()
    {
        if (!IsServer) return;

        CheckDestinationReached();
        HandleRotation();
    }

    private void HandleRotation()
    {
        if (_shouldLookAtTarget && _lookAtTarget.HasValue)
        {
            Vector3 lookDirection = _lookAtTarget.Value - transform.position;
            lookDirection.y = 0;

            if (lookDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _lookAtSpeed * Time.deltaTime);
            }
        }
    }

    private void CheckDestinationReached()
    {
        if (_hasDestination && HasReachedDestination)
        {
            _hasDestination = false;
            OnDestinationReached?.Invoke();
        }
    }
    
    public void SetLookAtTarget(Vector3 target, bool shouldLook = true)
    {
        _lookAtTarget = target;
        _shouldLookAtTarget = shouldLook;
    }
    
    public void SetLookAtSpeed(float speed)
    {
        _lookAtSpeed = speed;
    }
    
    public void StopLookAt()
    {
        _shouldLookAtTarget = false;
        _lookAtTarget = null;
    }

    public void SetDestination(Vector3 destination)
    {
        if (_agent == null) return;

        _currentDestination = destination;
        _hasDestination = true;

        _agent.SetDestination(destination);
    }
    
    public void StopMovement()
    {
        if (_agent == null) return;

        _agent.ResetPath();
        _hasDestination = false;
        StopLookAt();
    }
    
    
    public Vector3? GetRandomPointAround(float radius)
    {
        if (_agent == null) return null;

        Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * radius;
        randomDirection += transform.position;

        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, radius, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return null;
    }

    public void SetStoppingDistance(float distance)
    {
        _stoppingDistance = distance;
        if (_agent != null)
        {
            _agent.stoppingDistance = distance;
        }
    }
    
    public void SetAgentEnabled(bool enabled)
    {
        if (_agent != null)
        {
            _agent.enabled = enabled;
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (_agent == null || !_agent.hasPath) return;
        
        Vector3[] corners = _agent.path.corners;
        for (int i = 0; i < corners.Length - 1; i++)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(corners[i], corners[i + 1]);
        }
        
        if (_hasDestination)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_currentDestination, 0.5f);
        }
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _stoppingDistance);
    }
}