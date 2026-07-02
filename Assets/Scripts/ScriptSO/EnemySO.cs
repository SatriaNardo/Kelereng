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

    [Header("Visual Shadow")]
    public bool showEnemyShadow = true;
    public Color enemyShadowColor = new Color(0f, 0f, 0f, 0.35f);
    [Min(0.01f)] public float enemyShadowWidth = 1.25f;
    [Min(0.01f)] public float enemyShadowHeight = 0.35f;
    public Vector2 enemyShadowOffset = new Vector2(0f, -0.35f);
    public int enemyShadowSortingOrderOffset = -1;

    [Header("Action SFX")]
    [Tooltip("One clip per enemy move. Move 1 uses slot 0, move 2 uses slot 1, and so on.")]
    public AudioClip[] enemyActionSfx;
    [Range(0f, 1f)] public float enemyActionSfxVolume = 1f;

    // Fungsi aksi abstrak yang WAJIB diisi oleh setiap jenis musuh unik
    public abstract void ExecuteEnemyAction(ArenaManager arena);

    protected virtual int EnemyActionSfxSlotCount => 1;

    protected virtual void OnValidate()
    {
        int targetSlotCount = Mathf.Max(0, EnemyActionSfxSlotCount);
        if (targetSlotCount <= 0) return;

        if (enemyActionSfx == null)
        {
            enemyActionSfx = new AudioClip[targetSlotCount];
            return;
        }

        if (enemyActionSfx.Length == targetSlotCount) return;

        AudioClip[] resizedSfx = new AudioClip[targetSlotCount];
        int copyCount = Mathf.Min(enemyActionSfx.Length, resizedSfx.Length);
        for (int i = 0; i < copyCount; i++)
        {
            resizedSfx[i] = enemyActionSfx[i];
        }

        enemyActionSfx = resizedSfx;
    }

    protected void PlayActionSfx(ArenaManager arena, int actionIndex)
    {
        if (enemyActionSfx == null || actionIndex < 0 || actionIndex >= enemyActionSfx.Length) return;

        AudioClip clip = enemyActionSfx[actionIndex];
        if (clip == null) return;

        Vector3 position = Vector3.zero;
        if (arena != null)
        {
            if (arena.enemyPlace != null)
            {
                position = arena.enemyPlace.position;
            }
            else if (arena.arenaCenter != null)
            {
                position = arena.arenaCenter.position;
            }
            else
            {
                position = arena.transform.position;
            }
        }

        GameObject sfxObject = new GameObject($"{name}_ActionSFX_{actionIndex + 1}");
        sfxObject.transform.position = position;

        AudioSource audioSource = sfxObject.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = enemyActionSfxVolume;
        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake = false;
        audioSource.Play();

        Destroy(sfxObject, clip.length + 0.1f);
    }
}
