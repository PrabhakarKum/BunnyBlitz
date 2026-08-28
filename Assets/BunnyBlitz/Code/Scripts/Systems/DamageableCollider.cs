using System;
using UnityEngine;

namespace BunnyBlitz
{
    public class DamageableCollider : MonoBehaviour
    {
        [Flags]
        public enum DamageType
        {
            None = 0,
            Contact = 1,
            Projectile = 2,
            Explosive = 4,
        
            All = ~None
        }

        public class DamageData
        {
            public int Amount;
            public Collider2D Other;
            public DamageType Type;
        }

        public DamageType Vulnerabilities = DamageType.All;
    
        /// <summary>
        /// OnDamage will be called when that Collider get damaged (e.g. projectile hit, player hit from above)
        /// </summary>
        public Action<DamageData> OnDamage;
        /// <summary>
        /// OnRetaliate will be called if the player didn't touch this from above, and can be used by registered listener
        /// to damage back the player if needed
        /// </summary>
        public Action<DamageData> OnRetaliate;
    
        private void OnCollisionEnter2D(Collision2D other)
        {
            //As child collider could also trigger this, we ensure that the collider that trigger that message is the one
            //on this same GameObject
            if (other.otherCollider.gameObject == gameObject)
            {
                if (other.gameObject.CompareTag("Player"))
                {
                    // Calculate the distance and separation normal between this trigger and the other collider
                    ColliderDistance2D distanceInfo = other.otherCollider.Distance(other.collider);

                    // The distanceInfo.normal points from the closest point on selfCollider to the closest point on otherCollider
                    float cpAngle = Vector2.Angle(Vector2.up, distanceInfo.normal);
                    if (cpAngle <= 40.0f)
                    {
                        //if this is not vulnerable to contact damage, we ignore
                        if(!Vulnerabilities.HasFlag(DamageType.Contact))
                            return;

                        OnDamage?.Invoke(new DamageData()
                            { Amount = 1, Other = other.collider, Type = DamageType.Contact });
                    }
                    else
                    {
                        OnRetaliate?.Invoke(new DamageData() {Amount = 1, Other = other.collider, Type = DamageType.Contact});
                    }
                }
                else
                {
                    if (other.gameObject.CompareTag(PoolingSystem.ProjectileTag))
                    {
                        GameManager.Instance.VfxController.PlayVfx(VfxInteractDescBase.VfxTypeEnum.Pomegranate, other.transform.position);
                        GameManager.Instance.PoolingSystem.Return(other.gameObject);
                        //if this is not vulnerable to contact damage, we ignore
                        if(!Vulnerabilities.HasFlag(DamageType.Projectile))
                            return;
                    
                        OnDamage?.Invoke(new DamageData() {Amount = 1, Other = other.collider, Type = DamageType.Projectile});
                    }
                }
            }
        }

        public void ExternalDamage(DamageData data)
        {
            if (!Vulnerabilities.HasFlag(data.Type))
                return;
        
            OnDamage?.Invoke(data);
        }
    }
}