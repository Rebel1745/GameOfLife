using UnityEngine;
using System.Collections.Generic;

public class SimulationInstance
{
    public string Name;
    public LifeData Data;
    public LifeRules Rules;
    public ITopology Topology;
    public LifeRule RuleConfig;

    // Rendering properties
    public Texture2D Texture;
    public SpriteRenderer SpriteRenderer;
    public Sprite Sprite;
    public Color32[] ColorBuffer;

    // Visuals
    public int Padding = 10; // Pixels of black space around the grid
    public bool IsSelected = false;
    public Color32 _selectionColor = Color.yellow;
    private Color32 _aliveColour;
    private Color32 _deadColour;

    // Derived dimensions (Texture width = Grid Width + Padding*2)
    public int TextureWidth
    {
        get => Data.Width + (Padding * 2);
    }
    public int TextureHeight
    {
        get => Data.Height + (Padding * 2);
    }

    public SimulationInstance(string name, int width, int height, string ruleString, Color32 alive, Color32 dead)
    {
        Name = name;
        Data = new LifeData(width, height);
        Rules = new LifeRules();
        Topology = new SquareBoundedTopology(width, height);

        RuleConfig = new LifeRule { RuleString = ruleString };
        RuleConfig.Parse();

        _aliveColour = alive;
        _deadColour = dead;

        InitializeTexture();
    }

    private void InitializeTexture()
    {
        // Create texture with padding
        int tWidth = Data.Width + (Padding * 2);
        int tHeight = Data.Height + (Padding * 2);

        Texture = new Texture2D(tWidth, tHeight, TextureFormat.RGBA32, false);
        Texture.filterMode = FilterMode.Point;

        ColorBuffer = new Color32[tWidth * tHeight];

        // Create Sprite
        Sprite = Sprite.Create(Texture, new Rect(0, 0, tWidth, tHeight), new Vector2(0.5f, 0.5f), 1.0f);
    }

    public void Step()
    {
        Rules.Step(Data, Topology, RuleConfig);
        Render(); // Auto-render on step
    }

    public void Render()
    {
        // 1. Fill the padding border
        int tWidth = TextureWidth;
        int tHeight = TextureHeight;

        // Fill entire buffer with background color (black)
        // We will draw the grid on top of the black background
        for (int i = 0; i < ColorBuffer.Length; i++)
        {
            ColorBuffer[i] = _deadColour;
        }

        // 2. Draw the simulation grid (with padding offset)
        int offsetX = Padding;
        int offsetY = Padding; // Unity Texture (0,0) is bottom-left

        for (int y = 0; y < Data.Height; y++)
        {
            for (int x = 0; x < Data.Width; x++)
            {
                // Calculate texture coordinates
                // Texture Y: (Height - 1 - y) + offsetY
                int texX = offsetX + x;
                int texY = tHeight - 1 - y - offsetY;

                int index = texY * tWidth + texX;

                // Determine cell color
                Color32 cellColor = (Data.Cells[Data.GetIndex(x, y)] == 1) ? _aliveColour : _deadColour;

                // If selected, maybe add a slight tint? Or just rely on the border
                ColorBuffer[index] = cellColor;
            }
        }

        // 3. Draw Selection Border (if selected)
        if (IsSelected)
        {
            DrawSelectionBorder(tWidth, tHeight);
        }

        Texture.SetPixels32(ColorBuffer);
        Texture.Apply();

        // Update Sprite
        //Sprite.SetTexture(Texture);

        // Update UI Label if you have one
        // UpdateLabel();
    }

    private void DrawSelectionBorder(int width, int height)
    {
        // Draw Top Border
        for (int x = 0; x < width; x++) SetPixel(x, height - 1, _selectionColor);
        // Draw Bottom Border
        for (int x = 0; x < width; x++) SetPixel(x, 0, _selectionColor);
        // Draw Left Border
        for (int y = 0; y < height; y++) SetPixel(0, y, _selectionColor);
        // Draw Right Border
        for (int y = 0; y < height; y++) SetPixel(width - 1, y, _selectionColor);
    }

    private void SetPixel(int x, int y, Color32 color)
    {
        int index = y * Texture.width + x;
        if (index >= 0 && index < ColorBuffer.Length)
        {
            ColorBuffer[index] = color;
        }
    }

    public void Randomise()
    {
        for (int i = 0; i < Data.Cells.Length; i++)
        {
            Data.Cells[i] = Random.value > 0.7f ? (byte)1 : (byte)0;
        }
        Render();
    }

    public void Clear()
    {
        System.Array.Clear(Data.Cells, 0, Data.Cells.Length);
        Render();
    }
}