using UnityEngine;

/// <summary>
/// A fired shot. It is not a carryable Ammo item.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    readonly RaycastHit[] hits = new RaycastHit[8];

    float damage;
    float dieAt;
    bool spent;
    Vector3 previousPosition;

    public void Launch(Vector3 velocity, float shotDamage, float lifetime, Collider[] ignore)
    {
        damage = shotDamage;
        dieAt = Time.time + lifetime;

        Rigidbody body = GetComponent<Rigidbody>();
        body.useGravity = false;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.interpolation = RigidbodyInterpolation.Interpolate;

        Collider mine = GetComponent<Collider>();
        if (mine != null && ignore != null)
        {
            for (int i = 0; i < ignore.Length; i++)
            {
                if (ignore[i] != null)
                    Physics.IgnoreCollision(mine, ignore[i], true);
            }
        }

        body.linearVelocity = velocity;
        previousPosition = transform.position;
    }

    void FixedUpdate()
    {
        if (spent)
            return;

        Vector3 current = transform.position;
        Vector3 delta = current - previousPosition;
        float distance = delta.magnitude;
        if (distance > 0.001f)
        {
            int count = Physics.SphereCastNonAlloc(
                previousPosition,
                0.1f,
                delta / distance,
                hits,
                distance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            float nearest = float.MaxValue;
            Collider found = null;
            for (int i = 0; i < count; i++)
            {
                Collider collider = hits[i].collider;
                if (collider == null || collider.gameObject == gameObject)
                    continue;

                if (hits[i].distance < nearest)
                {
                    nearest = hits[i].distance;
                    found = collider;
                }
            }

            if (found != null)
                Hit(found);
        }

        previousPosition = transform.position;
    }

    void Update()
    {
        if (!spent && Time.time >= dieAt)
            Destroy(gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (spent || collision == null)
            return;

        Hit(collision.collider);
    }

    void Hit(Collider collider)
    {
        if (spent || collider == null)
            return;

        spent = true;
        TargetDummy target = collider.GetComponentInParent<TargetDummy>();
        if (target != null)
            target.TakeDamage(damage);

        Destroy(gameObject);
    }
}
