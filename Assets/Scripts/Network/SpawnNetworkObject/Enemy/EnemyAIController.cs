using System;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(EnemyNavMeshMovement))]
public class EnemyAIController : NetworkBehaviour, INetworkObjectPoolable
{
    public event Action<NetworkObject> OnEnemyDied;
    
    [Header("Components")]
    [SerializeField] private EnemyHealth _health;
    [SerializeField] private EnemyDetection _detection;
    [SerializeField] private EnemyCombat _combat;
    [SerializeField] private EnemyStateMachine _stateMachine;
    [SerializeField] private EnemyMovementSettings _movementSettings;
    [SerializeField] private EnemyNavMeshMovement _navMeshMovement;

    public EnemyHealth Health => _health;
    public EnemyDetection Detection => _detection;
    public EnemyCombat Combat => _combat;
    public EnemyStateMachine StateMachine => _stateMachine;
    public EnemyMovementSettings MovementSettings => _movementSettings;
    public EnemyNavMeshMovement NavMeshMovement => _navMeshMovement;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        InitializeComponents();
        SubscribeToEvents();
    }

    public override void OnNetworkDespawn()
    {
        UnsubscribeFromEvents();
        base.OnNetworkDespawn();
    }

    private void Update()
    {
        if (!IsServer) return;

        _detection.UpdateDetection();
        _stateMachine.UpdateStateMachine();
        _combat.UpdateCombat();
    }

    private void InitializeComponents()
    {
        _health.Initialize(this);
        _detection.Initialize(this);
        _combat.Initialize(this);
        _stateMachine.Initialize(this);
        _movementSettings.Initialize(this);
    }

    private void SubscribeToEvents()
    {
        if (_health != null)
            _health.OnDeath += HandleDeath;
    }

    private void UnsubscribeFromEvents()
    {
        if (_health != null)
            _health.OnDeath -= HandleDeath;
    }

    private void HandleDeath()
    {
        OnEnemyDied?.Invoke(NetworkObject);
        NetworkObject.Despawn(false);
    }
    
    public void InitializeForPool()
    {
        _health.ResetForPool();
        _detection.ResetForPool();
        _combat.ResetForPool();
        _stateMachine.ResetForPool();
        _movementSettings.ResetForPool();
        
        gameObject.SetActive(true);
    }

    public void ResetForPool()
    {
        _health.ResetForPool();
        _detection.ResetForPool();
        _combat.ResetForPool();
        _stateMachine.ResetForPool();
        _movementSettings.ResetForPool();
        
        gameObject.SetActive(false);
    }
}