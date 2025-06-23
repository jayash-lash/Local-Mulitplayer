using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class Projectile : NetworkBehaviour, INetworkObjectPoolable
{
    [Header("Projectile Settings")]
    [SerializeField] private float _speed = 10f;
    [SerializeField] private int _damage = 10;
    [SerializeField] private float _lifetime = 5f;
    [SerializeField] private LayerMask obstacleLayer;
    private Vector3 _direction;
    private float _lifeTimer;
    private NetworkObjectType projectileType;

    public void Initialize(Vector3 dir, NetworkObjectType type)
    {
        _direction = new Vector3(dir.x, 0f, dir.z).normalized;
        projectileType = type;
        _lifeTimer = 0f;
    }

    private void Update()
    {
        if (!IsServer) return;
        
        transform.position += _direction * _speed * Time.deltaTime;

        if (Physics.Raycast(transform.position, _direction, 2f, obstacleLayer))
        {
            ReturnToPool();
            return;
        }
        
        _lifeTimer += Time.deltaTime;
        if (_lifeTimer >= _lifetime)
        {
            ReturnToPool();
        }
    }

    
    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        
        if (other.TryGetComponent(out PlayerHealth health))
        {
            health.TakeDamage(_damage);
            ReturnToPool();
            return;
        }
    }

    private void ReturnToPool()
    {
        NetworkObjectFactory factory = FindObjectOfType<NetworkObjectFactory>();
        if (factory != null)
        {
            NetworkObject.Despawn(false);
            factory.ReturnObject(NetworkObject, projectileType);
        }
    }
    
    public void InitializeForPool()
    {
        _lifeTimer = 0f;
        _direction = Vector3.zero;
        gameObject.SetActive(true);
    }

    public void ResetForPool()
    {
        _direction = Vector3.zero;
        _lifeTimer = 0f;
        gameObject.SetActive(false);
    }
}