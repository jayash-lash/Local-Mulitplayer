using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public class RegistryObjectInfo
{
    public ulong NetworkId;
    public NetworkObject NetworkObject;
    public GameObject GameObject;
    public Transform Transform;
    public RegistryNetworkObjectType _networkObjectType;
    public NetworkObjectTeam Team;
    public Vector3 LastKnownPosition;
    public bool IsActive;
    public float LastUpdateTime;
    
    public string DisplayName;
    public int Health;
    public int MaxHealth;
    public float DetectionRange;
    public Dictionary<string, object> CustomData;

    public RegistryObjectInfo(NetworkObject netObj, RegistryNetworkObjectType type, NetworkObjectTeam team)
    {
        NetworkObject = netObj;
        NetworkId = netObj.NetworkObjectId;
        GameObject = netObj.gameObject;
        Transform = netObj.transform;
        _networkObjectType = type;
        Team = team;
        LastKnownPosition = Transform.position;
        IsActive = GameObject.activeInHierarchy;
        LastUpdateTime = Time.time;
        CustomData = new Dictionary<string, object>();
    }

    public void UpdatePosition()
    {
        if (Transform != null)
        {
            LastKnownPosition = Transform.position;
            LastUpdateTime = Time.time;
        }
    }

    public float GetDistanceTo(Vector3 position)
    {
        return Vector3.Distance(LastKnownPosition, position);
    }

    public float GetDistanceTo(RegistryObjectInfo other)
    {
        return Vector3.Distance(LastKnownPosition, other.LastKnownPosition);
    }
}

