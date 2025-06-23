using UnityEngine;

public class NetworkUIManager : MonoBehaviour
{
    [SerializeField] private NetworkUIController _uiController;
    [SerializeField] private NetcodeManagerAdapter _networkManager;
    [SerializeField] private NetworkBroadcaster _networkBroadcaster;
    
    private void Start()
    {
        _uiController.Initialize(_networkManager, _networkBroadcaster);
    }
    
}