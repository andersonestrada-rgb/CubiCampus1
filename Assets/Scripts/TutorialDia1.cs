using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Tutorial guiado para el Día 1. Solo se muestra la primera vez.
/// Avanzable con clic, Espacio o Enter. Botón para saltar.
/// Resalta zonas clave (fila, mesas, alumnos) sin bloquear todo el juego.
/// </summary>
public class TutorialDia1 : MonoBehaviour
{
    public const string PrefKey = "TutorialDia1Completado";
    
    [Header("Referencias")]
    public GameManager game;
    public HUDGame hud;
    public Canvas canvas;
    public Camera cam;
    
    [Header("UI Posicion")]
    public Vector2 posicionDialogo = new Vector2(180, 110);
    public Vector2 posicionNPC = new Vector2(70, 75);
    
    // UI
    GameObject panelTutorial;
    GameObject panelFondo;
    GameObject panelDialogo;
    GameObject npcAvatar;
    GameObject globo;
    Text txtNombreNPC;
    Text txtDialogo;
    Text txtContador;
    Text txtAyuda;
    Button btnSiguiente;
    Button btnSaltar;
    GameObject highlightBox;
    RectTransform rtDialogo;
    RectTransform rtAvatar;
    
    // Estado
    int pasoActual = -1;
    bool activo = false;
    readonly List<PasoTutorial> pasos = new List<PasoTutorial>();
    
    class PasoTutorial
    {
        public string titulo;
        public string descripcion;
        public Vector3 highlightPos;
        public Vector2 highlightSize;
        public bool requiereAccion;
        public string accionHint;
    }
    
    void Awake()
    {
        if (cam == null) cam = Camera.main;
        CrearUI();
        CrearPasos();
        if (!Application.isPlaying)
        {
            if (panelTutorial != null) panelTutorial.SetActive(true);
            if (npcAvatar != null) npcAvatar.SetActive(true);
            if (panelDialogo != null) panelDialogo.SetActive(true);
            if (highlightBox != null) highlightBox.SetActive(false);
            if (btnSiguiente != null) btnSiguiente.gameObject.SetActive(true);
            if (btnSaltar != null) btnSaltar.gameObject.SetActive(true);
            MoverBotonesDebajo();
            if (txtNombreNPC != null) txtNombreNPC.text = "JEFE DE CAMPUS";
            if (txtDialogo != null) txtDialogo.text = "Mueve posicionDialogo y posicionNPC en el Inspector";
            if (txtContador != null) txtContador.text = "1 / 6";
            if (rtDialogo != null) rtDialogo.anchoredPosition = posicionDialogo;
            if (rtAvatar != null) rtAvatar.anchoredPosition = posicionNPC;
            return;
        }
    }
    
    void Start()
    {
        // SOLO mostrar en GameScene
        string escenaActual = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (escenaActual != "GameScene")
        {
            Destroy(gameObject);
            return;
        }
        
        if (game == null) game = GameManager.Instance;
        if (hud == null) hud = FindAnyObjectByType<HUDGame>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        
        // Si está desactivado desde menú -> no mostrar
        if (PlayerPrefs.GetInt("MostrarTutorial", 1) == 0)
        {
            Desactivar();
            Destroy(gameObject);
            return;
        }
        
        IniciarTutorial();
    }
    
    void Update()
    {
        if (!activo) return;
        
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            {
                SiguientePaso();
            }
            if (kb.escapeKey.wasPressedThisFrame)
            {
                SaltarTutorial();
            }
        }
        
