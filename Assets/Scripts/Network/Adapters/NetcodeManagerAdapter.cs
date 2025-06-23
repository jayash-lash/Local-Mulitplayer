using System;
using Unity.Netcode;
using UnityEngine;

public class NetcodeManagerAdapter : MonoBehaviour, INetworkManager
{
    public bool IsHost => NetworkManager.Singleton.IsHost;
    public bool IsClient => NetworkManager.Singleton.IsClient;
    
    public event Action OnHostStarted;
    public event Action OnClientStarted;
    public event Action OnDisconnected;
    
    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }
    
    public bool StartHost()
    {
        bool success = NetworkManager.Singleton.StartHost();
        if (success)
        {
            Debug.Log("Host started successfully");
        }
        else
        {
            Debug.LogError("Failed to start host");
        }
        return success;
    }
    
    public bool StartClient()
    {
        bool success = NetworkManager.Singleton.StartClient();
        if (success)
        {
            Debug.Log("Client started successfully");
        }
        else
        {
            Debug.LogError("Failed to start client");
        }
        return success;
    }
    
    public void Shutdown()
    {
        NetworkManager.Singleton.Shutdown();
        OnDisconnected?.Invoke();
    }
    
    private void OnClientConnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            OnClientStarted?.Invoke();
        }
    }
    
    private void OnClientDisconnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            OnDisconnected?.Invoke();
        }
    }
    
    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }
}