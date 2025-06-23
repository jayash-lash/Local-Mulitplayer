using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class NetworkBroadcaster : MonoBehaviour, INetworkBroadcaster
{
    private const int MulticastPort = 12345;
    private const string MulticastGroup = "224.0.0.1";
    private const string HostAvailableMessage = "HOST AVAILABLE";
    private const string HostUnavailableMessage = "HOST UNAVAILABLE";
    
    public bool IsHostAvailable { get; private set; }
    public bool IsLocalHostRunning { get; private set; }
    
    public event Action OnHostAvailabilityChanged;
    
    private UdpClient _udpSender;
    private UdpClient _udpReceiver;
    private IPEndPoint _multicastEndPoint;
    private IPAddress _multicastAddress;
    
    private Coroutine _broadcastCoroutine;
    private Coroutine listenCoroutine;
    
    private void Start()
    {
        InitializeUDP();
        StartListening();
    }
    
    private void InitializeUDP()
    {
        try
        {
            _multicastAddress = IPAddress.Parse(MulticastGroup);
            _multicastEndPoint = new IPEndPoint(_multicastAddress, MulticastPort);
            
            _udpSender = new UdpClient();
            
            _udpReceiver = new UdpClient();
            _udpReceiver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpReceiver.Client.Bind(new IPEndPoint(IPAddress.Any, MulticastPort));
            _udpReceiver.JoinMulticastGroup(_multicastAddress);
            
            Debug.Log("UDP multicast initialized successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize UDP: {e.Message}");
        }
    }
    
    public void StartHostBroadcasting()
    {
        if (IsLocalHostRunning) return;
        
        IsLocalHostRunning = true;
        SetHostAvailability(true);
        
        if (_broadcastCoroutine != null)
            StopCoroutine(_broadcastCoroutine);
        
        _broadcastCoroutine = StartCoroutine(BroadcastHostStatus());
        Debug.Log("Started host broadcasting");
    }
    
    public void StopHostBroadcasting()
    {
        if (!IsLocalHostRunning) return;
        
        BroadcastMessage(HostUnavailableMessage);
        
        if (_broadcastCoroutine != null)
        {
            StopCoroutine(_broadcastCoroutine);
            _broadcastCoroutine = null;
        }
        
        IsLocalHostRunning = false;
        SetHostAvailability(false);
        Debug.Log("Stopped host broadcasting");
    }
    
    public void StartListening()
    {
        if (listenCoroutine != null) return;
        
        listenCoroutine = StartCoroutine(ListenForBroadcasts());
    }
    
    public void StopListening()
    {
        if (listenCoroutine != null)
        {
            StopCoroutine(listenCoroutine);
            listenCoroutine = null;
        }
    }
    
    private void SetHostAvailability(bool available)
    {
        if (IsHostAvailable != available)
        {
            IsHostAvailable = available;
            OnHostAvailabilityChanged?.Invoke();
        }
    }
    
    private IEnumerator BroadcastHostStatus()
    {
        while (IsLocalHostRunning)
        {
            BroadcastMessage(HostAvailableMessage);
            yield return new WaitForSeconds(1f);
        }
    }
    
    private new void BroadcastMessage(string message)
    {
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            _udpSender.Send(data, data.Length, _multicastEndPoint);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to broadcast message: {e.Message}");
        }
    }
    
    private IEnumerator ListenForBroadcasts()
    {
        while (true)
        {
            try
            {
                if (_udpReceiver != null && _udpReceiver.Available > 0)
                {
                    IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = _udpReceiver.Receive(ref remoteEndPoint);
                    string message = Encoding.UTF8.GetString(data);
                    
                    ProcessReceivedMessage(message);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error listening for broadcasts: {e.Message}");
            }
            
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    private void ProcessReceivedMessage(string message)
    {
        if (IsLocalHostRunning) return; // Don't process our own broadcasts
        
        switch (message)
        {
            case HostAvailableMessage:
                SetHostAvailability(true);
                break;
            case HostUnavailableMessage:
                SetHostAvailability(false);
                break;
        }
    }
    
    private void OnDestroy()
    {
        StopHostBroadcasting();
        StopListening();
        
        _udpSender?.Close();
        _udpReceiver?.Close();
    }
    
    private void OnApplicationPause(bool pauseStatus)
    {
        if (!IsLocalHostRunning) return;
        
        if (pauseStatus)
        {
            BroadcastMessage(HostUnavailableMessage);
        }
        else if (_broadcastCoroutine == null)
        {
            _broadcastCoroutine = StartCoroutine(BroadcastHostStatus());
        }
    }
}