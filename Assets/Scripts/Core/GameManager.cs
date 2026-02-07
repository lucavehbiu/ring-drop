using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Central game state machine. Controls flow from menu to gameplay to game over.
/// Singleton — access via GameManager.Instance.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Menu, Countdown, Playing, Success, Fail, GameOver }

    [Header("State")]
    public GameState State { get; private set; } = GameState.Menu;

    [Header("References")]
    [SerializeField] private RingController ring;
    [SerializeField] private StickController stick;
    [SerializeField] private CameraFollow cam;

    // Events for UI to listen to
    public UnityEvent<GameState> OnStateChanged;
    public UnityEvent<int> OnScoreChanged;
    public UnityEvent<int> OnLevelChanged;

    private int _score;
    private int _level = 1;
    private int _combo;
    private int _highScore;
    private float _countdownTimer;
    private float _successTimer;
    private float _failTimer;

    public int Score => _score;
    public int Level => _level;
    public int Combo => _combo;
    public int HighScore => _highScore;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _highScore = PlayerPrefs.GetInt("HighScore", 0);
    }

    public void StartGame()
    {
        _score = 0;
        _level = 1;
        _combo = 0;
        OnScoreChanged?.Invoke(_score);
        OnLevelChanged?.Invoke(_level);
        AnnounceLevel();
    }

    public void AnnounceLevel()
    {
        var cfg = LevelConfig.Get(_level);
        stick.Setup(cfg);
        ring.Setup(cfg);
        cam.Reset();

        _countdownTimer = 0f;
        SetState(GameState.Countdown);
    }

    private void Update()
    {
        switch (State)
        {
            case GameState.Countdown:
                _countdownTimer += Time.deltaTime;
                if (_countdownTimer >= 1.2f)
                    SetState(GameState.Playing);
                break;

            case GameState.Success:
                _successTimer += Time.deltaTime;
                if (_successTimer >= 1.5f)
                {
                    _level++;
                    OnLevelChanged?.Invoke(_level);
                    AnnounceLevel();
                }
                break;

            case GameState.Fail:
                _failTimer += Time.deltaTime;
                if (_failTimer >= 1f)
                    SetState(GameState.GameOver);
                break;
        }
    }

    public void OnRingReachedStick(bool aligned)
    {
        if (State != GameState.Playing) return;

        if (aligned)
        {
            _combo++;
            int pts = 100 + (_combo - 1) * 50 + (_level - 1) * 30;
            _score += pts;
            if (_score > _highScore)
            {
                _highScore = _score;
                PlayerPrefs.SetInt("HighScore", _highScore);
            }
            OnScoreChanged?.Invoke(_score);
            _successTimer = 0f;
            SetState(GameState.Success);
        }
        else
        {
            OnFail("miss");
        }
    }

    public void OnFail(string reason)
    {
        if (State != GameState.Playing) return;
        _combo = 0;
        _failTimer = 0f;
        SetState(GameState.Fail);
    }

    private void SetState(GameState newState)
    {
        State = newState;
        OnStateChanged?.Invoke(newState);
    }
}
