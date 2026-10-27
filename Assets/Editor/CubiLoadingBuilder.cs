using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Reconstruye LoadingScene: pantalla de transicion 16:9 con barra de progreso
/// y LoadingManager (SceneManager.LoadSceneAsync).
/// Menu: Tools > CubiCampus > Rebuild Loading Scene
/// </summary>
public static class CubiLoadingBuilder
{
    const string ScenePath = "Assets/Scenes/LoadingScene.unity";

    static readonly Color ColFondo      = new Color(0.06f, 0.07f, 0.11f, 1f);
    static readonly Color ColTexto      = new Color(0.95f, 0.95f, 0.98f, 1f);
    static readonly Color ColTextoTenue = new Color(0.62f, 0.65f, 0.75f, 1f);
    static readonly Color ColBarraFondo = new Color(0.16f, 0.17f, 0.23f, 1f);
    static readonly Color ColBarraFill  = new Color(0.25f, 0.65f, 1f, 1f);

    [MenuItem("Tools/CubiCampus/Rebuild Loading Scene")]
    public static void Rebuild()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath);

        foreach (GameObject root in scene.GetRootGameObjects())
            Object.DestroyImmediate(root);

        // Camara
        GameObject camGO = new GameObject("MainCamera");
        camGO.tag = "MainCamera";
        Camera cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = ColFondo;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        camGO.AddComponent<AudioListener>();

        // Canvas
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

        // Fondo
        GameObject fondoGO = NewUI("PanelFondo", canvasT);
        Stretch(fondoGO.GetComponent<RectTransform>());
        Image fondoImg = AddImage(fondoGO, ColFondo);
        fondoImg.raycastTarget = false;

        // Titulo
        GameObject tituloGO = NewUI("Titulo", canvasT);
        Center(tituloGO.GetComponent<RectTransform>(), new Vector2(0f, 250f), new Vector2(1200f, 120f));
        Text titulo = AddText(tituloGO, "CUBI-CAMPUS", 70, TextAnchor.MiddleCenter, ColTexto);
        titulo.fontStyle = FontStyle.Bold;

        // Estado
        GameObject estadoGO = NewUI("TextoEstado", canvasT);
        Center(estadoGO.GetComponent<RectTransform>(), new Vector2(0f, 165f), new Vector2(1000f, 60f));
        Text estado = AddText(estadoGO, "Preparando la sala...", 30, TextAnchor.MiddleCenter, ColTextoTenue);

        // Barra: fondo
        GameObject barraFondoGO = NewUI("BarraFondo", canvasT);
        Center(barraFondoGO.GetComponent<RectTransform>(), new Vector2(0f, 40f), new Vector2(900f, 40f));
        AddImage(barraFondoGO, ColBarraFondo);

        // Barra: relleno (hijo estirado)
        GameObject barraFillGO = NewUI("BarraProgreso", barraFondoGO.transform);
        RectTransform fillRT = barraFillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = new Vector2(4f, 4f);
        fillRT.offsetMax = new Vector2(-4f, -4f);
        Image fillImg = AddImage(barraFillGO, ColBarraFill);
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImg.fillAmount = 0f;

        // Porcentaje
        GameObject pctGO = NewUI("TextoProgreso", canvasT);
        Center(pctGO.GetComponent<RectTransform>(), new Vector2(0f, -40f), new Vector2(400f, 50f));
        AddText(pctGO, "0%", 26, TextAnchor.MiddleCenter, ColTexto);

        // Consejo inferior
        GameObject tipGO = NewUI("TextoTip", canvasT);
        Center(tipGO.GetComponent<RectTransform>(), new Vector2(0f, -400f), new Vector2(1300f, 50f));
        AddText(tipGO, "Consejo: prioriza a los estudiantes con menos paciencia.", 22, TextAnchor.MiddleCenter, ColTextoTenue);

        // Manager
        GameObject managerGO = new GameObject("LoadingManagerGO", typeof(RectTransform));
        managerGO.transform.SetParent(canvasT, false);
        managerGO.layer = 5;
        LoadingManager lm = managerGO.AddComponent<LoadingManager>();
        lm.barraProgreso = fillImg;
        lm.textoProgreso = pctGO.GetComponent<Text>();
        lm.textoEstado = estado;
        lm.tiempoMinimo = 1.1f;
        lm.escenaPorDefecto = "GameScene";

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[CubiCampus] LoadingScene reconstruida con barra de progreso asincrona.");
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
}
