using UnityEngine;

// สั่นกล้องแบบ trauma: เรียก ScreenShake.Add(0..1) แล้วความแรงจะสะสมและค่อยๆ ลดลงเอง
// ไม่ต้องวางในฉากก็ได้ จะถูกเพิ่มเข้า Main Camera เองตอนเรียกครั้งแรก
// (ถ้าอยากปรับค่าใน Inspector ให้ลากสคริปต์นี้ไปใส่ Main Camera เอง)
public class ScreenShake : MonoBehaviour
{
    public float maxOffset = 0.8f;      // ระยะเลื่อนสูงสุด (หน่วย world)
    public float maxAngle = 4f;         // หมุนสูงสุด (องศา)
    public float frequency = 25f;       // ความถี่การสั่น
    public float decay = 1.5f;          // trauma ลดลงต่อวินาที

    static ScreenShake instance;

    // ระยะที่กล้องถูกเลื่อนออกจากตำแหน่งจริงอยู่ตอนนี้ (GameManager ใช้หักออกตอนคำนวณ PlayArea)
    public static Vector2 Offset => instance != null ? (Vector2)instance.offset : Vector2.zero;

    float trauma;
    float rumble;
    float seed;
    Vector3 offset;
    float angle;

    // สั่นแบบกระแทก: บวกเข้า trauma แล้วค่อยๆ ลดลง
    public static void Add(float amount)
    {
        if (amount <= 0f || !Find()) return;
        instance.trauma = Mathf.Clamp01(instance.trauma + amount);
    }

    // สั่นค้างไว้อย่างน้อยเท่านี้จนกว่าจะเรียกใหม่ (เรียกทุกเฟรม, 0 = หยุด)
    public static void SetRumble(float amount)
    {
        if (instance == null && amount <= 0f) return;
        if (!Find()) return;
        instance.rumble = Mathf.Clamp01(amount);
    }

    static bool Find()
    {
        if (instance != null) return true;
        Camera cam = Camera.main;
        if (cam == null) return false;
        instance = cam.GetComponent<ScreenShake>();
        if (instance == null) instance = cam.gameObject.AddComponent<ScreenShake>();
        return true;
    }

    void Awake()
    {
        instance = this;
        seed = Random.value * 100f;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void LateUpdate()
    {
        // เอาการสั่นของเฟรมก่อนออกก่อน กล้องจะได้ไม่ลอยออกจากตำแหน่งเดิม
        transform.position -= offset;
        transform.Rotate(0f, 0f, -angle);

        // ใช้ unscaled time จะได้ยังสั่นอยู่ตอน slow-mo / hit stop
        trauma = Mathf.Max(0f, trauma - decay * Time.unscaledDeltaTime);
        float level = Mathf.Max(trauma, rumble);
        float shake = level * level;
        float t = Time.unscaledTime * frequency;

        offset = new Vector3(
            maxOffset * shake * (Mathf.PerlinNoise(seed, t) * 2f - 1f),
            maxOffset * shake * (Mathf.PerlinNoise(seed + 1f, t) * 2f - 1f),
            0f);
        angle = maxAngle * shake * (Mathf.PerlinNoise(seed + 2f, t) * 2f - 1f);

        transform.position += offset;
        transform.Rotate(0f, 0f, angle);
    }
}
