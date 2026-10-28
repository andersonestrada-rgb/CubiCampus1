using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Spawner que crea el tutorial en GameScene si está habilitado desde Opciones.
/// </summary>
public class TutorialAutoSpawner : MonoBehaviour
{
    void Awake()
    {
        if (SceneManager.GetActiveScene().name != "GameScene")
        {
            Destroy(gameObject);
            return;
        }

        if (PlayerPrefs.GetInt("MostrarTutorial", 1) == 0)
        {
            Destroy(gameObject);
            return;
        }

        if (FindAnyObjectByType<TutorialDia1>() != null)
        {
            Destroy(gameObject);
            return;
        }

        GameObject go = new GameObject("TutorialDia1");
        go.AddComponent<TutorialDia1>();
        Destroy(gameObject);
    }
}
