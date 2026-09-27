using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// O'yin holati (state, ochko, level, tangalar, rekord) shu yerda. UI'ni o'zi chizmaydi —
// har o'zgarishni UIManager'ga xabar qiladi. Restart sahnani qayta yuklamaydi, obyektlarni reset qiladi.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // PlayerPrefs kalitlari — o'zgartirilsa saqlangan qiymatlar yo'qoladi
    private const string HighScoreKey = "HighScore";
    private const string CoinsKey = "Coins";

    [Header("Havolalar (bo'sh bo'lsa sahnadan topiladi)")]
    [SerializeField] UIManager ui;
    [SerializeField] BirdScript bird;
    [SerializeField] SpawnerScript spawner;

    [Header("Level")]
    [SerializeField] int pipesPerLevel = 5;
    [SerializeField] float baseSpeed = 3f;       // quvurlar tezligi, level 1
    [SerializeField] float baseSpawnRate = 2f;   // quvurlar orasidagi vaqt (s), level 1

    private int score = 0;
    private int runCoins = 0;     // shu o'yinda yig'ilgan
    private int totalCoins = 0;   // PlayerPrefs'dagi jami
    private int highScore = 0;

    [Header("Qush fizikasi")]
    // Original Flappy Bird'ga yaqin: sakrash balandligi = jumpForce² / (2 · 9.81 · gravityScale) ≈ 1 birlik
    // (bo'shliqning ~⅓ qismi). Sahnadagi Rigidbody2D'ga Editor'da ham qo'llash: Tools → Apply Bird Physics
    [SerializeField] float birdGravityScale = 2.5f;
    [SerializeField] float birdJumpForce = 7f;
    // Qulash tezligi chegarasi; qush shu tezlikka yetganda -60° ga egiladi
    [SerializeField] float birdMaxFallSpeed = 8f;

    public float BirdGravityScale => birdGravityScale;
    public float BirdJumpForce => birdJumpForce;
    public float BirdMaxFallSpeed => birdMaxFallSpeed;

    [Header("Game feel")]
    [SerializeField] float gameOverPanelDelay = 0.5f;  // urilgandan keyin panel chiqquncha (qush aylanib tushadi)
    [SerializeField] float shakeDuration = 0.2f;
    [SerializeField] float shakeMagnitude = 0.15f;      // dunyo birligida

    private GameState stateBeforePause = GameState.Playing;
    private Coroutine gameOverRoutine;

    public GameState State { get; private set; } = GameState.Menu;
    public bool IsGameOver => State == GameState.GameOver;
    public int Level { get; private set; } = 1;

    // Holat o'zgargan kadr — tugma bosilgan kadrdagi o'sha bosish qushga "tap" bo'lib o'tmasin
    public int StateChangedFrame { get; private set; }

    // Har level +12%, ko'pi bilan 2 barobar
    public float CurrentSpeed => baseSpeed * Mathf.Min(1f + 0.12f * (Level - 1), 2f);

    // Har level +8% tezroq spawn, lekin 1.1 s dan tez emas
    public float CurrentSpawnRate => Mathf.Max(baseSpawnRate / (1f + 0.08f * (Level - 1)), 1.1f);

    // Fon kabi boshqa harakatlanuvchi obyektlar uchun: 1 = level 1 tezligi
    public float SpeedMultiplier => CurrentSpeed / baseSpeed;

    void Awake()
    {
        Instance = this;

        if (ui == null) ui = FindAnyObjectByType<UIManager>();
        if (bird == null) bird = FindAnyObjectByType<BirdScript>();
        if (spawner == null) spawner = FindAnyObjectByType<SpawnerScript>();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Start()
    {
        totalCoins = PlayerPrefs.GetInt(CoinsKey, 0);
        highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        ui.SetCoins(totalCoins);
        ui.SetHighScore(highScore);

        bird.ConfigurePhysics(birdJumpForce, birdGravityScale, birdMaxFallSpeed);
        ResetRun();
        SetState(GameState.Menu);
    }

    void Update()
    {
        // Android "Orqaga" tugmasi Input System'da Keyboard.escapeKey sifatida keladi; kompyuterda — Esc
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            if (State == GameState.Playing || State == GameState.Ready) Pause();
            else if (State == GameState.Paused) Resume();
        }
    }

    void SetState(GameState newState)
    {
        State = newState;
        StateChangedFrame = Time.frameCount;

        // Vaqt faqat Ready (qush tebranadi), Playing va Dying (qush aylanib tushadi) holatlarida yuradi
        bool timeRuns = newState == GameState.Ready || newState == GameState.Playing || newState == GameState.Dying;
        Time.timeScale = timeRuns ? 1f : 0f;
        ui.OnStateChanged(newState);
    }

    // Yangi o'yin uchun hamma narsani boshlang'ich holatga qaytarish (sahnani qayta yuklamasdan)
    void ResetRun()
    {
        if (gameOverRoutine != null)
        {
            StopCoroutine(gameOverRoutine);
            gameOverRoutine = null;
        }

        score = 0;
        runCoins = 0;
        Level = 1;

        spawner.ResetSpawner(); // ekrandagi quvurlar (ichidagi tangalar bilan) o'chiriladi
        bird.ResetBird();

        ui.SetScore(0);
        ui.SetLevel(1);
    }

    // --- UI tugmalari chaqiradigan metodlar ---

    // BOSHLASH / QAYTA O'YNASH → "Ekranga teging" holati
    public void StartGame()
    {
        ResetRun();
        SetState(GameState.Ready);
    }

    public void Restart() => StartGame();

    // Ready holatida birinchi tap (BirdScript chaqiradi) → o'yin boshlanadi
    public void BeginPlaying()
    {
        if (State != GameState.Ready)
        {
            return;
        }
        SetState(GameState.Playing);
        bird.StartFlying();
    }

    public void Pause()
    {
        if (State == GameState.Playing || State == GameState.Ready)
        {
            stateBeforePause = State;
            SetState(GameState.Paused);
        }
    }

    public void Resume()
    {
        if (State == GameState.Paused)
        {
            SetState(stateBeforePause);
        }
    }

    public void GoToMenu()
    {
        ResetRun();
        SetState(GameState.Menu);
    }

    public void Quit()
    {
        PlayerPrefs.Save();
#if UNITY_EDITOR
        // Editor'da Application.Quit() hech narsa qilmaydi — Play Mode'dan chiqamiz
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // --- O'yin hodisalari ---

    public void AddScore()
    {
        if (State != GameState.Playing)
        {
            return;
        }

        score++;
        ui.SetScore(score);

        // Har pipesPerLevel ta quvurdan o'tganda keyingi level
        if (score % pipesPerLevel == 0)
        {
            Level++;
            ui.SetLevel(Level);
            ui.ShowLevelUp(Level);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayLevelUp();
        }
    }

    // worldPosition — "+1" yozuvi shu joydan uchib chiqadi
    public void AddCoin(Vector3 worldPosition)
    {
        if (State != GameState.Playing)
        {
            return;
        }

        runCoins++;
        totalCoins++;
        ui.SetCoins(totalCoins);
        ui.ShowCoinPopup(worldPosition);
        // Diskka yozish GameOver/pauza paytida — har tangada PlayerPrefs.Save() qimmat
        PlayerPrefs.SetInt(CoinsKey, totalCoins);
    }

    // hit = quvurga urildi (flash + kamera silkinishi); false = ekrandan chiqib ketdi
    public void GameOver(bool hit)
    {
        if (State != GameState.Playing)
        {
            return;
        }

        // Yangi rekord bo'lsa saqlash
        bool newRecord = score > highScore;
        if (newRecord)
        {
            highScore = score;
            PlayerPrefs.SetInt(HighScoreKey, highScore);
            ui.SetHighScore(highScore);
        }
        PlayerPrefs.Save(); // rekord va tangalarni darhol diskka yozish

        // Dying: dunyo to'xtaydi, lekin vaqt yuradi — qush aylanib tushadi, panel biroz kechikib chiqadi
        SetState(GameState.Dying);
        if (hit)
        {
            ui.Flash();
            CameraShake.Shake(shakeDuration, shakeMagnitude);
        }
        gameOverRoutine = StartCoroutine(ShowGameOverAfterDelay(newRecord));
    }

    IEnumerator ShowGameOverAfterDelay(bool newRecord)
    {
        yield return new WaitForSecondsRealtime(gameOverPanelDelay);
        gameOverRoutine = null;

        ui.ShowGameOver(score, highScore, runCoins, newRecord);
        SetState(GameState.GameOver);
    }

    void OnApplicationPause(bool paused)
    {
        // Mobil qurilmada ilova fonga o'tganda: saqlash va o'yinni pauzaga qo'yish
        if (paused)
        {
            PlayerPrefs.Save();
            Pause();
        }
    }
}
