using UnityEngine;

public enum EnemyType { Normal, Elite, Boss }

public abstract class EnemySO : ScriptableObject
{
    [Header("Base Enemy Info")]
    public string enemyName;
    public EnemyType enemyType;
    public int baseHP = 100;
    
    [Header("Visual Display")]
    public Sprite enemySprite;
    public Color themeColor = Color.white;
    [Min(0.01f)] public float enemyVisualScale = 1f;

    // Fungsi aksi abstrak yang WAJIB diisi oleh setiap jenis musuh unik
    public abstract void ExecuteEnemyAction(ArenaManager arena);
}
