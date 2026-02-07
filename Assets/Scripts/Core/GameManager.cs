using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Central game state machine. Controls flow from menu to gameplay to game over.
/// Singleton — access via GameManager.Instance.
///
/// Flow: Menu → Countdown → Playing → Success/Fail → GameOver
/// Ring uses Rigidbody physics. Success/fail detected via OnCollisionEnter in RingController.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Menu, Countdown, Playing, Threading, Success, Fail, GameOver }

    [Header("State")]
    public GameState State { get; private set; } = GameState.Menu;

    [Header("References")]
    [SerializeField] private RingController ring;
    [SerializeField] private StickController stick;
    [SerializeField] private CameraFollow cam;

    public UnityEvent<GameState> OnStateChanged = new UnityEvent<GameState>();
    public UnityEvent<int> OnScoreChanged = new UnityEvent<int>();
    public UnityEvent<int> OnLevelChanged = new UnityEvent<int>();

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

    public StickController Stick => stick;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _highScore = PlayerPrefs.GetInt("HighScore", 0);
    }

    private void Start()
    {
        if (ring == null) ring = FindAnyObjectByType<RingController>();
        if (stick == null) stick = FindAnyObjectByType<StickController>();
        if (cam == null) cam = FindAnyObjectByType<CameraFollow>();

        if (ring == null || stick == null || cam == null)
            Debug.LogError("[RingDrop] GameManager missing references!");
        else
            Debug.Log("[RingDrop] Ready. Press Space or Click to start.");

        // Wire up camera state switching
        OnStateChanged.AddListener(state => cam?.OnStateChanged(state));

        // Ring starts frozen in menu
        ring?.Freeze();
    }

    private bool StartInputPressed()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.spaceKey.wasPressedThisFrame) return true;

        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;

        var ts = Touchscreen.current;
        if (ts != null && ts.primaryTouch.press.wasPressedThisFrame) return true;

        return false;
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
        ring.Freeze(); // frozen during countdown
        cam.Reset();

        _countdownTimer = 0f;
        SetState(GameState.Countdown);
    }

    private void Update()
    {
        switch (State)
        {
            case GameState.Menu:
                if (StartInputPressed())
                    StartGame();
                break;

            case GameState.Countdown:
                _countdownTimer += Time.deltaTime;
                if (_countdownTimer >= 2.5f)
                {
                    ring.Unfreeze(); // enable physics
                    SetState(GameState.Playing);
                }
                break;

            case GameState.Success:
                _successTimer += Time.deltaTime;
                if (_successTimer >= 2.5f)
                {
                    _level++;
                    OnLevelChanged?.Invoke(_level);
                    AnnounceLevel();
                }
                break;

            case GameState.Fail:
                _failTimer += Time.deltaTime;
                if (_failTimer >= 2.5f)
                    SetState(GameState.GameOver);
                break;

            case GameState.GameOver:
                if (StartInputPressed())
                    StartGame();
                break;
        }
    }

    public void OnSuccess()
    {
        if (State != GameState.Playing) return;
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
        ring.BeginSuccessAnimation(stick.transform.position);
        SFXManager.Instance?.PlaySuccess();
    }

    public void OnFail(string reason)
    {
        if (State != GameState.Playing) return;
        _combo = 0;
        _failTimer = 0f;
        Debug.Log($"[RingDrop] Fail reason: {reason}");
        SetState(GameState.Fail);
        ring.BeginFailAnimation();
        SFXManager.Instance?.PlayFail();
    }

    private void SetState(GameState newState)
    {
        State = newState;
        Debug.Log($"[RingDrop] State -> {newState}");
        OnStateChanged?.Invoke(newState);
    }
}
