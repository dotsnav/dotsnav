using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    public void Pathfinding()
    {
        SceneManager.LoadScene("pathfinding");
    }

    public void Avoidance()
    {
        SceneManager.LoadScene("avoidance");
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
