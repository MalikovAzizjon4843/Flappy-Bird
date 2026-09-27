using UnityEngine;

public class SpawnerScript : MonoBehaviour
{
    public GameObject pipePrefab; // Nusxa olinadigan shablon
    public GameObject coinPrefab; // Tools → Create Coin Prefab yaratgan Assets/Prefabs/Coin.prefab
    private float timer = 0f;
    public float heightOffset = 2.5f; // Quvurlarning vertikal o'zgarish oralig'i

    // Tangalar orasidagi gorizontal masofa
    private const float CoinSpacing = 0.7f;
    private const int MaxCoins = 4;

    // Quvur ekranning o'ng chetidan shuncha tashqarida paydo bo'ladi
    private const float OffScreenMargin = 0.5f;

    // Yangi o'yin: ekrandagi barcha quvurlarni (ichidagi tangalar bilan) o'chirish.
    // Birinchi quvur o'yin boshlangan kadrda yaratiladi (taymer darhol "to'lgan").
    public void ResetSpawner()
    {
        foreach (PipeScript pipe in FindObjectsByType<PipeScript>())
        {
            Destroy(pipe.gameObject);
        }
        timer = float.MaxValue;
    }

    void Update()
    {
        // Menyu, pauza va game over'da yangi quvur chiqmasin
        if (GameManager.Instance.State != GameState.Playing)
        {
            return;
        }

        // Yaratilish oralig'i levelga bog'liq va GameManager'dan olinadi
        if (timer < GameManager.Instance.CurrentSpawnRate)
        {
            timer += Time.deltaTime; // Vaqtni sanash
        }
        else
        {
            SpawnPipe();
            timer = 0; // Taymerni nollash
        }
    }

    void SpawnPipe()
    {
        // Y o'qi bo'yicha -heightOffset va heightOffset orasida tasodifiy nuqta tanlash
        float randomHeight = Random.Range(-heightOffset, heightOffset);
        Vector3 spawnPos = new Vector3(transform.position.x, randomHeight, 0);

        // Sahnada yangi obyekt yaratish
        GameObject pipePair = Instantiate(pipePrefab, spawnPos, transform.rotation);
        PlaceOffScreen(pipePair);
        SpawnCoins(pipePair);
    }

    // Quvurning chap cheti kameraning o'ng chetidan biroz o'ngda bo'lsin — har qanday aspect'da (CameraFitter)
    // quvur ekranning tashqarisida paydo bo'ladi va bir xil vaqtda ko'rinadi. Spawner'ning X'i hisobga olinmaydi.
    void PlaceOffScreen(GameObject pipePair)
    {
        Camera cam = Camera.main;
        SpriteRenderer[] renderers = pipePair.GetComponentsInChildren<SpriteRenderer>();
        if (cam == null || renderers.Length == 0)
        {
            return;
        }

        float left = float.MaxValue;
        foreach (SpriteRenderer sr in renderers)
        {
            left = Mathf.Min(left, sr.bounds.min.x);
        }

        float screenRight = cam.transform.position.x + cam.orthographicSize * cam.aspect;
        pipePair.transform.position += Vector3.right * (screenRight + OffScreenMargin - left);
    }

    void SpawnCoins(GameObject pipePair)
    {
        if (coinPrefab == null)
        {
            return;
        }

        // 0..N ta tanga, N = 1 + level/2 (ko'pi bilan 4)
        int maxCount = Mathf.Min(1 + GameManager.Instance.Level / 2, MaxCoins);
        int count = Random.Range(0, maxCount + 1);
        if (count == 0)
        {
            return;
        }

        // Bo'shliq chegaralarini quvur collider'laridan topish: pastki quvurning tepasi va yuqori quvurning pasti.
        // Prefabdagi qo'lda kiritilgan koordinatalarga bog'lanmaslik uchun.
        // Yangi yaratilgan collider'larning bounds'i aniq bo'lishi uchun fizikani transformlar bilan sinxronlash.
        Physics2D.SyncTransforms();
        Collider2D bottomPipe = null;
        Collider2D topPipe = null;
        foreach (BoxCollider2D col in pipePair.GetComponentsInChildren<BoxCollider2D>())
        {
            if (col.isTrigger)
            {
                continue; // ScoreSensor
            }
            if (bottomPipe == null || col.bounds.center.y < bottomPipe.bounds.center.y) bottomPipe = col;
            if (topPipe == null || col.bounds.center.y > topPipe.bounds.center.y) topPipe = col;
        }
        if (bottomPipe == null || bottomPipe == topPipe)
        {
            return;
        }

        float gapBottom = bottomPipe.bounds.max.y;
        float gapTop = topPipe.bounds.min.y;
        float centerX = (bottomPipe.bounds.center.x + topPipe.bounds.center.x) * 0.5f;
        float centerY = (gapBottom + gapTop) * 0.5f;

        // Tangalarni quvur o'rtasiga nisbatan simmetrik qator qilib joylash.
        // PipePair'ning bolasi qilib yaratiladi — u bilan birga chapga siljiydi va birga o'chiriladi.
        for (int i = 0; i < count; i++)
        {
            float x = centerX + (i - (count - 1) * 0.5f) * CoinSpacing;
            Instantiate(coinPrefab, new Vector3(x, centerY, 0f), Quaternion.identity, pipePair.transform);
        }
    }
}
