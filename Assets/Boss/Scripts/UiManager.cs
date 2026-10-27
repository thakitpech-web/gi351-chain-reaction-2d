using TMPro;
using UnityEngine;

public class UiManager : MonoBehaviour
{
    public TMP_Text levelText;
    public TMP_Text scoreText;
    public TMP_Text hpText;
    public TMP_Text ammoText;
    public GameObject gameEndPanel;
    public TMP_Text gameEndText;
    public TMP_Text gameEndScoreText;

    [Header("Level Panel (ขึ้นตอนจบด่าน แต่ละบรรทัดแยก text ไว้ จัดตำแหน่ง/ฟอนต์ในซีนได้)")]
    public GameObject levelPanel;
    public TMP_Text levelResultText;        // บรรทัดบน: ผ่าน = เขียว / ไม่ผ่าน = แดง (ด่านแรกซ่อน)
    public TMP_Text levelTitleText;
    public TMP_Text levelGoalText;
    public TMP_Text levelScoreText;
    public TMP_Text pressText;
    public Color passColor = new Color(0f, 0.894f, 0.212f);
    public Color failColor = new Color(1f, 0f, 0.302f);

    public Color normalColor = Color.white;
    public Color goalReachedColor = new Color(0.4f, 1f, 0.5f);

    public void ShowLevel(int level, int totalLevels)
    {
        levelText.text = $"Level {level} / {totalLevels}";
    }

    public void ShowScore(int exploded, int goal, int totalDots)
    {
        scoreText.text = $"{exploded} / {goal}   (dots: {totalDots})";
        scoreText.color = exploded >= goal ? goalReachedColor : normalColor;
    }

    public void ShowHealth(int health, int maxHealth)
    {
        hpText.text = $"HP {health} / {maxHealth}";
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
