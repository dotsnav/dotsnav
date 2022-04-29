using UnityEngine;

namespace DotsNav.Samples.Code
{
    class MaintainOrthoSize : MonoBehaviour
    {
        float _size;
        Vector3 _scale;

        void Start()
        {
            _size = Camera.main.orthographicSize;
            _scale = transform.localScale;
        }

        void Update()
        {
            transform.localScale = _scale * (Camera.main.orthographicSize / _size);
        }
    }
}