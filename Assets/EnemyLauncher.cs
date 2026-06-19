using UnityEngine;
using System.Collections;

public class EnemyLauncher : MonoBehaviour
{
    public static EnemyLauncher Instance;

    [Header("Launcher Settings")]
    public GameObject enemyMarblePrefab; // Prefab for enemy projectile
    public Transform enemyLaunchPoint;   // Positioned at top of screen
    public float launchForceMultiplier = 15f;
    public float aiDecisionDelay = 1.5f; // Gives look/feel of thinking delay

    private void Awake()
    {
        Instance = this;
    }

    public void RequestAIShot()
    {
        StartCoroutine(ExecuteAIShotRoutine());
    }

    private IEnumerator ExecuteAIShotRoutine()
    {
        // Wait a brief moment to look organic on screen
        yield return new WaitForSeconds(aiDecisionDelay);

        if (enemyMarblePrefab == null || enemyLaunchPoint == null) yield break;

        // 1. Determine aim target position
        Vector2 targetPosition = ArenaManager.Instance.arenaCenter.position; // Default to center

        // If there are target marbles left, pick a random one to target sniper style
        if (ArenaManager.Instance.allMarblesInArena.Count > 0)
        {
            int randomIndex = Random.Range(0, ArenaManager.Instance.allMarblesInArena.Count);
            Rigidbody2D selectedTarget = ArenaManager.Instance.allMarblesInArena[randomIndex];
            
            if (selectedTarget != null)
            {
                targetPosition = selectedTarget.transform.position;
            }
        }

        // 2. Calculate launch vector direction
        //Vector2 randOffset = new Vector2(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f));
        Vector2 launchDirection = (targetPosition - (Vector2)enemyLaunchPoint.position).normalized;


        // 3. Spawn and fire the enemy gacoan
        GameObject enemyGacoan = Instantiate(enemyMarblePrefab, enemyLaunchPoint.position, Quaternion.identity);
        Rigidbody2D rb = enemyGacoan.GetComponent<Rigidbody2D>();

        rb.bodyType = RigidbodyType2D.Dynamic;
        ArenaManager.Instance.allMarblesInArena.Add(rb);

        // Apply force down towards target
        rb.AddForce(launchDirection * launchForceMultiplier, ForceMode2D.Impulse);

        // 4. Notify ArenaManager (pass false because it's the enemy shooting)
        ArenaManager.Instance.OnMarbleFlicked(rb, false);
    }
}