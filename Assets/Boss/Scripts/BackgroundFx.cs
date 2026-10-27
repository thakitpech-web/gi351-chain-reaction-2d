using UnityEngine;

// ฉากหลัง: สีพื้นตามด่าน + สว่างวาบตอนระเบิด + บรรทัดโน้ต (5 เส้น) เส้นตรง กระเพื่อมตามแรงระเบิด
// ไม่ต้องวางในฉากก็ได้ จะสร้างตัวเองตอนถูกเรียกครั้งแรก
// (ถ้าอยากปรับค่าใน Inspector ให้สร้าง GameObject เปล่าแล้วใส่สคริปต์นี้เอง)
public class BackgroundFx : MonoBehaviour
{
    [Header("Level Colors")]
    public Color[] levelColors =           // ขาวดำ: แต่ละด่านเป็นเทาคนละเฉด
    {
        new Color(0.16f, 0.16f, 0.16f),
        new Color(0.20f, 0.20f, 0.20f),
        new Color(0.13f, 0.13f, 0.13f),
        new Color(0.22f, 0.22f, 0.22f),
        new Color(0.15f, 0.15f, 0.15f),
        new Color(0.19f, 0.19f, 0.19f),
        new Color(0.12f, 0.12f, 0.12f),
        new Color(0.21f, 0.21f, 0.21f),
    };
    public float colorChangeSpeed = 4f;     // เปลี่ยนสีเข้าหาสีด่านใหม่เร็วแค่ไหน

    [Header("Flash")]
    [Range(0f, 1f)] public float flashAmount = 0.12f;   // วาบเท่าไหร่ต่อระเบิด 1 ลูก
    [Range(0f, 1f)] public float maxFlash = 0.35f;      // วาบสะสมได้สูงสุดเท่านี้
    public float flashDecay = 1.5f;                     // วาบจางลงต่อวินาที

    [Header("Staff (บรรทัด 5 เส้น)")]
    public Material lineMaterial;           // (ไม่ใส่ก็ได้) ใช้ Sprites/Default
    public int linesPerStaff = 5;
    public float lineGap = 0.1f;            // ระยะห่างระหว่างเส้นในบรรทัดเดียวกัน
    public float staffGap = 1.1f;           // ระยะจากบรรทัดหนึ่งถึงบรรทัดถัดไป
    public float pointSpacing = 0.2f;       // ความละเอียดของเส้น (ยิ่งน้อยยิ่งโค้งเนียนตอนกระเพื่อม)
    public float lineWidth = 0.015f;
    public Color lineColor = new Color(0f, 0f, 0f, 0.15f);  // alpha น้อย = เส้นจาง
    public int sortingOrder = -100;         // อยู่หลังทุกอย่าง

    [Header("Ripple")]
    public float rippleForce = 2.5f;         // แรงผลักเส้นตอนระเบิด
    public float rippleRadius = 3f;
    public float stiffness = 25f;           // แรงดึงกลับที่เดิม
    public float waveSpread = 60f;          // แรงส่งต่อให้จุดข้างๆ (ทำให้เป็นคลื่นวิ่งออกไป)
    public float damping = 3f;

    static BackgroundFx instance;

    Camera cam;
    Color baseColor, targetColor, flashColor;
    float flash;

    int cols, staves;
    Vector2 origin;
    Vector2[,] disp, vel;                   // [จุดตามแนวนอน, บรรทัด] เส้นทั้ง 5 ในบรรทัดขยับไปด้วยกัน
    LineRenderer[] lines;                   // staves * linesPerStaff เส้น
    Vector3[] buffer;

    // ---------- เรียกจากสคริปต์อื่น ----------

    public static void SetLevel(int levelIndex)
    {
        BackgroundFx fx = Get();
        if (fx == null || fx.levelColors.Length == 0) return;
        fx.targetColor = fx.levelColors[levelIndex % fx.levelColors.Length];
    }

    public static void OnExplosion(Vector2 position, Color color)
    {
        BackgroundFx fx = Get();
        if (fx == null) return;

        color.a = 1f;
        fx.flashColor = fx.flash <= 0.01f ? color : Color.Lerp(fx.flashColor, color, 0.5f);
        fx.flash = Mathf.Min(fx.maxFlash, fx.flash + fx.flashAmount);
        fx.Ripple(position);
    }

    static BackgroundFx Get()
    {
        if (instance != null) return instance;
        instance = FindAnyObjectByType<BackgroundFx>();
        if (instance == null) instance = new GameObject("Background").AddComponent<BackgroundFx>();
        return instance;
    }

    // ---------- ภายใน ----------

