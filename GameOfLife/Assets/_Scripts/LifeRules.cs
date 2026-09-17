using UnityEngine;

/// <summary>
/// The Simulation Engine.
/// Responsible for computing the next generation of LifeData.
/// </summary>
public class LifeRules
{
    // Reusable buffer to avoid garbage collection
    private readonly int[] _neighborBuffer = new int[16]; // Max 8 neighbours * 2 coords

    public void Step(LifeData data, ITopology topology, LifeRule rule)
    {
        data.ClearNextBuffer();

        for (int y = 0; y < data.Height; y++)
        {
            for (int x = 0; x < data.Width; x++)
            {
                int currentIndex = data.GetIndex(x, y);
                byte currentState = data.Cells[currentIndex];

                int neighborCount = topology.GetNeighbours(x, y, _neighborBuffer);
                int liveNeighbours = CountLiveNeighbours(data, neighborCount);

                // Apply Rules from the Data Object
                if (currentState == 0)
                {
                    if (rule.BirthCounts.Contains(liveNeighbours))
                    {
                        data.NextCells[currentIndex] = 1;
                    }
                }
                else
                {
                    if (rule.SurvivalCounts.Contains(liveNeighbours))
                    {
                        data.NextCells[currentIndex] = 1;
                    }
                }
            }
        }

        data.SwapBuffers();
    }

    private int CountLiveNeighbours(LifeData data, int neighborCount)
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