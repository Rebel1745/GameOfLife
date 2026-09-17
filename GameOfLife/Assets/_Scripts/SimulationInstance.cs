using UnityEngine;

public class SimulationInstance
{
    private int _id;
    public int Id => _id;
    private string _name;
    public string Name => _name;

    private LifeData _data;
    public int DataWidth => _data.Width;
    public int DataHeight => _data.Height;

    private LifeRules _rules;
    private ITopology _topology;
    private LifeRule _ruleConfig;

    // Rendering properties
    private Texture2D _texture;
    public int TextureWidth => _data.Width + (_borderWidth * 2);
    public int TextureHeight => _data.Height + (_borderWidth * 2);

    private SpriteRenderer _spriteRenderer;
    public SpriteRenderer SpriteRenderer => _spriteRenderer;
    private Sprite _sprite;
    private Color32[] _colorBuffer;

    // Visuals
    private int _borderWidth = 1;
    private bool _isSelected = false;
    private Color32 _activeBorderColor;
    private Color32 _aliveColour;
    private Color32 _deadColour;


    public SimulationInstance(int id, string name, int width, int height, string ruleString, Color32 alive, Color32 dead, Color32 active)
    {
        _id = id;
        _name = name;
        _data = new LifeData(width, height);
        _rules = new LifeRules();
        _topology = new SquareBoundedTopology(width, height);

        _ruleConfig = new LifeRule { RuleString = ruleString };
        _ruleConfig.Parse();

        _aliveColour = alive;
        _deadColour = dead;
        _activeBorderColor = active;

        _borderWidth = Mathf.FloorToInt(width * 0.05f);

        InitializeTexture();
    }

    public void SetupSpriteRenderer(Transform parent)
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

    private void InitializeTexture()
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

    public void Step()
    {
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
        for (int i = 0; i < _data.Cells.Length; i++)
        {
            _data.Cells[i] = Random.value > 0.7f ? (byte)1 : (byte)0;
        }
        Render();
    }

    public void Clear()
    {
        System.Array.Clear(_data.Cells, 0, _data.Cells.Length);
        Render();
    }

    public void SetName(string name)
    {
        if (!string.IsNullOrEmpty(name)) _name = name;
        // UpdateLabel();
    }

    public void SetRuleConfig(string rule)
    {
        if (!string.IsNullOrEmpty(rule)) _ruleConfig.RuleString = rule;
        _ruleConfig.Parse();
        // UpdateLabel();
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
}