using UnityEngine;

public class Bullet : MonoBehaviour, IPoolableObject
{
    [SerializeField]
    private float lifeTime = 3f;
    
    private const string BULLET_TAG = "Bullet"; // Must match the pooler config

    // IPoolableObject implementation
    public void OnObjectSpawn()
    {
        // Use Invoke to return the bullet to the pool after its lifetime.
        Invoke(nameof(ReturnToPool), lifeTime);
    }

    // IPoolableObject implementation
    public void OnObjectDespawn()
    {
        // Cancel the Invoke in case the object is returned to the pool before
        // its lifetime is up (e.g., it hits an enemy).
        CancelInvoke(nameof(ReturnToPool));
    }

    private void ReturnToPool()
    {
        ObjectPooler.Instance.ReturnToPool(BULLET_TAG, this.gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        ReturnToPool();
    }
}
