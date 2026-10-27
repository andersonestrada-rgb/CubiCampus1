using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SeatKind
{
    Simple,   // cubículo de silencio, 1 persona
    Doble,    // mesa doble flexible, 2 personas
    Ruido     // zona de ruido, solo grupos
}

/// <summary>
/// Un puesto de la sala. Controla ocupacion, suciedad, Apartados y contaminacion.
/// Toda la parte visual son cuadrados geometricos (SpriteRenderer).
/// </summary>
public class Seat : MonoBehaviour
{
    [Header("Configuracion")]
    public int seatId;
    public SeatKind kind = SeatKind.Simple;
    public int capacity = 1;
    public bool hasOutlet;
    public Vector2 size = new Vector2(1.6f, 1.0f);

    [Header("Estado")]
    public List<Student> occupants = new List<Student>();
    public bool blocked;
    [Range(0f, 1f)] public float dirtAmount;   // 0 = limpia, 1 = mancha completa
    public bool pairBonus;
    public float contaminatedUntil;

    [Header("Limpieza")]
    [Tooltip("Distancia total de arrastre (unidades de mundo) para quitar una mancha completa.")]
    public float scrubDistance = 2.2f;

    [Header("Visual (asignado por el builder)")]
    public SpriteRenderer fill;
    public SpriteRenderer border;
    public SpriteRenderer outletIcon;
    public Transform blockerRoot;
    public Transform dirtRoot;

    float blockedTime;
    bool contaminatedNeighbor;
    bool cleaning;
    float cleaningLeft;
    int highlight;

    public bool dirty { get { return dirtAmount > 0.001f; } }

    public float DirtAmount { get { return dirtAmount; } }

    public bool IsCondemned
    {
        get { return Time.time < contaminatedUntil; }
    }

    public bool IsFree
    {
        get
        {
            return !blocked && !dirty && !cleaning && !IsCondemned && occupants.Count < capacity;
        }
    }

    public bool HasReadyOccupant
    {
        get
        {
            for (int i = 0; i < occupants.Count; i++)
            {
                if (occupants[i] == null) continue;
                if (occupants[i].state == StudentState.Ready || occupants[i].state == StudentState.Packing) return true;
            }
            return false;
        }
    }

    // ------------------------------------------------------------------ visual

    public void BuildVisuals()
    {
        if (fill != null) return;

        Color cFill = BaseFill(kind);
        Color cBorder = BaseBorder(kind);

        // Borde (detras) y cuerpo
        border = PixelFactory.Quad("Borde", transform, Vector3.zero, size, cBorder, 2);
        fill = PixelFactory.Quad("Cuerpo", transform, Vector3.zero, size * 0.88f, cFill, 3);

        // Enchufe (rayo) en la esquina
        if (hasOutlet)
        {
            outletIcon = PixelFactory.Quad("Enchufe", transform,
                new Vector3(size.x * 0.5f - 0.18f, size.y * 0.5f - 0.18f, 0f),
                new Vector2(0.20f, 0.20f), new Color(1f, 0.86f, 0.30f), 5);
        }

        // Marca de zona de ruido
        if (kind == SeatKind.Ruido)
        {
            PixelFactory.Quad("Ondas1", transform,
                new Vector3(-size.x * 0.5f + 0.20f, -size.y * 0.5f + 0.16f, 0f),
                new Vector2(0.10f, 0.10f), new Color(1f, 0.75f, 0.40f, 0.85f), 5);
            PixelFactory.Quad("Ondas2", transform,
                new Vector3(-size.x * 0.5f + 0.34f, -size.y * 0.5f + 0.16f, 0f),
                new Vector2(0.08f, 0.08f), new Color(1f, 0.75f, 0.40f, 0.55f), 5);
        }

        // Mesa doble: marca los dos huecos para que se vea que caben 2 alumnos.
        if (capacity > 1)
        {
            Color slot = new Color(1f, 1f, 1f, 0.14f);
            for (int i = 0; i < capacity; i++)
            {
                PixelFactory.Quad("Hueco" + (i + 1), transform,
                    new Vector3(SpotOffsetX(i), -0.05f, 0f),
                    new Vector2(0.30f, 0.34f), slot, 4);
            }
        }

        BuildBlocker();
        BuildDirt();
        RefreshVisual();
    }

