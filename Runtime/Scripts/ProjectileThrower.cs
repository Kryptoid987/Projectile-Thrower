using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
//using ElRaccoone.Tweens;
using System;
using UnityEngine.Events;
//using Sirenix.OdinInspector;
using MobileInput_NewInputSystem;

namespace KKG.ProjectileThrower
{
    public struct ProjectileThrownArgs
    {
        public ProjectileThrownArgs(float deltaX, float deltaY, float percentDeltaX, float percentDeltaY)
        {
            this.deltaX = deltaX;
            this.deltaY = deltaY;
            this.percentDeltaX = percentDeltaX;
            this.percentDeltaY = percentDeltaY;
        }
        
        public readonly float deltaX;
        public readonly float deltaY;
        public readonly float percentDeltaX;
        public readonly float percentDeltaY;
    }

    public class ProjectileThrower : MonoBehaviour
    {
        public enum TweenDir
        {
            FromBottom,
            FromLeft,
            FromRight
        }

        [Header("Prefabs")]
        public List<BaseProjectile> projectilePrefabs = new List<BaseProjectile>();

        [Header("References")]
        public Camera playerCam;

        [Header("General Settings")]
        public bool enableOnStart = true;

        [Header("Manual Spawn Settings")]
        [Tooltip("Should the spawned prefab from ManualSpawn be randomized, or the first in the list if no projectile is passed when calling ManualSpawn()")]
        public bool randomizeManualSpawnedPrefab = true;
        [Tooltip("Should manually spawned projectiles randomize their rotation. False will use prefabs start rotation. If rotation is sent in when ManuallySpawnProjectile() is called, than this will be ignored")]
        public bool randomizeManualSpawnRotation = true;

        [Header("Auto Respawn Settings")]
        public bool autoSpawn = true;
        [Tooltip("If true, will randomly pick a prefab from projectilePrefabs list, otherwise will spawn the first index.")]//, ShowIf("autoRespawn")]
        public bool randomizeAutoRespawnPrefab = true;
        [Tooltip("If true, will randomize rotation on all axis, otherwise will use the prefabs start rotation.")]//, ShowIf("autoRespawn")]
        public bool randomizeAutoRespawnRotation = true;
        public float respawnDelay = 0.5f;
        [Tooltip("Amount of auto respawns until auto respawn disables itself due to running out of ammo. If unlimited ammo is wanted, set to -1")]
        [SerializeField] public int ammoCount = -1;

        [Header("General Spawn Settings")]
        public bool tweenIn = false;
        /*[HideIf("@!tweenIn")]*/
        public float tweenInTime = 0.2f;
        /*[HideIf("@!tweenIn")]*/
        public float tweenOffset = 0.02f;
        /*[HideIf("@!tweenIn")]*/
        public TweenDir tweenDir = TweenDir.FromBottom;

        [Header("Throw Settings")]
        public float minForwardForce = 0.5f;
        public float fowardPower; // Controls the power of the throw.
        public float sidePower = 0.5f;
        [Tooltip("The scale the projectile should spawn in at while the user holds it. On throw it tweens back to a scale of 1")]
        public float inViewScale = 0.5f;
        [SerializeField] protected float swipeThreshold = 50f; // Minimum swipe distance.    
        protected float timeThreshold = 0.2f; // Maximum swipe time. Make sure its actually a "flick" and not a slow drag.        

        [Header("Components")]
        [SerializeField] protected Rigidbody spinMechanicRigidbody; //Rigidbody for the visual placeholder spin mechanic
        [SerializeField] protected Transform visualTransformRoot; // on screen location. Used to spawn projectile.
        [SerializeField, /*PropertySpace(0,10)*/] protected Collider touchCollider; /// The collider used for the touch controls.

        //Touch / Swiping local variables
        private Vector3 initialPos; //Initial position of the on screen placeholder.
        private Vector2 fingerDown;
        private float fingerDownTime;
        private Vector2 fingerUp;
        private float fingerUpTime;
        private Sequence moveSequence;
        //All spawned projectile so far for cleaning up
        protected List<BaseProjectile> allSpawnedProjectiles = new List<BaseProjectile>();

