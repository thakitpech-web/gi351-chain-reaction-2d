using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UiManager : MonoBehaviour
{
    public TMP_Text levelText;
    public TMP_Text scoreText;
    public TMP_Text hpText;                 // (ไม่ใส่ก็ได้) เลขเลือดแบบเดิม ตอนนี้ใช้หัวใจแทน

    [Header("HP Hearts (ลูกใน Hearts = หัวใจแต่ละดวง ไม่พอจะ copy ดวงแรกเพิ่มเอง)")]
    public RectTransform heartContainer;
    public Sprite heartFull;
    public Sprite heartEmpty;
    public float heartPopScale = 1.5f;      // ดวงที่เพิ่งเสียขยายเด้งแล้วหดกลับ
    public float heartPopTime = 0.3f;

    public TMP_Text ammoText;
    public GameObject gameEndPanel;
    public TMP_Text gameEndText;
    public TMP_Text gameEndScoreText;

    [Header("Level Panel (ขึ้นตอนจบด่าน แต่ละบรรทัดแยก text ไว้ จัดตำแหน่ง/ฟอนต์ในซีนได้)")]
    public GameObject levelPanel;
    public TMP_Text levelResultText;        // บรรทัดบน: ผ่าน / ไม่ผ่าน สีตาม passColor / failColor (ตอนนี้ขาวทั้งคู่ เท่าข้อความอื่น) (ด่านแรกซ่อน)
    public TMP_Text levelTitleText;
    public TMP_Text levelGoalText;
    public TMP_Text levelScoreText;
    public TMP_Text pressText;
    public Color passColor = Color.white;
    public Color failColor = Color.white;

    public Color normalColor = Color.white;
    public Color goalReachedColor = Color.black;

    public void ShowLevel(int level, int totalLevels)
    {
        levelText.text = $"Level {level} / {totalLevels}";
    }

    public void ShowScore(int exploded, int goal, int totalDots)
    {
        scoreText.text = $"{exploded} / {goal}   (dots: {totalDots})";
        scoreText.color = exploded >= goal ? goalReachedColor : normalColor;
    }

    int shownHealth = -1;

    public void ShowHealth(int health, int maxHealth)
    {
        if (hpText != null) hpText.text = $"HP {health} / {maxHealth}";
        if (heartContainer == null || heartContainer.childCount == 0) return;

        // หัวใจในซีนไม่พอ maxHealth -> copy ดวงแรกเพิ่ม
        while (heartContainer.childCount < maxHealth)
            Instantiate(heartContainer.GetChild(0).gameObject, heartContainer);

        for (int i = 0; i < heartContainer.childCount; i++)
        {
            Transform heart = heartContainer.GetChild(i);
            heart.gameObject.SetActive(i < maxHealth);
            heart.localScale = Vector3.one;
            if (heart.TryGetComponent(out Image image))
                image.sprite = i < health ? heartFull : heartEmpty;
        }

        // เพิ่งโดนชน -> ดวงที่เพิ่งว่างเด้ง
        if (health < shownHealth && health >= 0 && health < heartContainer.childCount)
            StartCoroutine(PopHeart(heartContainer.GetChild(health)));
        shownHealth = health;
    }

    // ใช้ unscaled time เพราะตอนโดนชนมี hit stop หยุดเวลา
    IEnumerator PopHeart(Transform heart)
    {
        for (float t = 0f; t < heartPopTime; t += Time.unscaledDeltaTime)
        {
            heart.localScale = Vector3.one * Mathf.Lerp(heartPopScale, 1f, t / heartPopTime);
            yield return null;
        }
        heart.localScale = Vector3.one;
    }

    public void ShowAmmo(int ammo)
    {
        ammoText.text = $"Ammo {ammo}";
    }

    public void ShowLevelPanel(string result, bool passed, int level, int goal, int totalDots, int score)
    {
        levelPanel.SetActive(true);
        levelResultText.text = result;
        levelResultText.color = passed ? passColor : failColor;
        levelTitleText.text = $"LEVEL {level}";
        levelGoalText.text = $"GOAL: {goal} OF {totalDots}";
        levelScoreText.text = $"SCORE: {score}";
        pressText.text = $"PRESS SPACE TO PLAY LEVEL {level}";
    }

    public void HideLevelPanel()
    {
        levelPanel.SetActive(false);
    }

    public void ShowGameEnd(int totalShots, int score)
    {
        levelPanel.SetActive(false);
        gameEndPanel.SetActive(true);
        gameEndText.text = $"Total Shots: {totalShots}";
        gameEndScoreText.text = $"Score: {score}";
    }

    public void HideGameEnd()
    {
        gameEndPanel.SetActive(false);
    }
}
