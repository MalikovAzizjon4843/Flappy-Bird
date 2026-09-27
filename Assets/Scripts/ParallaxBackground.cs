using UnityEngine;

// Fonni cheksiz chapga aylantiradi: o'zining nusxasini o'ng tomonga qo'yib, ikkalasini navbatma-navbat suradi.
// Game over bo'lganda GameManager Time.timeScale = 0 qiladi — Time.deltaTime ham 0 bo'lib, fon o'zi to'xtaydi.
[RequireComponent(typeof(SpriteRenderer))]
public class ParallaxBackground : MonoBehaviour
{
    // Quvurlar 3 tezlikda yuradi; chuqurlik effekti uchun fon sekinroq bo'lishi kerak
    [SerializeField] float speed = 0.5f;

    // Fon kamera balandligidan past bo'lsa, uni balandlik bo'yicha kattalashtirish
    [SerializeField] bool fitCameraHeight = true;

    private float width;
    private SpriteRenderer[] parts;
    private Camera cam;

    void Start()
    {
        cam = Camera.main;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        // Uzun telefonlarda (9:21) CameraFitter ko'proq balandlik ko'rsatadi — fon tepa/pastda bo'sh joy qoldirmasin.
        // Kerak bo'lsa proporsional kattalashtiriladi (hech qachon kichraytirilmaydi); nusxa ham shu scale'ni oladi.
        if (fitCameraHeight)
        {
            float cameraHeight = cam.orthographicSize * 2f;
            float spriteHeight = sr.bounds.size.y;
            if (spriteHeight > 0f && spriteHeight < cameraHeight)
            {
                transform.localScale *= cameraHeight / spriteHeight;
            }
        }

        // Sprite'ning dunyodagi kengligi (scale ham hisobga olingan)
        width = sr.bounds.size.x;

        // Nusxani o'ng tomonga yonma-yon qo'yish
        Vector3 rightPos = transform.position + Vector3.right * width;
        GameObject copy = Instantiate(gameObject, rightPos, transform.rotation, transform.parent);
        copy.name = gameObject.name + " (Copy)";

        // Nusxa o'z nusxasini yaratmasligi uchun undagi skriptni o'chirish (o'chirilgan komponentda Start chaqirilmaydi)
        ParallaxBackground copyScript = copy.GetComponent<ParallaxBackground>();
        copyScript.enabled = false;
        Destroy(copyScript);

        parts = new SpriteRenderer[] { GetComponent<SpriteRenderer>(), copy.GetComponent<SpriteRenderer>() };

        // Ikki bo'lak ekranni to'liq yopishi uchun sprite kamida ekran kengligida bo'lishi kerak
        float screenWidth = cam.orthographicSize * 2f * cam.aspect;
        if (width < screenWidth)
        {
            Debug.LogWarning($"ParallaxBackground: sprite kengligi ({width:F2}) ekran kengligidan ({screenWidth:F2}) kichik — bo'shliq ko'rinadi. Scale'ni kattalashtiring.");
        }
    }

    void Update()
    {
        // Fon Ready ("Ekranga teging") va Playing'da yuradi; Dying'da dunyo bilan birga to'xtaydi,
        // menyu/pauza/game over'da esa Time.timeScale = 0 — Time.deltaTime ham 0
        GameManager gm = GameManager.Instance;
        if (gm != null && gm.State != GameState.Ready && gm.State != GameState.Playing)
        {
            return;
        }

        // Level oshganda quvurlar bilan proporsional tezlashadi
        float multiplier = gm != null ? gm.SpeedMultiplier : 1f;
        float step = speed * multiplier * Time.deltaTime;
        if (step == 0f)
        {
            return;
        }

        foreach (SpriteRenderer part in parts)
        {
            part.transform.position += Vector3.left * step;
        }

        // Ekranning chap chegarasi (dunyo koordinatasida)
        float screenLeft = cam.transform.position.x - cam.orthographicSize * cam.aspect;

        for (int i = 0; i < parts.Length; i++)
        {
            Transform part = parts[i].transform;
            Transform other = parts[(i + 1) % parts.Length].transform;

            // Bo'lakning o'ng cheti ham ekrandan chiqib ketdi — uni ikkinchisining o'ng tomoniga ko'chirish.
            // Pozitsiya "other" ga nisbatan olinadi, shunda float xatolari yig'ilib chok (tirqish) paydo bo'lmaydi.
            if (parts[i].bounds.max.x < screenLeft)
            {
                part.position = new Vector3(other.position.x + width, part.position.y, part.position.z);
            }
        }
    }
}
