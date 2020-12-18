using Unity.Assertions;
using Unity.Mathematics;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float ZoomSpeed = .1f;
    public float MinSize = .0001f;
    [Range(0, 1)]
    public float ZoomSmoothness =.01f;
    public float MoveSpeed = 2;
    [Range(0, 1)]
    public float MoveSmoothness = .001f;
    public float Margin = .1f;

    public bool IgnoreZoomOnCtrl;
    public bool IgnoreZoomOnAlt;
    public bool IgnoreZoomOnShift;

    float2 _size;
    float _width;
    Camera _camera;
    float2 _cameraTarget;
    float2 _previousMouse;
    Vector2 _scrollPos;
    float _maxSize;
    float _sizeTarget;
    public float ZoomOutPanMultiplier = 1;

    public void Initialize(float2 size, float zoom = 1)
    {
        _size = size;
        _camera = Camera.main;
        _maxSize = _size.y / 2 + Margin * _size.y;
        _sizeTarget = math.clamp(_maxSize * zoom, MinSize, _maxSize);
        _camera.orthographicSize = _sizeTarget;
        _cameraTarget = float2.zero;
    }

    void Update()
    {
        Assert.IsTrue(math.all(_size > 0), "CameraController not initialized");

        var mousePosRaw = Input.mousePosition;
        mousePosRaw.z = _camera.nearClipPlane;
        var mousePos = ((float3) _camera.ScreenToWorldPoint(mousePosRaw)).xz;

        var wasd = new float2();

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            wasd.x -= 1;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            wasd.x += 1;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            wasd.y += 1;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            wasd.y -= 1;

        if (math.all(wasd != 0))
            wasd = math.normalize(wasd);

        var scrollDelta = Input.mouseScrollDelta.y;
        var mouseDelta = mousePos - _previousMouse;

        if (scrollDelta != 0 && !IgnoreZoom)
            _sizeTarget = math.clamp(_sizeTarget - scrollDelta * ZoomSpeed * _sizeTarget, MinSize, _maxSize);

        var orthoSize = _camera.orthographicSize;
        var newSize = math.lerp(orthoSize, _sizeTarget, 1 - math.pow(ZoomSmoothness, Time.deltaTime));
        var change = orthoSize - newSize;
        var t = mousePos.xxy;
        t.y = _camera.transform.position.y;
        var multiplier = change / orthoSize;
        var offset = ((Vector3) t - _camera.transform.position) * multiplier;

        if (math.sign(change) == -1)
            offset *= ZoomOutPanMultiplier;

        orthoSize -= change;
        orthoSize = Mathf.Clamp(orthoSize, MinSize, _maxSize);

        if (Input.GetMouseButton(2))
            _cameraTarget -= mouseDelta;
        _previousMouse = ((float3) _camera.ScreenToWorldPoint(mousePosRaw)).xz;

        var zoomLevel = orthoSize / _maxSize;
        var margin = Margin * _size * zoomLevel;
        var cameraSize = new float2(_camera.aspect * orthoSize, orthoSize);
        var max = new float2(_size / 2 + margin) - cameraSize;

        _cameraTarget = math.clamp(_cameraTarget + wasd * MoveSpeed * orthoSize * Time.deltaTime, -max, max) + offset.xz();
        _camera.transform.position = Vector3.Lerp(_camera.transform.position, _cameraTarget.ToXxY(5), 1 - math.pow(MoveSmoothness, Time.deltaTime));
        _camera.transform.position += offset;

        var p = math.clamp(_camera.transform.position.xz(), math.min(0, -max), math.max(0, max));
        _camera.transform.position = new Vector3(p.x, 5, p.y);
        _camera.orthographicSize = orthoSize;
    }

    bool IgnoreZoom => IgnoreZoomOnCtrl && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) ||
                       IgnoreZoomOnAlt && (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) ||
                       IgnoreZoomOnShift && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

    public float Zoom => _camera.orthographicSize / _maxSize;
}
