using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Reconstruye GameScene: sala cenital 2D (cuadrados), 8 puestos, fila de espera,
/// HUD y managers del bucle de juego.
/// Menu: Tools > CubiCampus > Rebuild Game Scene
/// </summary>
public static class CubiGameBuilder
{
    const string ScenePath = "Assets/Scenes/GameScene.unity";

    static readonly Color ColFondo      = new Color(0.055f, 0.06f, 0.09f, 1f);
    static readonly Color ColPiso       = new Color(0.13f, 0.13f, 0.18f, 1f);
    static readonly Color ColPared      = new Color(0.09f, 0.10f, 0.14f, 1f);
    static readonly Color ColZonaSil    = new Color(0.11f, 0.20f, 0.32f, 0.55f);
    static readonly Color ColZonaDob    = new Color(0.10f, 0.22f, 0.18f, 0.55f);
    static readonly Color ColZonaRui    = new Color(0.26f, 0.16f, 0.09f, 0.55f);
    static readonly Color ColFila       = new Color(0.10f, 0.12f, 0.18f, 0.85f);
    static readonly Color ColTexto      = new Color(0.95f, 0.95f, 0.98f, 1f);
    static readonly Color ColTextoTenue = new Color(0.68f, 0.72f, 0.82f, 1f);
    static readonly Color ColBotonAzul  = new Color(0.20f, 0.55f, 0.95f, 1f);
    static readonly Color ColBotonRojo  = new Color(0.80f, 0.26f, 0.26f, 1f);
    static readonly Color ColPanel      = new Color(0.10f, 0.11f, 0.16f, 0.98f);

