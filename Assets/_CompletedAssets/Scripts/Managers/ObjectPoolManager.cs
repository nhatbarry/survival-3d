using System.Collections.Generic;
using UnityEngine;
using CaptainPinkTurd.Core.DesignPattern.Singleton;

namespace CaptainPinkTurd.Core.Utilities
{
    public class ObjectPoolManager : Singleton<ObjectPoolManager>
    {
        private List<PooledObjectInfo> objectPools = new List<PooledObjectInfo>();
        private GameObject objectPoolEmptyHolder;
        private GameObject gameObjectEmpty;
        private GameObject vfxEmpty;
        private GameObject unitEmpty;
        private GameObject collectableEmpty;
        private GameObject popupTextEmpty;
        
        public enum PoolType
        {
            VFX,
            GameObject,
            Unit,
            Collectable,
            PopupText,
            None
        }

        protected override void Awake()
        {
            base.Awake();
            SetupEmpties();
        }

        private void SetupEmpties()
        {
            objectPoolEmptyHolder = new GameObject("Pooled Objects");

            gameObjectEmpty = new GameObject("GameObjects");
            gameObjectEmpty.transform.SetParent(objectPoolEmptyHolder.transform);
            
            vfxEmpty = new GameObject("VFXs");
            vfxEmpty.transform.SetParent(objectPoolEmptyHolder.transform);
            
            unitEmpty = new GameObject("Units");
            unitEmpty.transform.SetParent(objectPoolEmptyHolder.transform);
            
            collectableEmpty = new GameObject("Collectables");
            collectableEmpty.transform.SetParent(objectPoolEmptyHolder.transform);

            popupTextEmpty = new GameObject("Popup Texts");
            popupTextEmpty.transform.SetParent(objectPoolEmptyHolder.transform);
        }
        
        //Better to have precision and predictability over micro-optimization using ZLinq as it causes some weird null bug
        private GameObject GetInactiveObject(PooledObjectInfo pool)
        {
            if (pool.inactiveObjects.Count == 0) return null;
            
            var obj = pool.inactiveObjects[0];
            pool.inactiveObjects.RemoveAt(0);
            
            return obj;
        }
        public GameObject SpawnObject(GameObject objectToSpawn, Vector3 spawnPos, Quaternion spawnRotation, PoolType poolType = PoolType.None)
        {
            PooledObjectInfo pool = objectPools.Find(p => p.lookupString == objectToSpawn.name);

            //if pool doesn't exist, create it
            if (pool == null)
            {
                //Debug.Log("Creating new pool for: " + objectToSpawn.name); 
                pool = new PooledObjectInfo() { lookupString = objectToSpawn.name };
                objectPools.Add(pool);
            }

            //Check if there are any inactive objects in the pool
            //Debug.Log($"Number of inactive objects in {pool.lookupString} pool: {pool.inactiveObjects.Count}");
            GameObject spawnableObj = GetInactiveObject(pool);
            //Debug.Log($"Spawnable object is null: {(!spawnableObj ? "True" : "False")}");

            //Find the parent of the empty object
            GameObject parentObject = SetParentObject(poolType);
            if (!spawnableObj)
            {
                //if there are no inactive objects, create a new one
                //Debug.Log("Instantiate " + objectToSpawn.name);
                spawnableObj = Instantiate(objectToSpawn, spawnPos, spawnRotation);

                if (parentObject)
                {
                    spawnableObj.transform.SetParent(parentObject.transform);
                }
            }
            else
            {
                //if there are an inactive object, reactivate it
                //Debug.Log("Reuse " + spawnableObj.name);
                spawnableObj.transform.position = spawnPos;
                spawnableObj.transform.rotation = spawnRotation;
                if (parentObject && spawnableObj.transform.parent.gameObject != parentObject)
                {
                    spawnableObj.transform.SetParent(parentObject.transform);
                }
                
                pool.inactiveObjects.Remove(spawnableObj);
                spawnableObj.SetActive(true);
            }

            return spawnableObj;
        }
        public GameObject SpawnObject(GameObject objectToSpawn, Transform parentTransform)
        {
            PooledObjectInfo pool = objectPools.Find(p => p.lookupString == objectToSpawn.name);

            //if pool doesn't exist, create it
            if (pool == null)
            {
                //Debug.Log("Creating new pool for: " + objectToSpawn.name); 
                pool = new PooledObjectInfo() { lookupString = objectToSpawn.name };
                objectPools.Add(pool);
            }

            //Check if there are any inactive objects in the pool
            GameObject spawnableObj = GetInactiveObject(pool);

            if (!spawnableObj)
            {
                //if there are no inactive objects, create a new one
                spawnableObj = Instantiate(objectToSpawn, parentTransform);
                // Debug.Log("Instantiate " + spawnableObj.name + spawnableObj.activeSelf);
                // spawnableObj.SetActive(true);
            }
            else
            {
                //if there are an inactive object, reactivate it and relocate it to new parent if there is one
                if (spawnableObj.transform.parent != parentTransform)
                {
                    Vector3 currentLocalPos = spawnableObj.transform.localPosition;
                    spawnableObj.transform.SetParent(parentTransform);
                    spawnableObj.transform.localPosition = currentLocalPos;
                }
                
                pool.inactiveObjects.Remove(spawnableObj);
                spawnableObj.SetActive(true);
            }

            return spawnableObj;
        }
        public void ReturnObjectToPool(GameObject obj, bool originalName = false)
        {
            PooledObjectInfo pool;
            if (!originalName)
            {
                string goName = obj.name.Substring(0, obj.name.Length - 7); //by taking off 7, we are removing the (Clone) from the name of the passed in obj
                pool = objectPools.Find(p => p.lookupString == goName);
            }
            else
            {
                //pool group means this pool belong to a group of pool under the same PoolType parent
                //e.g: Goblin and Slime pool are under the Units gameObject pool holder, in reality there's no Units pool at all
                pool = objectPools.Find(p => p.lookupString == obj.name);
            }

            if (pool == null)
            {
                Debug.LogWarning("Trying to release an object that is not pooled: " + obj.name);
                Destroy(obj);
            }
            else
            {
                if (pool.inactiveObjects.Contains(obj))
                {
                    Debug.LogError($"Inactive Objects list contains duplicate {obj.name}, " +
                                   $"please make sure this logic doesn't have any unnecessary call when {obj.name} is already inactive");
                    return;
                }
                
                //Debug.Log("Return " + obj.name + " to "+  pool.lookupString + " pool");
                obj.SetActive(false);
                pool.inactiveObjects.Add(obj);
            }
        }
        
        private GameObject SetParentObject(PoolType poolType)
        {
            switch (poolType)
            {
                case PoolType.GameObject:
                    return gameObjectEmpty;
                case PoolType.VFX:
                    return vfxEmpty; 
                case PoolType.Unit:
                    return unitEmpty;   
                case PoolType.Collectable:
                    return collectableEmpty;  
                case PoolType.PopupText:
                    return popupTextEmpty;
                case PoolType.None:
                    return null;
                default:
                    return null;
            }
        }
    }
    public class PooledObjectInfo
    {
        public string lookupString;
        public List<GameObject> inactiveObjects = new List<GameObject>();
    }
}
