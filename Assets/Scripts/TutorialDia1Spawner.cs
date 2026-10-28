using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Spawner automático del tutorial. Se asegura de que aparezca solo en GameScene, Día 1, primera vez.
/// </summary>
public class TutorialDia1Spawner : MonoBehaviour
{
    void Awake()
    {
        if (SceneManager.GetActiveScene().name != "GameScene") 
        { 
            Destroy(gameObject); 
            return; 
        }
        
        if (PlayerPrefs.GetInt(TutorialDia1.PrefKey, 0) == 1)
        {
            Destroy(gameObject);
            return;
        }
        
        GameManager gm = GameManager.Instance;
        if (gm != null && gm.diaActual != 1)
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
