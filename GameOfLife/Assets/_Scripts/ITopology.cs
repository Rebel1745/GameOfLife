public interface ITopology
{
    int GetNeighbors(int x, int y, int[] outputBuffer);
    bool Contains(int x, int y);
}