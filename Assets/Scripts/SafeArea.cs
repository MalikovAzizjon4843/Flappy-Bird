using UnityEngine;

// RectTransform'ni Screen.safeArea ga moslaydi (notch, kamera teshigi, dumaloq burchaklar, navigatsiya paneli).
// Ota obyekt butun ekranni egallashi kerak (Canvas yoki to'liq cho'zilgan panel).
// GameUIBuilder uni HUD ildiziga va har panelning "SafeArea" konteyneriga o'zi qo'yadi.
[RequireComponent(typeof(RectTransform))]
public class SafeArea : MonoBehaviour
{
    private RectTransform rect;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;

    void Awake()
    {
        rect = (RectTransform)transform;
        Apply();
    }

    void Update()
    {
        // Ekran burilishi yoki Device Simulator'da qurilma almashishi — faqat o'zgarganda qayta hisoblash
        if (Screen.safeArea != lastSafeArea || Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y)
        {
            Apply();
        }
    }

    void Apply()
    {
        lastSafeArea = Screen.safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);

        if (Screen.width <= 0 || Screen.height <= 0)
        {
            return;
        }

        // Piksel koordinatalarini 0..1 anchor'larga aylantirish; offset'lar nolga — to'liq safe area'ni egallaydi
        Vector2 min = lastSafeArea.position;
        Vector2 max = lastSafeArea.position + lastSafeArea.size;
        min.x /= Screen.width;
        min.y /= Screen.height;
        max.x /= Screen.width;
        max.y /= Screen.height;

        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
