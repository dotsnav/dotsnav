using Unity.Mathematics;
using UnityEngine;

namespace DotsNav.Samples.Code
{
    public class DiscCastBehaviour : MonoBehaviour
    {
        public Transform Other;
        SphereCollider _collider;
        Transform _start;
        public float2 Centre => transform.position.xz();
        public float Radius => math.length(Other.transform.position.xz() - Centre);

        void Awake()
        {
            _collider = GetComponent<SphereCollider>();
            _start = transform.Find("Start");
        }

        void LateUpdate()
        {
            _collider.radius = _start.localScale.x / 2;
        }
    }
}
