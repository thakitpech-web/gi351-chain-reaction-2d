using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.RemoteConfig;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// ดึงค่าจาก Unity Remote Config แล้วแจ้งให้ dot เปลี่ยนสีตาม
// วางใน scene แรก (MainMenu) ได้เลย จะอยู่ข้าม scene ให้เอง
// ถ้าไม่ได้วางไว้ จะสร้างตัวเองอัตโนมัติหลังโหลด scene
//
// Key ใน Remote Config:
//   dot_color_mode : string  "random" (ปกติ) | "grayscale" (ขาวดำ ช่วง Halloween)
//   event_name     : string  ชื่ออีเวนต์ที่แสดงบนจอ เช่น "Halloween" (ว่าง = ไม่มีอีเวนต์)
//
// ช่วง Halloween ใช้ Game Override ใน Dashboard ตั้งเวลาเริ่ม/จบ ให้ทับค่า dot_color_mode = "grayscale"
public class RemoteConfigManager : MonoBehaviour
{
    public struct UserAttributes { }
    public struct AppAttributes { }

    public static RemoteConfigManager Instance { get; private set; }

    // ค่าที่เกมใช้ (ค่า default ตอนยังโหลดไม่เสร็จ / ออฟไลน์)
    public static bool Grayscale { get; private set; }
    public static string EventName { get; private set; } = "";
    public static event Action OnConfigChanged;

    [Tooltip("ชื่อ environment เช่น development / production (ว่าง = ใช้ที่เลือกใน Project Settings > Services)")]
    public string environmentName = "";

    [Tooltip("ดึงค่าใหม่ทุกกี่วินาที (0 = ไม่ดึงซ้ำ) กด F5 เพื่อดึงทันที")]
    public float refreshInterval = 10f;
    public bool showHud = true;

    string status = "Remote Config: connecting...";
    bool ready;
    bool fetching;
    float nextFetch;

    // AfterSceneLoad: ตัวที่วางใน scene ได้ Awake ไปก่อนแล้ว จะไม่สร้างซ้ำ
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        new GameObject("RemoteConfigManager").AddComponent<RemoteConfigManager>();
    }

    async void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        try
        {
            if (string.IsNullOrWhiteSpace(environmentName))
                await UnityServices.InitializeAsync();
            else
                await UnityServices.InitializeAsync(new InitializationOptions().SetEnvironmentName(environmentName.Trim()));
            Debug.Log("[RemoteConfig] Unity Services initialized"
                      + (string.IsNullOrWhiteSpace(environmentName) ? "" : $" (environment: {environmentName.Trim()})"));

            // Remote Config ต้อง sign in ก่อน
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"[RemoteConfig] Signed in. PlayerId = {AuthenticationService.Instance.PlayerId}");

            RemoteConfigService.Instance.FetchCompleted += ApplyRemoteConfig;
            ready = true;
            await FetchAsync();
        }
        catch (Exception e)
        {
            status = "Remote Config: offline (using defaults)";
            Debug.LogWarning($"[RemoteConfig] Init failed, using defaults: {e.Message}");
        }
    }

    void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;
        if (ready) RemoteConfigService.Instance.FetchCompleted -= ApplyRemoteConfig;
    }

    void Update()
    {
        if (!ready || fetching) return;

        bool refreshKey = false;
#if ENABLE_INPUT_SYSTEM
        refreshKey = Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame;
#else
        refreshKey = Input.GetKeyDown(KeyCode.F5);
#endif
        bool timerUp = refreshInterval > 0f && Time.unscaledTime >= nextFetch;

        if (refreshKey || timerUp)
            _ = FetchAsync();
    }

    public async Task FetchAsync()
    {
        if (!ready || fetching) return;
        fetching = true;
        nextFetch = Time.unscaledTime + refreshInterval;
        try
        {
            await RemoteConfigService.Instance.FetchConfigsAsync(new UserAttributes(), new AppAttributes());
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[RemoteConfig] Fetch failed: {e.Message}");
        }
        finally
        {
            fetching = false;
        }
    }

    void ApplyRemoteConfig(ConfigResponse response)
    {
        RuntimeConfig config = RemoteConfigService.Instance.appConfig;

        string mode = config.GetString("dot_color_mode", "random");
        bool grayscale = string.Equals(mode, "grayscale", StringComparison.OrdinalIgnoreCase);
        string eventName = config.GetString("event_name", "");

        bool changed = grayscale != Grayscale || eventName != EventName;
        Grayscale = grayscale;
        EventName = eventName;

        status = $"Remote Config ({response.requestOrigin}) | dot_color_mode = {mode}"
               + (string.IsNullOrEmpty(eventName) ? "" : $" | Event: {eventName}");
        Debug.Log($"[RemoteConfig] Fetched from {response.requestOrigin}: dot_color_mode={mode}, event_name='{eventName}', assignmentId={config.assignmentId}");

        if (changed)
        {
            Debug.Log($"[RemoteConfig] Config changed -> grayscale={Grayscale}");
            OnConfigChanged?.Invoke();
        }
    }

    void OnGUI()
    {
        if (!showHud) return;

        var style = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(Screen.height / 40f) };
        style.normal.textColor = Grayscale ? new Color(1f, 0.55f, 0.1f) : Color.white;

        string text = status;
        if (!string.IsNullOrEmpty(EventName))
            text = $"*** {EventName} Event! ***\n" + text;

        GUI.Label(new Rect(10, 10, Screen.width - 20, style.fontSize * 3f), text, style);
    }
}
