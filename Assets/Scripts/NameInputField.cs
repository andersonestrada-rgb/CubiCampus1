using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Campo de nombre propio para el menu. No usa el InputField clasico porque el
/// proyecto tiene desactivado el Input Manager antiguo; escucha el teclado del
/// nuevo Input System y dibuja el cursor como texto parpadeante.
/// </summary>
public class NameInputField : MonoBehaviour, IPointerClickHandler
{
    [Header("Referencias")]
    public Text txtValor;
    public Image fondo;
    public Image borde;

    [Header("Configuracion")]
    public int maxLength = 16;
    public string placeholder = "ESCRIBE TU NOMBRE";

    [Header("Colores")]
    public Color colorTexto = new Color(0.95f, 0.95f, 0.98f, 1f);
    public Color colorPlaceholder = new Color(0.55f, 0.58f, 0.68f, 1f);
    public Color colorFondo = new Color(0.13f, 0.14f, 0.20f, 1f);
    public Color colorFondoFoco = new Color(0.17f, 0.19f, 0.27f, 1f);
    public Color colorBorde = new Color(0.30f, 0.34f, 0.46f, 1f);
    public Color colorBordeFoco = new Color(0.30f, 0.78f, 1f, 1f);

    bool inicializado;
    bool enfocado;
    bool caretVisible = true;
    string valor = "";
    float parpadeo;
    float siguienteBorrado;
    Keyboard teclado;

    public string Valor { get { return valor; } }
    public bool Enfocado { get { return enfocado; } }

    void Start()
    {
        Inicializar();
    }

    void OnEnable()
    {
        Inicializar();
    }

    void OnDisable()
    {
        SoltarFoco();
    }

    void OnDestroy()
    {
        DesuscribirTeclado();
    }

    void Inicializar()
    {
        if (inicializado) return;
        inicializado = true;

        string guardado = PlayerPrefs.GetString(LeaderboardStore.KeyNombreJugador, "");
        valor = string.IsNullOrEmpty(guardado)
            ? LeaderboardStore.DefaultNombre
            : LeaderboardStore.Normalizar(guardado);

        Refrescar();
    }

    void Update()
    {
        if (Keyboard.current != teclado) SuscribirTeclado();

        if (!enfocado) return;

        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame ||
            kb.escapeKey.wasPressedThisFrame)
        {
            SoltarFoco();
            return;
        }

        bool borrarAhora = kb.backspaceKey.wasPressedThisFrame || kb.deleteKey.wasPressedThisFrame;
        bool borrarMantenido = (kb.backspaceKey.isPressed || kb.deleteKey.isPressed) &&
                               Time.unscaledTime >= siguienteBorrado;

        if (borrarAhora)
        {
            Borrar();
            siguienteBorrado = Time.unscaledTime + 0.35f;
        }
        else if (borrarMantenido)
        {
            Borrar();
            siguienteBorrado = Time.unscaledTime + 0.06f;
        }

        parpadeo += Time.unscaledDeltaTime;
        if (parpadeo >= 0.55f)
        {
            parpadeo = 0f;
            caretVisible = !caretVisible;
            Refrescar();
        }

        Pointer puntero = Pointer.current;
        if (puntero != null && puntero.press.wasPressedThisFrame &&
            !RectTransformUtility.RectangleContainsScreenPoint(
                (RectTransform)transform, puntero.position.ReadValue(), CamaraCanvas()))
        {
            SoltarFoco();
        }
    }

    void SuscribirTeclado()
    {
        DesuscribirTeclado();
        teclado = Keyboard.current;
        if (teclado != null) teclado.onTextInput += OnTextInput;
    }

    void DesuscribirTeclado()
    {
        if (teclado != null) teclado.onTextInput -= OnTextInput;
        teclado = null;
    }

    void OnTextInput(char c)
    {
        if (!enfocado || !inicializado) return;
        if (c == '\b' || c == '\n' || c == '\r' || c == '\t') return;
        if (!EsCaracterValido(c)) return;
        if (valor.Length >= maxLength) return;
        if (c == ' ' && (valor.Length == 0 || valor[valor.Length - 1] == ' ')) return;

        valor += c;
        caretVisible = true;
        parpadeo = 0f;
        Refrescar();
        Guardar(false);
    }

    static bool EsCaracterValido(char c)
    {
        return char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '_' ||
               c == '.' || c == '\'' || c == '&';
    }

    void Borrar()
    {
        if (valor.Length == 0) return;

        valor = valor.Substring(0, valor.Length - 1);
        caretVisible = true;
        parpadeo = 0f;
        Refrescar();
        Guardar(false);
    }

    public void Enfocar()
    {
        Inicializar();
        if (enfocado) return;

        enfocado = true;
        caretVisible = true;
        parpadeo = 0f;
        Refrescar();
    }

    public void SoltarFoco()
    {
        if (!enfocado) return;

        enfocado = false;
        valor = valor.Trim();
        if (string.IsNullOrEmpty(valor)) valor = LeaderboardStore.DefaultNombre;

        Refrescar();
        Guardar(true);
    }

    /// <summary>Coloca un nombre desde codigo (utilidad para pruebas).</summary>
    public void Establecer(string nombre)
    {
        Inicializar();
        valor = string.IsNullOrEmpty(nombre) ? "" : LeaderboardStore.Normalizar(nombre);
        Refrescar();
        Guardar(false);
    }

    void Guardar(bool guardarEnDisco)
    {
        if (guardarEnDisco)
        {
            LeaderboardStore.GuardarNombre(valor);
        }
        else
        {
            PlayerPrefs.SetString(LeaderboardStore.KeyNombreJugador, valor);
        }
    }

    void Refrescar()
    {
        if (txtValor == null) return;

        bool vacio = string.IsNullOrEmpty(valor);

        if (enfocado)
        {
            txtValor.text = valor + (caretVisible ? "|" : " ");
            txtValor.color = colorTexto;
        }
        else if (vacio)
        {
            txtValor.text = placeholder;
            txtValor.color = colorPlaceholder;
        }
        else
        {
            txtValor.text = valor;
            txtValor.color = colorTexto;
        }

        if (fondo != null) fondo.color = enfocado ? colorFondoFoco : colorFondo;
        if (borde != null) borde.color = enfocado ? colorBordeFoco : colorBorde;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        Enfocar();
    }

    Camera CamaraCanvas()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;
        return canvas.worldCamera;
    }
}
