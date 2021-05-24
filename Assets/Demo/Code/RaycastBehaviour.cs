using Unity.Mathematics;
using UnityEngine;

public class RaycastBehaviour : MonoBehaviour
{
    public Transform Goal;
    public float2 GetStart() => transform.position.xz();
    public float2 GetGoal() => Goal.transform.position.xz();
}
