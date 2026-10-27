using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Pantalla de resolucion del turno. Lee el resultado guardado por GameManager,
/// decide Victoria (You Win) o Derrota (Game Over) segun el umbral de eficiencia
/// del dia, muestra el reporte, registra la marca en la tabla de ese dia y
/// permite reintentar o volver al menu.
/// </summary>
public class GameOverManager : MonoBehaviour
{
    [Header("Referencias UI")]
    public CanvasGroup panelPrincipal;
    public Text txtTitulo;
    public Text txtSubtitulo;
    public Image franja;
    public Text txtEficiencia;
    public Image barraEficiencia;
    public Text txtObjetivo;
    public Text txtSiguiente;
    public Image marcadorObjetivo;
    public Text txtEstadisticas;
    public Text txtPuntos;
    public Text txtMensaje;
    public Text txtRango;
    public Text txtAyuda;
    public Button btnReiniciar;
    public Button btnMenu;

    [Header("Escenas")]
    public string escenaJuego = "GameScene";
    public string escenaCarga = "LoadingScene";
    public string escenaMenu = "MenuScene";

    [Header("Umbrales del GDD")]
    [Tooltip("Dias 1-2 (tutorial): eficiencia minima para superar el dia.")]
    public float umbralDiasTutorial = 60f;
    [Tooltip("Dia 3 en adelante: eficiencia minima para superar el dia.")]
    public float umbralNormal = 75f;

    // --- datos del ultimo turno ---
    int dia;
    int diaSiguiente;
    int puntos;
    int atendidos;
    int abandonos;
    int llegados;
    float eficiencia;
    float umbral;
    bool victoria;
    bool ultimoDia;
    bool campanaCompletada;
    bool nuevoRecord;
    int puesto;
    bool navegando;
    Text txtBotonPrincipal;

    readonly Color colVerde = new Color(0.35f, 0.90f, 0.45f);
    readonly Color colRojo = new Color(1f, 0.35f, 0.35f);
    readonly Color colAmbar = new Color(0.98f, 0.78f, 0.25f);
    readonly Color colOro = new Color(1f, 0.84f, 0.35f);
    readonly Color colPlata = new Color(0.82f, 0.86f, 0.94f);
    readonly Color colBronce = new Color(0.88f, 0.58f, 0.31f);
    readonly Color colNeutro = new Color(0.60f, 0.63f, 0.72f);

    void Awake()
    {
        ResolverReferencias();

        AudioManager.Ensure();
        AudioManager.SetMusicLevel(0.55f);
        AudioManager.SetMusicDuck(false);

        if (btnReiniciar != null)
        {
            btnReiniciar.onClick.AddListener(Reintentar);
            txtBotonPrincipal = btnReiniciar.GetComponentInChildren<Text>();
        }
        if (btnMenu != null) btnMenu.onClick.AddListener(VolverAlMenu);
    }

    void Start()
    {
        Time.timeScale = 1f;

        CargarDatos();
        EvaluarResultado();
        AvanzarProgreso();
        RefrescarUI();
        RegistrarEnMarcadores();

        AudioManager.Play(campanaCompletada ? Sfx.Victory : (victoria ? Sfx.Victory : Sfx.Defeat), 1f, 1f);

        if (panelPrincipal != null) panelPrincipal.alpha = 0f;
    }

    void Update()
    {
        if (panelPrincipal != null && panelPrincipal.alpha < 1f)
            panelPrincipal.alpha = Mathf.MoveTowards(panelPrincipal.alpha, 1f, Time.unscaledDeltaTime * 3.5f);

        if (navegando) return;

        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
            Reintentar();
        else if (kb.escapeKey.wasPressedThisFrame)
            VolverAlMenu();
    }

    // ------------------------------------------------------------------ datos

