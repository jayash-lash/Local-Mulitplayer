using Unity.Netcode;
using UnityEngine;

public class EnemyDetection : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private float _detectionRange = 15f;
    [SerializeField] private float _targetSearchInterval = 0.5f;
    [SerializeField] private bool _useRegistry = true;
    
    [Header("Line of Sight")]
    [SerializeField] private LayerMask _obstacleLayerMask;
    [SerializeField] private float _eyeHeight = 1.5f;
    [SerializeField] private float _playerHeight = 1.8f;

    public float DetectionRange => _detectionRange;
    public RegistryObjectInfo CurrentTarget { get; private set; }

    private EnemyAIController _aiController;
    private float _targetSearchTimer;

    public void Initialize(EnemyAIController aiController)
    {
        _aiController = aiController;
    }

    public void UpdateDetection()
    {
        _targetSearchTimer += Time.deltaTime;

        if (_targetSearchTimer >= _targetSearchInterval)
        {
            _targetSearchTimer = 0f;
            FindTarget();
        }
    }

    private void FindTarget()
    {
        RegistryObjectInfo potentialTarget = null;
        
        if (_useRegistry)
        {
            potentialTarget = ObjectRegistry.Instance.FindClosestPlayer(transform.position, _detectionRange);
        }
        else
        {
            GameObject closestPlayer = FindClosestPlayerOldWay();
            if (closestPlayer != null)
            {
                potentialTarget = ObjectRegistry.Instance.GetObjectInfo(
                    closestPlayer.GetComponent<NetworkObject>().NetworkObjectId);
            }
        }
        
        if (potentialTarget != null && potentialTarget.IsActive)
        {
            if (HasLineOfSight(potentialTarget.LastKnownPosition))
            {
                CurrentTarget = potentialTarget;
            }
            else if (CurrentTarget == null || CurrentTarget != potentialTarget)
            {
                CurrentTarget = null;
            }
        }
        else
        {
            CurrentTarget = null;
        }
    }

    public bool HasLineOfSight(Vector3 targetPosition)
    {
        Vector3 eyePosition = transform.position + Vector3.up * _eyeHeight;
        Vector3 targetEyePosition = targetPosition + Vector3.up * _playerHeight;
        Vector3 direction = targetEyePosition - eyePosition;
        float distance = direction.magnitude;
    
        return !Physics.Raycast(eyePosition, direction.normalized, distance, _obstacleLayerMask);
    }

    private GameObject FindClosestPlayerOldWay()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        GameObject closest = null;
        float minDist = _detectionRange;

        foreach (var player in players)
        {
            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = player;
            }
        }

        return closest;
    }

    public void ResetForPool()
    {
        CurrentTarget = null;
        _targetSearchTimer = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRange);
        
        if (CurrentTarget != null && CurrentTarget.IsActive)
        {
            Vector3 eyePosition = transform.position + Vector3.up * _eyeHeight;
            Vector3 targetPosition = CurrentTarget.LastKnownPosition + Vector3.up * _playerHeight;
            
            Gizmos.color = HasLineOfSight(CurrentTarget.LastKnownPosition) ? Color.green : Color.red;
            Gizmos.DrawLine(eyePosition, targetPosition);
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(eyePosition, 0.2f);
        }
    }
}