        //Public get/sets
        public BaseProjectile currentHeldProjectile { get; private set; }
        public bool isHeld { get; private set; } // True when finger is currently touching the on screen placeholder.
        public bool isThrowingActive { get; private set; } = true;//True when throwing is active
        public bool isResetting { get; private set; } = false; //Is the projectile thrower currently resetting

        //Events
        [Tooltip("Called when a projectile had started to be held")]//, FoldoutGroup("Events")]
        public UnityEvent OnProjectileHeld = new UnityEvent();
        public static event Action onProjectileHeld;
        [Tooltip("Called when a new projectile is spawned")]//, FoldoutGroup("Events")]
        public UnityEvent OnProjectileSpawned = new UnityEvent();
        public static event Action onProjectileSpawned;
        [Tooltip("Called when a new projectile is equipped")]//, FoldoutGroup("Events")]
        public UnityEvent OnProjectileEquipped = new UnityEvent();
        public static event Action onProjectileEquipped;
        [Tooltip("Called right before the projectile is initiated and thrown.")]//, FoldoutGroup("Events")]
        public UnityEvent<ProjectileThrownArgs> OnBeforeProjectileThrown = new UnityEvent<ProjectileThrownArgs>();
        public static event Action<ProjectileThrownArgs> onBeforeProjectileThrown;
        [Tooltip("Called when the held projectile is successfully thrown")]//, FoldoutGroup("Events")]
        public UnityEvent<ProjectileThrownArgs> OnProjectileThrown = new UnityEvent<ProjectileThrownArgs>();
        public static event Action<ProjectileThrownArgs> onProjectileThrown;
        [Tooltip("Called when the held projectile is released, but is not thrown (aka it returns to its start point)")]//, FoldoutGroup("Events")]
        public UnityEvent OnProjectileReleased = new UnityEvent();
        public static event Action onProjectileReleased;

        private void Awake()
        {
            initialPos = this.gameObject.transform.localPosition;
            touchCollider.enabled = false;

            if (enableOnStart)
            {
                EnableThrower();
            }
            else DisableThrower();
        }

        private void Start()
        {
            //Add mobile input manager to scene if it doesnt exist
            if (FindObjectOfType<MobileInputManager>() == null)
            {
                GameObject go = new GameObject("Mobile Input Manager", typeof(MobileInputManager));
            }
        }

        private void OnTouch(TouchInput touch)
        {
            if (!isThrowingActive) return;

            OnObjectSelected(touch);

            // Only move the object on screen if its being held
            if (!isHeld)
            {
                return;
            }
            UpdateObjectPositionOnScreen(touch);
        }

        #region ManagementMethods
        public void EnableThrower()
        {
            isThrowingActive = true;

            visualTransformRoot.gameObject.SetActive(true);

            if (currentHeldProjectile == null && autoSpawn)
            {
                AutoRespawnProjectile();
            }

            MobileInputManager.OnTouch += OnTouch;
            this.enabled = true;
        }
        public void DisableThrower()
        {
            isThrowingActive = false;

            if (currentHeldProjectile != null)
            {
                DOTween.Kill(currentHeldProjectile.transform);
                //currentHeldProjectile.gameObject.TweenCancelAll();

                Destroy(currentHeldProjectile.gameObject);
                currentHeldProjectile = null;

                //If auto spawning (and ammo is not set to infinite), replenish the ammo by 1 since the projectile was not thrown on disabling
                if (ammoCount != -1) ammoCount++;
            }
            visualTransformRoot.gameObject.SetActive(false);
            touchCollider.enabled = false;

            MobileInputManager.OnTouch -= OnTouch;
            this.enabled = false;
        }
        public void DisposeHeldProjectile()
        {
            if (currentHeldProjectile == null) Debug.Log("No held projectile to dispose of.");
            else
            {
                if (currentHeldProjectile != null)
                {
                    DOTween.Kill(currentHeldProjectile.transform);
                    //currentHeldProjectile.gameObject.TweenCancelAll();

                    Destroy(currentHeldProjectile.gameObject);

                    if (autoSpawn) Debug.Log("Current held projectile was disposed of, however a new one will respawn in auto respawn mode.");
                }

                //Send in true here, as its the same as if the ball was thrown, the projectile thrower now is empty
                StartCoroutine(ResetForNextThrow(true));
            }
        }
        public void CleanAllSpawnedProjectiles()
        {
            foreach (var projectile in allSpawnedProjectiles)
            {
                if (projectile != null) Destroy(projectile.gameObject);
            }

            allSpawnedProjectiles.Clear();
        }
        #endregion

