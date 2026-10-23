using System;
using Unity.Services.LevelPlay;
using UnityEngine;

// โฆษณาทั้ง 3 แบบ (LevelPlay) อยู่ที่นี่ที่เดียว
// ไม่ต้องวางในฉาก สร้างตัวเองตอนเกมเริ่ม และอยู่ข้ามฉาก (DontDestroyOnLoad)
// ใส่ App Key / Ad Unit ID จาก LevelPlay dashboard ด้านล่าง (ใน Editor ใช้ค่าอะไรก็ได้ จะขึ้น mock ads)
//
// การจัดวาง:
//   Banner       -> ล่างจอ โชว์ในเมนู/หน้าสรุปผล ซ่อนตอนกำลังเล่น (dot เกิดทั่วจอ ไม่ให้บัง)
//   Interstitial -> ตอนเปลี่ยนด่าน / กลับเมนู เท่านั้น และจำกัดความถี่
//   Rewarded     -> ผู้เล่นกดเลือกเองตอนแพ้ (ดูจบ = ได้กระสุน +1 เล่นต่อ)
public class AdsManager : MonoBehaviour
{
    const string AppKey = "28f254d6d";
    const string BannerAdUnitId = "wq5lymvw4h0cmikl";
    const string InterstitialAdUnitId = "kewurud5246iyx16";
    const string RewardedAdUnitId = "vfjr1y3iqm75s9mt";

    const int InterstitialEveryN = 2;               // โชว์ทุกๆ กี่ครั้งที่มีจังหวะเปลี่ยนด่าน
    const float MinSecondsBetweenInterstitials = 30f;
    const float RetryLoadDelay = 30f;               // โหลดไม่สำเร็จ รอกี่วินาทีค่อยลองใหม่

    public static AdsManager Instance { get; private set; }

    LevelPlayBannerAd bannerAd;
    LevelPlayInterstitialAd interstitialAd;
    LevelPlayRewardedAd rewardedAd;

