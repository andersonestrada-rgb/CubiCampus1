using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Reconstruye GameOverScene: reporte del turno 16:9 con veredicto You Win / Game Over,
/// eficiencia contra el umbral del dia, estadisticas, top-10 local y botones de
/// Reintentar / Menu. Menu: Tools > CubiCampus > Rebuild Game Over Scene
/// </summary>
public static class CubiGameOverBuilder
{
    const string ScenePath = "Assets/Scenes/GameOverScene.unity";

    static readonly Color ColFondo      = new Color(0.06f, 0.07f, 0.11f, 1f);
    static readonly Color ColPanel      = new Color(0.10f, 0.11f, 0.17f, 0.98f);
    static readonly Color ColTexto      = new Color(0.95f, 0.95f, 0.98f, 1f);
    static readonly Color ColTextoTenue = new Color(0.62f, 0.65f, 0.75f, 1f);
    static readonly Color ColBoton      = new Color(0.20f, 0.55f, 0.95f, 1f);
    static readonly Color ColBotonGris  = new Color(0.24f, 0.26f, 0.34f, 1f);
    static readonly Color ColVerde      = new Color(0.35f, 0.90f, 0.45f, 1f);
    static readonly Color ColBarraFondo = new Color(0.16f, 0.17f, 0.23f, 1f);

