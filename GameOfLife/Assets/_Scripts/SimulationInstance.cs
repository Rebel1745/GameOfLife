using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimulationInstance
{
    private readonly int _id;
    public int Id => _id;
    private string _name;
    public string Name => _name;

    private readonly LifeData _data;
    public int DataWidth => _data.Width;
    public int DataHeight => _data.Height;

    private readonly LifeRules _rules;
    private readonly ITopology _topology;
    private readonly LifeRule _ruleConfig;
    public string RuleString => _ruleConfig.RuleString;

    // Rendering properties
    private Texture2D _texture;
    public int TextureWidth => _data.Width + (_borderWidth * 2);
    public int TextureHeight => _data.Height + (_borderWidth * 2);

    private SpriteRenderer _spriteRenderer;
    public SpriteRenderer SpriteRenderer => _spriteRenderer;
    private Sprite _sprite;
    private Color32[] _colorBuffer;

    // label
    private GameObject _labelGameObject;
    private TextMeshPro _labelText;
    private SpriteRenderer _backgroundSR;

    // Visuals
    private readonly int _borderWidth = 1;
    private bool _isRunning;
    private bool _isSelected = false;
    private Color32 _activeBorderColor;
    private Color32 _aliveColour;
    private Color32 _deadColour;
    private int _height;
    private float _gridSpacingY;

    public SimulationInstance(int id, string name, int width, int height, string ruleString, Color32 alive, Color32 dead, Color32 active, float gridSpacingY)
    {
        _id = id;
        _name = name;
        _data = new LifeData(width, height);
        _rules = new LifeRules();
        _topology = new SquareBoundedTopology(width, height);

        _ruleConfig = new LifeRule { RuleString = ruleString };
        _ruleConfig.Parse();

        _height = height;
        _gridSpacingY = gridSpacingY;

        _aliveColour = alive;
        _deadColour = dead;
        _activeBorderColor = active;

        _borderWidth = Mathf.FloorToInt(width * 0.05f);

        InitialiseTexture();
    }

    public void InitialiseSpriteRenderer(Transform parent)
    {
        // Create a GameObject for this simulation to hold the SpriteRenderer
        GameObject obj = new GameObject(_name);
        obj.transform.SetParent(parent);

        _spriteRenderer = obj.AddComponent<SpriteRenderer>();
        _spriteRenderer.sprite = _sprite;
        _spriteRenderer.sortingOrder = 1; // Below UI

        // Set initial position
        _spriteRenderer.transform.localPosition = Vector3.zero;
    }

    private void InitialiseTexture()
    {
        // Create texture with padding
        int tWidth = DataWidth + (_borderWidth * 2);
        int tHeight = DataHeight + (_borderWidth * 2);

        _texture = new Texture2D(tWidth, tHeight, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point
        };

        _colorBuffer = new Color32[tWidth * tHeight];

        // Create Sprite
        _sprite = Sprite.Create(_texture, new Rect(0, 0, tWidth, tHeight), new Vector2(0.5f, 0.5f), 1.0f);
    }

    public void InitialiseLabel()
    {
        // 1. Create the Label GameObject (Container)
        _labelGameObject = new GameObject($"{_name}_Label");
        _labelGameObject.transform.SetParent(_spriteRenderer.transform); // Parent to Simulation
        _labelGameObject.transform.localPosition = new Vector3(0, (_height / 2) + _borderWidth + (_gridSpacingY / 2f), 0);
        _labelGameObject.transform.localEulerAngles = Vector3.zero;

        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(_labelGameObject.transform);
        bgObj.transform.localPosition = Vector3.zero;
        bgObj.transform.localScale = new Vector3(DataWidth - (_borderWidth * 2), _gridSpacingY / 2f, 1); // Slightly narrower than sim

        // Use SpriteRenderer instead of Image for world-space backgrounds
        _backgroundSR = bgObj.AddComponent<SpriteRenderer>();

        // Create a simple white square sprite for the background
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        Sprite squareSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);

        _backgroundSR.sprite = squareSprite;
        _backgroundSR.color = _deadColour;
        _backgroundSR.sortingOrder = 2; // Render behind text but in front of sim

        // 3. Create a Child GameObject for the Text
        GameObject textObj = new GameObject("LabelText");
        textObj.transform.SetParent(_labelGameObject.transform); // Parent to the Image (Panel)
        textObj.transform.localPosition = Vector3.zero; // Center relative to panel
        textObj.transform.localEulerAngles = Vector3.zero;

        // 4. Add Text Component to the Child
        _labelText = textObj.AddComponent<TextMeshPro>();
        _labelText.alignment = TextAlignmentOptions.Center;
        _labelText.fontSize = 20;
        _labelText.fontStyle = TMPro.FontStyles.Bold;
        _labelText.color = _aliveColour;
        _labelText.raycastTarget = false;
        _labelText.sortingOrder = 3;

        RectTransform textRect = _labelText.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(DataWidth - (_borderWidth * 2), _gridSpacingY / 2f);

        // 5. Setup Layout (Anchors)
        // RectTransform panelRect = _labelGameObject.GetComponent<RectTransform>();
        // panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        // panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        // panelRect.pivot = new Vector2(0.5f, 0.5f);

        // // Set panel size (slightly smaller than simulation)
        // int padding = 20;
        // int width = _data.Width - padding;
        // if (width < 100) width = 100;
        // panelRect.sizeDelta = new Vector2(width, 40);

        // // Setup text rect to fill the panel
        // RectTransform textRect = _labelText.GetComponent<RectTransform>();
        // // textRect.anchorMin = new Vector2(0.5f, 0.5f);
        // // textRect.anchorMax = new Vector2(0.5f, 0.5f);
        // // textRect.pivot = new Vector2(0.5f, 0.5f);
        // textRect.sizeDelta = new Vector2(DataWidth, 5); // Fill parent

        // 6. Set initial text
        UpdateLabelText();
    }

    public void UpdateLabelText()
    {
        if (_labelText != null)
        {
            _labelText.text = $"{_name} - {_ruleConfig.RuleString}";
        }
    }

    public void SetLabelVisible(bool isVisible)
    {
        if (_labelGameObject != null)
        {
            _labelGameObject.SetActive(isVisible);
        }
    }

    public void Step(bool once = false)
    {
        if (!_isRunning && !once) return;

        _rules.Step(_data, _topology, _ruleConfig);
        Render(); // Auto-render on step
    }

    public void Render()
    {
        // 1. Fill the padding border
        int tWidth = TextureWidth;
        int tHeight = TextureHeight;

        // Fill entire buffer with background color (black)
        // We will draw the grid on top of the black background
        for (int i = 0; i < _colorBuffer.Length; i++)
        {
            _colorBuffer[i] = _deadColour;
        }

        // 2. Draw the simulation grid (with padding offset)
        int offsetX = _borderWidth;
        int offsetY = _borderWidth; // Unity Texture (0,0) is bottom-left

        for (int y = 0; y < DataHeight; y++)
        {
            for (int x = 0; x < DataWidth; x++)
            {
                // Calculate texture coordinates
                // Texture Y: (Height - 1 - y) + offsetY
                int texX = offsetX + x;
                int texY = tHeight - 1 - y - offsetY;

                int index = texY * tWidth + texX;

                // Determine cell color
                Color32 cellColor = (_data.Cells[_data.GetIndex(x, y)] == 1) ? _aliveColour : _deadColour;

                // If selected, maybe add a slight tint? Or just rely on the border
                _colorBuffer[index] = cellColor;
            }
        }

        // 3. Draw Selection Border (if selected)
        if (_isSelected)
        {
            DrawSelectionBorder(tWidth, tHeight);
        }

        _texture.SetPixels32(_colorBuffer);
        _texture.Apply();

        // Update UI Label if you have one
        // UpdateLabel();
    }

    private void DrawSelectionBorder(int width, int height)
    {
        // Draw Top Border
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < _borderWidth; y++)
                SetPixel(x, height - y - 1, _activeBorderColor);
        }
        // Draw Bottom Border
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < _borderWidth; y++)
                SetPixel(x, y, _activeBorderColor);
        }
        // Draw Left Border
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < _borderWidth; x++)
                SetPixel(x, y, _activeBorderColor);
        }
        // Draw Right Border
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < _borderWidth; x++)
                SetPixel(width - x - 1, y, _activeBorderColor);
        }
    }

    private void SetPixel(int x, int y, Color32 color)
    {
        int index = y * _texture.width + x;
        if (index >= 0 && index < _colorBuffer.Length)
        {
            _colorBuffer[index] = color;
        }
    }

    public void SetAllCells(byte[] pixels)
    {
        _data.Cells = pixels;
        Render();
    }

    public byte[] GetCells()
    {
        return _data.Cells;
    }

    public void Randomise()
    {
        _isRunning = false;

        for (int i = 0; i < _data.Cells.Length; i++)
        {
            _data.Cells[i] = Random.value > 0.7f ? (byte)1 : (byte)0;
        }
        Render();
    }

    public void Clear()
    {
        _isRunning = false;

        System.Array.Clear(_data.Cells, 0, _data.Cells.Length);
        Render();
    }

    public void SetName(string name)
    {
        if (!string.IsNullOrEmpty(name)) _name = name;

        UpdateLabelText();
    }

    public void SetRuleConfig(string rule)
    {
        _ruleConfig.RuleString = rule;
        _ruleConfig.Parse();

        UpdateLabelText();
    }

    public void SetSelected(bool selected)
    {
        _isSelected = selected;
        Render();
    }

    public bool TopologyContains(int gx, int gy)
    {
        return _topology.Contains(gx, gy);
    }

    public void ToggleIsRunning()
    {
        _isRunning = !_isRunning;
    }
}