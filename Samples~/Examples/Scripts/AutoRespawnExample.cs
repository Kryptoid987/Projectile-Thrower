using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace KKG.ProjectileThrower.Examples
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
