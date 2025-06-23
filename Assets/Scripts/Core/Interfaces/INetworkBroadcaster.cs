using System;

public interface INetworkBroadcaster
{
    event Action OnHostAvailabilityChanged;
    bool IsHostAvailable { get; }
    bool IsLocalHostRunning { get; }

    void StartHostBroadcasting();
    void StopHostBroadcasting();
    void StartListening();
    void StopListening();
}