    void CargarDatos()
    {
        dia = Mathf.Clamp(PlayerPrefs.GetInt("UltimoDia", DayProgression.DiaGuardado()), 1, DayProgression.TotalDias);
        puntos = PlayerPrefs.GetInt("UltimosPuntos", PlayerPrefs.GetInt("UltimoScore", 0));
        eficiencia = Mathf.Clamp(PlayerPrefs.GetFloat("UltimaEficiencia", 0f), 0f, 100f);
        atendidos = Mathf.Max(0, PlayerPrefs.GetInt("UltimosAtendidos", 0));
        abandonos = Mathf.Max(0, PlayerPrefs.GetInt("UltimosAbandonos", 0));
        llegados = Mathf.Max(atendidos, PlayerPrefs.GetInt("UltimosLlegados", atendidos));
    }

    void EvaluarResultado()
    {
        umbral = DayProgression.Get(dia).umbralEficiencia;
        victoria = eficiencia >= umbral;
        ultimoDia = dia >= DayProgression.TotalDias;
        campanaCompletada = victoria && ultimoDia;
        diaSiguiente = ultimoDia ? dia : DayProgression.SiguienteDia(dia);
    }

    /// <summary>
    /// Al superar un dia se desbloquea el siguiente una sola vez por partida.
    /// Si se recarga esta pantalla no se avanza dos veces; si se pierde, se
    /// repite exactamente el mismo dia.
    /// </summary>
    void AvanzarProgreso()
    {
        if (!victoria)
        {
            DayProgression.GuardarDia(dia);
            return;
        }

        if (ultimoDia)
        {
            DayProgression.GuardarDia(DayProgression.TotalDias);
            return;
        }

        string runId = PlayerPrefs.GetString("UltimoRunId", "");

        if (!string.IsNullOrEmpty(runId) && PlayerPrefs.GetString("DiaAvanzadoRun", "") == runId)
        {
            diaSiguiente = DayProgression.DiaGuardado();
            return;
        }

        DayProgression.GuardarDia(dia + 1);
        if (!string.IsNullOrEmpty(runId)) PlayerPrefs.SetString("DiaAvanzadoRun", runId);
        PlayerPrefs.Save();

        diaSiguiente = DayProgression.DiaGuardado();
    }

    // ------------------------------------------------------------------ UI

