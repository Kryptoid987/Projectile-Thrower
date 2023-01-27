using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace ProjectileThrowing.Examples
{
    public class AutoRespawnExample : BaseProjectileThrowerExample
    {
        public void ReloadAmmo()
        {
            projectileThrower.SetAmmoCount(10);
        }

        public void SetAmmoInfinite()
        {
            projectileThrower.SetAmmoCount(-1);
        }
    }
}
