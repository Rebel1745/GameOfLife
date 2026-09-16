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
    public int GridPadding = 10; // Space between universes
    [SerializeField] private Color32 _aliveColour = Color.white;
    [SerializeField] private Color32 _deadColour = Color.black;
    [SerializeField] private Transform _uiControlPanel;

    private Camera _mainCamera;
    private bool _isRunning = false;
    private float _lastTime;
    private int _lastScreenWidth;
    private int _lastScreenHeight;
    private float _uiPanelWidthRatio;


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
        RecalculateUIRatio();
    }

    void Start()
    {
        // listen for a mouse click
        InputManager.Instance.OnCellLeftClicked += OnLeftClick;

        AddSimulation("Universe 1", "B3/S23");
        UpdateLayout();
    }

    void Update()
    {
        if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
        {
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;

            RecalculateUIRatio();
            UpdateLayout();
        }

        if (_isRunning && Time.time - _lastTime >= stepSpeed)
        {
            foreach (var sim in _simulations)
            {
                sim.Step();
            }
            _lastTime = Time.time;
        }
    }

    private void RecalculateUIRatio()
    {
        if (_uiControlPanel != null)
        {
            RectTransform rect = _uiControlPanel.GetComponent<RectTransform>();
            // Get width in pixels relative to screen
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            float panelPixelWidth = Mathf.Abs(corners[2].x - corners[0].x); // canvas-space
            _uiPanelWidthRatio = panelPixelWidth / Screen.width;
        }
    }

    // --- Public API ---

    public void AddSimulation(string name, string ruleString)
    {
        if (string.IsNullOrWhiteSpace(name)) name = "Universe " + (_simulations.Count + 1);

        var newSim = new SimulationInstance(_simulations.Count, name, defaultWidth, defaultHeight, ruleString, _aliveColour, _deadColour);
        _simulations.Add(newSim);

        // Create a GameObject for this simulation to hold the SpriteRenderer
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(transform);
        newSim.SpriteRenderer = obj.AddComponent<SpriteRenderer>();
        newSim.SpriteRenderer.sprite = newSim.Sprite;
        newSim.SpriteRenderer.sortingOrder = 1; // Below UI

        // Set initial position
        newSim.SpriteRenderer.transform.localPosition = Vector3.zero;

        SetActive(_simulations.Count - 1);
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
            SetActive(index - 1);
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
        {
            _simulations[ActiveIndex].Randomise();
        }
    }

    public void RandomiseAllDifferent()
    {
        _isRunning = false;

        foreach (SimulationInstance sim in _simulations)
            sim.Randomise();
    }

    public void RandomiseAllTheSame()
    {
        if (_simulations.Count == 0 || ActiveIndex > _simulations.Count) return;

        _isRunning = false;

        _simulations[ActiveIndex].Randomise();

        byte[] randomed = _simulations[ActiveIndex].GetCells();

        foreach (SimulationInstance sim in _simulations)
            sim.SetAllCells(randomed);

    }

    public void ClearActive()
    {
        if (ActiveIndex >= 0 && ActiveIndex < _simulations.Count)
            _simulations[ActiveIndex].Clear();
    }

    public void ClearAll()
    {
        _isRunning = false;

        foreach (SimulationInstance sim in _simulations)
            sim.Clear();
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

        // --- 1. Calculate Grid Dimensions (Content + Gaps) ---
        int columns = Mathf.CeilToInt(Mathf.Sqrt(_simulations.Count));
        int rows = Mathf.CeilToInt((float)_simulations.Count / columns);

        int simW = _simulations[0].Data.Width + GridPadding * 2;
        int simH = _simulations[0].Data.Height + GridPadding * 2;

        float totalWidth = columns * simW;
        float totalHeight = rows * simH;

        // --- 2. Calculate Camera Zoom (Fit into Effective Viewport) ---

        // We calculate zoom based on the usable space (excluding UI)
        float effectiveAspect = _mainCamera.aspect * (1.0f - _uiPanelWidthRatio);

        float camSizeForHeight = totalHeight / 2.0f;
        float camSizeForWidth = (totalWidth / effectiveAspect) / 2.0f;

        float targetOrthoSize = Mathf.Max(camSizeForHeight, camSizeForWidth) * 1.05f;

        // Apply zoom
        _mainCamera.orthographicSize = targetOrthoSize;

        // Keep camera at screen center (no X shift)
        _mainCamera.transform.position = new Vector3(0, 0, _mainCamera.transform.position.z);

        // --- 3. Calculate Layout Offset (Shift Universes LEFT) ---

        // The visible area is narrower than the screen. 
        // The center of the visible area is shifted LEFT by half the UI width.
        // We must move the entire grid to align with this new center.

        float visibleWorldWidth = targetOrthoSize * 2.0f * _mainCamera.aspect;
        float uiWorldWidth = visibleWorldWidth * _uiPanelWidthRatio;
        float layoutOffsetX = -uiWorldWidth / 2.0f; // Shift left

        // Start position for the grid (relative to the new effective center)
        float startX = (-totalWidth / 2.0f) + (simW / 2.0f) + layoutOffsetX;
        float startY = (-totalHeight / 2.0f) + (simH / 2.0f);

        // --- 4. Position Universes ---
        int index = 0;
        foreach (var sim in _simulations)
        {
            int col = index % columns;
            int row = index / columns;

            float x = startX + (col * simW);
            float y = startY + (row * simH);

            sim.SpriteRenderer.transform.localPosition = new Vector3(x, y, 0);
            index++;
        }
    }

    // --- Interaction ---

    private void OnLeftClick(Vector2 mousePos)
    {
        SetActive(GetSimulationAtMouse(mousePos));
    }

    public int GetSimulationAtMouse(Vector2 screenPos)
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
                    return sim.Id;
                }
            }
        }
        return -1;
    }

    void OnDrawGizmosSelected()
    {
        if (_mainCamera == null) return;

        Gizmos.color = Color.cyan;
        float halfH = _mainCamera.orthographicSize;
        float halfW = halfH * _mainCamera.aspect;

        // Effective viewport (excluding UI panel)
        Gizmos.color = Color.green;
        float effHalfW = halfW * (1.0f - _uiPanelWidthRatio);
        Vector3 effCenter = _mainCamera.transform.position
                          - new Vector3(halfW * _uiPanelWidthRatio, 0, 0);
        Gizmos.DrawWireCube(effCenter, new Vector3(effHalfW * 2, halfH * 2, 0));
    }
}