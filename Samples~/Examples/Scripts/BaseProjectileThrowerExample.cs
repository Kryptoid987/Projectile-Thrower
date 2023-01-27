using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectileThrowing.Examples
{
    public abstract class BaseProjectileThrowerExample : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] protected ProjectileThrower projectileThrower;
        [Header("Projectile Prefab")]
        [SerializeField] protected BaseProjectile spherePrefab;
        [SerializeField] protected List<BaseProjectile> replaceList = new List<BaseProjectile>();

        public void AddSphereToList()
        {
            if (projectileThrower.AddNewProjectileToSpawnList(spherePrefab))
                Debug.Log("Added to list successfully");
            else Debug.Log("Failed to add prefab to list");
        }

        public void RemoveSphereToList()
        {
            if (projectileThrower.RemoveProjectilePrefabFromSpawnList(spherePrefab))
                Debug.Log("Removed from list successfully");
            else Debug.Log("Failed to remove from list");
        }

        public void ReplacePrefabList()
        {
            if (projectileThrower.ReplaceCurrentProjectilePrefabList(replaceList))
                Debug.Log("Successfully replaced prefab list with new list");
            else Debug.Log("Failed to replace prefab list");
        }
    }
}