public class ObjectRegistry : NetworkBehaviour
{
    private static ObjectRegistry _instance;
    public static ObjectRegistry Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ObjectRegistry>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("ObjectRegistry");
                    _instance = go.AddComponent<ObjectRegistry>();
                }
            }
            return _instance;
        }
    }

    [Header("Registry Settings")]
    [SerializeField] private float _updateInterval = 0.1f;
    [SerializeField] private bool _enableDebugLog = false;
    
    private Dictionary<ulong, RegistryObjectInfo> allObjects = new Dictionary<ulong, RegistryObjectInfo>();
    private Dictionary<RegistryNetworkObjectType, List<RegistryObjectInfo>> objectsByType = new Dictionary<RegistryNetworkObjectType, List<RegistryObjectInfo>>();
    private Dictionary<NetworkObjectTeam, List<RegistryObjectInfo>> objectsByTeam = new Dictionary<NetworkObjectTeam, List<RegistryObjectInfo>>();
    
    public event Action<RegistryObjectInfo> OnObjectRegistered;
    public event Action<RegistryObjectInfo> OnObjectUnregistered;
    public event Action<RegistryObjectInfo> OnObjectHealthChanged;

    private float lastUpdateTime;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        InitializeCollections();
    }

    private void InitializeCollections()
    {
        foreach (RegistryNetworkObjectType type in Enum.GetValues(typeof(RegistryNetworkObjectType)))
        {
            objectsByType[type] = new List<RegistryObjectInfo>();
        }

        foreach (NetworkObjectTeam team in Enum.GetValues(typeof(NetworkObjectTeam)))
        {
            objectsByTeam[team] = new List<RegistryObjectInfo>();
        }
    }

    private void Update()
    {
        if (Time.time - lastUpdateTime >= _updateInterval)
        {
            UpdateAllPositions();
            CleanupInvalidObjects();
            lastUpdateTime = Time.time;
        }
    }

    #region Registration Methods
    public void RegisterObject(NetworkObject networkObject, RegistryNetworkObjectType type, NetworkObjectTeam team, string displayName = "")
    {
        if (networkObject == null) return;

        ulong id = networkObject.NetworkObjectId;
        
        if (allObjects.ContainsKey(id))
        {
            if (_enableDebugLog)
                Debug.LogWarning($"Object {displayName} already registered with ID {id}");
            return;
        }

        RegistryObjectInfo info = new RegistryObjectInfo(networkObject, type, team)
        {
            DisplayName = string.IsNullOrEmpty(displayName) ? networkObject.name : displayName
        };
        
        if (networkObject.TryGetComponent<PlayerHealth>(out var playerHealth))
        {
            info.Health = playerHealth.Health.Value;
            info.MaxHealth = playerHealth.Health.Value;
        }
        else if (networkObject.TryGetComponent<EnemyAIController>(out var enemyAI))
        {
            info.Health = enemyAI.Health.CurrentHealth;
            info.MaxHealth = enemyAI.Health.MaxHealth;
            info.DetectionRange = enemyAI.Detection.DetectionRange;
        }
        
        allObjects[id] = info;
        objectsByType[type].Add(info);
        objectsByTeam[team].Add(info);

        OnObjectRegistered?.Invoke(info);

        if (_enableDebugLog)
            Debug.Log($"Registered {type} '{displayName}' with ID {id}");
    }
    
    public void UnregisterObject(ulong networkId)
    {
        if (!allObjects.TryGetValue(networkId, out RegistryObjectInfo info))
            return;
        
        allObjects.Remove(networkId);
        objectsByType[info._networkObjectType].Remove(info);
        objectsByTeam[info.Team].Remove(info);

        OnObjectUnregistered?.Invoke(info);

        if (_enableDebugLog)
            Debug.Log($"Unregistered {info._networkObjectType} '{info.DisplayName}' with ID {networkId}");
    }
    
    public void UnregisterObject(NetworkObject networkObject)
    {
        if (networkObject != null)
            UnregisterObject(networkObject.NetworkObjectId);
    }

    #endregion

    #region Search Methods
    public RegistryObjectInfo FindClosest(Vector3 position, RegistryNetworkObjectType networkObjectType, float maxDistance = float.MaxValue)
    {
        var objects = objectsByType[networkObjectType];
        RegistryObjectInfo closest = null;
        float minDistance = maxDistance;

        foreach (var obj in objects)
        {
            if (!obj.IsActive || obj.Transform == null) continue;

            float distance = obj.GetDistanceTo(position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = obj;
            }
        }

        return closest;
    }
    
    public RegistryObjectInfo FindClosestPlayer(Vector3 position, float maxDistance = float.MaxValue)
    {
        return FindClosest(position, RegistryNetworkObjectType.Player, maxDistance);
    }
    
    public RegistryObjectInfo FindClosestEnemy(Vector3 position, float maxDistance = float.MaxValue)
    {
        return FindClosest(position, RegistryNetworkObjectType.Enemy, maxDistance);
    }
    
    public RegistryObjectInfo FindClosestByTeam(Vector3 position, NetworkObjectTeam team, float maxDistance = float.MaxValue)
    {
        var objects = objectsByTeam[team];
        RegistryObjectInfo closest = null;
        float minDistance = maxDistance;

        foreach (var obj in objects)
        {
            if (!obj.IsActive || obj.Transform == null) continue;

            float distance = obj.GetDistanceTo(position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = obj;
            }
        }

        return closest;
    }
    
    public List<RegistryObjectInfo> FindObjectsInRadius(Vector3 position, float radius, RegistryNetworkObjectType? objectType = null, NetworkObjectTeam? team = null)
    {
        List<RegistryObjectInfo> result = new List<RegistryObjectInfo>();
        List<RegistryObjectInfo> searchList;
        
        if (objectType.HasValue)
            searchList = objectsByType[objectType.Value];
        else if (team.HasValue)
            searchList = objectsByTeam[team.Value];
        else
            searchList = allObjects.Values.ToList();

        foreach (var obj in searchList)
        {
            if (!obj.IsActive || obj.Transform == null) continue;

            if (obj.GetDistanceTo(position) <= radius)
            {
                if (team.HasValue && obj.Team != team.Value) continue;
                if (objectType.HasValue && obj._networkObjectType != objectType.Value) continue;

                result.Add(obj);
            }
        }

        return result;
    }
    
    public List<RegistryObjectInfo> GetObjectsByType(RegistryNetworkObjectType networkObjectType)
    {
        return objectsByType[networkObjectType].Where(obj => obj.IsActive && obj.Transform != null).ToList();
    }
    
    public List<RegistryObjectInfo> GetObjectsByTeam(NetworkObjectTeam team)
    {
        return objectsByTeam[team].Where(obj => obj.IsActive && obj.Transform != null).ToList();
    }

    #endregion

    #region Update Methods
    private void UpdateAllPositions()
    {
        foreach (var obj in allObjects.Values)
        {
            if (obj.Transform != null)
            {
                obj.UpdatePosition();
                obj.IsActive = obj.GameObject.activeInHierarchy;
            }
        }
    }
    
    private void CleanupInvalidObjects()
    {
        List<ulong> toRemove = new List<ulong>();

        foreach (var kvp in allObjects)
        {
            if (kvp.Value.NetworkObject == null || kvp.Value.Transform == null)
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var id in toRemove)
        {
            UnregisterObject(id);
        }
    }
    
    public void UpdateObjectHealth(ulong networkId, int health, int maxHealth = -1)
    {
        if (allObjects.TryGetValue(networkId, out RegistryObjectInfo info))
        {
            info.Health = health;
            if (maxHealth > 0)
                info.MaxHealth = maxHealth;

            OnObjectHealthChanged?.Invoke(info);
        }
    }
    
    public void UpdateObjectCustomData(ulong networkId, string key, object value)
    {
        if (allObjects.TryGetValue(networkId, out RegistryObjectInfo info))
        {
            info.CustomData[key] = value;
        }
    }

    #endregion

    #region Utility Methods
    public RegistryObjectInfo GetObjectInfo(ulong networkId)
    {
        return allObjects.TryGetValue(networkId, out RegistryObjectInfo info) ? info : null;
    }
    
    public bool IsObjectRegistered(ulong networkId)
    {
        return allObjects.ContainsKey(networkId);
    }
    
    public int GetTotalObjectCount()
    {
        return allObjects.Count;
    }
    
    public int GetObjectCount(RegistryNetworkObjectType networkObjectType)
    {
        return objectsByType[networkObjectType].Count(obj => obj.IsActive);
    }
    
    public int GetTeamObjectCount(NetworkObjectTeam team)
    {
        return objectsByTeam[team].Count(obj => obj.IsActive);
    }
    
    public void ClearRegistry()
    {
        allObjects.Clear();
        foreach (var list in objectsByType.Values)
            list.Clear();
        foreach (var list in objectsByTeam.Values)
            list.Clear();

        if (_enableDebugLog)
            Debug.Log("Registry cleared");
    }

    #endregion

    #region Debug Methods


    [ContextMenu("Print Registry Info")]
    public void PrintRegistryInfo()
    {
        Debug.Log($"=== OBJECT REGISTRY INFO ===");
        Debug.Log($"Total Objects: {allObjects.Count}");
        
        foreach (RegistryNetworkObjectType type in Enum.GetValues(typeof(RegistryNetworkObjectType)))
        {
            int count = GetObjectCount(type);
            if (count > 0)
                Debug.Log($"{type}: {count}");
        }

        foreach (NetworkObjectTeam team in Enum.GetValues(typeof(NetworkObjectTeam)))
        {
            int count = GetTeamObjectCount(team);
            if (count > 0)
                Debug.Log($"Team {team}: {count}");
        }
    }

    #endregion
}