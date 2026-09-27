using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Canvas ustida turadi: HUD va panellarni GameState bo'yicha ko'rsatadi/yashiradi, tugmalarni GameManager'ga ulaydi.
// Ierarxiya va havolalar Tools → Build Game UI (Assets/Editor/GameUIBuilder.cs) orqali yaratiladi —
// maydon nomlarini o'zgartirsangiz, builder'dagi nomlarni ham o'zgartiring.
public class UIManager : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] GameObject hud;
    [SerializeField] TextMeshProUGUI scoreText;
    [SerializeField] TextMeshProUGUI levelText;     // star_gold ichidagi raqam
    [SerializeField] TextMeshProUGUI coinText;
    [SerializeField] TextMeshProUGUI levelUpText;   // "Level N" — 1.5 s ko'rinib, so'nadi
    [SerializeField] Button pauseButton;
    [SerializeField] TextMeshProUGUI tapHintText;       // "Ekranga teging" — faqat Ready holatida
    [SerializeField] TextMeshProUGUI coinPopupTemplate; // "+1" nusxasi olinadigan yashirin shablon (HUD ichida)

    [Header("Effektlar")]
    [SerializeField] Image flashImage;                  // urilganda oq chaqnash, butun ekran
    [SerializeField] float flashDuration = 0.15f;
    [SerializeField] float coinPopupDuration = 0.6f;
    [SerializeField] float coinPopupRise = 140f;        // UI birligida (1080x1920 reference)

    [Header("Main menu")]
    [SerializeField] GameObject mainMenuPanel;
    [SerializeField] TextMeshProUGUI menuHighScoreText;
    [SerializeField] Button startButton;
    [SerializeField] Button menuExitButton;
    [SerializeField] Button menuVibrationButton;

    [Header("Game over")]
    [SerializeField] GameObject gameOverPanel;
    [SerializeField] TextMeshProUGUI goScoreText;
    [SerializeField] TextMeshProUGUI goHighScoreText;
    [SerializeField] TextMeshProUGUI goCoinsText;
    [SerializeField] TextMeshProUGUI newRecordText;     // "YANGI REKORD!" — oltin, pulsatsiya
    [SerializeField] Button retryButton;
    [SerializeField] Button goMenuButton;
    [SerializeField] Button goExitButton;

    [Header("Pause")]
    [SerializeField] GameObject pausePanel;
    [SerializeField] Button resumeButton;
    [SerializeField] Button pauseMenuButton;
    [SerializeField] Button pauseExitButton;
    [SerializeField] Button pauseVibrationButton;

    [Header("Level up")]
    [SerializeField] float levelUpDuration = 1.5f;

    private Coroutine levelUpRoutine;
    private Coroutine flashRoutine;

    void Awake()
    {
        // GameManager.Instance tugma bosilgan paytda olinadi — Awake tartibiga bog'liq emas
        startButton.onClick.AddListener(() => GameManager.Instance.StartGame());
        menuExitButton.onClick.AddListener(() => GameManager.Instance.Quit());

        retryButton.onClick.AddListener(() => GameManager.Instance.Restart());
        goMenuButton.onClick.AddListener(() => GameManager.Instance.GoToMenu());
        goExitButton.onClick.AddListener(() => GameManager.Instance.Quit());

        pauseButton.onClick.AddListener(() => GameManager.Instance.Pause());
        resumeButton.onClick.AddListener(() => GameManager.Instance.Resume());
        pauseMenuButton.onClick.AddListener(() => GameManager.Instance.GoToMenu());
        pauseExitButton.onClick.AddListener(() => GameManager.Instance.Quit());

        menuVibrationButton.onClick.AddListener(ToggleVibration);
        pauseVibrationButton.onClick.AddListener(ToggleVibration);
        RefreshVibrationLabels();

        // Har qanday tugma bosilganda click.wav
        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            button.onClick.AddListener(PlayClick);
        }

        levelUpText.gameObject.SetActive(false);
        coinPopupTemplate.gameObject.SetActive(false);
        flashImage.gameObject.SetActive(false);
        newRecordText.gameObject.SetActive(false);
    }

    void Update()
    {
        // Pulsatsiyalar unscaled vaqt bilan — GameOver'da Time.timeScale = 0 bo'lsa ham ishlaydi
        float time = Time.unscaledTime;
        if (tapHintText.isActiveAndEnabled)
        {
            tapHintText.alpha = 0.55f + 0.45f * Mathf.Sin(time * 4f);
        }
        if (newRecordText.isActiveAndEnabled)
        {
            float pulse = 1f + 0.12f * Mathf.Sin(time * 6f);
            newRecordText.rectTransform.localScale = new Vector3(pulse, pulse, 1f);
        }
    }

    static void PlayClick()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
    }

    // "TEBRANISH: YONIQ / O'CHIQ" — sozlama PlayerPrefs'da (Haptics), ikkala paneldagi tugma bir xil ko'rinadi
    void ToggleVibration()
    {
        Haptics.Enabled = !Haptics.Enabled;
        RefreshVibrationLabels();
        Haptics.Vibrate(); // yoqilgan bo'lsa, tasdiq sifatida bir marta tebranadi
    }

    void RefreshVibrationLabels()
    {
        string label = Haptics.Enabled ? "TEBRANISH: YONIQ" : "TEBRANISH: O'CHIQ";
        menuVibrationButton.GetComponentInChildren<TextMeshProUGUI>(true).text = label;
        pauseVibrationButton.GetComponentInChildren<TextMeshProUGUI>(true).text = label;
    }

    public void OnStateChanged(GameState state)
    {
        // HUD o'yin davomida va urilgandan keyingi 0.5 s da (Dying) ko'rinadi; panel chiqqach yashiriladi
        hud.SetActive(state == GameState.Ready || state == GameState.Playing ||
                      state == GameState.Paused || state == GameState.Dying);
        tapHintText.gameObject.SetActive(state == GameState.Ready);
        mainMenuPanel.SetActive(state == GameState.Menu);
        gameOverPanel.SetActive(state == GameState.GameOver);
        pausePanel.SetActive(state == GameState.Paused);
        pauseButton.interactable = state == GameState.Ready || state == GameState.Playing;

        // "Level N" yozuvi menyu yoki game over ustida qotib qolmasin (pauzada esa vaqt bilan birga to'xtaydi)
        if (state == GameState.Menu || state == GameState.GameOver)
        {
            HideLevelUp();
        }
    }

    public void SetScore(int score) => scoreText.text = score.ToString();

    public void SetLevel(int level) => levelText.text = level.ToString();

    public void SetCoins(int coins) => coinText.text = coins.ToString();

    public void SetHighScore(int highScore) => menuHighScoreText.text = $"REKORD: {highScore}";

    public void ShowGameOver(int score, int highScore, int coins, bool newRecord)
    {
        goScoreText.text = $"Ochko: {score}";
        goHighScoreText.text = $"Rekord: {highScore}";
        goCoinsText.text = $"Tangalar: +{coins}";
        newRecordText.gameObject.SetActive(newRecord);
    }

    // Urilganda butun ekran bir lahza oq bo'lib, 0.15 s da so'nadi
    public void Flash()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }
        flashRoutine = StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        flashImage.gameObject.SetActive(true);
        Color color = Color.white;
        for (float t = 0f; t < flashDuration; t += Time.unscaledDeltaTime)
        {
            color.a = 0.85f * (1f - t / flashDuration);
            flashImage.color = color;
            yield return null;
        }
        flashImage.gameObject.SetActive(false);
        flashRoutine = null;
    }

    // Tanga olingan joyda "+1" paydo bo'lib, yuqoriga uchib so'nadi
    public void ShowCoinPopup(Vector3 worldPosition)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return;
        }

        RectTransform parent = (RectTransform)coinPopupTemplate.transform.parent;
        Vector2 screenPoint = cam.WorldToScreenPoint(worldPosition);
        // Canvas Screen Space Overlay — kamera parametri null
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, null, out Vector2 localPoint);

        TextMeshProUGUI popup = Instantiate(coinPopupTemplate, parent);
        popup.gameObject.SetActive(true);
        StartCoroutine(CoinPopupRoutine(popup, localPoint));
    }

    IEnumerator CoinPopupRoutine(TextMeshProUGUI popup, Vector2 start)
    {
        // Shablon anchor/pivot'i markazda — localPoint to'g'ridan-to'g'ri anchoredPosition bo'ladi
        RectTransform rect = popup.rectTransform;
        for (float t = 0f; t < coinPopupDuration; t += Time.unscaledDeltaTime)
        {
            float k = t / coinPopupDuration;
            rect.anchoredPosition = start + Vector2.up * (coinPopupRise * (1f - (1f - k) * (1f - k))); // ease-out
            popup.alpha = 1f - k * k;
            yield return null;
        }
        Destroy(popup.gameObject);
    }

    public void ShowLevelUp(int level)
    {
        HideLevelUp();
        levelUpRoutine = StartCoroutine(LevelUpRoutine(level));
    }

    void HideLevelUp()
    {
        if (levelUpRoutine != null)
        {
            StopCoroutine(levelUpRoutine);
            levelUpRoutine = null;
        }
        levelUpText.gameObject.SetActive(false);
    }

    IEnumerator LevelUpRoutine(int level)
    {
        const float fadeIn = 0.2f;
        const float fadeOut = 0.5f;

        levelUpText.text = $"Level {level}";
        levelUpText.gameObject.SetActive(true);

        for (float t = 0f; t < levelUpDuration; t += Time.deltaTime)
        {
            // Tez paydo bo'lish, ushlab turish, oxirida so'nish
            float alphaIn = Mathf.Clamp01(t / fadeIn);
            float alphaOut = Mathf.Clamp01((levelUpDuration - t) / fadeOut);
            levelUpText.alpha = Mathf.Min(alphaIn, alphaOut);
            yield return null;
        }

        levelUpText.gameObject.SetActive(false);
        levelUpRoutine = null;
    }
}
