using UnityEngine;

// Tebranish (vibratsiya). Sozlama PlayerPrefs["Vibration"] da saqlanadi, standart — yoqiq.
// Faqat Android build'da ishlaydi; Editor va Windows'da hech narsa qilmaydi.
// Handheld.Vibrate() ga murojaat bo'lgani uchun Unity Android manifestiga VIBRATE ruxsatini o'zi qo'shadi.
public static class Haptics
{
    private const string VibrationKey = "Vibration";

    public static bool Enabled
    {
        get => PlayerPrefs.GetInt(VibrationKey, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(VibrationKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static void Vibrate()
    {
        if (!Enabled)
        {
            return;
        }
#if UNITY_ANDROID && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
    }
}
