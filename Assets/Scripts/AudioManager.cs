using UnityEngine;

// Ovoz effektlarini chalish uchun yagona (singleton) menejer.
// Restart sahnani qayta yuklaganda ham o'chmaydi; sahnadagi ikkinchi nusxa o'zini yo'q qiladi.
[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] AudioClip jump;
    [SerializeField] AudioClip death;
    [SerializeField] AudioClip score;
    [SerializeField] AudioClip coin;
    [SerializeField] AudioClip levelUp;
    [SerializeField] AudioClip click;

    private AudioSource source;

    void Awake()
    {
        // Restart'dan keyin sahnadagi yangi nusxa — eskisi allaqachon ishlayapti
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // DontDestroyOnLoad faqat ildiz (parent'siz) obyektda ishlaydi
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Play(AudioClip clip)
    {
        // Clip Inspector'da berilmagan bo'lsa, jim o'tib ketamiz
        if (clip == null)
        {
            return;
        }

        // PlayOneShot bir nechta ovozni ustma-ust chalishga imkon beradi (tez-tez sakrash)
        source.PlayOneShot(clip);
    }

    public void PlayJump() => Play(jump);
    public void PlayDeath() => Play(death);
    public void PlayScore() => Play(score);
    public void PlayCoin() => Play(coin);
    public void PlayLevelUp() => Play(levelUp);
    public void PlayClick() => Play(click);
}
