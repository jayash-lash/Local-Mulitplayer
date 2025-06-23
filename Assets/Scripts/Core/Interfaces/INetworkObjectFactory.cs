using Unity.Netcode;
using UnityEngine;

public interface INetworkObjectFactory
{
    NetworkObject CreateObject(NetworkObjectType type, Vector3 position, Quaternion rotation);
    void ReturnObject(NetworkObject obj, NetworkObjectType type);
}