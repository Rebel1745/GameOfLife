using UnityEngine;

/// <summary>
/// Pure data container. No Unity dependencies (except Mathf for index math).
/// This is the "Model" in MVC.
/// </summary>
public class LifeData
{
    public readonly int Width;
    public readonly int Height;

    // The actual state of the world
    // 0 = Dead, 1 = Alive (for now)
    public byte[] Cells;
    public byte[] NextCells;

    public LifeData(int width, int height)
    {
        Width = width;
        Height = height;
        Cells = new byte[width * height];
        NextCells = new byte[width * height];
    }

    // Helper to convert 2D coords to 1D array index
    public int GetIndex(int x, int y) => y * Width + x;

    public void SwapBuffers()
    {
        // Swap the references. 
        // 'Cells' becomes the old 'NextCells' (which is now the current state)
        // 'NextCells' becomes the old 'Cells' (which we will overwrite next frame)
        (Cells, NextCells) = (NextCells, Cells);
    }

    public void ClearNextBuffer()
    {
        System.Array.Clear(NextCells, 0, NextCells.Length);
    }
}