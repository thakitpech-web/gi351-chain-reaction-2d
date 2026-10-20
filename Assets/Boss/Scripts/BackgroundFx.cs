using UnityEngine;

// ฉากหลัง: สีพื้นตามด่าน + สว่างวาบตอนระเบิด + ตารางที่กระเพื่อมตามแรงระเบิด
// ไม่ต้องวางในฉากก็ได้ จะสร้างตัวเองตอนถูกเรียกครั้งแรก
// (ถ้าอยากปรับค่าใน Inspector ให้สร้าง GameObject เปล่าแล้วใส่สคริปต์นี้เอง)
public class BackgroundFx : MonoBehaviour
{
    [Header("Level Colors")]
    public Color[] levelColors =
    {
        new Color(0.10f, 0.14f, 0.26f),   // น้ำเงินเข้ม
        new Color(0.20f, 0.10f, 0.28f),   // ม่วง
        new Color(0.06f, 0.20f, 0.22f),   // เขียวอมฟ้า
        new Color(0.26f, 0.10f, 0.14f),   // แดงไวน์
        new Color(0.08f, 0.20f, 0.12f),   // เขียวป่า
        new Color(0.24f, 0.15f, 0.08f),   // น้ำตาลอำพัน
        new Color(0.22f, 0.06f, 0.20f),   // บานเย็น
        new Color(0.12f, 0.12f, 0.18f),   // เทาอมน้ำเงิน
    };
    public float colorChangeSpeed = 4f;     // เปลี่ยนสีเข้าหาสีด่านใหม่เร็วแค่ไหน

    [Header("Flash")]
    [Range(0f, 1f)] public float flashAmount = 0.12f;   // วาบเท่าไหร่ต่อระเบิด 1 ลูก
    [Range(0f, 1f)] public float maxFlash = 0.35f;      // วาบสะสมได้สูงสุดเท่านี้
    public float flashDecay = 1.5f;                     // วาบจางลงต่อวินาที

    [Header("Grid")]
    public Material lineMaterial;           // (ไม่ใส่ก็ได้) ใช้ Sprites/Default
    public float spacing = 0.5f;            // ระยะห่างช่องตาราง
    public float lineWidth = 0.03f;
    [Range(0f, 1f)] public float gridBrightness = 0.25f;  // เส้นสว่างกว่าพื้นแค่ไหน
    [Range(0f, 1f)] public float gridAlpha = 0.5f;
    public int sortingOrder = -100;         // อยู่หลังทุกอย่าง

    [Header("Ripple")]
    public float rippleForce = 2.5f;         // แรงผลักจุดในตารางตอนระเบิด
    public float rippleRadius = 3f;
    public float stiffness = 25f;           // แรงดึงกลับที่เดิม
    public float waveSpread = 60f;          // แรงส่งต่อให้จุดข้างๆ (ทำให้เป็นคลื่นวิ่งออกไป)
    public float damping = 3f;

    static BackgroundFx instance;

    Camera cam;
    Color baseColor, targetColor, flashColor;
    float flash;

    int cols, rows;
    Vector2 origin;
    Vector2[,] disp, vel;
    LineRenderer[] hLines, vLines;
    Vector3[] hBuffer, vBuffer;

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
        BuildGrid();
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void BuildGrid()
    {
        // ใหญ่กว่าจอนิดหน่อย จะได้ไม่เห็นขอบตอนจอสั่น
        float halfH = cam.orthographicSize + 1.5f;
        float halfW = cam.orthographicSize * cam.aspect + 1.5f;
        Vector2 center = (Vector2)cam.transform.position - ScreenShake.Offset;

        cols = Mathf.CeilToInt(halfW * 2f / spacing) + 1;
        rows = Mathf.CeilToInt(halfH * 2f / spacing) + 1;
        origin = center - new Vector2((cols - 1) * spacing, (rows - 1) * spacing) * 0.5f;

        disp = new Vector2[cols, rows];
        vel = new Vector2[cols, rows];
        hBuffer = new Vector3[cols];
        vBuffer = new Vector3[rows];

        if (lineMaterial == null) lineMaterial = new Material(Shader.Find("Sprites/Default"));

        hLines = new LineRenderer[rows];
        for (int j = 0; j < rows; j++) hLines[j] = CreateLine("H" + j, cols);
        vLines = new LineRenderer[cols];
        for (int i = 0; i < cols; i++) vLines[i] = CreateLine("V" + i, rows);
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
        for (int j = 1; j < rows - 1; j++)
        {
            Vector2 diff = RestPosition(i, j) - position;
            float dist = diff.magnitude;
            if (dist >= rippleRadius || dist < 0.001f) continue;
            vel[i, j] += diff / dist * rippleForce * (1f - dist / rippleRadius);
        }
    }

    Vector2 RestPosition(int i, int j) => origin + new Vector2(i * spacing, j * spacing);

    void Update()
    {
        float dt = Mathf.Min(Time.deltaTime, 1f / 30f);

        // สีพื้น: ค่อยๆ ไปหาสีด่าน แล้วผสมกับสีวาบ
        baseColor = Color.Lerp(baseColor, targetColor, 1f - Mathf.Exp(-colorChangeSpeed * dt));
        flash = Mathf.Max(0f, flash - flashDecay * dt);
        Color bg = Color.Lerp(baseColor, flashColor, flash);
        cam.backgroundColor = bg;

        Color lineColor = Color.Lerp(bg, Color.white, gridBrightness);
        lineColor.a = gridAlpha;

        SimulateGrid(dt);
        DrawGrid(lineColor);
    }

    void SimulateGrid(float dt)
    {
        float keep = Mathf.Exp(-damping * dt);

        // ขอบนอกสุดตรึงไว้ ไม่ขยับ
        for (int i = 1; i < cols - 1; i++)
        for (int j = 1; j < rows - 1; j++)
        {
            Vector2 d = disp[i, j];
            Vector2 neighbours = disp[i - 1, j] + disp[i + 1, j] + disp[i, j - 1] + disp[i, j + 1] - 4f * d;
            vel[i, j] = (vel[i, j] + (neighbours * waveSpread - d * stiffness) * dt) * keep;
        }

        for (int i = 1; i < cols - 1; i++)
        for (int j = 1; j < rows - 1; j++)
            disp[i, j] += vel[i, j] * dt;
    }

    void DrawGrid(Color color)
    {
        for (int j = 0; j < rows; j++)
        {
            for (int i = 0; i < cols; i++) hBuffer[i] = RestPosition(i, j) + disp[i, j];
            hLines[j].SetPositions(hBuffer);
            hLines[j].startColor = hLines[j].endColor = color;
        }
        for (int i = 0; i < cols; i++)
        {
            for (int j = 0; j < rows; j++) vBuffer[j] = RestPosition(i, j) + disp[i, j];
            vLines[i].SetPositions(vBuffer);
            vLines[i].startColor = vLines[i].endColor = color;
        }
    }
}
