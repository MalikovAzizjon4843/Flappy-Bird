using UnityEngine;

public class PipeScript : MonoBehaviour
{
    void Update()
    {
        // Urilgandan keyin (Dying) vaqt yurib turadi, lekin dunyo to'xtaydi — faqat qush aylanib tushadi
        if (GameManager.Instance.State != GameState.Playing)
        {
            return;
        }

        // Har bir kadrda quvurni chap tomonga surish. Tezlik levelga bog'liq va GameManager'dan olinadi —
        // level oshganda ekrandagi barcha quvurlar birdek tezlashadi, oralaridagi masofa buzilmaydi.
        // Quvur ichidagi tangalar bola obyekt bo'lgani uchun u bilan birga siljiydi.
        transform.position += Vector3.left * GameManager.Instance.CurrentSpeed * Time.deltaTime;

        // Quvur ekrandan chiqib ketgach, xotirani to'ldirmasligi uchun uni o'chirib tashlash
        if (transform.position.x < -15f)
        {
            Destroy(gameObject);
        }
    }
}
