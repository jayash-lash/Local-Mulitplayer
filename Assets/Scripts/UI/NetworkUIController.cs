using UnityEngine;
using UnityEngine.UI;

public class NetworkUIController : MonoBehaviour, IUIController
{
    [SerializeField] private GameObject _buttonHolder;
    [SerializeField] private Button _startHostButton;
    [SerializeField] private Button _startClientButton;
    [SerializeField] private Button _disconnectButton;
    
    private INetworkManager _networkManager;
    private INetworkBroadcaster _networkBroadcaster;
    
    public void Initialize(INetworkManager networkManager, INetworkBroadcaster networkBroadcaster)
    {
        _networkManager = networkManager;
        _networkBroadcaster = networkBroadcaster;
        
        SetupButtonEvents();
        SubscribeToEvents();
        UpdateButtonStates();
    }
    
    private void SetupButtonEvents()
    {
        _startHostButton.onClick.AddListener(OnStartHostClicked);
        _startClientButton.onClick.AddListener(OnStartClientClicked);
        _disconnectButton.onClick.AddListener(OnDisconnectClicked);
    }
    
    private void SubscribeToEvents()
    {
        _networkManager.OnHostStarted += OnNetworkStateChanged;
        _networkManager.OnClientStarted += OnNetworkStateChanged;
        _networkManager.OnDisconnected += OnNetworkStateChanged;
        _networkBroadcaster.OnHostAvailabilityChanged += OnNetworkStateChanged;
    }
    
    private void UnsubscribeFromEvents()
    {
        if (_networkManager != null)
        {
            _networkManager.OnHostStarted -= OnNetworkStateChanged;
            _networkManager.OnClientStarted -= OnNetworkStateChanged;
            _networkManager.OnDisconnected -= OnNetworkStateChanged;
        }
        
        if (_networkBroadcaster != null)
        {
            _networkBroadcaster.OnHostAvailabilityChanged -= OnNetworkStateChanged;
        }
    }
    
    private void OnStartHostClicked()
    {
        if (_networkManager.StartHost())
        {
            _networkBroadcaster.StartHostBroadcasting();
            _buttonHolder.SetActive(false);
        }
    }
    
    private void OnStartClientClicked()
    {
        _networkManager.StartClient();
        _buttonHolder.SetActive(false);
    }
    
    private void OnDisconnectClicked()
    {
        if (_networkBroadcaster.IsLocalHostRunning)
        {
            _networkBroadcaster.StopHostBroadcasting();
            _buttonHolder.SetActive(false);
        }
        
        _networkManager.Shutdown();
    }
    
    private void OnNetworkStateChanged()
    {
        UpdateButtonStates();
    }
    
    public void UpdateButtonStates()
    {
        bool isConnected = _networkManager.IsHost || _networkManager.IsClient;
        
        // Host button: disabled if connected or if another host is available
        _startHostButton.interactable = !isConnected && !_networkBroadcaster.IsHostAvailable;
        
        // Client button: disabled if connected or if no host available or if we are the host
        _startClientButton.interactable = !isConnected && _networkBroadcaster.IsHostAvailable && !_networkBroadcaster.IsLocalHostRunning;
        
        // Disconnect button: enabled only if connected
        _disconnectButton.interactable = isConnected;
        
        LogUIState();
    }
    
    private void LogUIState()
    {
        Debug.Log($"UI Updated - Host Available: {_networkBroadcaster.IsHostAvailable}, " +
                  $"Is Local Host: {_networkBroadcaster.IsLocalHostRunning}, " +
                  $"Is Host: {_networkManager.IsHost}, Is Client: {_networkManager.IsClient}");
    }
    
    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
}