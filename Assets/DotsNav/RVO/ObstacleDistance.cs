using Unity.Entities;

struct ObstacleDistance : IBufferElementData
{
    public float Dist;
    public int Obstacle;

    public ObstacleDistance(float dist, int obstacle)
    {
        Dist = dist;
        Obstacle = obstacle;
    }
}