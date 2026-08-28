using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace BunnyBlitz
{
    [System.Serializable]
    public class PoolingSystem
    {
        public const string CoinTag = "ItemCoin";
        public const string ProjectileTag = "Projectile"; 
        public const string ProjectileEnemyTag = "ProjectileEnemy";
        public const string ProjectileCollectableTag = "ProjectileCollectable";
    
        public GameObject coinPhysicsPrefab;
        public GameObject projectilePhysicsPrefab;
        public GameObject projectileEnemyPhysicsPrefab;
        public GameObject projectileCollectiblePrefab;
        public GameObject healthCollectiblePrefab;
        public GameObject lifeCollectiblePrefab;
        public GameObject BombPrefab;
    
        private ObjectPool<GameObject> coinPhysicsPool;
        private ObjectPool<GameObject> projectilesPool;
        private ObjectPool<GameObject> projectilesEnemyPool;
        private ObjectPool<GameObject> m_ProjectileCollectiblePool;
        private ObjectPool<GameObject> m_HealthCollectiblePool;
        private ObjectPool<GameObject> m_LifeCollectiblePool;
        private ObjectPool<GameObject> m_BombCollectiblePool;

        private Dictionary<GameObject, ObjectPool<GameObject>> m_InstanceToPool = new();
        private Dictionary<GameObject, ObjectPool<GameObject>> m_GenericPools = new Dictionary<GameObject, ObjectPool<GameObject>>();

        public void Init()
        {
            // Initialize the pool with callbacks
            coinPhysicsPool = new ObjectPool<GameObject>(
                createFunc: () =>
                {
                    var itm = Object.Instantiate(coinPhysicsPrefab);
                    itm.tag = CoinTag;
                    m_InstanceToPool[itm] = coinPhysicsPool;
                    return itm;
                },
                actionOnGet: (obj) => obj.SetActive(true),
                actionOnRelease: (obj) => obj.SetActive(false),
                actionOnDestroy: (obj) =>
                {
                    if (Application.isPlaying) Object.Destroy(obj);
                },
                collectionCheck: true,
                defaultCapacity: 10,
                maxSize: 200
            );
            PrewarmPool(coinPhysicsPool, 10);

            projectilesPool = new ObjectPool<GameObject>(
                createFunc: () =>
                {
                    var p = Object.Instantiate(projectilePhysicsPrefab);
                    p.SetActive(false); //only set active when dir has been set
                    p.GetComponent<ItemsPhysicsBehaviour>().CustomDir = Vector2.zero;
                    p.SetActive(true);
                    p.tag = ProjectileTag;
                    m_InstanceToPool[p] = projectilesPool;
                    return p;
                },
                actionOnGet: (obj) =>
                {
                    obj.SetActive(true);
                },
                actionOnRelease: (obj) =>
                {
                    obj.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
                    //this is set on object enable, we have to zero it here so when it gets reenabled the old dir isn't applied
                    obj.GetComponent<ItemsPhysicsBehaviour>().CustomDir = Vector2.zero;
                    obj.SetActive(false);
                },
                actionOnDestroy: (obj) =>
                {
                    if (Application.isPlaying) Object.Destroy(obj);
                },
                collectionCheck: true,
                defaultCapacity: 5,
                maxSize: 20
            );
            PrewarmPool(projectilesPool, 5);

            projectilesEnemyPool = new ObjectPool<GameObject>(
                createFunc: () =>
                {
                    var p = Object.Instantiate(projectileEnemyPhysicsPrefab);
                    p.SetActive(false); //only set active when dir has been set
                    p.GetComponent<ItemsPhysicsBehaviour>().CustomDir = Vector2.zero;
                    p.SetActive(true);
                    p.tag = ProjectileEnemyTag;
                    m_InstanceToPool[p] = projectilesEnemyPool;
                    return p;
                },
                actionOnGet: (obj) =>
                {
                    //obj.GetComponent<ItemsPhysicsBehaviour>().CustomDir = m_currentProjectileDirection;
                    obj.SetActive(true);
                },
                actionOnRelease: (obj) =>
                {
                    obj.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
                    //this is set on object enable, we have to zero it here so when it gets reenabled the old dir isn't applied
                    obj.GetComponent<ItemsPhysicsBehaviour>().CustomDir = Vector2.zero;
                    obj.SetActive(false);
                },
                actionOnDestroy: (obj) =>
                {
                    if (Application.isPlaying) Object.Destroy(obj);
                },
                collectionCheck: true,
                defaultCapacity: 5,
                maxSize: 20
            );
            PrewarmPool(projectilesEnemyPool, 5);

            m_ProjectileCollectiblePool = new ObjectPool<GameObject>(
                createFunc: () =>
                {
                    var itm = Object.Instantiate(projectileCollectiblePrefab);
                    m_InstanceToPool[itm] = m_ProjectileCollectiblePool;
                    return itm;
                },
                actionOnGet: (obj) => obj.SetActive(true),
                actionOnRelease: (obj) => obj.SetActive(false),
                actionOnDestroy: (obj) =>
                {
                    if (Application.isPlaying) Object.Destroy(obj);
                },
                collectionCheck: true,
                defaultCapacity: 10,
                maxSize: 200
            );
            PrewarmPool(m_ProjectileCollectiblePool, 10);

            m_HealthCollectiblePool = new ObjectPool<GameObject>(
                createFunc: () =>
                {
                    var itm = Object.Instantiate(healthCollectiblePrefab);
                    m_InstanceToPool[itm] = m_HealthCollectiblePool;
                    return itm;
                },
                actionOnGet: (obj) => obj.SetActive(true),
                actionOnRelease: (obj) => obj.SetActive(false),
                actionOnDestroy: (obj) =>
                {
                    if (Application.isPlaying) Object.Destroy(obj);
                },
                collectionCheck: true,
                defaultCapacity: 10,
                maxSize: 200
            );
            PrewarmPool(m_HealthCollectiblePool, 10);

            m_LifeCollectiblePool = new ObjectPool<GameObject>(
                createFunc: () =>
                {
                    var itm = Object.Instantiate(lifeCollectiblePrefab);
                    m_InstanceToPool[itm] = m_LifeCollectiblePool;
                    return itm;
                },
                actionOnGet: (obj) => obj.SetActive(true),
                actionOnRelease: (obj) => obj.SetActive(false),
                actionOnDestroy: (obj) =>
                {
                    if (Application.isPlaying) Object.Destroy(obj);
                },
                collectionCheck: true,
                defaultCapacity: 10,
                maxSize: 200
            );
            PrewarmPool(m_LifeCollectiblePool, 10);
            
            m_BombCollectiblePool = new ObjectPool<GameObject>(
                createFunc: () =>
                {
                    var itm = Object.Instantiate(BombPrefab);
                    m_InstanceToPool[itm] = m_BombCollectiblePool;
                    return itm;
                },
                actionOnGet: (obj) => obj.SetActive(true),
                actionOnRelease: (obj) => obj.SetActive(false),
                actionOnDestroy: (obj) =>
                {
                    if (Application.isPlaying) Object.Destroy(obj);
                },
                collectionCheck: true,
                defaultCapacity: 10,
                maxSize: 200
            );
            PrewarmPool(m_BombCollectiblePool, 10);

            //register the specific pool into the generic pool too to be able to spawn with the generic spawn function
            m_GenericPools[coinPhysicsPrefab] = coinPhysicsPool;
            m_GenericPools[projectilePhysicsPrefab] = projectilesPool;
            m_GenericPools[projectileEnemyPhysicsPrefab] = projectilesEnemyPool;
            m_GenericPools[projectileCollectiblePrefab] = m_ProjectileCollectiblePool;
            m_GenericPools[healthCollectiblePrefab] = m_HealthCollectiblePool;
            m_GenericPools[lifeCollectiblePrefab] = m_LifeCollectiblePool;
            m_GenericPools[BombPrefab] = m_BombCollectiblePool;
        }

        public void Cleanup()
        {
            foreach (var pair in m_GenericPools)
            {
                pair.Value.Clear();
            }
        }

        public void InitNewPool(GameObject prefab, int prewarmCount)
        {
            if (m_GenericPools.ContainsKey(prefab))
                return;

            var newPool = new ObjectPool<GameObject>(
                createFunc: () =>
                {
                    var itm = Object.Instantiate(prefab);
                    return itm;
                },
                actionOnGet: (obj) => obj.SetActive(true),
                actionOnRelease: (obj) => obj.SetActive(false),
                actionOnDestroy: (obj) => Object.Destroy(obj),
                collectionCheck: true,
                defaultCapacity: prewarmCount,
                maxSize: 200
            );

            PrewarmPool(newPool, prewarmCount);
            m_GenericPools[prefab] = newPool;
        }
    
        public void Spawn(string item, int amount, Vector3 pos, Transform layer, GameObject[] spawnedItems)
        {
            switch (item)
            {
                case CoinTag:
                    Spawn(coinPhysicsPrefab, amount, pos, layer, spawnedItems);
                    break;
                case ProjectileCollectableTag:
                    Spawn(projectileCollectiblePrefab, amount, pos, layer, spawnedItems);
                    break;
                default:
                    Debug.LogError("Missing item");
                    break;
            }
        }
    
        public GameObject Spawn(GameObject prefab, Vector3 pos, Transform layer)
        {
            if (!m_GenericPools.TryGetValue(prefab, out var pool)) 
                return null;

            var itm = pool.Get();
            itm.transform.position = pos;
            itm.transform.SetParent(layer);
            Helpers.RecursiveLayerSet(itm.transform, layer.gameObject.layer);

            //not this is never "removed" from the map, but it's not a problem, as the item will then just reassign
            //next time its spawned
            m_InstanceToPool[itm] = pool;

            return itm;
        }

        public void Spawn(GameObject prefab, int amount, Vector3 pos, Transform layer, GameObject[] spawnedItems)
        {
            if (!m_GenericPools.TryGetValue(prefab, out var pool)) 
                return;

            int spawnedCount = 0;
            for (int i = 0; i < amount; i++)
            {
                var itm = pool.Get();
                itm.transform.position = pos;
                itm.transform.SetParent(layer);
                Helpers.RecursiveLayerSet(itm.transform, layer.gameObject.layer);

                if (spawnedItems != null && spawnedCount < spawnedItems.Length)
                {
                    spawnedItems[spawnedCount] = itm;
                    spawnedCount++;
                }

                //not this is never "removed" from the map, but it's not a problem, as the item will then just reassign
                //next time its spawned
                m_InstanceToPool[itm] = pool;
            }
        }

        public void SpawnHealthCollectible(int amount, Vector3 pos, Transform layer, GameObject[] spawnedHealth)
        {
            Spawn(healthCollectiblePrefab, amount, pos, layer, spawnedHealth);
        }

        public void SpawnLifeCollectible(int amount, Vector3 pos, Transform layer, GameObject[] spawnedHealth)
        {
            Spawn(lifeCollectiblePrefab, amount, pos, layer, spawnedHealth);
        }

        public void SpawnProjectileCollectible(int amount, Vector3 pos, Transform layer, GameObject[] spawnedProjectile)
        {
            Spawn(projectileCollectiblePrefab, amount, pos, layer, spawnedProjectile);
        }

        public void SpawnBomb(int amount, Vector3 pos, Transform layer, GameObject[] spawnedBomb)
        {
            Spawn(BombPrefab, amount, pos, layer, spawnedBomb);
        }

        //if spawnedProjectils is null, it will just be ignore. Otherwise ensure that spawnedProjectile have at least amount space
        public void SpawnProjectile(string item, int amount, Vector3 pos, Transform layer, Vector2 dir, GameObject[] spawnedProjectile)
        {
            int spawnCount = 0;
            switch (item)
            {
                case ProjectileTag:
                    for (int i = 0; i < amount; i++)
                    {
                        var itemSpawn = projectilesPool.Get();
                        itemSpawn.transform.position = pos;
                        itemSpawn.transform.SetParent(layer);
                        
                        Helpers.RecursiveLayerSet(itemSpawn.transform, layer.gameObject.layer);

                        itemSpawn.GetComponent<ItemsPhysicsBehaviour>().CustomDir = dir;

                        if (spawnedProjectile != null)
                        {
                            spawnedProjectile[spawnCount] = itemSpawn;
                            spawnCount += 1;
                        }
                    }
                    break;

                case ProjectileEnemyTag:
                    for (int i = 0; i < amount; i++)
                    {
                        var itemSpawn = projectilesEnemyPool.Get();
                        itemSpawn.transform.position = pos;
                        itemSpawn.transform.SetParent(layer);
                        Helpers.RecursiveLayerSet(itemSpawn.transform, layer.gameObject.layer);
                        itemSpawn.GetComponent<ItemsPhysicsBehaviour>().CustomDir = dir;

                        if (spawnedProjectile != null)
                        {
                            spawnedProjectile[spawnCount] = itemSpawn;
                            spawnCount += 1;
                        }
                    }
                    break;

                default:
                    Debug.LogError("Missing item");
                    break;
            }
        }
        
        public void Return(GameObject go)
        {
            if (m_InstanceToPool.TryGetValue(go, out var pool))
            {
                pool.Release(go);
            }
            else
            {
                //was not a pool object just destroy
                Object.Destroy(go);
            }
        }
    
        void PrewarmPool(UnityEngine.Pool.ObjectPool<GameObject> op, int capacity)
        {
            //We need to Get to create then Release to ensure the counting of amount of Object created are accurate.
            //Released decrease CountAll, but it will have never been increased if Get was not called before
            var list = new List<GameObject>(capacity);
            for (int i = 0; i < capacity; i++)
            {
                list.Add(op.Get());
            }

            foreach (var t in list)
            {
                op.Release(t);
            }
            
        }
    }
}