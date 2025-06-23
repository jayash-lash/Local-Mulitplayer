using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class RegistryQuery
{
    /// <summary>
    /// Быстрый поиск ближайшего игрока
    /// </summary>
    public static GameObject FindClosestPlayerGameObject(Vector3 position, float maxDistance = float.MaxValue)
    {
        var info = ObjectRegistry.Instance.FindClosestPlayer(position, maxDistance);
        return info?.GameObject;
    }

    /// <summary>
    /// Быстрый поиск ближайшего врага
    /// </summary>
    public static GameObject FindClosestEnemyGameObject(Vector3 position, float maxDistance = float.MaxValue)
    {
        var info = ObjectRegistry.Instance.FindClosestEnemy(position, maxDistance);
        return info?.GameObject;
    }

    /// <summary>
    /// Получить всех живых игроков
    /// </summary>
    public static List<GameObject> GetAlivePlayers()
    {
        return ObjectRegistry.Instance.GetObjectsByType(RegistryNetworkObjectType.Player).Where(info => info.Health > 0).Select(info => info.GameObject).ToList();
    }

    /// <summary>
    /// Получить всех живых врагов
    /// </summary>
    public static List<GameObject> GetAliveEnemies()
    {
        return ObjectRegistry.Instance.GetObjectsByType(RegistryNetworkObjectType.Enemy).Where(info => info.Health > 0).Select(info => info.GameObject).ToList();
    }

    /// <summary>
    /// Проверить, есть ли игроки в радиусе
    /// </summary>
    public static bool ArePlayersInRadius(Vector3 position, float radius)
    {
        var players = ObjectRegistry.Instance.FindObjectsInRadius(position, radius, RegistryNetworkObjectType.Player);
        return players.Count > 0;
    }
}