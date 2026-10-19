using TMPro;
using UnityEngine;

public class UiManager : MonoBehaviour
{
    public TMP_Text levelText;
    public TMP_Text scoreText;
    public TMP_Text hpText;
    public TMP_Text ammoText;
    public GameObject messagePanel;
    public TMP_Text messageText;

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

    public void ShowMessage(string message)
    {
        messagePanel.SetActive(true);
        messageText.text = message;
    }

    public void HideMessage()
    {
        messagePanel.SetActive(false);
    }
}
