using UnityEngine;

/// <summary>
/// Handles drawing the LifeData to the screen.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class LifeRenderer : MonoBehaviour
{
    [SerializeField] private int _cellSize = 10;
    public int CellSize => _cellSize;
    [SerializeField] private Color32 _aliveColour = Color.white;
    [SerializeField] private Color32 _deadColour = Color.black;

    private Texture2D _texture;
    private Color32[] _colorBuffer;
    private SpriteRenderer _sr;

    public void Initialize(LifeData data)
    {
        _texture = new Texture2D(data.Width, data.Height, TextureFormat.RGBA32, false);
        _texture.filterMode = FilterMode.Point;
        _colorBuffer = new Color32[data.Width * data.Height];

        _sr = GetComponent<SpriteRenderer>();
        if (_sr == null)
        {
            _sr = gameObject.AddComponent<SpriteRenderer>();
        }

        // Create a sprite from the texture
        _sr.sprite = Sprite.Create(_texture, new Rect(0, 0, data.Width, data.Height), new Vector2(0.5f, 0.5f), _cellSize);
    }

    public void Render(LifeData data)
    {
        // Update colors
        for (int i = 0; i < data.Cells.Length; i++)
        {
            _colorBuffer[i] = data.Cells[i] == 1 ? _aliveColour : _deadColour;
        }
        _texture.SetPixels32(_colorBuffer);
        _texture.Apply();
    }
}