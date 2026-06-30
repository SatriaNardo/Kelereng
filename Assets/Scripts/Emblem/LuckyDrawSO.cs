using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(
    fileName = "LuckyDraw",
    menuName = "Emblems/Fun/Lucky Draw")]
public class LuckyDrawSO : BaseEmblemSO
{
    [Header("Possible Rewards")]
    public List<BaseEmblemSO> possibleEmblems =
        new List<BaseEmblemSO>();

    // Melakukan roll dan mengembalikan hasilnya
    public BaseEmblemSO RollReward()
    {
        Debug.Log("ROLL DIPANGGIL");

        if (possibleEmblems.Count == 0)
        {
            Debug.LogWarning("Lucky Draw tidak memiliki reward.");
            return null;
        }

        Debug.Log($"Jumlah reward: {possibleEmblems.Count}");

        BaseEmblemSO reward =
            possibleEmblems[Random.Range(0, possibleEmblems.Count)];

        Debug.Log($"Reward terpilih: {reward}");

        if (reward != null)
        {
            Debug.Log($"🎲 Lucky Draw menghasilkan: {reward.emblemName}");
        }

        return reward;
    }
}