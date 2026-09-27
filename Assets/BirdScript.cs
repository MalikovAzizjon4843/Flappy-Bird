using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class BirdScript : MonoBehaviour
{
    // Editor'dagi Tag nomlari bilan aynan bir xil bo'lishi kerak
    private const string PipeTag = "pipe";
    private const string CoinTag = "Coin";

    // Asl qiymatlar GameManager Inspector'ida: runtime'da GameManager.Start() → ConfigurePhysics() ularni qo'yadi,
    // Editor'da Tools → Apply Bird Physics bu yerga yozadi. Bu yerda o'zgartirish runtime'da ustidan yoziladi.
    [Header("Fizika (GameManager'dan)")]
    [Tooltip("GameManager → Bird Jump Force dan olinadi")]
    [SerializeField] float jumpForce = 7f;
    [Tooltip("GameManager → Bird Max Fall Speed dan olinadi")]
    [SerializeField] float maxFallSpeed = 8f;

    public float JumpForce => jumpForce;
    public float MaxFallSpeed => maxFallSpeed;

    [Header("Egilish")]
    [SerializeField] float maxUpAngle = 25f;       // sakraganda tumshug'i tepaga
    [SerializeField] float maxDownAngle = -60f;    // qulaganda pastga
    [SerializeField] float upRotateSpeed = 720f;   // gradus/s — tepaga tez
    [SerializeField] float downRotateSpeed = 300f; // gradus/s — pastga silliq

    [Header("Sakrash effekti")]
    // 2-3 kadr berilsa — qanot qoqish animatsiyasi; bo'sh bo'lsa — Y-scale "squash"
    [SerializeField] Sprite[] flapFrames;
    [SerializeField] float flapFrameTime = 0.05f;
    [SerializeField] float squashDuration = 0.1f;

    [Header("Ready holati")]
    [SerializeField] float hoverAmplitude = 0.15f;
    [SerializeField] float hoverSpeed = 4f;

    [Header("O'lim")]
    [SerializeField] float deathSpin = -540f;      // gradus/s, soat mili bo'yicha aylanib tushadi
    [SerializeField] float deathHop = 3f;          // urilgandan keyin biroz sakrab, keyin qulaydi

    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer sr;

    // O'lim ovozi bir marta chalinishi va o'likdan keyin sakrash bo'lmasligi uchun
    private bool isDead = false;

    // Restart'da qush shu joyga qaytadi (sahna qayta yuklanmaydi)
    private Vector3 startPosition;
    private Vector3 baseScale;

    private int lastFlapFrame = -1;
    private float squashTimer = 0f;

    // UI raycast uchun qayta ishlatiladigan ro'yxat (har tapda yangi allocation bo'lmasin)
    private static readonly List<RaycastResult> uiHits = new List<RaycastResult>();
    private float flapAnimTime = -1f; // < 0 — animatsiya o'ynamayapti

    private bool HasFlapFrames => flapFrames != null && flapFrames.Length >= 2;

    void Awake()
    {
        // Awake'da — GameManager.Start() dagi ResetBird() dan oldin tayyor bo'lishi uchun
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
        startPosition = transform.position;
        baseScale = transform.localScale;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        UpdateFlapEffect();

        // Tugma bosilgan kadrdagi o'sha bosish (masalan BOSHLASH) qushga tap bo'lib o'tmasin
        bool tapped = WasTappedThisFrame() && Time.frameCount != gm.StateChangedFrame;

        if (gm.State == GameState.Ready)
        {
            // "Ekranga teging": qush joyida yengil tebranib turadi, birinchi tap o'yinni boshlaydi
            float y = Mathf.Sin(Time.time * hoverSpeed) * hoverAmplitude;
            transform.position = startPosition + Vector3.up * y;
            if (tapped)
            {
                gm.BeginPlaying();
            }
            return;
        }

        // Time.timeScale = 0 bo'lsa ham Update ishlayveradi — faqat o'yin paytida kiritishni qabul qilamiz
        if (isDead || gm.State != GameState.Playing)
        {
            return;
        }

        if (tapped)
        {
            Flap();
        }

        if (transform.position.y > 10f || transform.position.y < -10f)
        {
            Die(false);
        }
    }

    void FixedUpdate()
    {
        if (isDead || !rb.simulated)
        {
            return;
        }

        // Vertikal tezlik chegarasi: yuqoriga jumpForce'dan (sakrash balandligi doim bir xil),
        // pastga maxFallSpeed'dan oshmaydi (original Flappy Bird kabi — qulash boshqarib bo'ladigan tezlikda)
        Vector2 velocity = rb.linearVelocity;
        float clampedY = Mathf.Clamp(velocity.y, -maxFallSpeed, jumpForce);
        if (clampedY != velocity.y)
        {
            velocity.y = clampedY;
            rb.linearVelocity = velocity;
        }

        // Tezlikka qarab egilish: tepaga ketayotganda +25°, qulash maxFallSpeed'ga yetganda -60°
        float vy = velocity.y;
        float target = vy > 0f
            ? maxUpAngle
            : Mathf.Lerp(maxUpAngle, maxDownAngle, Mathf.Clamp01(-vy / maxFallSpeed));
        float speed = vy > 0f ? upRotateSpeed : downRotateSpeed;
        rb.rotation = Mathf.MoveTowardsAngle(rb.rotation, target, speed * Time.fixedDeltaTime);
    }

    // Ready → Playing: fizika yoqiladi va birinchi sakrash
    public void StartFlying()
    {
        // Tebranish to'xtagan joydan davom etadi (startPosition'ga sakramaydi)
        rb.position = transform.position;
        rb.simulated = true;
        Flap();
    }

    // GameManager.Start() chaqiradi: fizika qiymatlari bitta joyda (GameManager Inspector'i)
    public void ConfigurePhysics(float newJumpForce, float gravityScale, float newMaxFallSpeed)
    {
        jumpForce = newJumpForce;
        maxFallSpeed = newMaxFallSpeed;
        rb.gravityScale = gravityScale;
        // 60/120 Hz ekranlarda 50 Hz fizika (Fixed Timestep 0.02) tekis ko'rinishi uchun
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    public void Flap()
    {
        // Bir kadrda faqat bitta sakrash (masalan Ready→Playing o'tishi + o'sha tap)
        if (lastFlapFrame == Time.frameCount)
        {
            return;
        }
        lastFlapFrame = Time.frameCount;

        rb.linearVelocity = Vector2.up * jumpForce;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayJump();

        if (HasFlapFrames)
        {
            flapAnimTime = 0f;
        }
        else
        {
            squashTimer = squashDuration;
        }
    }

    // Qanot kadrlari yoki squash — ikkalasi ham oddiy vaqt bilan (pauzada to'xtaydi)
    void UpdateFlapEffect()
    {
        if (HasFlapFrames && flapAnimTime >= 0f)
        {
            flapAnimTime += Time.deltaTime;
            int frame = (int)(flapAnimTime / flapFrameTime);
            if (frame >= flapFrames.Length)
            {
                sr.sprite = flapFrames[0];
                flapAnimTime = -1f;
            }
            else
            {
                sr.sprite = flapFrames[frame];
            }
        }

        if (squashTimer > 0f)
        {
            squashTimer = Mathf.Max(squashTimer - Time.deltaTime, 0f);
            // 0 → 1 → 0: faqat Y qisqaradi. X o'zgarmaydi — CircleCollider2D radiusi max(scale.x, scale.y) ga
            // bog'liq, shuning uchun hitbox effekt davomida o'zgarmaydi
            float t = Mathf.Sin((1f - squashTimer / squashDuration) * Mathf.PI);
            transform.localScale = new Vector3(baseScale.x, baseScale.y * (1f - 0.2f * t), baseScale.z);
        }
    }

    // Yangi o'yin: boshlang'ich joy, tezlik va aylanish nolga; fizika Ready'da o'chiq (qush tebranib turadi)
    public void ResetBird()
    {
        isDead = false;
        squashTimer = 0f;
        flapAnimTime = -1f;
        transform.localScale = baseScale;
        if (HasFlapFrames) sr.sprite = flapFrames[0];

        transform.SetPositionAndRotation(startPosition, Quaternion.identity);
        rb.simulated = false;
        rb.position = startPosition;
        rb.rotation = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.freezeRotation = true; // tirikligida burilishni FixedUpdate boshqaradi, to'qnashuv emas
        col.enabled = true;
    }

    // Yangi Input System: shu kadrda UI'dan tashqarida bosish bo'ldimi? Natija bitta bool —
    // bir kadrda nechta qurilma yoki barmoq bosgan bo'lsa ham ko'pi bilan bitta Flap bo'ladi.
    static bool WasTappedThisFrame()
    {
        // Barcha barmoqlar tekshiriladi (faqat primaryTouch emas): ikki bosh barmoq bilan navbatma-navbat
        // bosilganda birinchisi hali ekranda tursa ham ikkinchisi sakratadi
        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen != null)
        {
            foreach (var touch in touchscreen.touches)
            {
                if (touch.press.wasPressedThisFrame && !IsOverUI(touch.position.ReadValue()))
                {
                    return true;
                }
            }
        }

        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame && !IsOverUI(mouse.position.ReadValue()))
        {
            return true;
        }

        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
    }

    // Bosilgan nuqtada UI (tugma, panel) bormi — o'zimiz raycast qilamiz. EventSystem.IsPointerOverGameObject()
    // UI modulining oldingi kadrdagi holatiga qaraydi va yangi joyga qilingan tapda xato javob berishi mumkin.
    static bool IsOverUI(Vector2 screenPosition)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return false;
        }
        PointerEventData data = new PointerEventData(eventSystem) { position = screenPosition };
        uiHits.Clear();
        eventSystem.RaycastAll(data, uiHits);
        return uiHits.Count > 0;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Faqat quvurga urilish o'ldiradi. Tag collider turgan obyektda tekshiriladi —
        // PipePair ichidagi "pipe" va "pipe (1)" ikkalasida ham shu tag bo'lishi kerak (katta-kichik harf farq qiladi)
        if (collision.gameObject.CompareTag(PipeTag))
        {
            Die(true);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDead)
        {
            return;
        }

        // Tanga
        if (collision.CompareTag(CoinTag))
        {
            Coin coin = collision.GetComponent<Coin>();
            if (coin != null) coin.Collect();
            return;
        }

        // Qolgan yagona trigger — PipePair ichidagi ScoreSensor (quvurdan o'tildi)
        GameManager.Instance.AddScore();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayScore();
    }

    // hit = quvurga urildi; false = ekrandan chiqib ketdi
    void Die(bool hit)
    {
        // Chegaradan chiqish har kadrda, collision esa bir necha marta chaqirishi mumkin
        if (isDead)
        {
            return;
        }
        isDead = true;

        if (AudioManager.Instance != null) AudioManager.Instance.PlayDeath();

        if (hit)
        {
            Haptics.Vibrate();
            // Aylanib tushish: collider o'chadi (quvurda ilinib qolmaydi), kichik sakrash va aylanish
            col.enabled = false;
            rb.freezeRotation = false;
            rb.linearVelocity = new Vector2(0f, deathHop);
            rb.angularVelocity = deathSpin;
        }

        // Rekord, flash, kamera silkinishi va kechiktirilgan game over paneli
        GameManager.Instance.GameOver(hit);
    }
}
