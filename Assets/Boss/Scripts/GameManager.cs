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

    public int TotalDots => circles + triangles + squares;
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

    [Header("Juice")]
    [Range(0f, 1f)] public float chainRumble = 0.35f;   // จอสั่นเบาๆ ค้างไว้ตลอดที่ยังมีระเบิดอยู่บนจอ
    public float spawnStartDelay = 0.2f;    // รอจอสว่างก่อนค่อยเริ่มโผล่
    public float spawnSpread = 0.6f;        // dot ใกล้กลางจอโผล่ก่อน ไกลสุดโผล่ช้ากว่าเท่านี้ (วินาที)
    [Range(0.05f, 1f)] public float goalSlowMo = 0.3f;  // ถึง goal แล้วเวลาช้าลงเหลือเท่านี้
    public float goalSlowMoHold = 0.6f;     // ค้างช้าไว้นานเท่าไหร่ (วินาทีจริง)
    public float goalSlowMoRecover = 0.4f;  // ค่อยๆ กลับเป็นปกติในกี่วินาที

    [Header("Sound")]
    [Range(0f, 1f)] public float chainVolume = 0.6f;    // เสียง dot แตก (chain ยาวๆ เสียงซ้อนกันเยอะ เลยเบาลงนิด)

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

    // Result = ขึ้น Level Panel รอผู้เล่นกดเล่นด่าน pendingLevel
    enum State { Playing, Result, Loading }

    State state;
    int levelIndex;
    int pendingLevel;
    int exploded;
    int totalDots;
    int ammoLeft;
    int totalShots;         // ยิงไปกี่นัดตั้งแต่ด่าน 1 (นับรวมตอนเล่นด่านซ้ำด้วย)
    int totalScore;         // รวม dot ที่ระเบิดในด่านที่ผ่านแล้ว
    readonly List<ChainDot> dots = new List<ChainDot>();
    readonly List<Explosion> explosions = new List<Explosion>();
    readonly List<Bullet> bullets = new List<Bullet>();

    LevelData CurrentLevel => levels[levelIndex];

    // จบด่านแล้ว (ผ่าน/แพ้/กำลังเปลี่ยนด่าน) -> player ไม่โดน damage
    public bool IsPlaying => state == State.Playing;

    void Awake()
    {
        Instance = this;
        UpdatePlayArea();
    }

    void Start()
    {
        totalShots = 0;
        totalScore = 0;
        StartLevel(0);
    }

    void Update()
    {
        UpdatePlayArea();
        ScreenShake.SetRumble(explosions.Count > 0 ? chainRumble : 0f);

        if (state == State.Playing && Input.GetKeyDown(KeyCode.R))
        {
            GoToLevel(levelIndex);
            return;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // ทดสอบ: กด E = ผ่านด่านทันที (นับว่าได้ goal พอดี) ใช้ได้แค่ใน Editor / Development Build
        if (state == State.Playing && Input.GetKeyDown(KeyCode.E))
        {
            exploded = Mathf.Max(exploded, CurrentLevel.goal);
            EndLevel();
            return;
        }
#endif

        // จบด่านเมื่อ: ไม่มีกระสุน/ระเบิดค้างอยู่ และ (กระสุนหมด หรือ dot หมดจอ)
        if (state == State.Playing && bullets.Count == 0 && explosions.Count == 0 &&
            (ammoLeft == 0 || dots.Count == 0))
        {
            EndLevel();
        }
        else if (state == State.Result &&
                 (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
        {
            PlayLevel();
        }
    }

    // จบด่าน -> ขึ้น Level Panel: ผลด่านที่แล้ว + ข้อมูลด่าน index ที่จะเล่นต่อ -> รอกด Space
    void ShowLevelPanel(int index, string result, bool passed)
    {
        pendingLevel = index;
        state = State.Result;
        ui.ShowLevelPanel(result, passed, index + 1, levels[index].goal, levels[index].TotalDots, totalScore);
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
        TimeFx.ResetTime();

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
        StaggerSpawn(playerStart);
        totalDots = dots.Count;
        exploded = 0;
        ammoLeft = ammoPerLevel;

        player.ResetForLevel(playerStart);
        state = State.Playing;
        BackgroundFx.SetLevel(levelIndex);

        ui.ShowLevel(levelIndex + 1, levels.Count);
        ui.ShowScore(exploded, CurrentLevel.goal, totalDots);
        ui.ShowAmmo(ammoLeft);
        ui.HideLevelPanel();
        ui.HideGameEnd();
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

    // ทยอยเด้งโผล่จากกลางจอออกไปเป็นวง
    void StaggerSpawn(Vector2 center)
    {
        float maxDist = 0.01f;
        foreach (ChainDot d in dots)
            maxDist = Mathf.Max(maxDist, Vector2.Distance(d.transform.position, center));

        foreach (ChainDot d in dots)
            d.spawnDelay = spawnStartDelay +
                           Vector2.Distance(d.transform.position, center) / maxDist * spawnSpread;
    }

    Vector2 RandomPoint()
    {
        return new Vector2(
            Random.Range(PlayArea.xMin, PlayArea.xMax),
            Random.Range(PlayArea.yMin, PlayArea.yMax));
    }

    void EndLevel()
    {
        int goal = CurrentLevel.goal;

        if (exploded < goal)
        {
            ShowLevelPanel(levelIndex, $"Not Enough! ONLY GOT {exploded}/{goal}", false);
            SoundManager.Play("8bit_Fail");
        }
        else
        {
            totalScore += exploded;
            if (levelIndex + 1 < levels.Count)
            {
                ShowLevelPanel(levelIndex + 1, $"GOOD JOB! YOU GOT {exploded}/{goal}", true);
                SoundManager.Play("8bit_Pass");
            }
            else
            {
                state = State.Result;
                ui.ShowGameEnd(totalShots, totalScore);    // ด่านสุดท้าย -> ขึ้น panel GameEnd
                SoundManager.Play("8bit_GameEnd");
            }
        }
    }

    // กด Space / Enter ตอน Level Panel ขึ้น
    void PlayLevel()
    {
        if (!ui.levelPanel.activeSelf) return;     // ตอนขึ้น GameEnd ไม่ใช้ Space
        SoundManager.Play("8bit_Button");
        GoToLevel(pendingLevel);
    }

    // ---------- ปุ่มใน panel GameEnd ----------

    public void PlayAgain()
    {
        if (state == State.Loading) return;
        SoundManager.Play("8bit_Button");
        totalShots = 0;
        totalScore = 0;
        GoToLevel(0);
    }

    public void BackToMainMenu()
    {
        if (state == State.Loading) return;
        SoundManager.Play("8bit_Button");
        state = State.Loading;
        SceneFader.FadeTo("MainMenu");
    }

    // ---------- เรียกจากสคริปต์อื่น ----------

    public bool TryUseAmmo()
    {
        if (state != State.Playing || ammoLeft <= 0) return false;
        ammoLeft--;
        totalShots++;
        ui.ShowAmmo(ammoLeft);
        return true;
    }

    public void OnPlayerHealthChanged(int health, int maxHealth)
    {
        ui.ShowHealth(health, maxHealth);
    }

    public void OnPlayerDied()
    {
        if (state != State.Playing) return;
        ShowLevelPanel(levelIndex, "GAME OVER! YOU GOT HIT", false);
        SoundManager.Play("8bit_GameOver");
    }

    public void OnDotExploded(ChainDot dot)
    {
        dots.Remove(dot);
        exploded++;
        ui.ShowScore(exploded, CurrentLevel.goal, totalDots);
        SoundManager.Play("Chain", chainVolume);   // กลุ่ม Chain มี 3 เสียง สุ่มเล่น จะได้ไม่ซ้ำตอน chain ยาวๆ

        // ลูกที่ทำให้ถึง goal พอดี -> slow-mo
        if (exploded == CurrentLevel.goal)
        {
            TimeFx.SlowMo(goalSlowMo, goalSlowMoHold, goalSlowMoRecover);
            SoundManager.Play("8bit_Goal");
        }
    }

    public void RegisterExplosion(Explosion e) => explosions.Add(e);
    public void UnregisterExplosion(Explosion e) => explosions.Remove(e);
    public void RegisterBullet(Bullet b) => bullets.Add(b);
    public void UnregisterBullet(Bullet b) => bullets.Remove(b);
}
