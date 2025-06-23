using System;

public interface INetworkManager
{
    bool IsHost { get; }
    bool IsClient { get; }
    bool StartHost();
    bool StartClient();
    void Shutdown();
    
    event Action OnHostStarted;
    event Action OnClientStarted;
    event Action OnDisconnected;
}