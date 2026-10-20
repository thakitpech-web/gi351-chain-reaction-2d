using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LevelData
{
    public int circles;
    public int triangles;
    public int squares;
    public int goal;        // ต้องระเบิดให้ได้กี่จุดถึงผ่าน

    public LevelData() { }

    public LevelData(int circles, int triangles, int squares, int goal)
    {
        this.circles = circles;
        this.triangles = triangles;
        this.squares = squares;
        this.goal = goal;
    }
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Dot Prefabs")]
    public ChainDot circleDot;
    public ChainDot triangleDot;
    public ChainDot squareDot;

    [Header("Scene")]
    public PlayerController player;
    public UiManager ui;
    public float edgePadding = 0.3f;        // ระยะเว้นจากขอบจอ
    public float safeSpawnRadius = 2f;      // ไม่เกิด dot ใกล้ player ตอนเริ่มด่าน

    [Header("Rules")]
    public int ammoPerLevel = 1;            // ยิงได้กี่นัดต่อด่าน
    public float retryDelay = 1f;           // แพ้แล้วรอกี่วินาทีก่อนเริ่มด่านใหม่อัตโนมัติ (0 = ทันที)

    [Header("Juice")]
    [Range(0f, 1f)] public float chainRumble = 0.35f;   // จอสั่นเบาๆ ค้างไว้ตลอดที่ยังมีระเบิดอยู่บนจอ

    [Header("Levels")]
    public List<LevelData> levels = new List<LevelData>
    {
        new LevelData(5, 0, 0, 1),
        new LevelData(6, 4, 0, 2),
        new LevelData(5, 5, 5, 4),
        new LevelData(7, 7, 6, 6),
        new LevelData(9, 8, 8, 10),
        new LevelData(10, 10, 10, 15),
        new LevelData(12, 12, 11, 21),
        new LevelData(14, 13, 13, 27),
    };

    public Rect PlayArea { get; private set; }

    enum State { Playing, Result, Loading }

    State state;
    int levelIndex;
    int exploded;
    int totalDots;
    int ammoLeft;
    bool passed;
    float retryTime;
    readonly List<ChainDot> dots = new List<ChainDot>();
    readonly List<Explosion> explosions = new List<Explosion>();
    readonly List<Bullet> bullets = new List<Bullet>();

    LevelData CurrentLevel => levels[levelIndex];

    void Awake()
    {
        Instance = this;
        UpdatePlayArea();
    }

    void Start()
    {
        StartLevel(0);
    }

    void Update()
    {
        UpdatePlayArea();
        ScreenShake.SetRumble(explosions.Count > 0 ? chainRumble : 0f);

        if (Input.GetKeyDown(KeyCode.R))
        {
            GoToLevel(levelIndex);
            return;
        }

        // จบด่านเมื่อ: ไม่มีกระสุน/ระเบิดค้างอยู่ และ (กระสุนหมด หรือ dot หมดจอ)
        if (state == State.Playing && bullets.Count == 0 && explosions.Count == 0 &&
            (ammoLeft == 0 || dots.Count == 0))
        {
            EndLevel();
        }
        else if (state == State.Result && !passed)
        {
            // แพ้ -> เริ่มด่านเดิมใหม่เองอัตโนมัติ
            if (Time.time >= retryTime) GoToLevel(levelIndex);
        }
        else if (state == State.Result &&
                 (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
        {
            if (levelIndex + 1 < levels.Count) GoToLevel(levelIndex + 1);
            else GoToLevel(0);
        }
    }

    // จอดำ -> เริ่มด่าน -> จอสว่างกลับ
    void GoToLevel(int index)
    {
        if (state == State.Loading) return;
        state = State.Loading;
        SceneFader.Transition(() => StartLevel(index));
    }

    // ขอบเขตเล่น = ขนาดกล้อง (orthographic) หักขอบ
    void UpdatePlayArea()
    {
        Camera cam = Camera.main;
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        Vector2 c = (Vector2)cam.transform.position - ScreenShake.Offset;   // ไม่ให้ขอบสั่นตามกล้อง
        PlayArea = Rect.MinMaxRect(
            c.x - halfW + edgePadding, c.y - halfH + edgePadding,
            c.x + halfW - edgePadding, c.y + halfH - edgePadding);
    }

    void StartLevel(int index)
    {
        levelIndex = index;

        foreach (ChainDot d in dots) if (d != null) Destroy(d.gameObject);
        foreach (Explosion e in explosions) if (e != null) Destroy(e.gameObject);
        foreach (Bullet b in bullets) if (b != null) Destroy(b.gameObject);
        dots.Clear();
        explosions.Clear();
        bullets.Clear();

        Vector2 playerStart = PlayArea.center;
        SpawnDots(circleDot, CurrentLevel.circles, playerStart);
        SpawnDots(triangleDot, CurrentLevel.triangles, playerStart);
        SpawnDots(squareDot, CurrentLevel.squares, playerStart);
        totalDots = dots.Count;
        exploded = 0;
        ammoLeft = ammoPerLevel;

        player.ResetForLevel(playerStart);
        state = State.Playing;
        BackgroundFx.SetLevel(levelIndex);

        ui.ShowLevel(levelIndex + 1, levels.Count);
        ui.ShowScore(exploded, CurrentLevel.goal, totalDots);
        ui.ShowAmmo(ammoLeft);
        ui.HideMessage();
    }

    void SpawnDots(ChainDot prefab, int count, Vector2 avoid)
    {
        if (prefab == null) return;
        for (int i = 0; i < count; i++)
        {
            Vector2 pos = RandomPoint();
            for (int tries = 0; tries < 20 && Vector2.Distance(pos, avoid) < safeSpawnRadius; tries++)
                pos = RandomPoint();

            dots.Add(Instantiate(prefab, pos, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f))));
        }
    }

    Vector2 RandomPoint()
    {
        return new Vector2(
            Random.Range(PlayArea.xMin, PlayArea.xMax),
            Random.Range(PlayArea.yMin, PlayArea.yMax));
    }

    void EndLevel()
    {
        passed = exploded >= CurrentLevel.goal;

        if (!passed)
            Fail($"NOT ENOUGH!\n{exploded} / {CurrentLevel.goal}");
        else
        {
            state = State.Result;
            if (levelIndex + 1 < levels.Count)
                ui.ShowMessage($"LEVEL CLEAR!\n{exploded} / {totalDots}\n\nPress Space for next level");
            else
                ui.ShowMessage("YOU WIN!\n\nPress Space to play again");
        }
    }

    void Fail(string message)
    {
        if (state == State.Loading) return;     // กำลังเปลี่ยนด่านอยู่แล้ว
        state = State.Result;
        passed = false;
        retryTime = Time.time + retryDelay;
        ui.ShowMessage(message);
    }

    // ---------- เรียกจากสคริปต์อื่น ----------

    public bool TryUseAmmo()
    {
        if (state != State.Playing || ammoLeft <= 0) return false;
        ammoLeft--;
        ui.ShowAmmo(ammoLeft);
        return true;
    }

    public void OnPlayerHealthChanged(int health, int maxHealth)
    {
        ui.ShowHealth(health, maxHealth);
    }

    public void OnPlayerDied()
    {
        Fail("GAME OVER");
    }

    public void OnDotExploded(ChainDot dot)
    {
        dots.Remove(dot);
        exploded++;
        ui.ShowScore(exploded, CurrentLevel.goal, totalDots);
    }

    public void RegisterExplosion(Explosion e) => explosions.Add(e);
    public void UnregisterExplosion(Explosion e) => explosions.Remove(e);
    public void RegisterBullet(Bullet b) => bullets.Add(b);
    public void UnregisterBullet(Bullet b) => bullets.Remove(b);
}
