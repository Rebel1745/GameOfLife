using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LifeRenderer))]
public class LifeController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int _width = 50;
    [SerializeField] private int _height = 50;
    [SerializeField] private bool _isRunning = false;
    [SerializeField] private float _speed = 0.5f;

    // Systems
    private LifeData _data;
    private LifeRules _rules;
    private ITopology _topology;
    private LifeRenderer _renderer;

    // Timing
    private float _lastTime;
    private bool _isInitialized = false;

    // Input Events
    private void OnEnable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnSimulationToggled += ToggleSimulation;
            InputManager.Instance.OnRandomiseRequested += Randomise;
            InputManager.Instance.OnClearRequested += Clear;
            InputManager.Instance.OnStepRequested += Step;
            InputManager.Instance.OnCellLeftClicked += PaintAlive;
            InputManager.Instance.OnCellRightClicked += PaintDead;
        }
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnSimulationToggled -= ToggleSimulation;
            InputManager.Instance.OnRandomiseRequested -= Randomise;
            InputManager.Instance.OnClearRequested -= Clear;
            InputManager.Instance.OnCellLeftClicked -= PaintAlive;
            InputManager.Instance.OnCellRightClicked -= PaintDead;
        }
    }

    void Start()
    {
        InitializeSystems();
        Clear();
        _isInitialized = true;
    }

    void Update()
    {
        // Only run simulation loop if running
        if (_isRunning && Time.time - _lastTime >= _speed)
        {
            Step();
        }
    }

    private void InitializeSystems()
    {
        _data = new LifeData(_width, _height);
        _rules = new LifeRules();
        _topology = new SquareBoundedTopology(_width, _height);

        _renderer = GetComponent<LifeRenderer>();
        if (_renderer == null) _renderer = gameObject.AddComponent<LifeRenderer>();

        _renderer.Initialize(_data);
    }

    // --- Event Handlers ---

    private void ToggleSimulation()
    {
        _isRunning = !_isRunning;
        Debug.Log($"Simulation is now {(_isRunning ? "Running" : "Paused")}");
    }

    private void Randomise()
    {
        for (int i = 0; i < _data.Cells.Length; i++)
        {
            _data.Cells[i] = Random.value > 0.7f ? (byte)1 : (byte)0;
        }
        _renderer.Render(_data);
    }

    private void Clear()
    {
        for (int i = 0; i < _data.Cells.Length; i++)
        {
            _data.Cells[i] = (byte)0;
        }
        _renderer.Render(_data);

        _isRunning = false;
    }

    private void Step()
    {
        _rules.Step(_data, _topology);
        _renderer.Render(_data);
        _lastTime = Time.time;
    }

    private void PaintAlive(Vector2 mousePos) => Paint(mousePos, 1);
    private void PaintDead(Vector2 mousePos) => Paint(mousePos, 0);

    private void Paint(Vector2 mousePos, byte alive)
    {
        if (!_isInitialized) return;

        // Convert Screen Mouse Pos to World Pos
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0));

        // Calculate Grid Coordinates (assuming Sprite centered at 0,0)
        // Adjust cell size logic to match your renderer
        float cellSize = _renderer.CellSize;

        int gx = Mathf.FloorToInt((worldPos.x * cellSize) + (_width / 2.0f));
        int gy = Mathf.FloorToInt((worldPos.y * cellSize) + (_height / 2.0f));

        if (_topology.Contains(gx, gy))
        {
            int index = _data.GetIndex(gx, gy);
            _data.Cells[index] = alive;
            _renderer.Render(_data);
        }
    }
}