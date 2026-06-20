namespace ObjectPooling
{
    /// <summary>
    /// Optional interface for components that want lifecycle callbacks
    /// when they are retrieved from or returned to a pool.
    ///
    /// Example:
    ///   public class Bullet : MonoBehaviour, IPoolable
    ///   {
    ///       public void OnSpawn()  { /* reset state */ }
    ///       public void OnDespawn() { /* cleanup */    }
    ///   }
    /// </summary>
    public interface IPoolable
    {
        /// <summary>Called immediately after the object is retrieved from the pool.</summary>
        void OnSpawn();

        /// <summary>Called immediately before the object is returned to the pool.</summary>
        void OnDespawn();
    }
}
