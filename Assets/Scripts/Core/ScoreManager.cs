using UnityEngine;

/// <summary>
/// Handles score persistence. Thin wrapper — GameManager owns the logic,
/// this just provides save/load if we need it standalone later.
/// </summary>
public static class ScoreManager
{
    private const string HIGH_SCORE_KEY = "HighScore";

    public static int LoadHighScore()
    {
        return PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
    }

    public static void SaveHighScore(int score)
    {
        if (score > LoadHighScore())
        {
            PlayerPrefs.SetInt(HIGH_SCORE_KEY, score);
            PlayerPrefs.Save();
        }
    }
}