    [MenuItem("Tools/CubiCampus/Rebuild Game Scene")]
    public static void Rebuild()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath);

        foreach (GameObject root in scene.GetRootGameObjects())
            Object.DestroyImmediate(root);

        // ---------------------------------------------------------------- camara
        GameObject camGO = new GameObject("MainCamera");
        camGO.tag = "MainCamera";
        Camera cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5.2f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = ColFondo;
        cam.transform.position = new Vector3(0.7f, -0.5f, -10f);
        camGO.AddComponent<AudioListener>();

        // ---------------------------------------------------------------- event system
        GameObject esGO = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        esGO.transform.SetParent(null);

        // ---------------------------------------------------------------- sala
        GameObject room = new GameObject("Sala");

        // Piso
        GameObject piso = NewSprite("Piso", room.transform, new Vector3(1.0f, -0.7f, 0f), new Vector2(14.6f, 7.6f), ColPiso, -20);

        // Paredes
        NewSprite("ParedArriba", room.transform, new Vector3(1.0f, 3.2f, 0f), new Vector2(14.9f, 0.25f), ColPared, -18);
        NewSprite("ParedAbajo", room.transform, new Vector3(1.0f, -4.6f, 0f), new Vector2(14.9f, 0.25f), ColPared, -18);
        NewSprite("ParedIzquierda", room.transform, new Vector3(-6.45f, -0.7f, 0f), new Vector2(0.25f, 7.9f), ColPared, -18);
        NewSprite("ParedDerecha", room.transform, new Vector3(8.45f, -0.7f, 0f), new Vector2(0.25f, 7.9f), ColPared, -18);

        // Zonas
        NewSprite("ZonaSilencio", room.transform, new Vector3(-2.0f, 1.25f, 0f), new Vector2(3.9f, 2.7f), ColZonaSil, -15);
        NewSprite("ZonaDoble", room.transform, new Vector3(2.4f, 1.25f, 0f), new Vector2(3.2f, 2.7f), ColZonaDob, -15);
        NewSprite("ZonaRuido", room.transform, new Vector3(6.0f, 1.25f, 0f), new Vector2(2.5f, 2.7f), ColZonaRui, -15);
        NewSprite("ZonaFila", room.transform, new Vector3(-5.2f, 1.05f, 0f), new Vector2(2.0f, 3.9f), ColFila, -15);

        // Estanterias decorativas
        for (int i = 0; i < 3; i++)
        {
            NewSprite("Estanteria" + i, room.transform,
                new Vector3(1.2f + i * 2.6f, -4.25f, 0f), new Vector2(2.2f, 0.42f),
                new Color(0.24f, 0.20f, 0.16f), -16);
        }

        // Puerta
        NewSprite("Puerta", room.transform, new Vector3(-5.2f, -4.42f, 0f), new Vector2(1.8f, 0.40f),
            new Color(0.30f, 0.42f, 0.55f), -14);
        NewSprite("PuertaLuz", room.transform, new Vector3(-5.2f, -4.18f, 0f), new Vector2(1.3f, 0.12f),
            new Color(0.55f, 0.85f, 1f, 0.75f), -13);

        // Etiquetas de zona (cuadros de color junto a cada zona)
        NewSprite("MarcaSilencio", room.transform, new Vector3(-3.75f, 2.55f, 0f), new Vector2(0.22f, 0.22f), new Color(0.34f, 0.60f, 0.92f), -12);
        NewSprite("MarcaDoble", room.transform, new Vector3(0.90f, 2.55f, 0f), new Vector2(0.22f, 0.22f), new Color(0.30f, 0.78f, 0.54f), -12);
        NewSprite("MarcaRuido", room.transform, new Vector3(4.85f, 2.55f, 0f), new Vector2(0.22f, 0.22f), new Color(0.95f, 0.60f, 0.28f), -12);

        // ---------------------------------------------------------------- puestos
        List<Seat> seats = new List<Seat>();

        seats.Add(MakeSeat(room.transform, 1, SeatKind.Simple, new Vector3(-2.9f, 1.90f, 0f), new Vector2(1.55f, 1.0f), true));
        seats.Add(MakeSeat(room.transform, 2, SeatKind.Simple, new Vector3(-1.1f, 1.90f, 0f), new Vector2(1.55f, 1.0f), false));
        seats.Add(MakeSeat(room.transform, 3, SeatKind.Simple, new Vector3(-2.9f, 0.60f, 0f), new Vector2(1.55f, 1.0f), false));
        seats.Add(MakeSeat(room.transform, 4, SeatKind.Simple, new Vector3(-1.1f, 0.60f, 0f), new Vector2(1.55f, 1.0f), true));
        seats.Add(MakeSeat(room.transform, 5, SeatKind.Doble, new Vector3(2.40f, 1.90f, 0f), new Vector2(2.50f, 1.0f), false));
        seats.Add(MakeSeat(room.transform, 6, SeatKind.Doble, new Vector3(2.40f, 0.60f, 0f), new Vector2(2.50f, 1.0f), false));
        seats.Add(MakeSeat(room.transform, 7, SeatKind.Ruido, new Vector3(6.00f, 1.90f, 0f), new Vector2(1.70f, 1.0f), false));
        seats.Add(MakeSeat(room.transform, 8, SeatKind.Ruido, new Vector3(6.00f, 0.60f, 0f), new Vector2(1.70f, 1.0f), false));

        // ---------------------------------------------------------------- fila
        GameObject queueRoot = new GameObject("Fila");
        Transform[] slots = new Transform[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject slot = new GameObject("SlotFila" + (i + 1));
            slot.transform.SetParent(queueRoot.transform, false);
            slot.transform.position = new Vector3(-5.2f, 2.10f - i * 1.20f, 0f);
            NewSprite("Marca", slot.transform, slot.transform.position + new Vector3(0f, -0.45f, 0f),
                new Vector2(0.75f, 0.06f), new Color(0.30f, 0.38f, 0.52f, 0.8f), -12);
            slots[i] = slot.transform;
        }

        GameObject doorPoint = new GameObject("PuntoPuerta");
        doorPoint.transform.SetParent(queueRoot.transform, false);
        doorPoint.transform.position = new Vector3(-5.2f, -3.7f, 0f);

        // ---------------------------------------------------------------- managers
        GameObject gmGO = new GameObject("GameManager");
        GameManager gm = gmGO.AddComponent<GameManager>();
        gm.seats = seats;

        GameObject qGO = new GameObject("QueueManager");
        QueueManager queue = qGO.AddComponent<QueueManager>();
        queue.game = gm;
        queue.doorPoint = doorPoint.transform;
        queue.slots = slots;
        queue.spawnInterval = 8f;

        GameObject dragGO = new GameObject("DragAssigner");
        DragAssigner drag = dragGO.AddComponent<DragAssigner>();
        drag.game = gm;
        drag.cam = cam;

        gm.queue = queue;
        gm.dragAssigner = drag;

        // ---------------------------------------------------------------- canvas
        GameObject canvasGO = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 10f;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        Transform canvasT = canvasGO.transform;
        HUDGame hud = canvasGO.AddComponent<HUDGame>();
        hud.game = gm;
        gm.hud = hud;

        // Barra superior
        GameObject topBar = NewUI("BarraSuperior", canvasT);
        RectTransform topRT = topBar.GetComponent<RectTransform>();
        topRT.anchorMin = new Vector2(0f, 1f);
        topRT.anchorMax = new Vector2(1f, 1f);
        topRT.pivot = new Vector2(0.5f, 1f);
        topRT.anchoredPosition = Vector2.zero;
        topRT.sizeDelta = new Vector2(0f, 130f);
        AddImage(topBar, new Color(0.06f, 0.07f, 0.11f, 0.95f)).raycastTarget = false;

        GameObject puntosGO = NewUI("TxtPuntos", canvasT);
        Place(puntosGO.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -22f), new Vector2(560f, 56f));
        Text txtPuntos = AddText(puntosGO, "PUNTOS  0", 38, TextAnchor.MiddleLeft, ColTexto);
        txtPuntos.fontStyle = FontStyle.Bold;
        hud.txtPuntos = txtPuntos;

        GameObject tiempoGO = NewUI("TxtTiempo", canvasT);
        Place(tiempoGO.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(360f, 62f));
        Text txtTiempo = AddText(tiempoGO, "03:00", 54, TextAnchor.MiddleCenter, ColTexto);
        txtTiempo.fontStyle = FontStyle.Bold;
        hud.txtTiempo = txtTiempo;

        hud.barraTiempo = MakeBar(canvasT, "BarraTiempo", new Vector2(0.5f, 1f), new Vector2(0f, -76f), new Vector2(540f, 16f), new Color(0.35f, 0.70f, 1f));

        GameObject efGO = NewUI("TxtEficiencia", canvasT);
        Place(efGO.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -20f), new Vector2(460f, 52f));
        Text txtEf = AddText(efGO, "EFICIENCIA  100%", 36, TextAnchor.MiddleRight, ColTexto);
        txtEf.fontStyle = FontStyle.Bold;
        hud.txtEficiencia = txtEf;

        hud.barraEficiencia = MakeBar(canvasT, "BarraEficiencia", new Vector2(1f, 1f), new Vector2(-40f, -74f), new Vector2(340f, 16f), new Color(0.35f, 0.90f, 0.45f));

        GameObject comboGO = NewUI("TxtCombo", canvasT);
        Place(comboGO.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(42f, -80f), new Vector2(420f, 38f));
        Text txtCombo = AddText(comboGO, "RACHA 0 / 3", 24, TextAnchor.MiddleLeft, ColTextoTenue);
        hud.txtCombo = txtCombo;

        GameObject rushGO = NewUI("TxtRush", canvasT);
        Place(rushGO.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -142f), new Vector2(560f, 42f));
        Text txtRush = AddText(rushGO, "MODO RUSH", 30, TextAnchor.MiddleCenter, new Color(1f, 0.55f, 0.25f));
        txtRush.fontStyle = FontStyle.Bold;
        txtRush.gameObject.SetActive(false);
        hud.txtRush = txtRush;

        GameObject aforoGO = NewUI("TxtAforo", canvasT);
        Place(aforoGO.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(40f, 68f), new Vector2(620f, 40f));
        Text txtAforo = AddText(aforoGO, "AFORO  0 / 10", 26, TextAnchor.MiddleLeft, ColTexto);
        txtAforo.fontStyle = FontStyle.Bold;
        hud.txtAforo = txtAforo;

        GameObject abGO = NewUI("TxtAbandonos", canvasT);
        Place(abGO.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(40f, 28f), new Vector2(980f, 36f));
        hud.txtAbandonos = AddText(abGO, "ATENDIDOS 0   ABANDONOS 0", 21, TextAnchor.MiddleLeft, ColTextoTenue);

        GameObject hintGO = NewUI("TxtHint", canvasT);
        Place(hintGO.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -94f), new Vector2(900f, 34f));
        hud.txtHint = AddText(hintGO, "Arrastra estudiantes a cualquier mesa  |  Clic repetido para que salgan  |  Manten clic y mueve para raspar manchas", 19, TextAnchor.MiddleCenter, ColTextoTenue);

        // Botones
        Button btnBono = MakeButton(canvasT, "BtnBono", "BONO SILENCIO", ColBotonAzul, new Vector2(280f, 92f), 24);
        Place(btnBono.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-320f, 40f), new Vector2(280f, 92f));
        hud.btnBono = btnBono;
        hud.txtBono = btnBono.GetComponentInChildren<Text>();

        Button btnPausa = MakeButton(canvasT, "BtnPausa", "PAUSA", new Color(0.25f, 0.28f, 0.38f), new Vector2(160f, 92f), 24);
        Place(btnPausa.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 40f), new Vector2(160f, 92f));
        hud.btnPausa = btnPausa;

        // Overlay de alerta
        GameObject alertaGO = NewUI("OverlayAlerta", canvasT);
        Stretch(alertaGO.GetComponent<RectTransform>());
        Image alertaImg = AddImage(alertaGO, new Color(1f, 0.15f, 0.15f, 0.12f));
        alertaImg.raycastTarget = false;
        alertaGO.SetActive(false);
        hud.overlayAlerta = alertaImg;

        // Texto de fin de turno
        GameObject estadoGO = NewUI("TxtEstado", canvasT);
        Place(estadoGO.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1300f, 240f));
        Text txtEstado = AddText(estadoGO, "TURNO FINALIZADO", 56, TextAnchor.MiddleCenter, ColTexto);
        txtEstado.fontStyle = FontStyle.Bold;
        Outline outline = estadoGO.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(3f, -3f);
        estadoGO.SetActive(false);
        hud.txtEstado = txtEstado;

        // Panel de pausa
        GameObject panelPausa = NewUI("PanelPausa", canvasT);
        Stretch(panelPausa.GetComponent<RectTransform>());
        AddImage(panelPausa, new Color(0f, 0f, 0f, 0.78f));

        GameObject contPausa = NewUI("ContenedorPausa", panelPausa.transform);
        Place(contPausa.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 560f));
        AddImage(contPausa, ColPanel);

        GameObject pauseTitleGO = NewUI("TituloPausa", contPausa.transform);
        Place(pauseTitleGO.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(620f, 80f));
        Text pauseTitle = AddText(pauseTitleGO, "PAUSA", 54, TextAnchor.MiddleCenter, ColTexto);
        pauseTitle.fontStyle = FontStyle.Bold;

        Button btnReanudar = MakeButton(contPausa.transform, "BtnReanudar", "REANUDAR", ColBotonAzul, new Vector2(460f, 88f), 30);
        Place(btnReanudar.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(460f, 88f));
        hud.btnReanudar = btnReanudar;

        Button btnMenu = MakeButton(contPausa.transform, "BtnMenu", "SALIR AL MENU", ColBotonRojo, new Vector2(460f, 88f), 30);
        Place(btnMenu.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -100f), new Vector2(460f, 88f));
        hud.btnMenu = btnMenu;

        GameObject pauseInfoGO = NewUI("InfoPausa", contPausa.transform);
        Place(pauseInfoGO.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(680f, 40f));
        AddText(pauseInfoGO, "ESC para reanudar", 22, TextAnchor.MiddleCenter, ColTextoTenue);

        panelPausa.SetActive(false);
        hud.panelPausa = panelPausa;

        // ---------------------------------------------------------------- guardar
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[CubiCampus] GameScene reconstruida con 8 puestos, fila, HUD y bucle de juego.");
    }

    // ---------------------------------------------------------------- helpers

    static Seat MakeSeat(Transform parent, int id, SeatKind kind, Vector3 pos, Vector2 size, bool outlet)
    {
        GameObject go = new GameObject("Mesa_" + id + "_" + kind);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;

        Seat seat = go.AddComponent<Seat>();
        seat.seatId = id;
        seat.kind = kind;
        seat.capacity = kind == SeatKind.Doble ? 2 : 1;
        seat.hasOutlet = outlet;
        seat.size = size;
        seat.scrubDistance = 2.2f;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = size;

        seat.BuildVisuals();
        return seat;
    }

    static GameObject NewSprite(string name, Transform parent, Vector3 pos, Vector2 size, Color color, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PixelFactory.White;
        sr.color = color;
        sr.sortingOrder = order;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        return go;
    }

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

    static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static Image MakeBar(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color fillColor)
    {
        GameObject fondo = NewUI(name, parent);
        Place(fondo.GetComponent<RectTransform>(), anchor, anchor, pos, size);
        AddImage(fondo, new Color(0.06f, 0.07f, 0.10f, 0.95f)).raycastTarget = false;

        GameObject relleno = NewUI("Relleno", fondo.transform);
        RectTransform rt = relleno.GetComponent<RectTransform>();
        Stretch(rt);
        rt.offsetMin = new Vector2(2f, 2f);
        rt.offsetMax = new Vector2(-2f, -2f);

        Image img = AddImage(relleno, fillColor);
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillOrigin = (int)Image.OriginHorizontal.Left;
        img.fillAmount = 1f;
        img.raycastTarget = false;
        return img;
    }

    static Button MakeButton(Transform parent, string name, string label, Color bg, Vector2 size, int fontSize)
    {
        GameObject go = NewUI(name, parent);
        go.GetComponent<RectTransform>().sizeDelta = size;

        Image img = AddImage(go, bg);

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
        cb.pressedColor = new Color(0.70f, 0.70f, 0.70f, 1f);
        cb.selectedColor = Color.white;
        cb.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.6f);
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
