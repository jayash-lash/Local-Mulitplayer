using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkObjectFactory : MonoBehaviour, INetworkObjectFactory
{
    [Serializable]
    public class ObjectTypeMapping
    {
        public NetworkObjectType Type;
        public GameObject Prefab;
    }

    [SerializeField] private List<ObjectTypeMapping> _objectMappings = new List<ObjectTypeMapping>();
    
    private Dictionary<NetworkObjectType, GameObject> _typeToPrefabMap = new Dictionary<NetworkObjectType, GameObject>();
    private Dictionary<GameObject, NetworkObjectType> _prefabToTypeMap = new Dictionary<GameObject, NetworkObjectType>();

    private void Awake()
    {
        foreach (var mapping in _objectMappings)
        {
            _typeToPrefabMap[mapping.Type] = mapping.Prefab;
            _prefabToTypeMap[mapping.Prefab] = mapping.Type;
        }
    }

    public NetworkObject CreateObject(NetworkObjectType type, Vector3 position, Quaternion rotation)
    {
        if (!_typeToPrefabMap.TryGetValue(type, out GameObject prefab))
        {
            Debug.LogError($"Prefab for type {type} not found in factory!");
            return null;
        }
        
        return NetworkObjectPool.Singleton.GetNetworkObject(prefab, position, rotation);
    }
    

    public void ReturnObject(NetworkObject obj, NetworkObjectType type)
    {
        if (!_typeToPrefabMap.TryGetValue(type, out GameObject prefab))
        {
            Debug.LogError($"Prefab for type {type} not found in factory!");
            return;
        }

        NetworkObjectPool.Singleton.ReturnNetworkObject(obj, prefab);
    }

    public GameObject GetPrefabForType(NetworkObjectType type)
    {
        return _typeToPrefabMap.TryGetValue(type, out GameObject prefab) ? prefab : null;
    }

    public NetworkObjectType GetTypeForPrefab(GameObject prefab)
    {
        return _prefabToTypeMap.TryGetValue(prefab, out NetworkObjectType type) ? type : NetworkObjectType.BasicEnemy;
    }
}