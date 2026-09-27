using UnityEngine;

// Portrait ekranlarda (9:16 dan 9:21 gacha) o'yin maydonining kengligi bir xil ko'rinishi uchun
// orthographicSize'ni aspect bo'yicha hisoblaydi: kenglik qat'iy, uzunroq telefonda tepa/past ko'proq ko'rinadi.
// Kengroq ekranda (9:16 dan keng, masalan Editor'ning landscape Game oynasi) balandlik qat'iy qoladi —
// aks holda kamera haddan tashqari yaqinlashib ketardi.
// Awake'da ishlaydi — ParallaxBackground va SpawnerScript Start/Update'da yangi o'lchamni ko'radi.
[RequireComponent(typeof(Camera))]
public class CameraFitter : MonoBehaviour
{
    // 9:16 da orthographicSize 5 ga teng kenglik (10 × 9/16) — loyihaning asl ko'rinishi
    [SerializeField] float targetWidth = 5.625f;
    [SerializeField] float minOrthographicSize = 5f;

    private Camera cam;
    private float lastAspect;

    void Awake()
    {
        cam = GetComponent<Camera>();
        Fit();
    }

    void Update()
    {
        // Device Simulator'da qurilma almashtirilganda yoki ekran o'lchami o'zgarganda
        if (!Mathf.Approximately(cam.aspect, lastAspect))
        {
            Fit();
        }
    }

    void Fit()
    {
        lastAspect = cam.aspect;
        cam.orthographicSize = Mathf.Max(targetWidth / cam.aspect * 0.5f, minOrthographicSize);
    }
}
