using Unity.Netcode;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    public bool IsAlive => _health.Value > 0;
    public NetworkVariable<int> Health => _health;
    public int MaxHealth => _maxHealth;
    
    [Header("Health Settings")]
    [SerializeField] private NetworkVariable<int> _health = new NetworkVariable<int>(100);
    [SerializeField] private int _maxHealth = 100;

    private RegistryAutoRegister _registryComponent;

    private void Awake()
    {
        _registryComponent = gameObject.GetComponent<RegistryAutoRegister>();
        if (_registryComponent == null)
        {
            _registryComponent = gameObject.AddComponent<RegistryAutoRegister>();
            _registryComponent._networkObjectType = RegistryNetworkObjectType.Player;
            _registryComponent.team = NetworkObjectTeam.Players;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        _health.OnValueChanged += OnHealthChanged;
        
        if (IsServer)
        {
            _health.Value = _maxHealth;
        }
    }

    public override void OnNetworkDespawn()
    {
        _health.OnValueChanged -= OnHealthChanged;
        base.OnNetworkDespawn();
    }

    public void TakeDamage(int amount)
    {
        if (!IsServer) return;

        int newHealth = Mathf.Max(0, _health.Value - amount);
        _health.Value = newHealth;

        if (newHealth <= 0)
        {
            Die();
        }
    }
    

    private void OnHealthChanged(int oldValue, int newValue)
    {
        _registryComponent.UpdateHealthInRegistry(newValue, _maxHealth);
        
        if (newValue <= 0 && oldValue > 0)
        {
            OnPlayerDied();
        }
        else if (newValue > 0 && oldValue <= 0)
        {
            OnPlayerRevived();
        }
    }

    private void Die()
    {
        Debug.Log($"Player {OwnerClientId} died.");
        
        _registryComponent.SetCustomData("isDead", true);
        _registryComponent.SetCustomData("deathTime", Time.time);
    }

    private void OnPlayerDied()
    {
        Debug.Log($"Player {OwnerClientId} health reached zero.");
    }

    private void OnPlayerRevived()
    {
        _registryComponent.SetCustomData("isDead", false);
        Debug.Log($"Player {OwnerClientId} was revived.");
    }
}

