// HealDropSpawner.cs
using System.Collections;
using UnityEngine;

public class HealDropSpawner : MonoBehaviour
{
    [Header("Prefab & Targets")]
    public GameObject healDropPrefab;   // 사과 프리팹 (HealPickup2D 또는 HealOnHitPickup2D 붙은 것)
    public Transform player;
    public Transform bot;

    [Header("Spawn X Settings")]
    public XMode xMode = XMode.RandomBetweenPlayers;
    public float xOffset = 0f;
    public float betweenPadding = 0.2f; // 플레이어/봇 사이 랜덤일 때 양끝 여유
    public float rangeMinX = -8f;       // Range 모드에서 사용
    public float rangeMaxX = 8f;
    public Transform[] points;          // Points 모드에서 사용
    public bool pickPointsSequential = false;
    int _pointIndex = 0;

    [Header("Spawn Y Settings")]
    public bool useFixedYWorld = false; // true이면 고정 Y에서 스폰
    public float fixedY = 6f;           // 고정 Y
    public float dropHeight = 8f;       // false면 max(player,bot).y + dropHeight

    [Header("Auto Spawn (optional)")]
    public float interval = 0f;         // 0이면 자동 소환 안함

    public enum XMode
    {
        Midpoint,              // (player.x + bot.x)/2
        RandomBetweenPlayers,  // 두 캐릭터 사이 랜덤
        RandomInRange,         // rangeMinX ~ rangeMaxX 랜덤
        Points                 // points 배열에서 선택
    }

    void OnValidate()
    {
        if (rangeMaxX < rangeMinX) rangeMaxX = rangeMinX;
    }

    IEnumerator Start()
    {
        if (interval > 0f)
        {
            while (true)
            {
                SpawnOnce();
                yield return new WaitForSeconds(interval);
            }
        }
    }

    public void SpawnOnce()
    {
        if (!healDropPrefab || !player || !bot) return;

        float x = CalcSpawnX();
        float y = CalcSpawnY();
        Instantiate(healDropPrefab, new Vector3(x, y, 0f), Quaternion.identity);
    }

    float CalcSpawnX()
    {
        float px = player.position.x;
        float bx = bot.position.x;

        switch (xMode)
        {
            case XMode.Midpoint:
                return ((px + bx) * 0.5f) + xOffset;

            case XMode.RandomBetweenPlayers:
                {
                    float min = Mathf.Min(px, bx) + betweenPadding;
                    float max = Mathf.Max(px, bx) - betweenPadding;
                    if (min > max) { var t = min; min = max; max = t; }
                    return Random.Range(min, max) + xOffset;
                }

            case XMode.RandomInRange:
                return Random.Range(rangeMinX, rangeMaxX) + xOffset;

            case XMode.Points:
                {
                    if (points == null || points.Length == 0) return (px + bx) * 0.5f + xOffset;
                    int idx = pickPointsSequential ? (_pointIndex++ % points.Length) : Random.Range(0, points.Length);
                    return points[idx].position.x + xOffset;
                }
        }
        // fallback
        return ((px + bx) * 0.5f) + xOffset;
    }

    float CalcSpawnY()
    {
        if (useFixedYWorld) return fixedY;
        float py = player.position.y;
        float by = bot.position.y;
        return Mathf.Max(py, by) + dropHeight;
    }
}
