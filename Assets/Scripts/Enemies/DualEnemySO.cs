using UnityEngine;

[CreateAssetMenu(fileName = "NewDualEnemyEncounter", menuName = "Enemies/Dual Enemy Encounter")]
public class DualEnemySO : EnemySO
{
    [Header("Enemy Pair")]
    public EnemySO firstEnemy;
    public EnemySO secondEnemy;

    public override void ExecuteEnemyAction(ArenaManager arena)
    {
        if (firstEnemy != null)
        {
            firstEnemy.ExecuteEnemyAction(arena);
        }

        if (secondEnemy != null)
        {
            secondEnemy.ExecuteEnemyAction(arena);
        }
    }
}