        #region SpawningMethods
        //This method will only ever be called internally. If you want to manually spawn a projectile, use the ManuallySpawnProjectile() method.
        protected void AutoRespawnProjectile()
        {
            if (!isThrowingActive)
            {
                Debug.LogError("Projectile Thrower is not currently enabled. Call EnabledThrower() before trying to manually spawn projectiles.");
                return;
            }
            if (isResetting)
            {
                Debug.Log("Projectile Thrower is currently resetting and cannot spawn a projectile until it is done");
                return;
            }

            //Exit if no ammo is left, check for exactly zero because -1 == infinite
            if (ammoCount == 0) return;

            //Decrement ammo count if we have ammo and ammo is not set to infinite
            if (ammoCount != -1 && ammoCount > 0)
                ammoCount--;

            visualTransformRoot.gameObject.SetActive(true);

            //Get projectile to spawn
            int spawnIndex = randomizeAutoRespawnPrefab ? UnityEngine.Random.Range(0, projectilePrefabs.Count) : 0;
            //Get spawn rotation
            Quaternion rot = !randomizeAutoRespawnRotation
                ? projectilePrefabs[spawnIndex].transform.rotation
                : Quaternion.Euler(UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360));

            //Instantiate and initiate the projectile
            FinishEquippingSpawnedProjectile(
                Instantiate(projectilePrefabs[spawnIndex], spinMechanicRigidbody.transform.position, rot)
                , rot
                , true
            );
        }

        /// <summary>
        /// Manually call projectile spawn. Only works when auto respawn is not enabled. Can send in a specific prefab to spawn, otherwise
        /// will pull from the list of assigned prefabs to spawn.
        /// </summary>
        /// <param name="prefabToSpawn"></param>
        public void ManuallySpawnProjectile(BaseProjectile prefabToSpawn = null, Quaternion? spawnRotation = null)
        {
            if (!CanManuallyEquipOrSpawn()) return;

            visualTransformRoot.gameObject.SetActive(true);

            //Get projectile to spawn
            BaseProjectile projectileToSpawn;
            if (prefabToSpawn != null) projectileToSpawn = prefabToSpawn;
            else projectileToSpawn = randomizeManualSpawnedPrefab ? projectilePrefabs[UnityEngine.Random.Range(0, projectilePrefabs.Count)] : projectilePrefabs[0];

            //Get spawn rotation
            Quaternion rot;
            if (spawnRotation != null) rot = spawnRotation.Value;
            else rot = !randomizeAutoRespawnRotation
                ? projectileToSpawn.transform.rotation
                : Quaternion.Euler(UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360));

