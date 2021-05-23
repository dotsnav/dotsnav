#if !UNITY_EDITOR
using System.Linq;
#endif
using Unity.Mathematics;
using UnityEngine;

public class ResolutionManager : MonoBehaviour
{
    public int2 Resolution;

#if !UNITY_EDITOR
    static bool _present;
    bool _previousFullscreen;

    void Awake()
    {
        if (_present)
            DestroyImmediate(gameObject);
        else
        {
            _present = true;
            DontDestroyOnLoad(gameObject);
            _previousFullscreen = Screen.fullScreen;
            Screen.SetResolution(Resolution.x, Resolution.y, false);
        }
    }

    void Update()
    {
        if (_previousFullscreen != Screen.fullScreen)
            AdjustResolution();
    }

    void AdjustResolution()
    {
        _previousFullscreen = Screen.fullScreen;

        if (Screen.fullScreen)
        {
            var r = Screen.resolutions.Last();
            Screen.SetResolution(r.width, r.height, true, r.refreshRate);
        }
        else
            Screen.SetResolution(Resolution.x, Resolution.y, false);
    }
#endif
}