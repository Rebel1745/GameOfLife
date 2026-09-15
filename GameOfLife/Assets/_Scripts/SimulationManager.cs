using UnityEngine;
using System.Collections.Generic;

public class SimulationManager : MonoBehaviour
{
    [Header("Defaults")]
    public int defaultWidth = 50;
    public int defaultHeight = 50;
    public float stepSpeed = 0.5f;

    public static SimulationManager Instance;

    private List<SimulationInstance> _simulations = new List<SimulationInstance>();
    public int ActiveIndex = 0;

    // Layout settings
    public int Columns = 2;
    public int Rows = 2;
    public int GridPadding = 10; // Space between universes
    public Vector2 GridOffset = new Vector2(0, 0); // Center the grid
    [SerializeField] private Color32 _aliveColour = Color.white;
    [SerializeField] private Color32 _deadColour = Color.black;

    private Camera _mainCamera;
    private bool _isRunning = false;
    private float _lastTime;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        _mainCamera = Camera.main;
        if (_mainCamera == null) _mainCamera = Camera.main;
        _mainCamera.orthographic = true;
    }

    void Start()
    {
        AddSimulation("Universe 1", "B3/S23");
        SetActive(0);
        UpdateLayout();
    }

    void Update()
    {
        if (_isRunning && Time.time - _lastTime >= stepSpeed)
        {
            foreach (var sim in _simulations)
            {
                sim.Step();
            }
            _lastTime = Time.time;
        }
    }

    // --- Public API ---

    public void AddSimulation(string name, string ruleString)
    {
        var newSim = new SimulationInstance(name, defaultWidth, defaultHeight, ruleString, _aliveColour, _deadColour);
        _simulations.Add(newSim);

        // Create a GameObject for this simulation to hold the SpriteRenderer
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(transform);
        newSim.SpriteRenderer = obj.AddComponent<SpriteRenderer>();
        newSim.SpriteRenderer.sprite = newSim.Sprite;
        newSim.SpriteRenderer.sortingOrder = 1; // Below UI

        // Set initial position
        newSim.SpriteRenderer.transform.localPosition = Vector3.zero;

        ActiveIndex = _simulations.Count - 1;
        UpdateLayout();
    }

    public void RemoveSimulation(int index)
    {
        if (index < 0 || index >= _simulations.Count) return;

        // Destroy the GameObject
        if (_simulations[index].SpriteRenderer != null)
        {
            Destroy(_simulations[index].SpriteRenderer.gameObject);
        }

        _simulations.RemoveAt(index);

        if (_simulations.Count == 0)
        {
            AddSimulation("Empty", "B3/S23");
        }
        else if (ActiveIndex >= _simulations.Count)
        {
            ActiveIndex = _simulations.Count - 1;
        }

        UpdateLayout();
    }

    public void SetActive(int index)
    {
        if (index < 0 || index >= _simulations.Count) return;

        // Deselect previous
        if (ActiveIndex >= 0 && ActiveIndex < _simulations.Count)
        {
            _simulations[ActiveIndex].IsSelected = false;
            _simulations[ActiveIndex].Render(); // Re-render to remove border
        }

        // Select new
        ActiveIndex = index;
        _simulations[ActiveIndex].IsSelected = true;
        _simulations[ActiveIndex].Render(); // Re-render to add border
    }

    public void ToggleRunning()
    {
        _isRunning = !_isRunning;
    }

    public void StepOnce()
    {
        foreach (var sim in _simulations) sim.Step();
    }

    public void RandomiseActive()
    {
        if (ActiveIndex >= 0 && ActiveIndex < _simulations.Count)
            _simulations[ActiveIndex].Randomise();
    }

    public void ClearActive()
    {
        if (ActiveIndex >= 0 && ActiveIndex < _simulations.Count)
            _simulations[ActiveIndex].Clear();
    }

    public void UpdateRuleForActive(string newRule)
    {
        if (ActiveIndex >= 0 && ActiveIndex < _simulations.Count)
        {
            _simulations[ActiveIndex].RuleConfig.RuleString = newRule;
            _simulations[ActiveIndex].RuleConfig.Parse();
        }
    }

    // --- Layout & Camera ---

    private void UpdateLayout()
    {
        if (_simulations.Count == 0) return;

        Columns = Mathf.CeilToInt(Mathf.Sqrt(_simulations.Count));
        Rows = Mathf.CeilToInt((float)_simulations.Count / Columns);

        // Calculate total grid size
        int simW = _simulations[0].TextureWidth + GridPadding;
        int simH = _simulations[0].TextureHeight + GridPadding;

        float totalWidth = (Columns * simW) - GridPadding;
        float totalHeight = (Rows * simH) - GridPadding;

        // Center the grid
        float startX = (-totalWidth / 2.0f) + (simW / 2.0f) - GridOffset.x;
        float startY = (-totalHeight / 2.0f) + (simH / 2.0f) - GridOffset.y;

        int index = 0;
        foreach (var sim in _simulations)
        {
            int col = index % Columns;
            int row = index / Columns;

            float x = startX + (col * simW);
            float y = startY + (row * simH);

            sim.SpriteRenderer.transform.localPosition = new Vector3(x, y, 0);

            // Update Camera to fit the whole grid
            float aspect = totalWidth / totalHeight;
            float camH = totalHeight / 2.0f + 1.0f; // Add 1 unit buffer
            float camW = camH * aspect;

            _mainCamera.orthographicSize = camH;
            _mainCamera.transform.position = new Vector3(0, 0, _mainCamera.transform.position.z);

            index++;
        }
    }

    // --- Interaction ---

    public SimulationInstance GetSimulationAtMouse(Vector2 screenPos)
    {
        // Get World Position
        Vector3 worldPos = _mainCamera.ScreenToWorldPoint(screenPos);

        // Iterate backwards (top to bottom) to catch clicks on top-most items
        for (int i = _simulations.Count - 1; i >= 0; i--)
        {
            var sim = _simulations[i];
            Vector3 localPos = sim.SpriteRenderer.transform.localPosition;

            // Check bounds
            float halfW = sim.TextureWidth / 2.0f;
            float halfH = sim.TextureHeight / 2.0f;

            if (worldPos.x >= localPos.x - halfW && worldPos.x <= localPos.x + halfW &&
                worldPos.y >= localPos.y - halfH && worldPos.y <= localPos.y + halfH)
            {
                // Inside this universe
                // Convert to local grid coordinates
                float relX = worldPos.x - localPos.x;
                float relY = worldPos.y - localPos.y;

                // Map to grid (0 to Width-1)
                int gx = Mathf.FloorToInt((relX + halfW) / sim.TextureWidth * sim.Data.Width);
                int gy = Mathf.FloorToInt((relY + halfH) / sim.TextureHeight * sim.Data.Height);

                // Flip Y for texture coordinate
                gy = sim.Data.Height - 1 - gy;

                if (sim.Topology.Contains(gx, gy))
                {
                    return sim;
                }
            }
        }
        return null;
    }

    public bool IsRunning => _isRunning;
}