    [MenuItem("Tools/CubiCampus/Rebuild Game Over Scene")]
    public static void Rebuild()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath);

        foreach (GameObject root in scene.GetRootGameObjects())
            Object.DestroyImmediate(root);

        // --- Camara ---
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
        Image fondoImg = AddImage(fondoGO, ColFondo);
        fondoImg.raycastTarget = false;

        // --- Panel central ---
        GameObject panelGO = NewUI("PanelReporte", canvasT);
        Center(panelGO.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1240f, 930f));
        AddImage(panelGO, ColPanel);
        CanvasGroup grupo = panelGO.AddComponent<CanvasGroup>();

        // Franja de acento (verde/rojo segun el resultado)
        GameObject franjaGO = NewUI("Franja", panelGO.transform);
        Center(franjaGO.GetComponent<RectTransform>(), new Vector2(0f, 395f), new Vector2(760f, 6f));
        Image franjaImg = AddImage(franjaGO, ColVerde);
        franjaImg.raycastTarget = false;

        // --- Veredicto ---
        GameObject tituloGO = NewUI("TxtTitulo", panelGO.transform);
        Center(tituloGO.GetComponent<RectTransform>(), new Vector2(0f, 322f), new Vector2(1100f, 120f));
        Text titulo = AddText(tituloGO, "YOU WIN", 84, TextAnchor.MiddleCenter, ColVerde);
        titulo.fontStyle = FontStyle.Bold;

        GameObject subtituloGO = NewUI("TxtSubtitulo", panelGO.transform);
        Center(subtituloGO.GetComponent<RectTransform>(), new Vector2(0f, 248f), new Vector2(1000f, 50f));
        AddText(subtituloGO, "DIA 1 SUPERADO", 30, TextAnchor.MiddleCenter, new Color(0.82f, 0.85f, 0.92f));

        // --- Eficiencia final ---
        GameObject effLabelGO = NewUI("TxtEficienciaLabel", panelGO.transform);
        Center(effLabelGO.GetComponent<RectTransform>(), new Vector2(0f, 186f), new Vector2(700f, 36f));
        AddText(effLabelGO, "EFICIENCIA FINAL", 24, TextAnchor.MiddleCenter, ColTextoTenue);

        GameObject effGO = NewUI("TxtEficiencia", panelGO.transform);
        Center(effGO.GetComponent<RectTransform>(), new Vector2(0f, 120f), new Vector2(700f, 110f));
        Text effTxt = AddText(effGO, "100%", 92, TextAnchor.MiddleCenter, ColVerde);
        effTxt.fontStyle = FontStyle.Bold;

        // Barra de eficiencia
        GameObject barraFondoGO = NewUI("BarraFondo", panelGO.transform);
        Center(barraFondoGO.GetComponent<RectTransform>(), new Vector2(0f, 55f), new Vector2(760f, 30f));
        AddImage(barraFondoGO, ColBarraFondo);

        GameObject barraFillGO = NewUI("BarraEficiencia", barraFondoGO.transform);
        RectTransform fillRT = barraFillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = new Vector2(3f, 3f);
        fillRT.offsetMax = new Vector2(-3f, -3f);
        Image fillImg = AddImage(barraFillGO, ColVerde);
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImg.fillAmount = 1f;
        fillImg.raycastTarget = false;

        // Marcador vertical del umbral (se coloca en runtime segun el dia)
        GameObject marcadorGO = NewUI("MarcadorObjetivo", barraFondoGO.transform);
        RectTransform marcRT = marcadorGO.GetComponent<RectTransform>();
        marcRT.anchorMin = new Vector2(0.6f, 0.5f);
        marcRT.anchorMax = new Vector2(0.6f, 0.5f);
        marcRT.anchoredPosition = Vector2.zero;
        marcRT.sizeDelta = new Vector2(4f, 46f);
        Image marcImg = AddImage(marcadorGO, ColTexto);
        marcImg.raycastTarget = false;

        GameObject objetivoGO = NewUI("TxtObjetivo", panelGO.transform);
        Center(objetivoGO.GetComponent<RectTransform>(), new Vector2(0f, 8f), new Vector2(760f, 40f));
        AddText(objetivoGO, "OBJETIVO DEL DIA 1   60%", 22, TextAnchor.MiddleCenter, ColTextoTenue);

        GameObject siguienteGO = NewUI("TxtSiguiente", panelGO.transform);
        Center(siguienteGO.GetComponent<RectTransform>(), new Vector2(0f, -24f), new Vector2(1000f, 34f));
        Text siguienteTxt = AddText(siguienteGO, "SIGUIENTE: DIA 2 / 10   -   META 60%", 19, TextAnchor.MiddleCenter, ColTextoTenue);
        siguienteTxt.fontStyle = FontStyle.Bold;

        // Separador
        GameObject sepGO = NewUI("Separador", panelGO.transform);
        Center(sepGO.GetComponent<RectTransform>(), new Vector2(0f, -52f), new Vector2(920f, 2f));
        AddImage(sepGO, new Color(0.24f, 0.26f, 0.34f, 1f)).raycastTarget = false;

        // --- Reporte ---
        GameObject statsGO = NewUI("TxtEstadisticas", panelGO.transform);
        Center(statsGO.GetComponent<RectTransform>(), new Vector2(0f, -92f), new Vector2(1000f, 46f));
        AddText(statsGO, "ATENDIDOS 0      ABANDONOS 0      LLEGADOS 0", 27, TextAnchor.MiddleCenter, ColTexto);

        GameObject puntosGO = NewUI("TxtPuntos", panelGO.transform);
        Center(puntosGO.GetComponent<RectTransform>(), new Vector2(0f, -156f), new Vector2(900f, 64f));
        Text puntosTxt = AddText(puntosGO, "0 PTS", 44, TextAnchor.MiddleCenter, ColTexto);
        puntosTxt.fontStyle = FontStyle.Bold;

        GameObject mensajeGO = NewUI("TxtMensaje", panelGO.transform);
        Center(mensajeGO.GetComponent<RectTransform>(), new Vector2(0f, -218f), new Vector2(1020f, 56f));
        Text mensajeTxt = AddText(mensajeGO, "Objetivo cumplido.", 25, TextAnchor.MiddleCenter, new Color(0.80f, 0.83f, 0.92f));
        mensajeTxt.fontStyle = FontStyle.Italic;

        GameObject rangoGO = NewUI("TxtRango", panelGO.transform);
        Center(rangoGO.GetComponent<RectTransform>(), new Vector2(0f, -276f), new Vector2(1000f, 50f));
        Text rangoTxt = AddText(rangoGO, "PUESTO #1 EN MARCADORES", 28, TextAnchor.MiddleCenter, new Color(1f, 0.84f, 0.35f));
        rangoTxt.fontStyle = FontStyle.Bold;

        // --- Botones ---
        Button btnReiniciar = MakeButton(panelGO.transform, "BtnReiniciar", "SIGUIENTE DIA", ColBoton,
            new Vector2(-200f, -362f), new Vector2(380f, 78f), 30);
        Button btnMenu = MakeButton(panelGO.transform, "BtnMenu", "MENU", ColBotonGris,
            new Vector2(200f, -362f), new Vector2(380f, 78f), 30);

        // Pista de teclado
        GameObject hintGO = NewUI("TxtAyuda", panelGO.transform);
        Center(hintGO.GetComponent<RectTransform>(), new Vector2(0f, -428f), new Vector2(900f, 36f));
        Text hintTxt = AddText(hintGO, "ENTER para avanzar   |   ESC para volver al menu", 20, TextAnchor.MiddleCenter, ColTextoTenue);

        // --- Manager ---
        GameObject managerGO = new GameObject("GameOverManagerGO", typeof(RectTransform));
        managerGO.transform.SetParent(canvasT, false);
        managerGO.layer = 5;
        GameOverManager gom = managerGO.AddComponent<GameOverManager>();
        gom.panelPrincipal = grupo;
        gom.txtTitulo = titulo;
        gom.txtSubtitulo = subtituloGO.GetComponent<Text>();
        gom.franja = franjaImg;
        gom.txtEficiencia = effTxt;
        gom.barraEficiencia = fillImg;
        gom.txtObjetivo = objetivoGO.GetComponent<Text>();
        gom.txtSiguiente = siguienteTxt;
        gom.marcadorObjetivo = marcImg;
        gom.txtEstadisticas = statsGO.GetComponent<Text>();
        gom.txtPuntos = puntosTxt;
        gom.txtMensaje = mensajeTxt;
        gom.txtRango = rangoTxt;
        gom.txtAyuda = hintTxt;
        gom.btnReiniciar = btnReiniciar;
        gom.btnMenu = btnMenu;
        gom.escenaJuego = "GameScene";
        gom.escenaCarga = "LoadingScene";
        gom.escenaMenu = "MenuScene";
        gom.umbralDiasTutorial = 60f;
        gom.umbralNormal = 75f;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[CubiCampus] GameOverScene reconstruida: veredicto por umbral, reporte y marca en la tabla del dia.");
    }

    // ---------------- Helpers ----------------

    static GameObject NewUI(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.layer = 5;
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
        Center(go.GetComponent<RectTransform>(), pos, size);

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