        // Click para avanzar
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            // No avanzar si clickamos sobre botones
            UnityEngine.EventSystems.PointerEventData ped = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            ped.position = mouse.position.ReadValue();
            System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult> results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(ped, results);
            bool sobreBoton = false;
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].gameObject.GetComponent<Button>() != null) { sobreBoton = true; break; }
            }
            if (!sobreBoton) SiguientePaso();
        }
        
        ActualizarHighlight();
    }
    
    void CrearUI()
    {
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;
        
        // Panel principal
        panelTutorial = new GameObject("TutorialPanel");
        panelTutorial.transform.SetParent(canvas.transform, false);
        RectTransform rtPanel = panelTutorial.AddComponent<RectTransform>();
        rtPanel.anchorMin = Vector2.zero;
        rtPanel.anchorMax = Vector2.one;
        rtPanel.sizeDelta = Vector2.zero;
        
        // Fondo muy tenue para no tapar todo
        panelFondo = new GameObject("Fondo");
        panelFondo.transform.SetParent(panelTutorial.transform, false);
        RectTransform rtFondo = panelFondo.AddComponent<RectTransform>();
        rtFondo.anchorMin = Vector2.zero;
        rtFondo.anchorMax = Vector2.one;
        rtFondo.sizeDelta = Vector2.zero;
        Image imgFondo = panelFondo.AddComponent<Image>();
        imgFondo.color = new Color(0f, 0f, 0f, 0.45f);
        
        // Highlight box
        highlightBox = new GameObject("Highlight");
        highlightBox.transform.SetParent(panelTutorial.transform, false);
        RectTransform rtHL = highlightBox.AddComponent<RectTransform>();
        rtHL.sizeDelta = new Vector2(300, 200);
        Image imgHL = highlightBox.AddComponent<Image>();
        imgHL.color = Color.clear;
        CrearRect(highlightBox.transform, new Vector2(0, 148), new Vector2(600, 6), new Color(1f, 0.9f, 0.3f)); // top
        CrearRect(highlightBox.transform, new Vector2(0, -148), new Vector2(600, 6), new Color(1f, 0.9f, 0.3f)); // bottom
        CrearRect(highlightBox.transform, new Vector2(-297, 0), new Vector2(6, 300), new Color(1f, 0.9f, 0.3f)); // left
        CrearRect(highlightBox.transform, new Vector2(297, 0), new Vector2(6, 300), new Color(1f, 0.9f, 0.3f)); // right
        highlightBox.SetActive(false);
        
        // Panel dialogo tipo nube
        panelDialogo = new GameObject("DialogoNube");
        panelDialogo.transform.SetParent(panelTutorial.transform, false);
        rtDialogo = panelDialogo.AddComponent<RectTransform>();
        rtDialogo.anchorMin = new Vector2(0f, 0f);
        rtDialogo.anchorMax = new Vector2(0f, 0f);
        rtDialogo.pivot = new Vector2(0f, 0f);
        rtDialogo.sizeDelta = new Vector2(680, 190);
        rtDialogo.anchoredPosition = posicionDialogo;
        Image imgDialogo = panelDialogo.AddComponent<Image>();
        imgDialogo.color = new Color(0.98f, 0.99f, 1f, 1f);
        // Borde
        CrearRect(panelDialogo.transform, new Vector2(0, 93), new Vector2(680, 4), new Color(0.15f, 0.18f, 0.25f));
        CrearRect(panelDialogo.transform, new Vector2(0, -93), new Vector2(680, 4), new Color(0.15f, 0.18f, 0.25f));
        CrearRect(panelDialogo.transform, new Vector2(-338, 0), new Vector2(4, 190), new Color(0.15f, 0.18f, 0.25f));
        CrearRect(panelDialogo.transform, new Vector2(338, 0), new Vector2(4, 190), new Color(0.15f, 0.18f, 0.25f));
        // Pico de la nube
        GameObject pico = new GameObject("Pico");
        pico.transform.SetParent(panelDialogo.transform, false);
        RectTransform rtPico = pico.AddComponent<RectTransform>();
        rtPico.anchorMin = new Vector2(0f, 0f);
        rtPico.anchorMax = new Vector2(0f, 0f);
        rtPico.pivot = new Vector2(0.5f, 1f);
        rtPico.sizeDelta = new Vector2(26, 26);
        rtPico.anchoredPosition = new Vector2(60, -2);
        rtPico.rotation = Quaternion.Euler(0, 0, 45);
        Image imgPico = pico.AddComponent<Image>();
        imgPico.color = new Color(0.98f, 0.99f, 1f, 1f);
        CrearRect(pico.transform, new Vector2(-15, 0), new Vector2(4, 30), new Color(0.15f, 0.18f, 0.25f));
        CrearRect(pico.transform, new Vector2(0, 15), new Vector2(30, 4), new Color(0.15f, 0.18f, 0.25f));
        
        // Avatar NPC (cuadrado redondeado geométrico)
        npcAvatar = new GameObject("NPC_Avatar");
        npcAvatar.transform.SetParent(panelTutorial.transform, false);
        rtAvatar = npcAvatar.AddComponent<RectTransform>();
        rtAvatar.anchorMin = new Vector2(0f, 0f);
        rtAvatar.anchorMax = new Vector2(0f, 0f);
        rtAvatar.pivot = new Vector2(0.5f, 0f);
        rtAvatar.sizeDelta = new Vector2(110, 130);
        rtAvatar.anchoredPosition = posicionNPC;
        Image imgAvatar = npcAvatar.AddComponent<Image>();
        imgAvatar.color = new Color(0.25f, 0.45f, 0.9f, 1f);
        // Detalles geométricos
        CrearRect(npcAvatar.transform, new Vector2(0, 20), new Vector2(70, 70), new Color(0.95f, 0.85f, 0.75f)); // cara
        CrearRect(npcAvatar.transform, new Vector2(-12, 28), new Vector2(10, 10), Color.black); // ojo izq
        CrearRect(npcAvatar.transform, new Vector2(12, 28), new Vector2(10, 10), Color.black); // ojo der
        CrearRect(npcAvatar.transform, new Vector2(0, 10), new Vector2(18, 4), Color.black); // boca
        CrearRect(npcAvatar.transform, new Vector2(0, -35), new Vector2(60, 40), new Color(0.2f, 0.35f, 0.75f)); // cuerpo
        CrearRect(npcAvatar.transform, new Vector2(0, -30), new Vector2(40, 8), new Color(1f, 0.84f, 0.35f)); // corbata
        
        // Nombre NPC
        GameObject goNombre = new GameObject("TxtNombre");
        goNombre.transform.SetParent(panelDialogo.transform, false);
        RectTransform rtNombre = goNombre.AddComponent<RectTransform>();
        rtNombre.anchorMin = new Vector2(0f, 1f);
        rtNombre.anchorMax = new Vector2(1f, 1f);
        rtNombre.pivot = new Vector2(0f, 1f);
        rtNombre.sizeDelta = new Vector2(-30, 35);
        rtNombre.anchoredPosition = new Vector2(20, -8);
        txtNombreNPC = goNombre.AddComponent<Text>();
        txtNombreNPC.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txtNombreNPC.fontSize = 24;
        txtNombreNPC.color = new Color(0.15f, 0.18f, 0.25f);
        txtNombreNPC.alignment = TextAnchor.MiddleLeft;
        txtNombreNPC.fontStyle = FontStyle.Bold;
        txtNombreNPC.text = "JEFE DE CAMPUS";
        
        // Dialogo
        GameObject goDialogo = new GameObject("TxtDialogo");
        goDialogo.transform.SetParent(panelDialogo.transform, false);
        RectTransform rtDlg = goDialogo.AddComponent<RectTransform>();
        rtDlg.anchorMin = new Vector2(0f, 0f);
        rtDlg.anchorMax = new Vector2(1f, 1f);
        rtDlg.offsetMin = new Vector2(25, 40);
        rtDlg.offsetMax = new Vector2(-25, -50);
        txtDialogo = goDialogo.AddComponent<Text>();
        txtDialogo.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txtDialogo.fontSize = 22;
        txtDialogo.color = new Color(0.12f, 0.14f, 0.2f);
        txtDialogo.alignment = TextAnchor.UpperLeft;
        txtDialogo.horizontalOverflow = HorizontalWrapMode.Wrap;
        txtDialogo.verticalOverflow = VerticalWrapMode.Truncate;
        txtDialogo.lineSpacing = 1.1f;
        
        // Contador
        GameObject goCont = new GameObject("TxtContador");
        goCont.transform.SetParent(panelDialogo.transform, false);
        RectTransform rtCont = goCont.AddComponent<RectTransform>();
        rtCont.anchorMin = new Vector2(1f, 0f);
        rtCont.anchorMax = new Vector2(1f, 0f);
        rtCont.pivot = new Vector2(1f, 0f);
        rtCont.sizeDelta = new Vector2(120, 30);
        rtCont.anchoredPosition = new Vector2(-20, 12);
        txtContador = goCont.AddComponent<Text>();
        txtContador.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txtContador.fontSize = 18;
        txtContador.color = new Color(0.4f, 0.44f, 0.52f);
        txtContador.alignment = TextAnchor.MiddleRight;
        
        // Ayuda
        GameObject goAyuda = new GameObject("TxtAyuda");
        goAyuda.transform.SetParent(panelDialogo.transform, false);
        RectTransform rtAyuda = goAyuda.AddComponent<RectTransform>();
        rtAyuda.anchorMin = new Vector2(0f, 0f);
        rtAyuda.anchorMax = new Vector2(0f, 0f);
        rtAyuda.pivot = new Vector2(0f, 0f);
        rtAyuda.sizeDelta = new Vector2(320, 30);
        rtAyuda.anchoredPosition = new Vector2(25, 12);
        txtAyuda = goAyuda.AddComponent<Text>();
        txtAyuda.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txtAyuda.fontSize = 18;
        txtAyuda.color = new Color(0.35f, 0.38f, 0.48f);
        txtAyuda.alignment = TextAnchor.MiddleLeft;
        txtAyuda.text = "Clic / ESPACIO / ENTER para continuar";
        
        // Botones
        btnSaltar = CrearBotonPanel(panelDialogo.transform, "SALTAR TUTORIAL", new Vector2(170, 32), new Vector2(660, -6), delegate { SaltarTutorial(); }, new Color(0.85f, 0.4f, 0.4f, 1f));
        btnSiguiente = CrearBotonPanel(panelDialogo.transform, "SIGUIENTE >", new Vector2(150, 32), new Vector2(490, -6), delegate { SiguientePaso(); }, new Color(0.25f, 0.55f, 0.35f, 1f));
        
        Desactivar();
    }
    
    GameObject CrearRect(Transform parent, Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = new GameObject("Borde");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }
    
    Button CrearBotonPanel(Transform parent, string texto, Vector2 size, Vector2 pos, UnityEngine.Events.UnityAction accion, Color bg)
    {
        GameObject go = new GameObject("Btn_" + texto);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        Image img = go.AddComponent<Image>();
        img.color = bg;
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock cb = btn.colors;
        cb.normalColor = bg;
        cb.highlightedColor = bg * 1.2f;
        cb.pressedColor = bg * 0.8f;
        btn.colors = cb;
        btn.onClick.AddListener(accion);
        
        GameObject goTxt = new GameObject("Texto");
        goTxt.transform.SetParent(go.transform, false);
        RectTransform rtTxt = goTxt.AddComponent<RectTransform>();
        rtTxt.anchorMin = Vector2.zero;
        rtTxt.anchorMax = Vector2.one;
        rtTxt.sizeDelta = Vector2.zero;
        Text txt = goTxt.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 18;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.text = texto;
        return btn;
    }
    
    void CrearPasos()
    {
        pasos.Clear();
        pasos.Add(new PasoTutorial { titulo = "BIENVENIDA", descripcion = "¡Hola, nuevo becario!\nYo soy el Jefe de Campus y te voy a enseñar lo esencial para mantener esta sala funcionando.", highlightPos = Vector3.zero, highlightSize = Vector2.zero });
        pasos.Add(new PasoTutorial { titulo = "ASIGNAR ALUMNOS", descripcion = "Mira la FILA de la izquierda. Arrastra cada alumno hasta una MESA libre.\nSi es su MESA IDEAL recibirás más puntos. Si es otra mesa válida, también funciona pero con menos recompensa.", highlightPos = new Vector3(-5.5f, -0.5f, 0), highlightSize = new Vector2(5, 4) });
        pasos.Add(new PasoTutorial { titulo = "AYUDAR A IRSE", descripcion = "Cuando un alumno acaba y queda LISTO, aparece sobre él un indicador.\nPara que recoja y se vaya, ¡HAZ CLIC repetidos sobre ese alumno!\nCuantos más clics, antes se marcha.", highlightPos = Vector3.zero, highlightSize = new Vector2(2.5f, 2.5f) });
        pasos.Add(new PasoTutorial { titulo = "APARTADOS Y LIMPIEZA", descripcion = "De vez en cuando verás un APARTADO en alguna mesa: estará bloqueada.\nPara quitarlo, mantén el CLIC y mueve el cursor sobre la mancha arrastrando hasta que desaparezca.\nTambién tendrás que limpiar manchas sueltas de la misma forma.", highlightPos = Vector3.zero, highlightSize = new Vector2(3.5f, 2.8f) });
        pasos.Add(new PasoTutorial { titulo = "RUSH Y BONO", descripcion = "En los ÚLTIMOS 30 SEGUNDOS llega el RUSH: más alumnos en menos tiempo.\nSi aparece el botón de BONO DE SILENCIO, úsalo con inteligencia: limpia toda la sala y congela la paciencia 6 segundos.\nSiempre puedes PAUSAR con el botón superior.", highlightPos = Vector3.zero, highlightSize = new Vector2(3, 2) });
        pasos.Add(new PasoTutorial { titulo = "TODO LISTO", descripcion = "Ya lo tienes claro.\nRecuerda: mantén la EFICIENCIA por encima del OBJETIVO del día para superarlo.\n¡VAMOS, que empieza la jornada!", highlightPos = Vector3.zero, highlightSize = Vector2.zero });
    }
    
    void IniciarTutorial()
    {
        activo = true;
        pasoActual = -1;
        if (panelTutorial != null) panelTutorial.SetActive(true);
        if (npcAvatar != null) npcAvatar.SetActive(true);
        if (panelDialogo != null) panelDialogo.SetActive(true);
        MoverBotonesDebajo();
        SiguientePaso();
        if (game != null) game.SetPaused(true);
        if (AudioManager.Instance != null) AudioManager.Play(Sfx.UiClick, 0.7f, 1.1f);
    }

    void MoverBotonesDebajo()
    {
        if (panelDialogo == null) return;

        if (btnSaltar != null)
        {
            RectTransform rt = btnSaltar.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.SetParent(panelDialogo.transform, false);
                rt.anchorMin = new Vector2(1f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(1f, 0f);
                rt.anchoredPosition = new Vector2(-15, -38);
            }
        }

        if (btnSiguiente != null)
        {
            RectTransform rt = btnSiguiente.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.SetParent(panelDialogo.transform, false);
                rt.anchorMin = new Vector2(1f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(1f, 0f);
                rt.anchoredPosition = new Vector2(-200, -38);
            }
        }
    }
    
    void SiguientePaso()
    {
        pasoActual++;
        if (pasoActual >= pasos.Count)
        {
            FinalizarTutorial();
            return;
        }
        
        MostrarPaso(pasos[pasoActual]);
        if (AudioManager.Instance != null) AudioManager.Play(Sfx.UiClick, 0.7f, 1.1f);
    }
    
    void MostrarPaso(PasoTutorial paso)
    {
        if (txtNombreNPC != null) txtNombreNPC.text = "JEFE DE CAMPUS";
        if (txtDialogo != null) txtDialogo.text = paso.descripcion;
        if (txtContador != null) txtContador.text = (pasoActual + 1) + " / " + pasos.Count;
        
        if (paso.highlightSize.magnitude > 0.1f && highlightBox != null)
        {
            highlightBox.SetActive(true);
            RectTransform rtHL = highlightBox.GetComponent<RectTransform>();
            Vector3 worldPos = paso.highlightPos;
            if (cam != null)
            {
                Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
                rtHL.position = screenPos;
            }
            else rtHL.anchoredPosition = worldPos;
            rtHL.sizeDelta = paso.highlightSize * 110f;
        }
        else if (highlightBox != null)
        {
            highlightBox.SetActive(false);
        }
        
        if (btnSiguiente != null)
        {
            Text txtBtn = btnSiguiente.GetComponentInChildren<Text>();
            if (pasoActual == pasos.Count - 1)
            {
                if (txtBtn != null) txtBtn.text = "¡VAMOS! >";
            }
            else
            {
                if (txtBtn != null) txtBtn.text = "SIGUIENTE >";
            }
        }
    }
    
    void ActualizarHighlight()
    {
        if (!activo || highlightBox == null || !highlightBox.activeSelf) return;
        // Mantener highlight visible
    }
    
    void SaltarTutorial()
    {
        if (AudioManager.Instance != null) AudioManager.Play(Sfx.UiClick, 0.7f, 0.9f);
        FinalizarTutorial();
    }
    
    void FinalizarTutorial()
    {
        activo = false;
        PlayerPrefs.Save();
        Desactivar();
        if (game != null && game.IsPaused) game.SetPaused(false);
        if (AudioManager.Instance != null) AudioManager.Play(Sfx.UiClick, 0.8f, 1.15f);
    }
    
    void Desactivar()
    {
        if (panelTutorial != null) panelTutorial.SetActive(false);
        if (highlightBox != null) highlightBox.SetActive(false);
        if (npcAvatar != null) npcAvatar.SetActive(false);
        if (panelDialogo != null) panelDialogo.SetActive(false);
    }
    
    public bool EstaActivo() { return activo; }
}