    void BuildBlocker()
    {
        GameObject root = new GameObject("Apartado");
        root.transform.SetParent(transform, false);
        blockerRoot = root.transform;

        PixelFactory.Quad("Mochila", blockerRoot, Vector3.zero, new Vector2(0.62f, 0.70f),
            new Color(0.40f, 0.26f, 0.14f), 9);

        SpriteRenderer x1 = PixelFactory.Quad("X1", blockerRoot, new Vector3(0f, 0f, 0f),
            new Vector2(0.80f, 0.10f), new Color(1f, 0.30f, 0.30f), 10);
        x1.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        SpriteRenderer x2 = PixelFactory.Quad("X2", blockerRoot, new Vector3(0f, 0f, 0f),
            new Vector2(0.80f, 0.10f), new Color(1f, 0.30f, 0.30f), 10);
        x2.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);

        blockerRoot.gameObject.SetActive(false);
    }

    void BuildDirt()
    {
        GameObject root = new GameObject("Suciedad");
        root.transform.SetParent(transform, false);
        dirtRoot = root.transform;

        // Manchas que se van quitando una a una mientras el jugador "raspa".
        Color dust = new Color(0.62f, 0.52f, 0.36f, 0.95f);
        Vector2[] posiciones = {
            new Vector2(-0.30f, 0.12f),
            new Vector2(0.02f, -0.16f),
            new Vector2(0.32f, 0.14f),
            new Vector2(-0.08f, 0.22f),
            new Vector2(0.20f, -0.22f)
        };
        Vector2[] tamanos = {
            new Vector2(0.28f, 0.18f),
            new Vector2(0.34f, 0.16f),
            new Vector2(0.18f, 0.12f),
            new Vector2(0.14f, 0.10f),
            new Vector2(0.22f, 0.14f)
        };
        float[] giros = { 14f, -9f, 22f, -18f, 8f };

        float escalaX = size.x / 1.55f;
        for (int i = 0; i < posiciones.Length; i++)
        {
            Vector3 p = new Vector3(posiciones[i].x * escalaX, posiciones[i].y, 0f);
            SpriteRenderer blob = PixelFactory.Quad("Polvo" + (i + 1), dirtRoot, p, tamanos[i], dust, 9);
            blob.transform.localRotation = Quaternion.Euler(0f, 0f, giros[i]);
        }

        dirtRoot.gameObject.SetActive(false);
    }

    void RefreshDirt()
    {
        if (dirtRoot == null) return;

        dirtRoot.gameObject.SetActive(dirty);
        if (!dirty) return;

        // Cada mancha desaparece cuando la suciedad restante baja de su umbral.
        int total = dirtRoot.childCount;
        for (int i = 0; i < total; i++)
        {
            float umbral = (total - i) / (float)(total + 1);
            Transform blob = dirtRoot.GetChild(i);
            blob.gameObject.SetActive(dirtAmount >= umbral);
        }
    }

    static Color BaseFill(SeatKind k)
    {
        switch (k)
        {
            case SeatKind.Doble: return new Color(0.13f, 0.33f, 0.26f);
            case SeatKind.Ruido: return new Color(0.40f, 0.22f, 0.11f);
            default: return new Color(0.14f, 0.28f, 0.45f);
        }
    }

    static Color BaseBorder(SeatKind k)
    {
        switch (k)
        {
            case SeatKind.Doble: return new Color(0.30f, 0.78f, 0.54f);
            case SeatKind.Ruido: return new Color(0.95f, 0.60f, 0.28f);
            default: return new Color(0.34f, 0.60f, 0.92f);
        }
    }

    public void SetHighlight(int state)
    {
        highlight = state;
        RefreshVisual();
    }

    void RefreshVisual()
    {
        if (fill == null || border == null) return;

        Color cFill = BaseFill(kind);
        Color cBorder = BaseBorder(kind);

        if (blocked)
        {
            cFill = new Color(0.40f, 0.10f, 0.10f);
            cBorder = new Color(1f, 0.28f, 0.28f);
        }
        else if (dirty)
        {
            float t = Mathf.Clamp01(dirtAmount);
            cFill = Color.Lerp(BaseFill(kind), new Color(0.34f, 0.27f, 0.17f), t);
            cBorder = Color.Lerp(BaseBorder(kind), new Color(0.60f, 0.46f, 0.28f), t);
        }
        else if (IsCondemned)
        {
            cFill = new Color(0.30f, 0.22f, 0.10f);
            cBorder = new Color(0.85f, 0.60f, 0.20f);
        }
        else if (cleaning)
        {
            cFill = new Color(0.22f, 0.30f, 0.42f);
            cBorder = new Color(0.55f, 0.90f, 1f);
        }

        fill.color = cFill;
        border.color = cBorder;

        if (highlight == 1) border.color = new Color(0.30f, 1f, 0.45f);        // compatible
        else if (highlight == 2) border.color = new Color(1f, 0.25f, 0.25f);   // no se puede
        else if (highlight == 3) border.color = new Color(1f, 0.78f, 0.25f);   // se puede, pero inadecuada
        else if (highlight == 4) border.color = new Color(0.55f, 0.90f, 1f);   // raspando

        if (blockerRoot != null) blockerRoot.gameObject.SetActive(blocked);
        RefreshDirt();
        if (outletIcon != null) outletIcon.gameObject.SetActive(hasOutlet);
    }

    // ------------------------------------------------------------------ logica

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || !gm.IsPlaying) return;

        if (blocked)
        {
            if (cleaning)
            {
                cleaningLeft -= Time.deltaTime;
                float pulse = 0.65f + Mathf.PingPong(Time.time * 3f, 0.35f);
                if (border != null) border.color = new Color(0.55f, 0.90f, pulse);
                if (cleaningLeft <= 0f) EndClean();
            }
            else
            {
                blockedTime += Time.deltaTime;

                if (blockerRoot != null)
                {
                    float s = 1f + Mathf.Sin(Time.time * 4f) * 0.05f;
                    blockerRoot.localScale = new Vector3(s, s, 1f);
                }

                if (!contaminatedNeighbor && blockedTime >= 15f)
                {
                    contaminatedNeighbor = true;
                    gm.ContaminateNeighbor(this);
                }
            }
        }

    }

    /// <summary>
    /// Ahora cualquier mesa libre acepta alumnos. La "compatibilidad" ya no bloquea
    /// la asignacion: si la mesa no es la adecuada se gana menos puntaje.
    /// </summary>
    public bool CanAccept(StudentProfile p)
    {
        if (p == null) return false;
        return IsFree;
    }

    /// <summary>La mesa es la ideal para ese perfil (silencio/enchufe/zona de ruido).</summary>
    public bool MatchesProfile(StudentProfile p)
    {
        if (p == null) return false;

        if (p.requiresNoise) return kind == SeatKind.Ruido;
        if (kind == SeatKind.Ruido) return false;
        if (p.requiresOutlet && !hasOutlet) return false;

        return true;
    }

    public string RejectReason(StudentProfile p)
    {
        if (blocked) return "Mesa apartada: manten clic para liberarla";
        if (dirty) return "Mesa sucia: manten clic y mueve para limpiar";
        if (IsCondemned) return "Mesa contaminada por un apartado";
        if (occupants.Count >= capacity) return "Mesa ocupada";
        if (p != null && !MatchesProfile(p)) return MismatchReason(p);
        return "No disponible";
    }

    public string MismatchReason(StudentProfile p)
    {
        if (p == null) return "Mesa inadecuada";
        if (p.requiresNoise && kind != SeatKind.Ruido) return "El grupo necesita zona de ruido";
        if (!p.requiresNoise && kind == SeatKind.Ruido) return "La zona de ruido es solo para grupos";
        if (p.requiresOutlet && !hasOutlet) return "Esta mesa no tiene enchufe";
        return "Mesa inadecuada";
    }

    public Vector3 SpotPosition(int index)
    {
        return transform.position + new Vector3(SpotOffsetX(index), -0.05f, 0f);
    }

    /// <summary>Desplazamiento local de cada hueco (0 = izquierda, 1 = derecha en la mesa doble).</summary>
    public float SpotOffsetX(int index)
    {
        if (capacity <= 1) return 0f;
        float step = size.x * 0.42f;
        return index <= 0 ? -step : step;
    }

    public int AddOccupant(Student s)
    {
        occupants.Add(s);
        pairBonus = (kind == SeatKind.Doble && occupants.Count >= 2);

        if (kind == SeatKind.Doble)
        {
            for (int i = 0; i < occupants.Count; i++)
            {
                if (occupants[i] != null) occupants[i].sharedDouble = true;
            }
        }

        RefreshVisual();
        return occupants.Count - 1;
    }

    public void RemoveOccupant(Student s)
    {
        int idx = occupants.IndexOf(s);
        if (idx < 0) return;

        occupants.RemoveAt(idx);
        pairBonus = (kind == SeatKind.Doble && occupants.Count >= 2);

        // Reacomoda a quien se queda: si el que salio era el de la izquierda,
        // el otro debe correrse a ese hueco para que no queden dos alumnos encima.
        for (int i = 0; i < occupants.Count; i++)
        {
            Student o = occupants[i];
            if (o == null) continue;
            if (o.state == StudentState.Seated || o.state == StudentState.Ready || o.state == StudentState.Packing)
                o.WalkTo(SpotPosition(i), 6f);
        }

        RefreshVisual();
    }

    public void MarkDirty()
    {
        if (occupants.Count > 0) return;
        dirtAmount = 1f;
        RefreshVisual();
    }

    /// <summary>
    /// RASPADO: se llama mientras el jugador mantiene el clic y mueve el cursor sobre
    /// la mesa. Devuelve true cuando la mancha desaparece del todo.
    /// </summary>
    public bool Scrub(float dragDistance)
    {
        if (!dirty || dragDistance <= 0f) return false;

        float need = Mathf.Max(0.2f, scrubDistance);
        dirtAmount = Mathf.Clamp01(dirtAmount - dragDistance / need);

        if (dirtAmount <= 0.001f)
        {
            dirtAmount = 0f;
            RefreshVisual();
            return true;
        }

        RefreshVisual();
        return false;
    }

    public void CleanDirty()
    {
        if (!dirty) return;
        dirtAmount = 0f;
        RefreshVisual();
    }

    public void Block()
    {
        if (blocked) return;
        blocked = true;
        blockedTime = 0f;
        contaminatedNeighbor = false;
        cleaning = false;
        RefreshVisual();
    }

    public void StartClean()
    {
        if (!blocked || cleaning) return;
        cleaning = true;
        cleaningLeft = 2f;
        RefreshVisual();
    }

    void EndClean()
    {
        cleaning = false;
        blocked = false;
        blockedTime = 0f;
        RefreshVisual();
    }

    public void ForceCleanAll()
    {
        cleaning = false;
        blocked = false;
        blockedTime = 0f;
        dirtAmount = 0f;
        contaminatedUntil = 0f;
        RefreshVisual();
    }

    public void Contaminate(float seconds)
    {
        contaminatedUntil = Time.time + seconds;
        RefreshVisual();
    }
}
