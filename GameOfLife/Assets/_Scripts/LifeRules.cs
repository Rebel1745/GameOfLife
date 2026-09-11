using UnityEngine;

/// <summary>
/// The Simulation Engine.
/// Responsible for computing the next generation of LifeData.
/// </summary>
public class LifeRules
{
    // Reusable buffer to avoid garbage collection
    private readonly int[] _neighborBuffer = new int[16]; // Max 8 neighbors * 2 coords

    public void Step(LifeData data, ITopology topology)
    {
        data.ClearNextBuffer();

        for (int y = 0; y < data.Height; y++)
        {
            for (int x = 0; x < data.Width; x++)
            {
                int currentIndex = data.GetIndex(x, y);
                byte currentState = data.Cells[currentIndex];

                int neighborCount = topology.GetNeighbors(x, y, _neighborBuffer);
                int liveNeighbors = CountLiveNeighbors(data, neighborCount);

                // Apply B3/S23 Rules
                if (currentState == 0)
                {
                    if (liveNeighbors == 3)
                    {
                        data.NextCells[currentIndex] = 1;
                    }
                }
                else
                {
                    if (liveNeighbors == 2 || liveNeighbors == 3)
                    {
                        data.NextCells[currentIndex] = 1;
                    }
                }
            }
        }

        data.SwapBuffers();
    }

    private int CountLiveNeighbors(LifeData data, int neighborCount)
    {
        int live = 0;
        for (int i = 0; i < neighborCount; i++)
        {
            int nx = _neighborBuffer[i * 2];
            int ny = _neighborBuffer[i * 2 + 1];
            if (data.Cells[data.GetIndex(nx, ny)] == 1)
            {
                live++;
            }
        }
        return live;
    }
}