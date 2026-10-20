using UnityEngine;

// คุม Time.timeScale ที่เดียว: hit stop (หยุดแว้บ) กับ slow-mo จะได้ไม่ทับกัน
// ไม่ต้องวางในฉาก สร้างตัวเองตอนถูกเรียกครั้งแรก
// เวลาที่ใส่ทั้งหมดเป็นวินาทีจริง (ไม่ช้าตาม slow-mo)
public class TimeFx : MonoBehaviour
{
    static TimeFx instance;

    float hitStopLeft;
    float slowScale = 1f;
    float slowHoldLeft;
    float slowRecover;
    float slowRecoverLeft;
    bool active;

    // หยุดเวลาแว้บหนึ่ง
    public static void HitStop(float duration)
    {
        if (duration <= 0f) return;
        TimeFx fx = Get();
        fx.hitStopLeft = Mathf.Max(fx.hitStopLeft, duration);
        fx.Apply();
    }

    // ช้าลงเหลือ scale นาน hold วินาที แล้วค่อยๆ กลับเป็นปกติใน recover วินาที
    public static void SlowMo(float scale, float hold, float recover)
    {
        TimeFx fx = Get();
        fx.slowScale = scale;
        fx.slowHoldLeft = hold;
        fx.slowRecover = Mathf.Max(recover, 0.001f);
        fx.slowRecoverLeft = recover;
        fx.Apply();
    }

    // ยกเลิกทั้งหมด (เรียกตอนเริ่มด่านใหม่)
    public static void ResetTime()
    {
        if (instance == null) return;
        instance.hitStopLeft = instance.slowHoldLeft = instance.slowRecoverLeft = 0f;
        instance.Apply();
    }

    static TimeFx Get()
    {
        if (instance == null) instance = new GameObject("TimeFx").AddComponent<TimeFx>();
        return instance;
    }

    void Update()
    {
        if (!active) return;

        // ระหว่าง hit stop ไม่นับเวลาของ slow-mo
        float dt = Time.unscaledDeltaTime;
        if (hitStopLeft > 0f) hitStopLeft -= dt;
        else if (slowHoldLeft > 0f) slowHoldLeft -= dt;
        else if (slowRecoverLeft > 0f) slowRecoverLeft -= dt;
        Apply();
    }

    void Apply()
    {
        float scale = 1f;
        if (slowHoldLeft > 0f)
            scale = slowScale;
        else if (slowRecoverLeft > 0f)
            scale = Mathf.SmoothStep(1f, slowScale, slowRecoverLeft / slowRecover);
        if (hitStopLeft > 0f)
            scale = 0f;

        bool nowActive = hitStopLeft > 0f || slowHoldLeft > 0f || slowRecoverLeft > 0f;
        if (nowActive || active) Time.timeScale = scale;    // ไม่ไปยุ่งกับ timeScale ตอนไม่ได้ใช้งาน
        active = nowActive;
    }

    void OnDestroy()
    {
        if (active) Time.timeScale = 1f;
        if (instance == this) instance = null;
    }
}
