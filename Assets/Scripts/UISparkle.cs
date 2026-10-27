using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Brillito geometrico para los tres primeros puestos de los marcadores.
/// Anima opacidad, tamano y giro con tiempo sin escalar (funciona en pausa).
/// </summary>
public class UISparkle : MonoBehaviour
{
    public float velocidad = 2.4f;
    public float fase;
    public float alfaMin = 0.12f;
    public float alfaMax = 1f;
    public float escalaMin = 0.55f;
    public float escalaMax = 1.18f;
    public float giro = 40f;

    Image imagen;
    Vector3 escalaBase = Vector3.one;
    float rotacionBase;
    float t;

    void Awake()
    {
        Capturar();
    }

    void OnEnable()
    {
        Capturar();
        t = fase;
    }

    public void Configurar(float nuevaFase)
    {
        fase = nuevaFase;
        t = nuevaFase;
        Capturar();
    }

    void Capturar()
    {
        imagen = GetComponent<Image>();
        escalaBase = transform.localScale;
        rotacionBase = transform.localEulerAngles.z;
    }

    void Update()
    {
        if (imagen == null) return;

        t += Time.unscaledDeltaTime * velocidad;
        float u = (Mathf.Sin(t) + 1f) * 0.5f;

        Color c = imagen.color;
        c.a = Mathf.Lerp(alfaMin, alfaMax, u);
        imagen.color = c;

        transform.localScale = escalaBase * Mathf.Lerp(escalaMin, escalaMax, u);
        transform.localRotation = Quaternion.Euler(0f, 0f, rotacionBase + t * giro);
    }
}
