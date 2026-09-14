using System;

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
        (Cells, NextCells) = (NextCells, Cells);
    }

    public void ClearNextBuffer()
    {
        Array.Clear(NextCells, 0, NextCells.Length);
    }
}