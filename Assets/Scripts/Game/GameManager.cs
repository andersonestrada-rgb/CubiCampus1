using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Cerebro del turno: reloj global, eficiencia, puntaje, combos, bono y apartados.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Referencias")]
    public List<Seat> seats = new List<Seat>();
    public QueueManager queue;
    public HUDGame hud;
    public DragAssigner dragAssigner;

    [Header("Progresion de la campana")]
    [Tooltip("Dia que se esta jugando (1..10). Se carga desde PlayerPrefs al iniciar.")]
    public int diaActual = 1;
    [Tooltip("Parametros del dia activo. Los aplica GameManager.Awake.")]
    public DayConfig configDia;

    [Header("Turno")]
    public float turnDuration = 180f;
    public float rushLastSeconds = 30f;

    [Header("Apartados")]
    public float apartadoMin = 25f;
    public float apartadoMax = 40f;
    public float apartadoContaminationRadius = 3f;

    [Header("Penalizaciones")]
    public int penaltyAbandon = 150;
    public int penaltyEarlyLeave = 50;
    public int penaltyBadAssign = 50;
    public int penaltyRelocate = 30;

    [Header("Eficiencia (arranca al 100% y se mueve segun la atencion)")]
    [Range(0f, 100f)] public float efficiencyStart = 100f;
    public float effGainHappy = 4f;          // por alumno atendido (escala con su paciencia)
    public float effGainAttention = 2f;      // resolver una distraccion
    public float effLossAbandon = 12f;       // abandono en la fila
    public float effLossEarlyLeave = 8f;     // se distrajo y se fue
    public float effLossWrongSeat = 6f;      // sentarlo en una mesa inadecuada
    public float effLossBadAssign = 4f;      // soltarlo sobre una mesa no disponible
    public float effLossIgnoredReady = 5f;   // alumno listo esperando demasiado (una sola vez)
    public float effReadyGrace = 8f;         // segundos de gracia antes de esa penalizacion

    [Header("Modo Rush")]
    [Tooltip("En rush llegan mas alumnos: su paciencia en la fila baja mas despacio para que no abandonen solos.")]
    [Range(0.1f, 1f)] public float rushPatienceFactor = 0.55f;

    [Header("Puntos")]
    [Range(0f, 1f)] public float wrongSeatPointsFactor = 0.4f; // mesa inadecuada = menos puntos

    [Header("Salida por clics")]
    public float packDuration = 2.6f;    // segundos que tarda en recoger sin ayuda
    public float packClickPower = 0.35f; // segundos que quita cada clic

    // --- estado del turno ---
    float timeLeft;
    bool playing;
    float apartadoTimer;
    float freezeLeft;
    float comboLeft;

    int score;
    int happy;
    int arrived;
    int abandoned;
    int earlyLeaves;
    int badAssignments;
    int relocations;
    float efficiency;

    int perfectStreak;
    bool bonoAvailable = true;
    bool relocationAvailable = true;
    bool rush;
    int lastTickSecond = -1;

    string message = "";
    float messageUntil;

    readonly Dictionary<StudentKind, StudentProfile> profiles = new Dictionary<StudentKind, StudentProfile>();

    // --- propiedades para el HUD ---
    public bool IsPlaying { get { return playing; } }
    public bool IsPaused { get { return Time.timeScale <= 0f; } }
    public float TimeLeft { get { return Mathf.Max(0f, timeLeft); } }
    public float TurnDuration { get { return turnDuration; } }
    public int Dia { get { return diaActual; } }
    public DayConfig ConfigDia { get { return configDia; } }
    public int TotalDias { get { return DayProgression.TotalDias; } }
    public int Score { get { return score; } }
    public int Happy { get { return happy; } }
    public int Arrived { get { return arrived; } }
    public int Abandoned { get { return abandoned; } }
    public int EarlyLeaves { get { return earlyLeaves; } }
    public int BadAssignments { get { return badAssignments; } }
    public bool IsRush { get { return rush; } }
    public bool PatienceFrozen { get { return freezeLeft > 0f; } }
    public bool BonoAvailable { get { return bonoAvailable; } }
    public bool RelocationAvailable { get { return relocationAvailable; } }
    public int ComboMultiplier { get { return comboLeft > 0f ? 2 : 1; } }
    public float ComboLeft { get { return Mathf.Max(0f, comboLeft); } }
    public int PerfectStreak { get { return perfectStreak; } }
    // La eficiencia arranca en 100% y solo baja si la atencion es mala.
    public bool IsAlert
    {
        get
        {
            float alerta = configDia != null ? configDia.umbralEficiencia * 0.55f : 40f;
            return efficiency < alerta;
        }
    }

    public string Message
    {
        get { return Time.time < messageUntil ? message : ""; }
    }

    public Vector3 DoorPosition
    {
        get { return queue != null && queue.doorPoint != null ? queue.doorPoint.position : new Vector3(-6f, -3.6f, 0f); }
    }

    public int CapacityTotal
    {
        get
        {
            int total = 0;
            for (int i = 0; i < seats.Count; i++)
            {
                if (seats[i] != null && seats[i].gameObject.activeInHierarchy) total += seats[i].capacity;
            }
            return total;
        }
    }

    public int CapacityUsed
    {
        get
        {
            int used = 0;
            for (int i = 0; i < seats.Count; i++)
            {
                if (seats[i] != null && seats[i].gameObject.activeInHierarchy) used += seats[i].occupants.Count;
            }
            return used;
        }
    }

    /// <summary>
    /// Eficiencia dinamica 0-100. Empieza en 100% y sube o baja segun la atencion:
    /// baja con abandonos, distracciones, mesas inadecuadas y alumnos listos sin atender;
    /// sube al atender bien a los alumnos.
    /// </summary>
    public float Efficiency
    {
        get { return Mathf.Clamp(efficiency, 0f, 100f); }
    }

    public void AddEfficiency(float delta)
    {
        efficiency = Mathf.Clamp(efficiency + delta, 0f, 100f);
    }

    // ------------------------------------------------------------------ ciclo

    void Awake()
    {
        Instance = this;

        AudioManager.Ensure();
        AudioManager.SetMusicLevel(0.62f);
        AudioManager.SetMusicDuck(false);

        // La campana se juega en la misma sala: el dia guardado define turno,
        // mesas activas, ritmo de alumnos, apartados y paciencia.
        diaActual = DayProgression.DiaGuardado();
        configDia = DayProgression.Get(diaActual);
        AplicarConfigDia();
    }

    void AplicarConfigDia()
    {
        if (configDia == null) configDia = DayProgression.Get(1);

        turnDuration = configDia.turnDuration;
        rushLastSeconds = configDia.rushLastSeconds;
        apartadoMin = configDia.apartadoMin;
        apartadoMax = configDia.apartadoMax;

        if (queue != null)
        {
            queue.spawnInterval = configDia.spawnInterval;
            queue.firstSpawnDelay = configDia.firstSpawnDelay;
            queue.weightLector = configDia.weightLector;
            queue.weightTesis = configDia.weightTesis;
            queue.weightGrupo = configDia.weightGrupo;
            queue.weightDespistado = configDia.weightDespistado;
            queue.maxDistracted = configDia.maxDistracted;
        }

        // Dias 1-2: solo los 4 cubiculos de silencio. Desde el dia 3 se abren
        // las mesas dobles y la zona de ruido.
        for (int i = 0; i < seats.Count; i++)
        {
            if (seats[i] == null) continue;
            seats[i].gameObject.SetActive(i < configDia.activeSeats);
        }
    }

    void Start()
    {
        timeLeft = turnDuration;
        playing = true;
        efficiency = Mathf.Clamp(efficiencyStart, 0f, 100f);
        apartadoTimer = Random.Range(apartadoMin, apartadoMax);
        Time.timeScale = 1f;

        AudioManager.Play(Sfx.DayStart, 0.85f, 1f);

        SetMessage("DIA " + diaActual + ": meta " + Mathf.RoundToInt(configDia.umbralEficiencia) +
                   "%. Arrastra cada estudiante a su mesa", 5f);
    }

    void Update()
    {
        if (!playing) return;

        timeLeft -= Time.deltaTime;

        if (!rush && timeLeft <= rushLastSeconds)
        {
            rush = true;
            AudioManager.Play(Sfx.Rush, 0.9f, 1f);
            AudioManager.SetMusicDuck(true);
            SetMessage("MODO RUSH: llegan mas estudiantes (la fila espera mas)", 3f);
        }

        int segundoActual = Mathf.CeilToInt(timeLeft);
        if (segundoActual != lastTickSecond)
        {
            lastTickSecond = segundoActual;
            if (segundoActual > 0 && segundoActual <= 10)
                AudioManager.Play(Sfx.Tick, 0.7f, segundoActual <= 3 ? 1.25f : 1f);
        }

        if (freezeLeft > 0f) freezeLeft -= Time.deltaTime;
        if (comboLeft > 0f)
        {
            comboLeft -= Time.deltaTime;
            if (comboLeft <= 0f) comboLeft = 0f;
        }

        // Alumnos listos esperando demasiado: penalizacion UNICA por alumno (no drenaje continuo).
        int ignored = 0;
        for (int i = 0; i < seats.Count; i++)
        {
            Seat st = seats[i];
            if (st == null) continue;
            for (int j = 0; j < st.occupants.Count; j++)
            {
                Student oc = st.occupants[j];
                if (oc == null || oc.readyPenalized) continue;
                if (oc.state != StudentState.Ready) continue;
                if (oc.ReadySeconds <= effReadyGrace) continue;

                oc.readyPenalized = true;
                ignored++;
            }
        }
        if (ignored > 0)
        {
            AddEfficiency(-effLossIgnoredReady * ignored);
            SetMessage("Alumno esperando sin atender -" + Mathf.RoundToInt(effLossIgnoredReady * ignored), 1.8f);
        }

        apartadoTimer -= Time.deltaTime;
        if (apartadoTimer <= 0f)
        {
            apartadoTimer = Random.Range(apartadoMin, apartadoMax);
            BlockRandomSeat();
        }

        if (timeLeft <= 0f)
        {
            timeLeft = 0f;
            EndTurn();
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Time.timeScale = 1f;
    }

    // ------------------------------------------------------------------ perfiles

    public StudentProfile GetProfile(StudentKind kind)
    {
        if (!profiles.ContainsKey(kind)) profiles[kind] = StudentProfile.Create(kind);

        StudentProfile p = profiles[kind].Clone();
        // Los primeros dias son mas tolerantes con la fila.
        float mult = configDia != null ? Mathf.Clamp(configDia.patienceMultiplier, 0.3f, 3f) : 1f;
        p.patience *= mult;
        return p;
    }

    // ------------------------------------------------------------------ registro

    public void RegisterArrival(Student s)
    {
        arrived++;
    }

    public void RegisterAssigned(Student s)
    {
        // Mesa inadecuada: se permite, pero cuesta eficiencia y menos puntos.
        if (s != null && s.wrongSeat)
        {
            perfectStreak = 0;
            AddEfficiency(-effLossWrongSeat);
            AudioManager.Play(Sfx.Error, 0.8f, 0.9f);
            SetMessage("Mesa inadecuada: ganaras menos puntos por " + s.profile.displayName, 2f);
            return;
        }

        perfectStreak++;

        if (perfectStreak >= 3)
        {
            perfectStreak = 0;
            comboLeft = 15f;
            bonoAvailable = true;
            AudioManager.Play(Sfx.Combo, 1f, 1f);
            SetMessage("COMBO x2 durante 15s", 2.5f);
        }
        else
        {
            AudioManager.Play(Sfx.Assign, 0.9f, 0.97f + 0.06f * (perfectStreak % 2));
            SetMessage("+" + s.profile.basePoints + " " + s.profile.displayName, 1.2f);
        }
    }

    public void RegisterHappy(Student s, float patienceFrac, bool pairBonus, bool withBonus)
    {
        int pts = s.profile.basePoints;

        AudioManager.Play(Sfx.Happy, 0.85f, 0.92f + 0.14f * Mathf.Clamp01(patienceFrac));

        if (withBonus) pts += Mathf.RoundToInt(50f * Mathf.Clamp01(patienceFrac));

        if (s.sharedDouble)
            pts = Mathf.RoundToInt(pts * (pairBonus ? 1.25f : 0.8f));

        if (comboLeft > 0f) pts *= 2;

        if (s.wrongSeat)
        {
            pts = Mathf.Max(1, Mathf.RoundToInt(pts * wrongSeatPointsFactor));
            score += pts;
            happy++;
            SetMessage("+" + pts + " " + s.profile.displayName + " (mesa inadecuada)", 1.8f);
            return;
        }

        score += pts;
        happy++;
        AddEfficiency(effGainHappy * (0.35f + 0.65f * Mathf.Clamp01(patienceFrac)));
        SetMessage("+" + pts + " " + s.profile.displayName, 1.4f);
    }

    public void RegisterAbandon(Student s)
    {
        abandoned++;
        perfectStreak = 0;
        comboLeft = 0f;
        score -= penaltyAbandon;
        AddEfficiency(-effLossAbandon);
        AudioManager.Play(Sfx.Abandon, 1f, 1f);
        SetMessage("Abandono -" + penaltyAbandon, 1.8f);
    }

    public void RegisterEarlyLeave(Student s)
    {
        abandoned++;
        earlyLeaves++;
        perfectStreak = 0;
        comboLeft = 0f;
        score -= penaltyEarlyLeave;
        AddEfficiency(-effLossEarlyLeave);
        AudioManager.Play(Sfx.EarlyLeave, 0.9f, 1f);
        SetMessage("Se distrajo y se fue -" + penaltyEarlyLeave, 1.8f);
    }

    public void RegisterBadAssignment()
    {
        badAssignments++;
        perfectStreak = 0;
        score -= penaltyBadAssign;
        AddEfficiency(-effLossBadAssign);
        AudioManager.Play(Sfx.Error, 0.85f, 1f);
        SetMessage("Mesa no disponible -" + penaltyBadAssign, 1.6f);
    }

    public void RegisterRelocate()
    {
        relocations++;
        relocationAvailable = false;
        score -= penaltyRelocate;
        AudioManager.Play(Sfx.Error, 0.6f, 0.8f);
        SetMessage("Reubicacion -" + penaltyRelocate, 1.6f);
    }

    public void RegisterAttention()
    {
        score += 25;
        AddEfficiency(effGainAttention);
        AudioManager.Play(Sfx.Attention, 1f, 1f);
        SetMessage("+25 atencion", 1.2f);
    }

    public void SetDragOrder(Student s, bool on)
    {
        if (s == null) return;
        UnityEngine.Rendering.SortingGroup group = s.GetComponent<UnityEngine.Rendering.SortingGroup>();
        if (group != null) group.sortingOrder = on ? 60 : 20;
    }

    // ------------------------------------------------------------------ eventos

    public void BlockRandomSeat()
    {
        List<Seat> candidates = new List<Seat>();
        for (int i = 0; i < seats.Count; i++)
        {
            Seat st = seats[i];
            if (st == null || !st.gameObject.activeInHierarchy) continue;
            if (!st.blocked && st.occupants.Count == 0) candidates.Add(st);
        }

        if (candidates.Count == 0) return;

        Seat target = candidates[Random.Range(0, candidates.Count)];
        target.Block();
        AudioManager.Play(Sfx.Apartado, 1f, 1f);
        SetMessage("Apartado en la mesa " + target.seatId + "!", 2f);
    }

    public void ContaminateNeighbor(Seat origin)
    {
        if (origin == null) return;

        Seat nearest = null;
        float best = float.MaxValue;

        for (int i = 0; i < seats.Count; i++)
        {
            Seat st = seats[i];
            if (st == null || st == origin || !st.gameObject.activeInHierarchy) continue;
            float d = Vector2.Distance(st.transform.position, origin.transform.position);
            if (d < best && d <= apartadoContaminationRadius)
            {
                best = d;
                nearest = st;
            }
        }

        if (nearest != null)
        {
            nearest.Contaminate(6f);
            AudioManager.Play(Sfx.Apartado, 0.7f, 0.85f);
            SetMessage("El apartado contamino una mesa vecina", 2f);
        }
    }

    public void ActivateBono()
    {
        if (!playing || !bonoAvailable) return;

        bonoAvailable = false;
        freezeLeft = 6f;

        for (int i = 0; i < seats.Count; i++)
        {
            if (seats[i] != null) seats[i].ForceCleanAll();
        }

        AudioManager.Play(Sfx.Bono, 1f, 1f);
        SetMessage("BONO DE SILENCIO: sala limpia y paciencia congelada 6s", 2.5f);
    }

    public void SetPaused(bool paused)
    {
        if (!playing) return;
        Time.timeScale = paused ? 0f : 1f;
    }

    public void SetMessage(string text, float seconds)
    {
        message = text;
        messageUntil = Time.time + seconds;
    }

    // ------------------------------------------------------------------ fin

    void EndTurn()
    {
        if (!playing) return;
        playing = false;

        AudioManager.Play(Sfx.Whistle, 1f, 1f);
        AudioManager.SetMusicDuck(false);

        PlayerPrefs.SetInt("UltimoScore", score);
        PlayerPrefs.SetFloat("UltimaEficiencia", Efficiency);
        PlayerPrefs.SetInt("UltimosAtendidos", happy);
        PlayerPrefs.SetInt("UltimosAbandonos", abandoned);
        PlayerPrefs.SetInt("UltimosLlegados", arrived);
        PlayerPrefs.SetInt("UltimosPuntos", score);
        PlayerPrefs.SetInt("UltimoDia", diaActual);
        // Identificador unico del turno: evita registrar dos veces la misma partida en el top-10.
        PlayerPrefs.SetString("UltimoRunId", System.DateTime.UtcNow.Ticks + "-" + score);
        PlayerPrefs.Save();

        if (hud != null) hud.ShowEnd();
        StartCoroutine(GoToResults());
    }

    IEnumerator GoToResults()
    {
        yield return new WaitForSecondsRealtime(3f);
        Time.timeScale = 1f;
        SceneManager.LoadScene("GameOverScene");
    }
}