            //Instantiate and initiate the projectile
            FinishEquippingSpawnedProjectile(
                Instantiate(projectileToSpawn, spinMechanicRigidbody.transform.position, rot)
                , rot
                , true
            );
        }

        /// <summary>
        /// Only works when auto respawn is not enabled. Sends in a already spawned projectile from scene to be used instead of instantiating a new one with the ManuallySpawn methods
        /// </summary>
        /// <param name="spawnedProjectile"></param>
        public void ManuallyEquipSpawnedProjectile(BaseProjectile spawnedProjectile, Quaternion? spawnRotation = null)
        {
            if (!CanManuallyEquipOrSpawn()) return;

            visualTransformRoot.gameObject.SetActive(true);

            //Get spawn rotation
            Quaternion rot;
            if (spawnRotation != null) rot = spawnRotation.Value;
            else rot = !randomizeAutoRespawnRotation
                ? spawnedProjectile.transform.rotation
                : Quaternion.Euler(UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360));

            //Instantiate and initiate the projectile
            FinishEquippingSpawnedProjectile(spawnedProjectile, rot, false);
        }

        void FinishEquippingSpawnedProjectile(BaseProjectile spawnedProjectileToEquip, Quaternion spawnRotation, bool isSpawnedInternally)
        {
            currentHeldProjectile = spawnedProjectileToEquip;
            currentHeldProjectile.transform.position = spinMechanicRigidbody.transform.position;
            currentHeldProjectile.transform.rotation = spawnRotation;
            currentHeldProjectile.transform.parent = spinMechanicRigidbody.transform;
            currentHeldProjectile.transform.localScale = Vector3.one * inViewScale;

            if (tweenIn)
            {
                Vector3 dir = -playerCam.transform.up; //Default to from bottom
                if (tweenDir == TweenDir.FromLeft) dir = -playerCam.transform.right;
                else if (tweenDir == TweenDir.FromRight) dir = playerCam.transform.right;
                currentHeldProjectile.transform.position += dir * tweenOffset;

                currentHeldProjectile.transform.DOLocalMove(Vector3.zero, tweenInTime).OnComplete(() => touchCollider.enabled = true);
                //currentHeldProjectile.transform.TweenLocalPosition(Vector3.zero, tweenInTime).SetOnComplete(() => touchCollider.enabled = true);
            }
            else
                touchCollider.enabled = true;

            if (isSpawnedInternally)
            {
                OnProjectileSpawned?.Invoke();
                onProjectileSpawned?.Invoke();
            }
            else
            {
                OnProjectileEquipped?.Invoke();
                onProjectileEquipped?.Invoke();
            }
        }

        bool CanManuallyEquipOrSpawn()
        {
            if (!isThrowingActive)
            {
                Debug.LogError("Projectile Thrower is not currently enabled. Call EnabledThrower() before trying to manually spawn projectiles.");
                return false;
            }
            if (isResetting)
            {
                Debug.Log("Projectile Thrower is currently resetting and cannot spawn a projectile until it is done");
                return false;
            }
            if (currentHeldProjectile != null)
            {
                Debug.Log("Projectile thrower is already holding a spawned projectile. If you want to spawn a new projectile, either throw the current one of call DisposeHeldProjectile() first.");
                return false;
            }
            if (autoSpawn)
            {
                Debug.LogWarning("Manually spawning a projectile is not allowed when autoRespawn is enabled.");
                return false;
            }

            return true;
        }
        #endregion

        #region PrefabListMethods
        /// <summary>
        /// Adds a new projectile to the spawn list. Does not check for duplicates
        /// </summary>
        /// <param name="newProjectile"></param>
        /// <returns>Whether adding was a success</returns>
        public bool AddNewProjectileToSpawnList(BaseProjectile newProjectilePrefab)
        {
            if (newProjectilePrefab != null)
            {
                projectilePrefabs.Add(newProjectilePrefab);
                return true;
            }
            else return false;
        }
        /// <summary>
        /// Attempts to remove a prefab from the prefab list. Does not account for duplicates.
        /// </summary>
        /// <param name="projectileToRemove"></param>
        /// <returns>Whether was able to succesfully remove from list</returns>
        public bool RemoveProjectilePrefabFromSpawnList(BaseProjectile projectileToRemove)
        {
            if (projectilePrefabs.Contains(projectileToRemove))
            {
                projectilePrefabs.Remove(projectileToRemove);
                return true;
            }
            else return false;
        }
        /// <summary>
        /// Replaces the current projectile prefab list with the sent in list
        /// </summary>
        /// <param name="newProjectiles"></param>
        /// <returns>If was succesfully replaced</returns>
        public bool ReplaceCurrentProjectilePrefabList(List<BaseProjectile> newProjectiles)
        {
            if (newProjectiles != null)
            {
                projectilePrefabs = newProjectiles;
                return true;
            }
            else return false;
        }
        #endregion

        #region ThrowingAndSelectionMethods

        // If the raycast hit this object, than it is Held and can now be thrown
        protected void OnObjectSelected(TouchInput touch)
        {
            if (touch.TouchPhase == UnityEngine.InputSystem.TouchPhase.Began && touch.TouchContact)
            {
                Ray raycast = playerCam.ScreenPointToRay(touch.Position);
                RaycastHit raycastHit;
                if (Physics.Raycast(raycast, out raycastHit))
                {
                    if (raycastHit.collider.gameObject == this.gameObject)
                    {
                        OnProjectileHeld?.Invoke();
                        onProjectileHeld?.Invoke();
                        isHeld = true;
                        KillMoveSequence();
                    }
                }
            }
        }

        /// Moves the model rep on screen to follow the players finger when dragging around the screen.
        protected void UpdateObjectPositionOnScreen(TouchInput touch)
        {
            var movPos = transform.position;
            visualTransformRoot.transform.position = Vector3.Lerp(visualTransformRoot.transform.position, movPos, Time.deltaTime * 8f);

            // Init touch positions.
            if (touch.TouchPhase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                Vector3 touchPos = new Vector3(touch.Position.x, touch.Position.y, transform.localPosition.z);
                Vector3 objPosition = playerCam.ScreenToWorldPoint(touchPos);
                transform.localPosition = playerCam.transform.InverseTransformPoint(objPosition);

                this.fingerDown = touch.Position;
                this.fingerUp = touch.Position;
            }
            // Spin the snowball while dragging.
            if (touch.TouchPhase == UnityEngine.InputSystem.TouchPhase.Moved)
            {
                if (isHeld)
                {
                    Vector3 touchPos = new Vector3(touch.Position.x, touch.Position.y, transform.localPosition.z);
                    Vector3 objPosition = playerCam.ScreenToWorldPoint(touchPos);
                    transform.localPosition = playerCam.transform.InverseTransformPoint(objPosition);

                    var touchDeltaPosition = touch.DeltaPosition;
                    //If the finger actually moved more than a milemeter than update down time
                    if (touch.DeltaPosition.sqrMagnitude > 5) fingerDownTime = Time.time;
                    AddSpin(touchDeltaPosition);
                }

            }
            // End touch position.
            if (touch.TouchPhase == UnityEngine.InputSystem.TouchPhase.Ended)
            {
                this.fingerDown = touch.Position;
                this.fingerUpTime = Time.time;
                bool hasThrown = this.CheckSwipe();

                if (!hasThrown)
                {
                    OnProjectileReleased?.Invoke();
                    onProjectileReleased?.Invoke();
                }
                isHeld = false;

                StartCoroutine(ResetForNextThrow(hasThrown));
            }
        }

        // Rotates the projectile based on movement from finger touching the screen.
        protected void AddSpin(Vector2 touchDeltaPos)
        {
            spinMechanicRigidbody.AddTorque(new Vector3(touchDeltaPos.y, -touchDeltaPos.x, 0), ForceMode.Force);
        }

        // Check the swiping in its current state. Throw if hit the minimum swipe threshold
        protected bool CheckSwipe()
        {
            float duration = fingerUpTime - fingerDownTime;
            if (duration > this.timeThreshold) return false;
            if (!isHeld)
            {
                return false;
            }
            float deltaX = this.fingerDown.x - this.fingerUp.x;
            float deltaY = fingerDown.y - fingerUp.y;
            float percentDeltaX = ((this.fingerDown.x - this.fingerUp.x) / Screen.width * 100);// * Mathf.Sign(deltaX);
            float percentDeltaY = ((fingerDown.y - fingerUp.y) / Screen.height * 100);// *Mathf.Sign(deltaY); 
                                                                                      // Do not accept downswipes.
            if (Mathf.Abs(deltaY) > this.swipeThreshold)
            {
                if (deltaY < 0)
                {
                    return false;
                }
            }

            // Only throw if greater than swipe threshold
            if (Mathf.Abs(deltaY) > this.swipeThreshold || Mathf.Abs(deltaX) > this.swipeThreshold)
            {
                ThrowProjectile(deltaX * sidePower, deltaY * fowardPower, percentDeltaX * sidePower, percentDeltaY * fowardPower);
                return true;
            }
            this.fingerUp = this.fingerDown;

            return false;
        }

        // Throws the projectile.
        protected virtual void ThrowProjectile(float deltaX, float deltaY, float percentDeltaX, float percentDeltaY)
        {
            currentHeldProjectile.transform.parent = null;

            Vector3 forceToAdd = visualTransformRoot.forward * minForwardForce;
            visualTransformRoot.gameObject.SetActive(false);

            // Based on the power of the users' swipe, add extra force in that direction.
            if (Mathf.Abs(deltaX) > this.swipeThreshold)
            {
                if (deltaX > 0)
                {
                    forceToAdd += visualTransformRoot.right * percentDeltaX * .005f;
                }
                else if (deltaX < 0)
                {
                    forceToAdd += visualTransformRoot.right * percentDeltaX * .005f;
                }
            }

            if (Mathf.Abs(deltaY) > this.swipeThreshold)
            {
                if (deltaY > 0)
                {
                    forceToAdd += visualTransformRoot.forward * percentDeltaY * .01f;
                }
                else if (deltaY < 0) { }
            }

            allSpawnedProjectiles.Add(currentHeldProjectile);

            var args = new ProjectileThrownArgs(deltaX, deltaY, percentDeltaX, percentDeltaY);
            
            OnBeforeProjectileThrown?.Invoke(args);
            onBeforeProjectileThrown?.Invoke(args);
            
            currentHeldProjectile.Initiate(forceToAdd, spinMechanicRigidbody.angularVelocity);

            currentHeldProjectile.transform.DOScale(1, 0.5f);
            //currentHeldProjectile.transform.TweenLocalScale(Vector3.one, 0.5f);

            OnProjectileThrown?.Invoke(args);
            onProjectileThrown?.Invoke(args);
            //Null out the current projectile now that its thrown
            currentHeldProjectile = null;
        }

        //Resets the projectile thrower for the next throw. If the projectile has been thrown, than a new one will respawn if auto repsawn is set.
        private IEnumerator ResetForNextThrow(bool hasThrownProjectile)
        {
            if (isResetting) yield break;
            else isResetting = true;

            touchCollider.enabled = false;

            if (!hasThrownProjectile) //Tween ball back to center if it was not thrown
            {
                moveSequence = DOTween.Sequence();
                moveSequence.Append(gameObject.transform.DOLocalMove(initialPos, 0.5f).SetEase(Ease.OutSine));
                moveSequence.Join(visualTransformRoot.transform.DOLocalMove(initialPos, 0.5f).SetEase(Ease.OutSine));
                moveSequence.OnComplete(() =>
                {
                    isResetting = false;
                    touchCollider.enabled = true;
                });

                //gameObject.transform.TweenLocalPosition(initialPos, 0.5f).SetEaseSineOut();
                //visualTransformRoot.transform.TweenLocalPosition(initialPos, 0.5f).SetEaseSineOut().SetOnComplete(() =>
                //{
                //    isResetting = false;
                //    touchCollider.enabled = true;
                //});
            }
            else
            {
                transform.localPosition = initialPos;
                visualTransformRoot.position = transform.position;
                //If auto respawning, set isRestting to false after the respawn delay
                if (autoSpawn) //Auto respawn if autospawn is enabled and either there is ammo left still, or ammo is set to -1(infinite)
                {
                    yield return new WaitForSeconds(respawnDelay);

                    isResetting = false;
                    //Double check that autospawn is still active after the respawn delay and that it hasnt been updated since
                    //if (autoSpawn && (ammoCount - 1 > 0 || ammoCount == -1) && this.enabled == true)
                    if (autoSpawn && this.enabled)
                    {
                        AutoRespawnProjectile();
                    }
                }//If not auto respawning set isRestting to false right away, were done resetting
                else isResetting = false;
            }

            spinMechanicRigidbody.angularVelocity = Vector3.zero;
        }
        #endregion

        /// <summary>
        /// Sets ammo count for auto spawning. Ammo count is not used when manually spawning projectiles
        /// </summary>
        /// <param name="newAmmoCount"></param>
        public void SetAmmoCount(int newAmmoCount)
        {
            int oldAmmoCount = ammoCount;
            ammoCount = newAmmoCount;

            //If there was no ammo, and there is now, and resetting is not in progress than spawn a autospawn to get auto spawn going again
            if (oldAmmoCount == 0 && !isResetting && autoSpawn && (newAmmoCount > 0 || newAmmoCount == -1) && currentHeldProjectile == null)
                AutoRespawnProjectile();
        }

        // Ends the do move sequence if the player clicks on the projectile.
        void KillMoveSequence()
        {
            if (moveSequence != null)
            {
                if (moveSequence.IsPlaying())
                {
                    moveSequence.Kill();
                }
            }

            //gameObject.transform.TweenCancelAll();
            //visualTransformRoot.transform.TweenCancelAll();
        }

        private void OnDestroy()
        {
            MobileInputManager.OnTouch -= OnTouch;
        }
    }
}
