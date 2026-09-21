using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;

public class SimulationManager : MonoBehaviour
{
    public static SimulationManager Instance;

    public event Action<int> OnActiveSimulationChanged;

    [Header("Defaults")]
    [SerializeField] private int _defaultWidth = 50;
    [SerializeField] private int _defaultHeight = 50;
    [SerializeField] private float _stepSpeed = 0.5f;

    // Layout settings
    [SerializeField] private int _gridPaddingX = 5;
    [SerializeField] private int _gridPaddingY = 10;
    [SerializeField] private Color32 _aliveColour = Color.white;
    [SerializeField] private Color32 _deadColour = Color.black;
    [SerializeField] private Color32 _activeBorderColour = Color.yellow;
    [SerializeField] private Transform _uiControlPanel;

    private List<SimulationInstance> _simulations = new List<SimulationInstance>();
    private int _activeIndex = -1;
    public int ActiveIndex => _activeIndex;

    private Camera _mainCamera;
    private float _lastTime;
    private int _lastScreenWidth;
    private int _lastScreenHeight;
    private float _uiPanelWidthRatio;
    private float _singleSimOrthographicSize;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        _mainCamera = Camera.main;
        if (_mainCamera == null) _mainCamera = Camera.main;
        _mainCamera.orthographic = true;
        RecalculateUIRatio();
    }

    private void Start()
    {
        // listen for a mouse click
        InputManager.Instance.OnCellLeftClicked += OnLeftClick;
        InputManager.Instance.OnCellRightClicked += OnRightClick;

        StartCoroutine(AddInitialUniverse());
    }

    private void Update()
    {
        if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
        {
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;

            RecalculateUIRatio();
            UpdateLayout();
        }

        if (Time.time - _lastTime >= _stepSpeed)
        {
            foreach (var sim in _simulations)
            {
                sim.Step();
            }
            _lastTime = Time.time;
        }
    }

    private void OnDestroy()
    {
        InputManager.Instance.OnCellLeftClicked -= OnLeftClick;
        InputManager.Instance.OnCellRightClicked -= OnRightClick;
    }

    private IEnumerator AddInitialUniverse()
    {
        yield return new WaitForSeconds(0.5f);

        AddSimulation("Universe 1", "B3/S23");
        UpdateLayout();
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

        var newSim = new SimulationInstance(_simulations.Count, name, _defaultWidth, _defaultHeight, ruleString, _aliveColour, _deadColour, _activeBorderColour, _gridPaddingY);
        _simulations.Add(newSim);
        //new Vector3(0, (_height / 2) + _borderWidth + (_gridSpacingY / 2f), 0)
        newSim.InitialiseSpriteRenderer(transform);
        newSim.InitialiseLabel();

        SetActive(_simulations.Count - 1);
        UpdateLayout();
    }

    public void RemoveSimulation(int index)
    {
        if (index < 0 || index >= _simulations.Count) return;

        _activeIndex = -1;

        // Destroy the GameObject
        if (_simulations[index].SpriteRenderer != null)
        {
            Destroy(_simulations[index].SpriteRenderer.gameObject);
        }

        _simulations.RemoveAt(index);

        if (_simulations.Count == 0)
        {
            AddSimulation("Universe 1", "B3/S23");
        }
        else if (_activeIndex <= _simulations.Count)
        {
            SetActive(index - 1);
        }

        UpdateLayout();
    }

    public void UpdateSimulation(string name, string rules)
    {
        _simulations[_activeIndex].SetName(name);
        _simulations[_activeIndex].SetRuleConfig(rules);
    }

    private void SetActive(int index)
    {
        int currentIndex = _activeIndex;

        if (index == -1)
            UpdateLayout();

        // Deselect previous
        if (_activeIndex >= 0 && _activeIndex < _simulations.Count)
        {
            _simulations[_activeIndex].SetSelected(false);
        }

        _activeIndex = index;

        // Select new
        if (index >= 0 && index < _simulations.Count)
            _simulations[_activeIndex].SetSelected(true);

        if (currentIndex != index)
            OnActiveSimulationChanged?.Invoke(index);
        else if (index >= 0)
            FocusOnActiveSimulation();
    }

    public void ToggleActiveRunning()
    {
        _simulations[_activeIndex].ToggleIsRunning();
    }

    public void ToggleAllRunning()
    {
        foreach (SimulationInstance sim in _simulations)
            sim.ToggleIsRunning();
    }

    public void StepAll()
    {
        foreach (SimulationInstance sim in _simulations) sim.Step(true);
    }

    public void StepActive()
    {
        _simulations[_activeIndex].Step(true);
    }

    public void RandomiseActive()
    {
        if (_activeIndex >= 0 && _activeIndex < _simulations.Count)
        {
            _simulations[_activeIndex].Randomise();
        }
    }

    public void RandomiseAllDifferent()
    {
        foreach (SimulationInstance sim in _simulations)
            sim.Randomise();
    }

    public void RandomiseAllSame()
    {
        if (_simulations.Count == 0 || _activeIndex > _simulations.Count) return;

        _simulations[0].Randomise();

        byte[] randomed = _simulations[0].GetCells();

        foreach (SimulationInstance sim in _simulations)
            sim.SetAllCells(randomed);

    }

    public void ClearActive()
    {
        if (_activeIndex >= 0 && _activeIndex < _simulations.Count)
            _simulations[_activeIndex].Clear();
    }

    public void ClearAll()
    {
        foreach (SimulationInstance sim in _simulations)
            sim.Clear();
    }

    public void UpdateRuleForActive(string newRule)
    {
        if (_activeIndex >= 0 && _activeIndex < _simulations.Count)
        {
            _simulations[_activeIndex].SetRuleConfig(newRule);
        }
    }

    // --- Layout & Camera ---
    private void UpdateLayout()
    {
        if (_simulations.Count == 0) return;

        // --- 1. Calculate Grid Dimensions (Content + Gaps) ---
        int columns = Mathf.CeilToInt(Mathf.Sqrt(_simulations.Count));
        int rows = Mathf.CeilToInt((float)_simulations.Count / columns);

        int simW = _simulations[0].TextureWidth + _gridPaddingX * 2;
        int simH = _simulations[0].TextureHeight + _gridPaddingY * 2;

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

        // if we only have one simulation, save the variables for the camera so we can set them when we focus on a single simulation later
        if (_simulations.Count == 1)
        {
            _singleSimOrthographicSize = targetOrthoSize;
        }
    }

    private void FocusOnActiveSimulation()
    {
        // get the position of the active simulation
        SimulationInstance sim = _simulations[_activeIndex];
        _mainCamera.transform.position = new(sim.SpriteRenderer.transform.localPosition.x * (1.0f - _uiPanelWidthRatio), sim.SpriteRenderer.transform.localPosition.y, _mainCamera.transform.position.z);
        _mainCamera.orthographicSize = _singleSimOrthographicSize;
    }

    // --- Interaction ---

    private void OnLeftClick(Vector2 mousePos)
    {

        if (_uiControlPanel == null || !RectTransformUtility.RectangleContainsScreenPoint(_uiControlPanel.GetComponent<RectTransform>(), mousePos))
            SetActive(GetSimulationAtMouse(mousePos));
    }

    private void OnRightClick(Vector2 mousePos)
    {
        SetActive(-1);
    }

    private int GetSimulationAtMouse(Vector2 screenPos)
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
                int gx = Mathf.FloorToInt((relX + halfW) / sim.TextureWidth * sim.DataWidth);
                int gy = Mathf.FloorToInt((relY + halfH) / sim.TextureHeight * sim.DataHeight);

                // Flip Y for texture coordinate
                gy = sim.DataHeight - 1 - gy;

                if (sim.TopologyContains(gx, gy))
                {
                    return sim.Id;
                }
            }
        }
        return -1;
    }

    public SimulationInstance GetSimulationFromId(int index)
    {
        if (index >= 0 && index < _simulations.Count) return _simulations[index];

        return null;
    }

    public SimulationInstance GetSimulationFromName(string name, int ignore = -1)
    {
        foreach (SimulationInstance sim in _simulations)
            if (sim.Name == name && sim.Id != ignore) return sim;

        return null;
    }
}