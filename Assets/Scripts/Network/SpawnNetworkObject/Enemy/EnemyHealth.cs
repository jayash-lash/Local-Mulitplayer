using System;
using Unity.Netcode;
using UnityEngine;

public class EnemyHealth : NetworkBehaviour
{
    public event Action OnDeath;

    [Header("Health Settings")]
    [SerializeField] private int _maxHealth = 100;
    [SerializeField] private int _currentHealth = 100;

    public int CurrentHealth => _currentHealth;
    public int MaxHealth => _maxHealth;

    private EnemyAIController _aiController;
    private RegistryAutoRegister _registryComponent;

    public void Initialize(EnemyAIController aiController)
    {
        _aiController = aiController;
        _registryComponent = GetComponent<RegistryAutoRegister>();
        
        if (_registryComponent == null)
        {
            _registryComponent = gameObject.AddComponent<RegistryAutoRegister>();
            _registryComponent._networkObjectType = RegistryNetworkObjectType.Enemy;
            _registryComponent.team = NetworkObjectTeam.Enemies;
        }
        
        UpdateRegistryHealth();
    }

    private void UpdateRegistryHealth()
    {
        _registryComponent.UpdateHealthInRegistry(_currentHealth, _maxHealth);
    }

    public void ResetForPool()
    {
        _currentHealth = _maxHealth;
        UpdateRegistryHealth();
        _registryComponent.SetCustomData("isDead", false);
    }
}