using UnityEngine;

/// <summary>
/// 효과음(SFX) 재생 전담 싱글톤 클래스 (코드 리프레시용 주석 수정)
/// </summary>
public class SoundManager : MonoBehaviour
{
    private static SoundManager instance;
    public static SoundManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<SoundManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("SoundManager");
                    instance = go.AddComponent<SoundManager>();
                }
            }
            return instance;
        }
    }

    [Header("효과음 볼륨")]
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 0.8f;       // 인스펙터에서 전체 효과음 볼륨 조절

    private AudioSource sfxSource;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        // BGM용 AudioSource가 이미 존재할 수 있으므로, SFX 전용 AudioSource를 별도로 생성
        // 기존 AudioSource(BGM)는 건드리지 않는다
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
    }

    /// <summary>
    /// 효과음을 1회 재생합니다.
    /// </summary>
    /// <param name="clip">재생할 오디오 클립</param>
    /// <param name="volume">볼륨 배율 (0.0 ~ 1.0), sfxVolume과 곱해진다</param>
    public void PlaySFX(AudioClip clip, float volume = 1.0f)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, volume * sfxVolume);
    }
}
