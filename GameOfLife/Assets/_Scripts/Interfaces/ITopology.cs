public interface ITopology
{
    public int GetNeighbours(int x, int y, int[] outputBuffer);
    public bool Contains(int x, int y);
}