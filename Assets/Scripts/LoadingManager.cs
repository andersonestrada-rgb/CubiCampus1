using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Pantalla de carga universal. Lee la escena destino de PlayerPrefs ("EscenaDestino"),
/// carga de forma asincrona con SceneManager.LoadSceneAsync y actualiza una barra de progreso.
/// </summary>
public class LoadingManager : MonoBehaviour
{
    [Header("UI")]
    public Image barraProgreso;
    public Text textoProgreso;
    public Text textoEstado;

    [Header("Configuracion")]
    [Tooltip("Tiempo minimo visible de la pantalla de carga (segundos).")]
    public float tiempoMinimo = 1.1f;

    [Tooltip("Escena usada si no hay destino guardado.")]
    public string escenaPorDefecto = "GameScene";

    void Start()
    {
        AudioManager.Ensure();
        StartCoroutine(CargarEscena());
    }

    IEnumerator CargarEscena()
    {
        string destino = PlayerPrefs.GetString("EscenaDestino", escenaPorDefecto);

        // Validacion: si la escena no existe en Build Settings, usar el menu.
        if (!Application.CanStreamedLevelBeLoaded(destino))
        {
            Debug.LogWarning("[LoadingManager] Escena destino '" + destino + "' no valida. Volviendo a MenuScene.");
            destino = "MenuScene";
        }

        if (textoEstado != null) textoEstado.text = "Preparando la sala...";
        ActualizarBarra(0f);

        float inicio = Time.unscaledTime;

        AsyncOperation operacion = SceneManager.LoadSceneAsync(destino);
        if (operacion == null)
        {
            Debug.LogError("[LoadingManager] No se pudo iniciar la carga de '" + destino + "'.");
            yield break;
        }

        // No activar la escena hasta que la barra llegue al final.
        operacion.allowSceneActivation = false;

        // LoadSceneAsync llega a 0.9 mientras espera allowSceneActivation.
        while (operacion.progress < 0.9f)
        {
            float progreso = Mathf.Clamp01(operacion.progress / 0.9f);
            ActualizarBarra(progreso);
            yield return null;
        }

        // Relleno suave del ultimo 10%.
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.25f;
            ActualizarBarra(Mathf.Lerp(0.9f, 1f, Mathf.Clamp01(t)));
            yield return null;
        }
        ActualizarBarra(1f);

        if (textoEstado != null) textoEstado.text = "Listo";

        // Respetar el tiempo minimo de pantalla.
        while (Time.unscaledTime - inicio < tiempoMinimo)
            yield return null;

        operacion.allowSceneActivation = true;
    }

    void ActualizarBarra(float progreso)
    {
        progreso = Mathf.Clamp01(progreso);

        if (barraProgreso != null)
        {
            barraProgreso.type = Image.Type.Filled;
            barraProgreso.fillMethod = Image.FillMethod.Horizontal;
            barraProgreso.fillOrigin = (int)Image.OriginHorizontal.Left;
            barraProgreso.fillAmount = progreso;
        }

        if (textoProgreso != null)
            textoProgreso.text = Mathf.RoundToInt(progreso * 100f) + "%";
    }
}
