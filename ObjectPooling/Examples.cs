using UnityEngine;
using ObjectPooling;

// ════════════════════════════════════════════════════════════════════════════
// EXAMPLE 1 — Direct drop-in replacement for Instantiate / Destroy
// ════════════════════════════════════════════════════════════════════════════

public class BulletShooter : MonoBehaviour
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform  muzzle;
    [SerializeField] private float      bulletLifetime = 3f;

    void Update()
    {
        if (Input.GetButtonDown("Fire1"))
            Shoot();
    }

    void Shoot()
    {
        // ── Before (standard Unity) ──────────────────────────
        // var bullet = Instantiate(bulletPrefab, muzzle.position, muzzle.rotation);
        // Destroy(bullet, bulletLifetime);

        // ── After (pooled) ───────────────────────────────────
        var bullet = Pool.Instantiate(bulletPrefab, muzzle.position, muzzle.rotation);
        Pool.Destroy(bullet, bulletLifetime);   // returns to pool after delay
    }
}


// ════════════════════════════════════════════════════════════════════════════
// EXAMPLE 2 — Generic overload, get a component directly
// ════════════════════════════════════════════════════════════════════════════

public class ParticleSpawner : MonoBehaviour
{
    [SerializeField] private ParticleSystem fxPrefab;

    void SpawnEffect(Vector3 pos)
    {
        // Returns the ParticleSystem component directly — no GetComponent needed.
        var fx = Pool.Instantiate(fxPrefab, pos, Quaternion.identity);
        fx.Play();
        Pool.Destroy(fx.gameObject, fx.main.duration);
    }
}


// ════════════════════════════════════════════════════════════════════════════
// EXAMPLE 3 — IPoolable for per-object state reset
// ════════════════════════════════════════════════════════════════════════════

public class Enemy : MonoBehaviour, IPoolable
{
    private int _health;

    public void OnSpawn()
    {
        // Called automatically when retrieved from the pool.
        _health = 100;
        GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
    }

    public void OnDespawn()
    {
        // Called automatically before returned to the pool.
        // E.g. stop coroutines, clear effects.
        StopAllCoroutines();
    }

    public void TakeDamage(int dmg)
    {
        _health -= dmg;
        if (_health <= 0)
            Pool.Destroy(gameObject);
    }
}


// ════════════════════════════════════════════════════════════════════════════
// EXAMPLE 4 — Pre-warming pools at scene start
// ════════════════════════════════════════════════════════════════════════════

public class LevelManager : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private int        prewarmEnemies = 20;
    [SerializeField] private int        prewarmBullets = 50;

    void Awake()
    {
        // Allocate objects up-front so the first wave has zero GC spikes.
        Pool.Prewarm(enemyPrefab,  prewarmEnemies);
        Pool.Prewarm(bulletPrefab, prewarmBullets);
    }

    void OnDestroy()
    {
        // Optional: release all pool memory when the scene ends.
        Pool.ClearAll();
    }
}


// ════════════════════════════════════════════════════════════════════════════
// EXAMPLE 5 — Reading pool stats at runtime
// ════════════════════════════════════════════════════════════════════════════

public class PoolStatsUI : MonoBehaviour
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private TMPro.TextMeshProUGUI label;

    void Update()
    {
        var (active, inactive) = Pool.GetStats(bulletPrefab);
        label.text = $"Bullets — active: {active}  pooled: {inactive}";
    }
}
