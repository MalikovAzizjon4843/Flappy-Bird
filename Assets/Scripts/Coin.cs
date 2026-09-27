using UnityEngine;

// Tanga: trigger collider, "Coin" tagi. Qush tekkanda BirdScript Collect() ni chaqiradi.
// Prefab: Tools → Create Coin Prefab (Assets/Editor/CoinPrefabCreator.cs).
[RequireComponent(typeof(CircleCollider2D))]
public class Coin : MonoBehaviour
{
    private bool collected = false;

    void Reset()
    {
        // Komponent qo'shilganda collider'ni avtomatik trigger qilish
        GetComponent<CircleCollider2D>().isTrigger = true;
    }

    public void Collect()
    {
        // Bir kadrda ikki marta trigger bo'lsa ham tanga bir marta hisoblanadi
        if (collected)
        {
            return;
        }
        collected = true;

        GameManager.Instance.AddCoin(transform.position); // "+1" shu joydan uchib chiqadi
        if (AudioManager.Instance != null) AudioManager.Instance.PlayCoin();
        Haptics.Vibrate();
        Destroy(gameObject);
    }
}
