using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [Header("Referencias UI")]
    public Button btnJugar;
    public Button btnMarcadores;
    public Button btnOpciones;
    public Button btnSalir;
    public Text txtProgreso;

    [Header("Nombre del jugador")]
    public NameInputField campoNombre;

    [Header("Panel Marcadores")]
    public GameObject panelMarcadores;
    public Text txtTituloMarcadores;
    public Text txtSubtituloMarcadores;
    public RectTransform listaMarcadores;
    public Text txtVacioMarcadores;
    public Text txtTuPuesto;
    public Button[] botonesDia;
    public Button btnCerrarMarcadores;

    [Header("Panel Opciones")]
    public GameObject panelOpciones;
    public Toggle toggleFullscreen;
    public Slider sliderVolumen;
    public Button btnCerrarOpciones;

    [Header("Configuracion")]
    public string sceneJuego = "GameScene";
    public string sceneCarga = "LoadingScene";

    static readonly Color ColOro      = new Color(1f, 0.84f, 0.35f, 1f);
    static readonly Color ColPlata    = new Color(0.82f, 0.86f, 0.94f, 1f);
    static readonly Color ColBronce   = new Color(0.88f, 0.58f, 0.31f, 1f);
    static readonly Color ColVerde    = new Color(0.35f, 0.90f, 0.45f, 1f);
    static readonly Color ColTexto    = new Color(0.95f, 0.95f, 0.98f, 1f);
    static readonly Color ColTenue    = new Color(0.72f, 0.74f, 0.82f, 1f);
    static readonly Color ColTabNormal = new Color(0.15f, 0.19f, 0.29f, 1f);
    static readonly Color ColTabTexto  = new Color(0.08f, 0.09f, 0.13f, 1f);
    static readonly Color ColFondoOro  = new Color(1f, 0.84f, 0.35f, 0.22f);
    static readonly Color ColFondoPlata = new Color(0.82f, 0.86f, 0.94f, 0.18f);
    static readonly Color ColFondoBronce = new Color(0.88f, 0.58f, 0.31f, 0.20f);
    static readonly Color ColFondoVerde = new Color(0.35f, 0.90f, 0.45f, 0.13f);
    static readonly Color ColTransparente = new Color(0f, 0f, 0f, 0f);

    class FilaMarcador
    {
        public GameObject go;
        public Image fondo;
        public Image acento;
        public Text puesto;
        public Text nombre;
        public Text puntos;
        public Text eficiencia;
        public GameObject[] brillos;
    }

    readonly List<FilaMarcador> filas = new List<FilaMarcador>();
    int diaSeleccionado = 1;
    bool filasCreadas;
    float proximoPreviewVolumen;

    void Awake()
    {
        ResolverReferencias();

        AudioManager.Ensure();
        AudioManager.SetMusicLevel(0.9f);
        AudioManager.SetMusicDuck(false);

        if (panelMarcadores != null) panelMarcadores.SetActive(false);
        if (panelOpciones != null) panelOpciones.SetActive(false);

        if (btnJugar != null) btnJugar.onClick.AddListener(OnJugarClick);
        if (btnMarcadores != null) btnMarcadores.onClick.AddListener(OnMarcadoresClick);
        if (btnOpciones != null) btnOpciones.onClick.AddListener(OnOpcionesClick);
        if (btnSalir != null) btnSalir.onClick.AddListener(OnSalirClick);

        if (btnCerrarMarcadores != null) btnCerrarMarcadores.onClick.AddListener(CerrarPaneles);
        if (btnCerrarOpciones != null) btnCerrarOpciones.onClick.AddListener(CerrarPaneles);

        if (toggleFullscreen != null)
        {
            toggleFullscreen.isOn = Screen.fullScreen;
            toggleFullscreen.onValueChanged.AddListener(SetFullscreen);
        }

        if (sliderVolumen != null)
        {
            sliderVolumen.value = PlayerPrefs.GetFloat("Volumen", 0.8f);
            sliderVolumen.onValueChanged.AddListener(SetVolumen);
        }

        AudioManager.SetMasterVolume(PlayerPrefs.GetFloat("Volumen", 0.8f));
    }

    void Start()
    {
        ActualizarProgreso();
        PrepararMarcadores();
    }

    void ActualizarProgreso()
    {
        int dia = DayProgression.DiaGuardado();

        if (btnJugar != null)
        {
            Text etiqueta = btnJugar.GetComponentInChildren<Text>();
            if (etiqueta != null)
                etiqueta.text = dia > 1 ? "CONTINUAR  -  DIA " + dia : "JUGAR  -  DIA 1";
        }

        if (txtProgreso != null)
            txtProgreso.text = "CAMPANA  -  DIA " + dia + " / " + DayProgression.TotalDias +
                               "   |   META " + Mathf.RoundToInt(DayProgression.Get(dia).umbralEficiencia) + "%";
    }

    void ResolverReferencias()
    {
        if (btnJugar == null) btnJugar = BuscarBoton("BtnJugar");
        if (btnMarcadores == null) btnMarcadores = BuscarBoton("BtnMarcadores");
        if (btnOpciones == null) btnOpciones = BuscarBoton("BtnOpciones");
        if (btnSalir == null) btnSalir = BuscarBoton("BtnSalir");

        if (btnCerrarMarcadores == null) btnCerrarMarcadores = BuscarBoton("BtnCerrarMarcadores");
        if (btnCerrarOpciones == null) btnCerrarOpciones = BuscarBoton("BtnCerrarOpciones");

        if (panelMarcadores == null) panelMarcadores = BuscarGO("PanelMarcadores");
        if (panelOpciones == null) panelOpciones = BuscarGO("PanelOpciones");

        if (txtProgreso == null)
        {
            GameObject progresoGO = BuscarGO("TxtProgreso");
            if (progresoGO != null) txtProgreso = progresoGO.GetComponent<Text>();
        }

        if (campoNombre == null)
        {
            GameObject nombreGO = BuscarGO("NombreInput");
            if (nombreGO != null) campoNombre = nombreGO.GetComponent<NameInputField>();
        }

        if (txtTituloMarcadores == null)
        {
            GameObject go = BuscarGO("TituloMarcadores");
            if (go != null) txtTituloMarcadores = go.GetComponent<Text>();
        }

        if (txtSubtituloMarcadores == null)
        {
            GameObject go = BuscarGO("SubTituloMarcadores");
            if (go != null) txtSubtituloMarcadores = go.GetComponent<Text>();
        }

        if (listaMarcadores == null)
        {
            GameObject go = BuscarGO("ListaMarcadores");
            if (go != null) listaMarcadores = go.GetComponent<RectTransform>();
        }

        if (txtVacioMarcadores == null)
        {
            GameObject go = BuscarGO("TxtVacioMarcadores");
            if (go != null) txtVacioMarcadores = go.GetComponent<Text>();
        }

        if (txtTuPuesto == null)
        {
            GameObject go = BuscarGO("TxtTuPuesto");
            if (go != null) txtTuPuesto = go.GetComponent<Text>();
        }

        if (botonesDia == null || botonesDia.Length == 0)
        {
            botonesDia = new Button[DayProgression.TotalDias];
            for (int i = 0; i < botonesDia.Length; i++)
                botonesDia[i] = BuscarBoton("BtnDia" + (i + 1));
        }

        if (toggleFullscreen == null)
        {
            GameObject toggleGO = BuscarGO("ToggleFullscreen");
            if (toggleGO != null) toggleFullscreen = toggleGO.GetComponent<Toggle>();
        }

        if (sliderVolumen == null)
        {
            GameObject sliderGO = BuscarGO("SliderVolumen");
            if (sliderGO != null) sliderVolumen = sliderGO.GetComponent<Slider>();
        }
    }

    static GameObject BuscarGO(string nombre)
    {
        GameObject[] todos = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < todos.Length; i++)
        {
            if (todos[i] != null && todos[i].scene.IsValid() && todos[i].name == nombre)
                return todos[i];
        }
        return GameObject.Find(nombre);
    }

    static Button BuscarBoton(string nombre)
    {
        GameObject go = BuscarGO(nombre);
        return go != null ? go.GetComponent<Button>() : null;
    }

    // ------------------------------------------------------------------ marcadores

    void PrepararMarcadores()
    {
        if (listaMarcadores == null) return;

        if (!filasCreadas)
        {
            CrearFilas();
            filasCreadas = true;
        }

        ConfigurarBotonesDia();

        diaSeleccionado = Mathf.Clamp(DayProgression.DiaGuardado(), 1, DayProgression.TotalDias);
        ActualizarColoresTabs();
        RefrescarMarcadores();
    }

    void ConfigurarBotonesDia()
    {
        if (botonesDia == null) return;

        for (int i = 0; i < botonesDia.Length; i++)
        {
            if (botonesDia[i] == null) continue;

            int dia = i + 1;
            botonesDia[i].onClick.RemoveAllListeners();
            botonesDia[i].onClick.AddListener(delegate
            {
                AudioManager.Play(Sfx.UiClick, 0.65f, 1.12f);
                SeleccionarDia(dia);
            });
        }
    }

    void CrearFilas()
    {
        Font fuente = ObtenerFuente();

        for (int i = 0; i < LeaderboardStore.MaxEntradas; i++)
            filas.Add(CrearFila(i, fuente));
    }

    Font ObtenerFuente()
    {
        if (txtVacioMarcadores != null && txtVacioMarcadores.font != null) return txtVacioMarcadores.font;
        if (txtTuPuesto != null && txtTuPuesto.font != null) return txtTuPuesto.font;
        if (txtTituloMarcadores != null && txtTituloMarcadores.font != null) return txtTituloMarcadores.font;
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    FilaMarcador CrearFila(int indice, Font fuente)
    {
        FilaMarcador fila = new FilaMarcador();

        GameObject go = new GameObject("Fila" + (indice + 1), typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(listaMarcadores, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        Posicionar(rt, new Vector2(0f, 300f - 14.5f - indice * 29f), new Vector2(1440f, 29f));

        fila.go = go;
        fila.fondo = go.AddComponent<Image>();
        fila.fondo.color = ColTransparente;
        fila.fondo.raycastTarget = false;

        GameObject acentoGO = new GameObject("Acento", typeof(RectTransform));
        acentoGO.layer = 5;
        acentoGO.transform.SetParent(go.transform, false);
        Posicionar(acentoGO.GetComponent<RectTransform>(), new Vector2(-706f, 0f), new Vector2(5f, 19f));
        fila.acento = acentoGO.AddComponent<Image>();
        fila.acento.color = ColTransparente;
        fila.acento.raycastTarget = false;

        fila.puesto = CrearTexto(go.transform, "Puesto", fuente, 21, TextAnchor.MiddleCenter, ColTenue);
        Posicionar(fila.puesto.rectTransform, new Vector2(-672f, 0f), new Vector2(70f, 29f));
        fila.puesto.fontStyle = FontStyle.Bold;

        fila.nombre = CrearTexto(go.transform, "Nombre", fuente, 22, TextAnchor.MiddleLeft, ColTexto);
        Posicionar(fila.nombre.rectTransform, new Vector2(-305f, 0f), new Vector2(660f, 29f));

        fila.puntos = CrearTexto(go.transform, "Puntos", fuente, 22, TextAnchor.MiddleRight, ColTexto);
        Posicionar(fila.puntos.rectTransform, new Vector2(330f, 0f), new Vector2(260f, 29f));

        fila.eficiencia = CrearTexto(go.transform, "Eficiencia", fuente, 21, TextAnchor.MiddleRight, ColTexto);
        Posicionar(fila.eficiencia.rectTransform, new Vector2(630f, 0f), new Vector2(170f, 29f));

        if (indice < 3)
            fila.brillos = CrearBrillos(go.transform, indice);

        return fila;
    }

    GameObject[] CrearBrillos(Transform padre, int indiceFila)
    {
        Color color = indiceFila == 0 ? ColOro : (indiceFila == 1 ? ColPlata : ColBronce);
        GameObject[] brillos = new GameObject[4];
        Vector2[] posiciones =
        {
            new Vector2(-712f, 7f),
            new Vector2(-712f, -8f),
            new Vector2(712f, 7f),
            new Vector2(712f, -8f)
        };
        float[] tamanos = { 9f, 6f, 8f, 6f };

        for (int i = 0; i < brillos.Length; i++)
        {
            GameObject go = new GameObject("Brillo" + i, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(padre, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            Posicionar(rt, posiciones[i], new Vector2(tamanos[i], tamanos[i]));
            rt.localRotation = Quaternion.Euler(0f, 0f, 45f);

            Image img = go.AddComponent<Image>();
            img.color = new Color(color.r, color.g, color.b, 1f);
            img.raycastTarget = false;

            UISparkle brillo = go.AddComponent<UISparkle>();
            brillo.Configurar(indiceFila * 1.9f + i * 1.3f);

            brillos[i] = go;
        }

        return brillos;
    }

    static Text CrearTexto(Transform padre, string nombre, Font fuente, int tamano, TextAnchor anclaje, Color color)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(padre, false);

        Text t = go.AddComponent<Text>();
        t.font = fuente;
        t.fontSize = tamano;
        t.alignment = anclaje;
        t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    static void Posicionar(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        rt.localScale = Vector3.one;
    }

    void ActualizarColoresTabs()
    {
        if (botonesDia == null) return;

        for (int i = 0; i < botonesDia.Length; i++)
        {
            if (botonesDia[i] == null) continue;

            bool seleccionado = (i + 1) == diaSeleccionado;
            Image fondo = botonesDia[i].GetComponent<Image>();
            if (fondo != null) fondo.color = seleccionado ? ColOro : ColTabNormal;

            Text etiqueta = botonesDia[i].GetComponentInChildren<Text>();
            if (etiqueta != null) etiqueta.color = seleccionado ? ColTabTexto : ColTenue;
        }
    }

    public void SeleccionarDia(int dia)
    {
        diaSeleccionado = Mathf.Clamp(dia, 1, DayProgression.TotalDias);
        ActualizarColoresTabs();
        RefrescarMarcadores();
    }

    void RefrescarMarcadores()
    {
        if (txtTituloMarcadores != null) txtTituloMarcadores.text = "MARCADORES";

        if (txtSubtituloMarcadores != null)
        {
            float meta = DayProgression.Get(diaSeleccionado).umbralEficiencia;
            txtSubtituloMarcadores.text = "DIA " + diaSeleccionado + " / " + DayProgression.TotalDias +
                                          "   -   META " + Mathf.RoundToInt(meta) + "%" +
                                          "   -   TOP " + LeaderboardStore.MaxEntradas;
        }

        List<LeaderboardEntry> entradas = LeaderboardStore.Top(diaSeleccionado, LeaderboardStore.MaxEntradas);
        string jugador = LeaderboardStore.NombreJugador();

        for (int i = 0; i < filas.Count; i++)
        {
            if (i < entradas.Count) MostrarFila(filas[i], i + 1, entradas[i], jugador);
            else filas[i].go.SetActive(false);
        }

        if (txtVacioMarcadores != null)
        {
            txtVacioMarcadores.gameObject.SetActive(entradas.Count == 0);
            txtVacioMarcadores.text = "AUN NO HAY MARCAS EN EL DIA " + diaSeleccionado +
                                      "\nSE EL PRIMERO EN ENTRAR AL TOP";
        }

        if (txtTuPuesto != null)
        {
            int mejor = LeaderboardStore.MejorPuesto(diaSeleccionado, jugador);

            if (mejor > 0)
            {
                txtTuPuesto.text = jugador + "   -   TU MEJOR PUESTO EN ESTE DIA: #" + mejor;
                txtTuPuesto.color = ColorDePuesto(mejor, true);
            }
            else
            {
                txtTuPuesto.text = jugador + "   -   SIN MARCA EN ESTE DIA";
                txtTuPuesto.color = ColTenue;
            }
        }
    }

    void MostrarFila(FilaMarcador fila, int puesto, LeaderboardEntry entrada, string jugador)
    {
        fila.go.SetActive(true);

        bool esJugador = string.Equals(entrada.nombre, jugador, StringComparison.OrdinalIgnoreCase);
        Color acento = ColorDePuesto(puesto, false);

        fila.fondo.color = puesto == 1 ? ColFondoOro
                         : puesto == 2 ? ColFondoPlata
                         : puesto == 3 ? ColFondoBronce
                         : puesto <= 10 ? ColFondoVerde
                         : ColTransparente;

        fila.acento.color = puesto <= 10 ? acento : ColTransparente;

        fila.puesto.text = puesto.ToString();
        fila.puesto.color = puesto <= 10 ? acento : ColTenue;
        fila.puesto.fontStyle = puesto <= 3 ? FontStyle.Bold : FontStyle.Normal;

        fila.nombre.text = entrada.nombre + (esJugador ? "   -   TU" : "");
        fila.nombre.color = esJugador ? Color.white : (puesto <= 3 ? acento : (puesto <= 10 ? new Color(0.84f, 0.97f, 0.87f) : ColTenue));

        fila.puntos.text = entrada.score + " pts";
        fila.puntos.color = fila.nombre.color;

        fila.eficiencia.text = Mathf.RoundToInt(entrada.eficiencia) + "%";
        fila.eficiencia.color = puesto <= 10 ? acento : ColTenue;

        if (fila.brillos != null)
        {
            bool visibles = puesto <= 3;
            for (int i = 0; i < fila.brillos.Length; i++)
                fila.brillos[i].SetActive(visibles);
        }
    }

    static Color ColorDePuesto(int puesto, bool paraTexto)
    {
        if (puesto == 1) return ColOro;
        if (puesto == 2) return ColPlata;
        if (puesto == 3) return ColBronce;
        if (puesto <= 10) return ColVerde;
        return paraTexto ? ColTenue : ColTransparente;
    }

    public void MostrarMarcadores()
    {
        if (panelMarcadores == null) return;

        if (campoNombre != null) campoNombre.SoltarFoco();

        panelMarcadores.SetActive(true);
        diaSeleccionado = Mathf.Clamp(DayProgression.DiaGuardado(), 1, DayProgression.TotalDias);
        ActualizarColoresTabs();
        RefrescarMarcadores();
    }

    // ------------------------------------------------------------------ navegacion

    void OnJugarClick()
    {
        if (campoNombre != null) campoNombre.SoltarFoco();

        AudioManager.Play(Sfx.UiClick, 0.9f, 1.05f);

        PlayerPrefs.SetString("EscenaDestino", sceneJuego);
        PlayerPrefs.Save();
        SceneManager.LoadScene(sceneCarga);
    }

    void OnMarcadoresClick()
    {
        AudioManager.Play(Sfx.UiClick, 0.8f, 1.1f);
        if (panelOpciones != null) panelOpciones.SetActive(false);
        MostrarMarcadores();
    }

    void OnOpcionesClick()
    {
        AudioManager.Play(Sfx.UiClick, 0.8f, 0.95f);
        if (campoNombre != null) campoNombre.SoltarFoco();
        if (panelMarcadores != null) panelMarcadores.SetActive(false);
        if (panelOpciones == null) return;
        panelOpciones.SetActive(!panelOpciones.activeSelf);
    }

    void OnSalirClick()
    {
        AudioManager.Play(Sfx.UiClick, 0.8f, 0.8f);
        if (campoNombre != null) campoNombre.SoltarFoco();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void CerrarPaneles()
    {
        AudioManager.Play(Sfx.UiClick, 0.8f, 0.9f);
        if (panelMarcadores != null) panelMarcadores.SetActive(false);
        if (panelOpciones != null) panelOpciones.SetActive(false);
    }

    public void SetFullscreen(bool value)
    {
        AudioManager.Play(Sfx.UiClick, 0.7f, value ? 1.2f : 1f);
        Screen.fullScreen = value;
    }

    public void SetVolumen(float value)
    {
        AudioManager.SetMasterVolume(value);
        PlayerPrefs.SetFloat("Volumen", value);

        // El slider apenas tiene sonido propio: un blip corto para oir el cambio.
        if (Time.unscaledTime >= proximoPreviewVolumen && value > 0.01f)
        {
            proximoPreviewVolumen = Time.unscaledTime + 0.16f;
            AudioManager.Play(Sfx.Tick, 0.85f, 1.4f);
        }
    }
}
