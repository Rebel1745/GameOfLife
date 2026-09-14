public class SquareBoundedTopology : ITopology
{
    private readonly int _width;
    private readonly int _height;

    public SquareBoundedTopology(int width, int height)
    {
        _width = width;
        _height = height;
    }

    public int GetNeighbours(int x, int y, int[] outputBuffer)
    {
        int count = 0;
        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;

                int nx = x + dx;
                int ny = y + dy;

                if (Contains(nx, ny))
                {
                    outputBuffer[count * 2] = nx;
                    outputBuffer[count * 2 + 1] = ny;
                    count++;
                }
            }
        }
        return count;
    }

    public bool Contains(int x, int y) => x >= 0 && x < _width && y >= 0 && y < _height;
}