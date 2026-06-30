using UnityEngine;

public class HydraSpawner : MonoBehaviour
{
    public static HydraSpawner Instance;

    private void Awake()
    {
        Instance = this;
    }

    public void SpawnHydraClones(
        GameObject original,
        Vector2 velocity,
        HydraBuff hydra)
    {
        for (int i = 0; i < hydra.splitCount; i++)
        {
            float angle = Mathf.Lerp(
                -hydra.spreadAngle,
                hydra.spreadAngle,
                (float)i / (hydra.splitCount - 1));

            Vector2 direction =
                Quaternion.Euler(0, 0, angle) * velocity.normalized;

            GameObject clone =
                Instantiate(original,
                    original.transform.position,
                    Quaternion.identity);

            clone.AddComponent<HydraClone>();

            Rigidbody2D cloneRb =
                clone.GetComponent<Rigidbody2D>();

            cloneRb.linearVelocity = Vector2.zero;
            cloneRb.AddForce(direction * velocity.magnitude,
                ForceMode2D.Impulse);

            Destroy(clone.GetComponent<HydraBuff>());
        }
    }
}