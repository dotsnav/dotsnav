using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    public void Demo()
    {
        SceneManager.LoadScene("demo");
    }

    public void Sandbox()
    {
        SceneManager.LoadScene("sandbox");
    }

    public void StressTest()
    {
        SceneManager.LoadScene("stresstest");
    }

    public void LoadTest()
    {
        SceneManager.LoadScene("loadtest");
    }
}
