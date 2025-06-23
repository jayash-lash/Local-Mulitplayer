using Unity.Netcode;
using UnityEngine;

public class RegistryAutoRegister : NetworkBehaviour
{
    [Header("Registry Settings")]    
    public RegistryNetworkObjectType _networkObjectType = RegistryNetworkObjectType.Player;
    public NetworkObjectTeam team = NetworkObjectTeam.Players;
    public string displayName = "";
    public bool registerOnSpawn = true;
    public bool unregisterOnDespawn = true;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (registerOnSpawn)
        {
            string name = string.IsNullOrEmpty(displayName) ? gameObject.name : displayName;
            ObjectRegistry.Instance.RegisterObject(NetworkObject, _networkObjectType, team, name);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (unregisterOnDespawn)
        {
            ObjectRegistry.Instance.UnregisterObject(NetworkObject);
        }
        
        base.OnNetworkDespawn();
    }

    /// <summary>
    /// Обновляет здоровье в реестре
    /// </summary>
    public void UpdateHealthInRegistry(int health, int maxHealth = -1)
    {
        ObjectRegistry.Instance.UpdateObjectHealth(NetworkObject.NetworkObjectId, health, maxHealth);
    }

    /// <summary>
    /// Добавляет пользовательские данные в реестр
    /// </summary>
    public void SetCustomData(string key, object value)
    {
        ObjectRegistry.Instance.UpdateObjectCustomData(NetworkObject.NetworkObjectId, key, value);
    }
}