using UnityEngine;

/// <summary>
/// Minimal UI using OnGUI — shows score, level, state feedback, threading timer.
/// Quick and dependency-free. Will upgrade to Canvas UI later.
/// </summary>
public class UIManager : MonoBehaviour
{
    private GUIStyle _titleStyle;
    private GUIStyle _scoreStyle;
    private GUIStyle _subtitleStyle;
    private GUIStyle _smallStyle;
    private GUIStyle _timerStyle;
    private float _feedbackAlpha;
    private string _feedbackText = "";
    private float _feedbackScale = 1f;
    private float _feedbackTime;
    private bool _stylesReady;

    private void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged.AddListener(OnStateChanged);
    }

    private void InitStyles()
    {
        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 72,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        _titleStyle.normal.textColor = Constants.CYAN;

        _scoreStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 36,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperLeft
        };
        _scoreStyle.normal.textColor = Color.white;

        _subtitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 28,
            alignment = TextAnchor.MiddleCenter
        };
        _subtitleStyle.normal.textColor = new Color(1f, 1f, 1f, 0.7f);

        _smallStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            alignment = TextAnchor.UpperRight
        };
        _smallStyle.normal.textColor = new Color(1f, 1f, 1f, 0.5f);

        _timerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 96,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        _timerStyle.normal.textColor = Constants.GREEN;

        _stylesReady = true;
    }

    private void OnStateChanged(GameManager.GameState state)
    {
        switch (state)
        {
            case GameManager.GameState.Countdown:
                ShowFeedback("GET READY", Constants.CYAN);
                break;
            case GameManager.GameState.Playing:
                ShowFeedback("GO!", Constants.GREEN);
                break;
            case GameManager.GameState.Threading:
                ShowFeedback("DROP IT!", Constants.GOLD);
                break;
            case GameManager.GameState.Success:
                string[] cheers = { "GOOD JOB!", "PERFECT!", "NICE!", "AMAZING!", "SMOOTH!" };
                ShowFeedback(cheers[Random.Range(0, cheers.Length)], Constants.GOLD);
                break;
            case GameManager.GameState.Fail:
                ShowFeedback("MISS!", Constants.RED);
                break;
            case GameManager.GameState.GameOver:
                ShowFeedback("GAME OVER", Constants.MAGENTA);
                break;
        }
    }

    private void ShowFeedback(string text, Color color)
    {
        _feedbackText = text;
        _feedbackAlpha = 1f;
        _feedbackScale = 1.5f;
        _feedbackTime = 0f;
        _titleStyle.normal.textColor = color;
    }

    private void Update()
    {
        // Animate feedback text
        if (_feedbackAlpha > 0f)
        {
            _feedbackTime += Time.deltaTime;
            // Scale bounce: start big, settle to 1x
            _feedbackScale = 1f + Mathf.Exp(-_feedbackTime * 4f) * 0.5f;
            // Fade out after 1.5s
            if (_feedbackTime > 1.5f)
                _feedbackAlpha = Mathf.Max(0f, _feedbackAlpha - Time.deltaTime * 1.5f);
        }
    }

    private void OnGUI()
    {
        if (!_stylesReady) InitStyles();

        var gm = GameManager.Instance;
        if (gm == null) return;

        float w = Screen.width;
        float h = Screen.height;

        // Score (top-left)
        if (gm.State != GameManager.GameState.Menu)
        {
            GUI.Label(new Rect(20, 15, 300, 50), $"SCORE  {gm.Score}", _scoreStyle);
        }

        // Level (top-right)
        if (gm.State != GameManager.GameState.Menu)
        {
            GUI.Label(new Rect(w - 220, 15, 200, 40), $"LEVEL {gm.Level}", _smallStyle);
            if (gm.Combo > 1)
                GUI.Label(new Rect(w - 220, 50, 200, 40), $"x{gm.Combo} COMBO", _smallStyle);
        }

        // High score (top-right, below level)
        if (gm.HighScore > 0 && gm.State == GameManager.GameState.Menu)
        {
            _smallStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(0, h * 0.55f, w, 40), $"HIGH SCORE: {gm.HighScore}", _smallStyle);
            _smallStyle.alignment = TextAnchor.UpperRight;
        }

        // --- Threading countdown timer ---
        if (gm.State == GameManager.GameState.Threading)
        {
            float timeLeft = gm.ThreadingTimeLeft;

            // Color: green → gold → red as time runs out
            Color timerColor;
            if (timeLeft > 2f)
                timerColor = Constants.GREEN;
            else if (timeLeft > 1f)
                timerColor = Constants.GOLD;
            else
                timerColor = Constants.RED;

            // Pulse effect when low
            float pulse = 1f;
            if (timeLeft < 1.5f)
                pulse = 1f + Mathf.Sin(Time.time * 12f) * 0.15f;

            _timerStyle.fontSize = Mathf.RoundToInt(96 * pulse);
            _timerStyle.normal.textColor = timerColor;

            // Big countdown number at top
            GUI.Label(new Rect(0, h * 0.08f, w, 120), $"{timeLeft:F1}", _timerStyle);

            // Alignment guide — show how far off center
            float ringX = FindAnyObjectByType<RingController>()?.transform.position.x ?? 0f;
            float stickX = gm.Stick != null ? gm.Stick.transform.position.x : 0f;
            float offset = ringX - stickX;

            // Arrow indicator: ← CENTER → or ✓ when aligned
            string alignText;
            Color alignColor;
            float tolerance = LevelConfig.Get(gm.Level).tolerance;

            if (Mathf.Abs(offset) < tolerance)
            {
                alignText = "ALIGNED";
                alignColor = Constants.GREEN;
            }
            else if (offset < 0)
            {
                alignText = "STEER RIGHT >>>";
                alignColor = Constants.GOLD;
            }
            else
            {
                alignText = "<<< STEER LEFT";
                alignColor = Constants.GOLD;
            }

            _subtitleStyle.normal.textColor = alignColor;
            GUI.Label(new Rect(0, h * 0.85f, w, 50), alignText, _subtitleStyle);
        }

        // Center feedback text (animated)
        if (_feedbackAlpha > 0.01f)
        {
            Color c = _titleStyle.normal.textColor;
            c.a = _feedbackAlpha;
            _titleStyle.normal.textColor = c;

            // Scale effect via font size
            int baseSize = 72;
            _titleStyle.fontSize = Mathf.RoundToInt(baseSize * _feedbackScale);

            GUI.Label(new Rect(0, h * 0.3f, w, 120), _feedbackText, _titleStyle);
        }

        // Menu prompt
        if (gm.State == GameManager.GameState.Menu)
        {
            _subtitleStyle.normal.textColor = new Color(1f, 1f, 1f, 0.5f + Mathf.Sin(Time.time * 3f) * 0.3f);
            GUI.Label(new Rect(0, h * 0.65f, w, 50), "TAP OR PRESS SPACE TO START", _subtitleStyle);
        }

        // Game over prompt
        if (gm.State == GameManager.GameState.GameOver)
        {
            _subtitleStyle.normal.textColor = new Color(1f, 1f, 1f, 0.5f + Mathf.Sin(Time.time * 3f) * 0.3f);
            GUI.Label(new Rect(0, h * 0.55f, w, 50), $"SCORE: {gm.Score}", _subtitleStyle);
            GUI.Label(new Rect(0, h * 0.62f, w, 50), "TAP TO RETRY", _subtitleStyle);
        }
    }
}
