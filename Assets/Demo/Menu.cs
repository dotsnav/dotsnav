using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    void Start()
    {
        var em = World.All[0].EntityManager;
        em.DestroyEntity(em.UniversalQuery);
    }

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
