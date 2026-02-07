using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Camera system using Cinemachine virtual cameras.
/// Creates virtual cameras for each game state and switches between them
/// via priority. Cinemachine Brain on main camera handles blending.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private RingController ring;
    [SerializeField] private StickController stick;

    private CinemachineCamera _playingCam;
    private CinemachineCamera _menuCam;
    private CinemachineCamera _successCam;
    private Camera _mainCam;

    private float _menuTime;

    private void Awake()
    {
        _mainCam = GetComponent<Camera>();
        if (_mainCam == null) _mainCam = Camera.main;
    }

    private void Start()
    {
        if (ring == null) ring = FindAnyObjectByType<RingController>();
        if (stick == null) stick = FindAnyObjectByType<StickController>();
    }

    /// <summary>Create all Cinemachine virtual cameras. Called from SceneBootstrap.</summary>
    public void SetupCinemachine(Transform ringTransform, Transform stickTransform)
    {
        // === PLAYING CAMERA — follows the ring from behind and above ===
        var playObj = new GameObject("CM_Playing");
        _playingCam = playObj.AddComponent<CinemachineCamera>();
        _playingCam.Follow = ringTransform;
        _playingCam.LookAt = ringTransform;

        // Set lens
        var playLens = _playingCam.Lens;
        playLens.FieldOfView = Constants.BASE_FOV;
        playLens.NearClipPlane = 0.1f;
        playLens.FarClipPlane = 500f;
        _playingCam.Lens = playLens;

        // Follow behavior — position behind and above
        var follow = playObj.AddComponent<CinemachineFollow>();
        follow.FollowOffset = new Vector3(0f, 2.5f, 7f);

        // Rotation composer for smooth aim
        var composer = playObj.AddComponent<CinemachineRotationComposer>();

        _playingCam.Priority = 0;

        // === MENU CAMERA — static position, looking at scene ===
        var menuObj = new GameObject("CM_Menu");
        _menuCam = menuObj.AddComponent<CinemachineCamera>();

        var menuLens = _menuCam.Lens;
        menuLens.FieldOfView = Constants.BASE_FOV;
        menuLens.NearClipPlane = 0.1f;
        menuLens.FarClipPlane = 500f;
        _menuCam.Lens = menuLens;

        menuObj.transform.position = new Vector3(0f, 5.5f, 10f);
        menuObj.transform.LookAt(new Vector3(0f, 3f, -10f));
        _menuCam.Priority = 10;

        // === SUCCESS CAMERA — diagonal angle on stick/ring ===
        var successObj = new GameObject("CM_Success");
        _successCam = successObj.AddComponent<CinemachineCamera>();
        _successCam.Follow = stickTransform;
        _successCam.LookAt = stickTransform;

        var successLens = _successCam.Lens;
        successLens.FieldOfView = Constants.BASE_FOV;
        successLens.NearClipPlane = 0.1f;
        successLens.FarClipPlane = 500f;
        _successCam.Lens = successLens;

        // Follow with diagonal offset
        var successFollow = successObj.AddComponent<CinemachineFollow>();
        successFollow.FollowOffset = new Vector3(3f, 3f, 3f);

        _successCam.Priority = 0;
    }

    /// <summary>Switch active camera based on game state.</summary>
    public void OnStateChanged(GameManager.GameState state)
    {
        if (_menuCam != null) _menuCam.Priority = 0;
        if (_playingCam != null) _playingCam.Priority = 0;
        if (_successCam != null) _successCam.Priority = 0;

        switch (state)
        {
            case GameManager.GameState.Menu:
                if (_menuCam != null) _menuCam.Priority = 10;
                break;

            case GameManager.GameState.Countdown:
            case GameManager.GameState.Playing:
            case GameManager.GameState.Fail:
                if (_playingCam != null) _playingCam.Priority = 10;
                break;

            case GameManager.GameState.Success:
                if (_successCam != null) _successCam.Priority = 10;
                break;

            case GameManager.GameState.GameOver:
                break;
        }
    }

    private void LateUpdate()
    {
        if (GameManager.Instance == null) return;

        // Menu camera gentle float
        if (GameManager.Instance.State == GameManager.GameState.Menu && _menuCam != null)
        {
            _menuTime += Time.deltaTime;
            _menuCam.transform.position = new Vector3(
                Mathf.Sin(_menuTime * 0.3f) * 0.5f,
                5.5f + Mathf.Sin(_menuTime * 1f) * 0.3f,
                10f
            );
            _menuCam.transform.LookAt(new Vector3(0f, 3f, -10f));
        }
    }

    public void Reset()
    {
        // Cinemachine handles everything, nothing to reset
    }
}
