using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Reconstruye MenuScene con un layout 16:9 limpio, EventSystem funcional
/// (Input System nuevo) y paneles de Opciones / Marcadores correctamente armados.
/// Menu: Tools > CubiCampus > Rebuild Menu Scene
/// </summary>
public static class CubiMenuBuilder
{
    const string ScenePath = "Assets/Scenes/MenuScene.unity";

    static readonly Color ColFondo      = new Color(0.09f, 0.09f, 0.14f, 1f);
    static readonly Color ColBoton      = new Color(0.20f, 0.55f, 0.95f, 1f);
    static readonly Color ColBotonRojo  = new Color(0.85f, 0.28f, 0.28f, 1f);
    static readonly Color ColPanel      = new Color(0.10f, 0.10f, 0.16f, 0.98f);
    static readonly Color ColOverlay    = new Color(0f, 0f, 0f, 0.65f);
    static readonly Color ColTexto      = new Color(0.95f, 0.95f, 0.98f, 1f);
    static readonly Color ColTextoTenue = new Color(0.72f, 0.74f, 0.82f, 1f);

    [MenuItem("Tools/CubiCampus/Rebuild Menu Scene")]
    public static void Rebuild()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath);

        // --- Limpiar escena ---
        foreach (GameObject root in scene.GetRootGameObjects())
            Object.DestroyImmediate(root);

        // --- Cámara ---
        GameObject camGO = new GameObject("MainCamera");
        camGO.tag = "MainCamera";
        Camera cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = ColFondo;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        camGO.AddComponent<AudioListener>();

        // --- EventSystem (Input System nuevo) ---
        GameObject esGO = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        esGO.transform.SetParent(null);

        // --- Canvas ---
        GameObject canvasGO = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 10f;
        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        Transform canvasT = canvasGO.transform;

        // --- Fondo ---
        GameObject fondoGO = NewUI("PanelFondo", canvasT);
        Stretch(fondoGO.GetComponent<RectTransform>());
        AddImage(fondoGO, new Color(0.11f, 0.12f, 0.18f, 1f)).raycastTarget = false;

        // --- Título ---
        GameObject tituloGO = NewUI("Titulo", canvasT);
        RectTransform tituloRT = tituloGO.GetComponent<RectTransform>();
        Center(tituloRT, new Vector2(0f, 350f), new Vector2(1100f, 120f));
        Text titulo = AddText(tituloGO, "CUBI-CAMPUS", 78, TextAnchor.MiddleCenter, ColTexto);
        titulo.fontStyle = FontStyle.Bold;

        GameObject subtituloGO = NewUI("Subtitulo", canvasT);
        RectTransform subRT = subtituloGO.GetComponent<RectTransform>();
        Center(subRT, new Vector2(0f, 262f), new Vector2(1000f, 60f));
        AddText(subtituloGO, "AFORO AL LIMITE", 30, TextAnchor.MiddleCenter, ColTextoTenue);

        // --- Botones principales ---
        Button btnJugar      = MakeButton(canvasT, "BtnJugar", "JUGAR", ColBoton, new Vector2(0f, 120f), new Vector2(440f, 80f), 32);
        Button btnMarcadores = MakeButton(canvasT, "BtnMarcadores", "MARCADORES", ColBoton, new Vector2(0f, 20f), new Vector2(440f, 80f), 32);
        Button btnOpciones   = MakeButton(canvasT, "BtnOpciones", "OPCIONES", ColBoton, new Vector2(0f, -80f), new Vector2(440f, 80f), 32);
        Button btnSalir      = MakeButton(canvasT, "BtnSalir", "SALIR", ColBotonRojo, new Vector2(0f, -200f), new Vector2(440f, 80f), 32);

        // --- Panel Marcadores ---
        GameObject panelMarcadores = NewUI("PanelMarcadores", canvasT);
        Stretch(panelMarcadores.GetComponent<RectTransform>());
        AddImage(panelMarcadores, ColOverlay);

        GameObject contMarc = NewUI("ContenedorMarcadores", panelMarcadores.transform);
        RectTransform contMarcRT = contMarc.GetComponent<RectTransform>();
        Center(contMarcRT, Vector2.zero, new Vector2(1000f, 720f));
        AddImage(contMarc, ColPanel);

        GameObject tituloMarcGO = NewUI("TituloMarcadores", contMarc.transform);
        RectTransform tituloMarcRT = tituloMarcGO.GetComponent<RectTransform>();
        Center(tituloMarcRT, new Vector2(0f, 290f), new Vector2(880f, 80f));
        Text tituloMarc = AddText(tituloMarcGO, "MARCADORES", 46, TextAnchor.MiddleCenter, ColTexto);
        tituloMarc.fontStyle = FontStyle.Bold;

        GameObject txtMarcGO = NewUI("TxtMarcadoresPanel", contMarc.transform);
        RectTransform txtMarcRT = txtMarcGO.GetComponent<RectTransform>();
        Center(txtMarcRT, new Vector2(0f, 0f), new Vector2(900f, 500f));
        Text txtMarc = AddText(txtMarcGO, "Aun no hay puntajes registrados.", 26, TextAnchor.MiddleCenter, ColTexto);
        txtMarc.lineSpacing = 1.25f;

        Button btnCerrarMarcadores = MakeButton(contMarc.transform, "BtnCerrarMarcadores", "CERRAR", ColBoton, new Vector2(0f, -300f), new Vector2(240f, 64f), 26);

        // --- Panel Opciones ---
        GameObject panelOpciones = NewUI("PanelOpciones", canvasT);
        Stretch(panelOpciones.GetComponent<RectTransform>());
        AddImage(panelOpciones, ColOverlay);

        GameObject contOpc = NewUI("ContenedorOpciones", panelOpciones.transform);
        RectTransform contOpcRT = contOpc.GetComponent<RectTransform>();
        Center(contOpcRT, Vector2.zero, new Vector2(860f, 600f));
        AddImage(contOpc, ColPanel);

        GameObject tituloOpcGO = NewUI("TituloOpciones", contOpc.transform);
        RectTransform tituloOpcRT = tituloOpcGO.GetComponent<RectTransform>();
        Center(tituloOpcRT, new Vector2(0f, 230f), new Vector2(760f, 70f));
        Text tituloOpc = AddText(tituloOpcGO, "OPCIONES", 44, TextAnchor.MiddleCenter, ColTexto);
        tituloOpc.fontStyle = FontStyle.Bold;

        // Fila: Pantalla completa
        GameObject toggleGO = NewUI("ToggleFullscreen", contOpc.transform);
        RectTransform toggleRT = toggleGO.GetComponent<RectTransform>();
        Center(toggleRT, new Vector2(0f, 100f), new Vector2(620f, 60f));

        GameObject toggleBgGO = NewUI("Background", toggleGO.transform);
        RectTransform toggleBgRT = toggleBgGO.GetComponent<RectTransform>();
        Center(toggleBgRT, new Vector2(-260f, 0f), new Vector2(44f, 44f));
        Image toggleBg = AddImage(toggleBgGO, new Color(0.18f, 0.19f, 0.26f, 1f));

        GameObject checkGO = NewUI("Checkmark", toggleBgGO.transform);
        RectTransform checkRT = checkGO.GetComponent<RectTransform>();
        Stretch(checkRT, new Vector2(8f, 8f), new Vector2(-8f, -8f));
        Image checkImg = AddImage(checkGO, new Color(0.30f, 0.80f, 0.45f, 1f));

        GameObject toggleLabelGO = NewUI("Label", toggleGO.transform);
        RectTransform toggleLabelRT = toggleLabelGO.GetComponent<RectTransform>();
        Center(toggleLabelRT, new Vector2(20f, 0f), new Vector2(460f, 60f));
        AddText(toggleLabelGO, "Pantalla completa", 30, TextAnchor.MiddleLeft, ColTexto);

        Toggle toggle = toggleGO.AddComponent<Toggle>();
        toggle.targetGraphic = toggleBg;
        toggle.graphic = checkImg;
        toggle.isOn = true;

        // Fila: Volumen
        GameObject volLabelGO = NewUI("LabelVolumen", contOpc.transform);
        RectTransform volLabelRT = volLabelGO.GetComponent<RectTransform>();
        Center(volLabelRT, new Vector2(-270f, -50f), new Vector2(240f, 60f));
        AddText(volLabelGO, "Volumen", 30, TextAnchor.MiddleRight, ColTexto);

        GameObject sliderGO = NewUI("SliderVolumen", contOpc.transform);
        RectTransform sliderRT = sliderGO.GetComponent<RectTransform>();
        Center(sliderRT, new Vector2(110f, -50f), new Vector2(500f, 44f));

        GameObject sliderBgGO = NewUI("Background", sliderGO.transform);
        RectTransform sliderBgRT = sliderBgGO.GetComponent<RectTransform>();
        sliderBgRT.anchorMin = new Vector2(0f, 0.5f);
        sliderBgRT.anchorMax = new Vector2(1f, 0.5f);
        sliderBgRT.offsetMin = new Vector2(0f, -7f);
        sliderBgRT.offsetMax = new Vector2(0f, 7f);
        AddImage(sliderBgGO, new Color(0.18f, 0.19f, 0.26f, 1f));

        GameObject fillAreaGO = NewUI("Fill Area", sliderGO.transform);
        RectTransform fillAreaRT = fillAreaGO.GetComponent<RectTransform>();
        fillAreaRT.anchorMin = new Vector2(0f, 0.5f);
        fillAreaRT.anchorMax = new Vector2(1f, 0.5f);
        fillAreaRT.offsetMin = new Vector2(0f, -7f);
        fillAreaRT.offsetMax = new Vector2(0f, 7f);

        GameObject fillGO = NewUI("Fill", fillAreaGO.transform);
        RectTransform fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;
        AddImage(fillGO, new Color(0.25f, 0.70f, 1f, 1f));

        GameObject handleAreaGO = NewUI("Handle Slide Area", sliderGO.transform);
        RectTransform handleAreaRT = handleAreaGO.GetComponent<RectTransform>();
        handleAreaRT.anchorMin = Vector2.zero;
        handleAreaRT.anchorMax = Vector2.one;
        handleAreaRT.offsetMin = Vector2.zero;
        handleAreaRT.offsetMax = Vector2.zero;

        GameObject handleGO = NewUI("Handle", handleAreaGO.transform);
        RectTransform handleRT = handleGO.GetComponent<RectTransform>();
        handleRT.anchorMin = new Vector2(0f, 0.5f);
        handleRT.anchorMax = new Vector2(0f, 0.5f);
        handleRT.sizeDelta = new Vector2(28f, 28f);
        Image handleImg = AddImage(handleGO, Color.white);

        Slider slider = sliderGO.AddComponent<Slider>();
        slider.fillRect = fillRT;
        slider.handleRect = handleRT;
        slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.8f;

        Button btnCerrarOpciones = MakeButton(contOpc.transform, "BtnCerrarOpciones", "CERRAR", ColBoton, new Vector2(0f, -230f), new Vector2(240f, 64f), 26);

        // --- MenuManager ---
        GameObject mmGO = NewUI("MenuManagerGO", canvasT);
        MenuManager mm = mmGO.AddComponent<MenuManager>();
        mm.btnJugar = btnJugar;
        mm.btnMarcadores = btnMarcadores;
        mm.btnOpciones = btnOpciones;
        mm.btnSalir = btnSalir;
        mm.panelMarcadores = panelMarcadores;
        mm.txtMarcadores = txtMarc;
        mm.btnCerrarMarcadores = btnCerrarMarcadores;
        mm.panelOpciones = panelOpciones;
        mm.toggleFullscreen = toggle;
        mm.sliderVolumen = slider;
        mm.btnCerrarOpciones = btnCerrarOpciones;

        // Paneles ocultos por defecto
        panelMarcadores.SetActive(false);
        panelOpciones.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[CubiCampus] MenuScene reconstruida: layout 16:9, EventSystem (Input System) y paneles listos.");
    }

    // ---------------- Helpers ----------------

    static GameObject NewUI(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.layer = 5; // UI
        return go;
    }

    static Image AddImage(GameObject go, Color color)
    {
        Image img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    static Text AddText(GameObject go, string content, int size, TextAnchor anchor, Color color)
    {
        Text t = go.AddComponent<Text>();
        t.text = content;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (t.font == null) t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        t.fontSize = size;
        t.alignment = anchor;
        t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void Stretch(RectTransform rt, Vector2 offMin, Vector2 offMax)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = offMin;
        rt.offsetMax = offMax;
    }

    static void Center(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static Button MakeButton(Transform parent, string name, string label, Color bg, Vector2 pos, Vector2 size, int fontSize)
    {
        GameObject go = NewUI(name, parent);
        RectTransform rt = go.GetComponent<RectTransform>();
        Center(rt, pos, size);

        Image img = AddImage(go, bg);

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
        cb.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
        cb.selectedColor = Color.white;
        cb.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        cb.colorMultiplier = 1f;
        cb.fadeDuration = 0.08f;
        btn.colors = cb;

        GameObject labelGO = NewUI("Texto", go.transform);
        Stretch(labelGO.GetComponent<RectTransform>());
        Text t = AddText(labelGO, label, fontSize, TextAnchor.MiddleCenter, Color.white);
        t.fontStyle = FontStyle.Bold;

        return btn;
    }
}