    void RefrescarUI()
    {
        Color acento = campanaCompletada ? colOro : (victoria ? colVerde : colRojo);
        Color colorEff = eficiencia >= umbral ? colVerde : (eficiencia >= umbral * 0.55f ? colAmbar : colRojo);

        if (txtTitulo != null)
        {
            txtTitulo.text = campanaCompletada ? "CAMPUS COMPLETADO" : (victoria ? "YOU WIN" : "GAME OVER");
            txtTitulo.color = acento;
            txtTitulo.fontSize = campanaCompletada ? 62 : 84;
        }

        if (txtSubtitulo != null)
        {
            if (campanaCompletada) txtSubtitulo.text = "LOS 10 DIAS SUPERADOS";
            else if (victoria) txtSubtitulo.text = "DIA " + dia + " SUPERADO";
            else txtSubtitulo.text = "DIA " + dia + " NO SUPERADO";

            txtSubtitulo.color = new Color(0.82f, 0.85f, 0.92f);
        }

        if (franja != null) franja.color = acento;

        if (txtEficiencia != null)
        {
            txtEficiencia.text = Mathf.RoundToInt(eficiencia) + "%";
            txtEficiencia.color = colorEff;
        }

        if (barraEficiencia != null)
        {
            barraEficiencia.fillAmount = Mathf.Clamp01(eficiencia / 100f);
            barraEficiencia.color = colorEff;
        }

        if (txtObjetivo != null)
            txtObjetivo.text = "OBJETIVO DEL DIA " + dia + "   " + Mathf.RoundToInt(umbral) + "%";

        if (txtSiguiente != null)
        {
            txtSiguiente.text = TextoSiguiente();
            txtSiguiente.color = campanaCompletada ? colOro : (victoria ? colVerde : new Color(0.78f, 0.80f, 0.88f));
        }

        if (marcadorObjetivo != null)
        {
            RectTransform rt = marcadorObjetivo.rectTransform;
            float u = Mathf.Clamp01(umbral / 100f);
            rt.anchorMin = new Vector2(u, 0.5f);
            rt.anchorMax = new Vector2(u, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(4f, 46f);
        }

        if (txtEstadisticas != null)
        {
            txtEstadisticas.text = "ATENDIDOS " + atendidos +
                                   "      ABANDONOS " + abandonos +
                                   "      LLEGADOS " + llegados;
        }

        if (txtPuntos != null)
            txtPuntos.text = puntos + " PTS";

        if (txtMensaje != null)
        {
            txtMensaje.text = MensajeResultado();
            txtMensaje.color = new Color(0.80f, 0.83f, 0.92f);
        }

        if (txtAyuda != null)
        {
            txtAyuda.text = "ENTER para " + (victoria ? (campanaCompletada ? "reiniciar" : "avanzar") : "reintentar") +
                            "   |   ESC para volver al menu";
        }

        if (txtBotonPrincipal != null)
            txtBotonPrincipal.text = EtiquetaBotonPrincipal();
    }

    string TextoSiguiente()
    {
        if (campanaCompletada)
            return "CAMPUS COMPLETADO  -  REINICIA PARA VOLVER AL DIA 1";

        if (victoria)
        {
            DayConfig sig = DayProgression.Get(diaSiguiente);
            return "SIGUIENTE: DIA " + sig.dia + " / " + DayProgression.TotalDias +
                   "   -   META " + Mathf.RoundToInt(sig.umbralEficiencia) + "%";
        }

        DayConfig rep = DayProgression.Get(dia);
        return "REINTENTA EL DIA " + rep.dia + "   -   META " + Mathf.RoundToInt(rep.umbralEficiencia) + "%";
    }

    string EtiquetaBotonPrincipal()
    {
        if (campanaCompletada) return "REINICIAR CAMPUS";
        return victoria ? "SIGUIENTE DIA" : "REINTENTAR DIA";
    }

    string MensajeResultado()
    {
        string jugador = LeaderboardStore.NombreJugador();

        if (campanaCompletada) return "Diez dias, una sola sala y ni un solo motin. " + jugador + " es leyenda del campus.";
        if (eficiencia >= 95f) return "Sala perfecta: ni un solo conflicto. " + jugador + " esta orgulloso.";
        if (eficiencia >= 85f) return "Gestion impecable: la biblioteca fluyo sin caos.";
        if (victoria) return "Objetivo cumplido, aunque la sala estuvo a punto de desbordarse.";
        if (eficiencia >= 40f) return "La sala se te fue de las manos. Revisa la fila antes de que abandone.";
        return "Colapso total: demasiados alumnos se fueron sin ser atendidos.";
    }

    // ------------------------------------------------------------------ marcadores

    void RegistrarEnMarcadores()
    {
        if (llegados <= 0)
        {
            if (txtRango != null)
            {
                txtRango.text = "PARTIDA SIN REGISTRO";
                txtRango.color = colNeutro;
            }
            return;
        }

        string runId = PlayerPrefs.GetString("UltimoRunId", "");
        bool yaGuardado = !string.IsNullOrEmpty(runId) &&
                          PlayerPrefs.GetString("RankGuardadoRun", "") == runId;

        if (yaGuardado)
        {
            puesto = PlayerPrefs.GetInt("UltimoPuesto", 0);
            nuevoRecord = PlayerPrefs.GetInt("UltimoNuevoRecord", 0) == 1;

            if (puesto <= 0)
            {
                puesto = LeaderboardStore.MejorPuesto(dia, LeaderboardStore.NombreJugador());
                nuevoRecord = puesto == 1;
            }
        }
        else
        {
            LeaderboardStore.ResumenRegistro resumen = LeaderboardStore.Registrar(dia, puntos, eficiencia);
            puesto = resumen.puesto;
            nuevoRecord = resumen.nuevoRecord;

            if (!string.IsNullOrEmpty(runId)) PlayerPrefs.SetString("RankGuardadoRun", runId);
            PlayerPrefs.SetInt("UltimoPuesto", puesto);
            PlayerPrefs.SetInt("UltimoNuevoRecord", nuevoRecord ? 1 : 0);
            PlayerPrefs.Save();
        }

        MostrarRango();
    }

    void MostrarRango()
    {
        if (txtRango == null) return;

        string jugador = LeaderboardStore.NombreJugador();

        if (nuevoRecord && puesto == 1)
        {
            txtRango.text = jugador + "  -  NUEVO RECORD  -  #1 DEL DIA " + dia;
            txtRango.color = colOro;
            return;
        }

        if (puesto >= 1 && puesto <= LeaderboardStore.MaxEntradas)
        {
            txtRango.text = jugador + "  -  PUESTO #" + puesto + " DEL DIA " + dia;
            txtRango.color = ColorDePuesto(puesto);
            return;
        }

        txtRango.text = jugador + "  -  FUERA DEL TOP " + LeaderboardStore.MaxEntradas + " DEL DIA " + dia;
        txtRango.color = colNeutro;
    }

    Color ColorDePuesto(int posicion)
    {
        if (posicion == 1) return colOro;
        if (posicion == 2) return colPlata;
        if (posicion == 3) return colBronce;
        if (posicion <= 10) return colVerde;
        return colNeutro;
    }

    // ------------------------------------------------------------------ navegacion

    public void Reintentar()
    {
        if (navegando) return;
        navegando = true;

        AudioManager.Play(Sfx.UiClick, 0.9f, 1.05f);
        Time.timeScale = 1f;

        // Victoria normal: el dia ya quedo desbloqueado en AvanzarProgreso.
        // Derrota: se repite el mismo dia. Campana completada: vuelta al dia 1.
        if (campanaCompletada) DayProgression.Reiniciar();
        else if (!victoria) DayProgression.GuardarDia(dia);

        PlayerPrefs.SetString("EscenaDestino", escenaJuego);
        PlayerPrefs.Save();
        SceneManager.LoadScene(escenaCarga);
    }

    public void VolverAlMenu()
    {
        if (navegando) return;
        navegando = true;

        AudioManager.Play(Sfx.UiClick, 0.9f, 0.9f);
        Time.timeScale = 1f;
        SceneManager.LoadScene(escenaMenu);
    }

    // ------------------------------------------------------------------ referencias

    void ResolverReferencias()
    {
        if (panelPrincipal == null)
        {
            GameObject go = GameObject.Find("PanelReporte");
            if (go != null) panelPrincipal = go.GetComponent<CanvasGroup>();
        }

        if (txtTitulo == null) txtTitulo = BuscarTexto("TxtTitulo");
        if (txtSubtitulo == null) txtSubtitulo = BuscarTexto("TxtSubtitulo");
        if (txtEficiencia == null) txtEficiencia = BuscarTexto("TxtEficiencia");
        if (txtObjetivo == null) txtObjetivo = BuscarTexto("TxtObjetivo");
        if (txtSiguiente == null) txtSiguiente = BuscarTexto("TxtSiguiente");
        if (txtEstadisticas == null) txtEstadisticas = BuscarTexto("TxtEstadisticas");
        if (txtPuntos == null) txtPuntos = BuscarTexto("TxtPuntos");
        if (txtMensaje == null) txtMensaje = BuscarTexto("TxtMensaje");
        if (txtRango == null) txtRango = BuscarTexto("TxtRango");
        if (txtAyuda == null) txtAyuda = BuscarTexto("TxtAyuda");

        if (franja == null)
        {
            GameObject go = GameObject.Find("Franja");
            if (go != null) franja = go.GetComponent<Image>();
        }

        if (barraEficiencia == null)
        {
            GameObject go = GameObject.Find("BarraEficiencia");
            if (go != null) barraEficiencia = go.GetComponent<Image>();
        }

        if (marcadorObjetivo == null)
        {
            GameObject go = GameObject.Find("MarcadorObjetivo");
            if (go != null) marcadorObjetivo = go.GetComponent<Image>();
        }

        if (btnReiniciar == null) btnReiniciar = BuscarBoton("BtnReiniciar");
        if (btnMenu == null) btnMenu = BuscarBoton("BtnMenu");
    }

    static Text BuscarTexto(string nombre)
    {
        GameObject go = GameObject.Find(nombre);
        return go != null ? go.GetComponent<Text>() : null;
    }

    static Button BuscarBoton(string nombre)
    {
        GameObject go = GameObject.Find(nombre);
        return go != null ? go.GetComponent<Button>() : null;
    }
}
