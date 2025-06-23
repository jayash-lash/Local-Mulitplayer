using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class Spawner : NetworkBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private NetworkObjectType _networkObjectType = NetworkObjectType.BasicEnemy;
    [SerializeField] private float _spawnInterval = 5f;
    [SerializeField] private Vector2 _spawnArea = new(10, 10);
    [SerializeField] private int _maxEnemies = 10;
    
    [Header("Factory Reference")]
    [SerializeField] private NetworkObjectFactory _factory;

    private float _timer;
    private int _currentEnemyCount;

    private void Update()
    {
        if (!IsServer) return;

        _timer += Time.deltaTime;

        if (_timer >= _spawnInterval && _currentEnemyCount < _maxEnemies)
        {
            _timer = 0f;
            SpawnEnemy();
        }
    }

    private void SpawnEnemy()
    {
        Vector3 spawnPos = new Vector3(Random.Range(-_spawnArea.x, _spawnArea.x), 1, Random.Range(-_spawnArea.y, _spawnArea.y));
        
        NetworkObject enemy = _factory.CreateObject(_networkObjectType, spawnPos, Quaternion.identity);
        
        if (enemy != null)
        {
            enemy.Spawn();
            _currentEnemyCount++;
            
            if (enemy.TryGetComponent<EnemyAIController>(out var enemyAI))
            {
                enemyAI.OnEnemyDied += OnEnemyDied;
            }
        }
    }

    private void OnEnemyDied(NetworkObject enemy)
    {
        _currentEnemyCount--;
        
        if (enemy.TryGetComponent<EnemyAIController>(out var enemyAI))
        {
            enemyAI.OnEnemyDied -= OnEnemyDied;
        }
        
        _factory.ReturnObject(enemy, _networkObjectType);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, new Vector3(_spawnArea.x * 2, 1, _spawnArea.y * 2));
    }
}