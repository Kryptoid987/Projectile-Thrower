using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectileThrowing.Examples
{
    public class ManualRespawnExample : BaseProjectileThrowerExample
    {
        [SerializeField] BaseProjectile manualSpawnPrefab;
        [SerializeField] GameObject manualSpawnButton1;
        [SerializeField] GameObject manualSpawnButton2;

        private void Start()
        {
            projectileThrower.OnProjectileThrown.AddListener(OnThrow);
            projectileThrower.OnProjectileSpawned.AddListener(OnSpawned);
        }

        public void SpawnManuallyFromList()
        {
            projectileThrower.ManuallySpawnProjectile();
        }

        public void SpawnManuallySentInProjectile()
        {
            projectileThrower.ManuallySpawnProjectile(manualSpawnPrefab);
        }

        void OnSpawned()
        {
            manualSpawnButton1.SetActive(false);
            manualSpawnButton2.SetActive(false);
        }

        void OnThrow()
        {
            manualSpawnButton1.SetActive(true);
            manualSpawnButton2.SetActive(true);
        }
    }
}