    bool initialized;
    bool bannerLoaded;
    bool wantBanner = true;             // เริ่มเกมที่เมนู -> โชว์ banner
    int interstitialChances;
    float lastInterstitialTime = -999f;
    Action afterInterstitial;
    Action onRewarded;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        if (Instance != null) return;
        var go = new GameObject("AdsManager");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<AdsManager>();
    }

    void Start()
    {
        LevelPlay.OnInitSuccess += OnInitSuccess;
        LevelPlay.OnInitFailed += OnInitFailed;
        LevelPlay.Init(AppKey);
    }

    void OnInitSuccess(LevelPlayConfiguration config)
    {
        Debug.Log("LevelPlay SDK initialized successfully");
        initialized = true;

        var bannerConfig = new LevelPlayBannerAd.Config.Builder();
        bannerConfig.SetSize(LevelPlayAdSize.BANNER);
        bannerConfig.SetPosition(LevelPlayBannerPosition.BottomCenter);
        bannerConfig.SetDisplayOnLoad(false);   // โชว์/ซ่อนเองตามหน้าจอ
        bannerAd = new LevelPlayBannerAd(BannerAdUnitId, bannerConfig.Build());
        bannerAd.OnAdLoaded += OnBannerLoaded;
        bannerAd.OnAdLoadFailed += OnBannerLoadFailed;
        bannerAd.LoadAd();                      // banner โหลดครั้งเดียว แล้วรีเฟรชเอง

        interstitialAd = new LevelPlayInterstitialAd(InterstitialAdUnitId);
        interstitialAd.OnAdLoadFailed += OnInterstitialLoadFailed;
        interstitialAd.OnAdDisplayFailed += OnInterstitialDisplayFailed;
        interstitialAd.OnAdClosed += OnInterstitialClosed;
        interstitialAd.LoadAd();

        rewardedAd = new LevelPlayRewardedAd(RewardedAdUnitId);
        rewardedAd.OnAdLoadFailed += OnRewardedLoadFailed;
        rewardedAd.OnAdDisplayFailed += OnRewardedDisplayFailed;
        rewardedAd.OnAdRewarded += OnRewardedEarned;
        rewardedAd.OnAdClosed += OnRewardedClosed;
        rewardedAd.LoadAd();
    }

    void OnInitFailed(LevelPlayInitError error)
    {
        Debug.LogWarning($"LevelPlay initialization failed: {error.ErrorMessage}");
    }

    // ---------- Banner ----------

    public static void ShowBanner()
    {
        if (Instance == null) return;
        Instance.wantBanner = true;
        if (Instance.bannerLoaded) Instance.bannerAd.ShowAd();
    }

    public static void HideBanner()
    {
        if (Instance == null) return;
        Instance.wantBanner = false;
        if (Instance.bannerLoaded) Instance.bannerAd.HideAd();
    }

    void OnBannerLoaded(LevelPlayAdInfo info)
    {
        bannerLoaded = true;
        if (wantBanner) bannerAd.ShowAd();
        else bannerAd.HideAd();
    }

    void OnBannerLoadFailed(LevelPlayAdError error)
    {
        Debug.LogWarning($"Banner failed to load: {error.ErrorMessage}");
    }

    // ---------- Interstitial ----------

    // เรียกตรงจังหวะเปลี่ยนฉาก/ด่าน: ถ้าถึงรอบและโฆษณาพร้อมจะโชว์ก่อน แล้วค่อยเรียก then
    // ไม่พร้อม/ยังไม่ถึงรอบ -> เรียก then ทันที (เกมไม่ต้องรอโฆษณา)
    public static void ShowInterstitial(Action then)
    {
        AdsManager ads = Instance;
        if (ads == null || !ads.initialized) { then?.Invoke(); return; }

        ads.interstitialChances++;
        bool capped = ads.interstitialChances < InterstitialEveryN ||
                      Time.unscaledTime - ads.lastInterstitialTime < MinSecondsBetweenInterstitials;
        if (capped || !ads.interstitialAd.IsAdReady()) { then?.Invoke(); return; }

        ads.interstitialChances = 0;
        ads.lastInterstitialTime = Time.unscaledTime;
        ads.afterInterstitial = then;
        ads.interstitialAd.ShowAd();
    }

    void OnInterstitialClosed(LevelPlayAdInfo info)
    {
        FinishInterstitial();
        interstitialAd.LoadAd();    // เตรียมตัวถัดไป
    }

    void OnInterstitialDisplayFailed(LevelPlayAdInfo info, LevelPlayAdError error)
    {
        Debug.LogWarning($"Interstitial failed to display: {error.ErrorMessage}");
        FinishInterstitial();
        interstitialAd.LoadAd();
    }

    void OnInterstitialLoadFailed(LevelPlayAdError error)
    {
        Debug.LogWarning($"Interstitial failed to load: {error.ErrorMessage}");
        Invoke(nameof(LoadInterstitial), RetryLoadDelay);
    }

    void LoadInterstitial() => interstitialAd.LoadAd();

    void FinishInterstitial()
    {
        Action then = afterInterstitial;
        afterInterstitial = null;
        then?.Invoke();
    }

    // ---------- Rewarded ----------

    public static bool IsRewardedReady =>
        Instance != null && Instance.initialized && Instance.rewardedAd.IsAdReady();

    // ให้รางวัลเฉพาะตอนดูจบจริง (OnAdRewarded) ปิดกลางคันไม่ได้
    public static void ShowRewarded(Action reward)
    {
        if (!IsRewardedReady) return;
        Instance.onRewarded = reward;
        Instance.rewardedAd.ShowAd();
    }

    void OnRewardedEarned(LevelPlayAdInfo info, LevelPlayReward reward)
    {
        Action give = onRewarded;
        onRewarded = null;
        give?.Invoke();
    }

    void OnRewardedClosed(LevelPlayAdInfo info)
    {
        rewardedAd.LoadAd();        // ให้มีพร้อมไว้ตลอดสำหรับครั้งหน้า
    }

    void OnRewardedDisplayFailed(LevelPlayAdInfo info, LevelPlayAdError error)
    {
        Debug.LogWarning($"Rewarded failed to display: {error.ErrorMessage}");
        onRewarded = null;
        rewardedAd.LoadAd();
    }

    void OnRewardedLoadFailed(LevelPlayAdError error)
    {
        Debug.LogWarning($"Rewarded failed to load: {error.ErrorMessage}");
        Invoke(nameof(LoadRewarded), RetryLoadDelay);
    }

    void LoadRewarded() => rewardedAd.LoadAd();

    void OnDestroy()
    {
        LevelPlay.OnInitSuccess -= OnInitSuccess;
        LevelPlay.OnInitFailed -= OnInitFailed;

        if (bannerAd != null)
        {
            bannerAd.OnAdLoaded -= OnBannerLoaded;
            bannerAd.OnAdLoadFailed -= OnBannerLoadFailed;
            bannerAd.DestroyAd();
        }
        if (interstitialAd != null)
        {
            interstitialAd.OnAdLoadFailed -= OnInterstitialLoadFailed;
            interstitialAd.OnAdDisplayFailed -= OnInterstitialDisplayFailed;
            interstitialAd.OnAdClosed -= OnInterstitialClosed;
            interstitialAd.DestroyAd();
        }
        if (rewardedAd != null)
        {
            rewardedAd.OnAdLoadFailed -= OnRewardedLoadFailed;
            rewardedAd.OnAdDisplayFailed -= OnRewardedDisplayFailed;
            rewardedAd.OnAdRewarded -= OnRewardedEarned;
            rewardedAd.OnAdClosed -= OnRewardedClosed;
            rewardedAd.DestroyAd();
        }
        if (Instance == this) Instance = null;
    }
}
