public interface ITopology
{
    int GetNeighbours(int x, int y, int[] outputBuffer);
    bool Contains(int x, int y);
}