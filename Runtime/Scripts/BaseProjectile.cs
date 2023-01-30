using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ProjectileThrower
{

    [RequireComponent(typeof(Rigidbody))]
    public class BaseProjectile : MonoBehaviour
    {
        public Rigidbody rb;
        public UnityEvent OnInitiated;
        public UnityEvent<Collision> OnCollision = new UnityEvent<Collision>();
        bool hasCollided = false;

        protected virtual void Start()
        {
            rb.isKinematic = true;
        }

        public virtual void Initiate(Vector3 force, Vector3 angularForce)
        {
            rb.isKinematic = false;

            rb.AddForce(force, ForceMode.Impulse);
            rb.angularVelocity = angularForce;

            OnInitiated?.Invoke();
        }

        protected virtual void OnCollisionEnter(Collision collision)
        {
            if (hasCollided) return;

            hasCollided = true;
            OnCollision?.Invoke(collision);
        }
    }
}