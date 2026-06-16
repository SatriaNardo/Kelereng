using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ArenaManager : MonoBehaviour
{
    public static ArenaManager Instance;

    [Header("Game Settings")]
    public float circleRadius = 5f;
    public int currentAmmo = 4;
    public Transform arenaCenter;

    [Header("State Tracking")]
    public List<Rigidbody2D> allMarblesInArena = new List<Rigidbody2D>();
    private bool isTurnActive = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Cari semua kelereng di arena saat game dimulai
        GameObject[] targets = GameObject.FindGameObjectsWithTag("TargetMarble");
        foreach (GameObject target in targets)
        {
            if (target.TryGetComponent<Rigidbody2D>(out var rb))
            {
                allMarblesInArena.Add(rb);
            }
        }
        UpdateAmmoUI();
    }

    public void OnMarbleFlicked()
    {
        currentAmmo--;
        isTurnActive = true;
        UpdateAmmoUI();
        StartCoroutine(WaitForAllMarblesToStop());
    }

    public void AddAmmoFromOutsider()
    {
        currentAmmo++;
        UpdateAmmoUI();
        Debug.Log("Kelereng keluar! Amunisi bertambah. Total: " + currentAmmo);
    }

    private IEnumerator WaitForAllMarblesToStop()
    {
        yield return new WaitForSeconds(0.5f); // Jeda awal sebelum memeriksa kecepatan

        while (isTurnActive)
        {
            bool anyMarbleMoving = false;
            foreach (Rigidbody2D rb in allMarblesInArena)
            {
                if (rb != null && rb.linearVelocity.magnitude > 0.05f)
                {
                    anyMarbleMoving = true;
                    break;
                }
            }

            if (!anyMarbleMoving)
            {
                isTurnActive = false;
                EndTurnEvaluation();
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void EndTurnEvaluation()
    {
        Debug.Log("Giliran selesai. Sisa Amunisi: " + currentAmmo);
        if (allMarblesInArena.Count == 0)
        {
            Debug.Log("Menang! Semua kelereng bersih.");
        }
        else if (currentAmmo <= 0)
        {
            Debug.Log("Game Over! Kehabisan amunisi.");
        }
    }

    private void UpdateAmmoUI()
    {
        // Hubungkan ke skrip UI kamu di sini nanti
    }

    // Menggambar batas lingkaran di Unity Editor untuk visualisasi dev
    private void OnDrawGizmos()
    {
        if (arenaCenter != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(arenaCenter.position, circleRadius);
        }
    }
}