    void Awake()
    {
        instance = this;
        cam = Camera.main;
        cam.clearFlags = CameraClearFlags.SolidColor;
        baseColor = targetColor = cam.backgroundColor;
        BuildStaves();
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void BuildStaves()
    {
        // ใหญ่กว่าจอนิดหน่อย จะได้ไม่เห็นขอบตอนจอสั่น
        float halfH = cam.orthographicSize + 1.5f;
        float halfW = cam.orthographicSize * cam.aspect + 1.5f;
        Vector2 center = (Vector2)cam.transform.position - ScreenShake.Offset;

        cols = Mathf.CeilToInt(halfW * 2f / pointSpacing) + 1;
        staves = Mathf.CeilToInt(halfH * 2f / staffGap) + 1;
        origin = center - new Vector2((cols - 1) * pointSpacing, (staves - 1) * staffGap) * 0.5f;

        disp = new Vector2[cols, staves];
        vel = new Vector2[cols, staves];
        buffer = new Vector3[cols];

        if (lineMaterial == null) lineMaterial = FxMaterial.Sprite;

        lines = new LineRenderer[staves * linesPerStaff];
        for (int k = 0; k < lines.Length; k++) lines[k] = CreateLine("Staff" + k / linesPerStaff + "_" + k % linesPerStaff, cols);
    }

    LineRenderer CreateLine(string lineName, int points)
    {
        LineRenderer line = new GameObject(lineName).AddComponent<LineRenderer>();
        line.transform.SetParent(transform, false);
        line.sharedMaterial = lineMaterial;
        line.positionCount = points;
        line.widthMultiplier = lineWidth;
        line.useWorldSpace = true;
        line.sortingOrder = sortingOrder;
        line.numCapVertices = 0;
        return line;
    }

    void Ripple(Vector2 position)
    {
        for (int i = 1; i < cols - 1; i++)
        for (int s = 1; s < staves - 1; s++)
        {
            Vector2 diff = RestPosition(i, s) - position;
            float dist = diff.magnitude;
            if (dist >= rippleRadius || dist < 0.001f) continue;
            vel[i, s] += diff / dist * rippleForce * (1f - dist / rippleRadius);
        }
    }

    // กลางบรรทัด s ที่จุด i
    Vector2 RestPosition(int i, int s) => origin + new Vector2(i * pointSpacing, s * staffGap);

    void Update()
    {
        float dt = Mathf.Min(Time.deltaTime, 1f / 30f);

        // สีพื้น: ค่อยๆ ไปหาสีด่าน แล้วผสมกับสีวาบ
        baseColor = Color.Lerp(baseColor, targetColor, 1f - Mathf.Exp(-colorChangeSpeed * dt));
        flash = Mathf.Max(0f, flash - flashDecay * dt);
        cam.backgroundColor = Color.Lerp(baseColor, flashColor, flash);

        Simulate(dt);
        DrawStaves();
    }

    void Simulate(float dt)
    {
        float keep = Mathf.Exp(-damping * dt);
        // บรรทัดห่างกันกว่าจุดในเส้นมาก ลดแรงส่งต่อแนวตั้งลง ให้คลื่นวิ่งเร็วพอๆ กันทุกทิศ
        float across = (pointSpacing / staffGap) * (pointSpacing / staffGap);

        // ขอบนอกสุดตรึงไว้ ไม่ขยับ
        for (int i = 1; i < cols - 1; i++)
        for (int s = 1; s < staves - 1; s++)
        {
            Vector2 d = disp[i, s];
            Vector2 neighbours = disp[i - 1, s] + disp[i + 1, s] - 2f * d
                               + (disp[i, s - 1] + disp[i, s + 1] - 2f * d) * across;
            vel[i, s] = (vel[i, s] + (neighbours * waveSpread - d * stiffness) * dt) * keep;
        }

        for (int i = 1; i < cols - 1; i++)
        for (int s = 1; s < staves - 1; s++)
            disp[i, s] += vel[i, s] * dt;
    }

    void DrawStaves()
    {
        float half = (linesPerStaff - 1) * 0.5f;

        for (int s = 0; s < staves; s++)
        for (int l = 0; l < linesPerStaff; l++)
        {
            float offset = (l - half) * lineGap;
            for (int i = 0; i < cols; i++)
            {
                Vector2 p = RestPosition(i, s) + disp[i, s];
                p.y += offset;
                buffer[i] = p;
            }

            LineRenderer line = lines[s * linesPerStaff + l];
            line.SetPositions(buffer);
            line.startColor = line.endColor = lineColor;
        }
    }
}
