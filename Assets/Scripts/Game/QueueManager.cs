using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Genera estudiantes y gestiona la fila FIFO de 3 puestos visibles.
/// </summary>
public class QueueManager : MonoBehaviour
{
    [Header("Referencias")]
    public GameManager game;
    public Transform doorPoint;
    public Transform[] slots;

    [Header("Prefab")]
    public GameObject estudiantePrefab;

    [Header("Balance")]
    public float spawnInterval = 8f;
    public float firstSpawnDelay = 1.2f;

    [Range(0f, 1f)] public float weightLector = 0.34f;
    [Range(0f, 1f)] public float weightTesis = 0.18f;
    [Range(0f, 1f)] public float weightGrupo = 0.26f;
    [Range(0f, 1f)] public float weightDespistado = 0.22f;

    [Header("Progresion")]
    [Tooltip("Maximo de despistados a la vez entre fila y mesas (2 en los dias 8-10).")]
    public int maxDistracted = 1;

    public List<Student> Students { get { return students; } }

    readonly List<Student> students = new List<Student>();

    float timer;
    bool running;

    void Start()
    {
        timer = firstSpawnDelay;
        running = true;
    }

    void Update()
    {
        if (!running || game == null || !game.IsPlaying || game.IsPaused) return;
        if (slots == null || slots.Length == 0) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        timer = SpawnInterval();
        if (students.Count < slots.Length) SpawnRandom();
    }

    float SpawnInterval()
    {
        float interval = spawnInterval;
        if (game != null && game.IsRush) interval /= 1.5f;
        return Mathf.Max(2.5f, interval);
    }

    public Student SpawnRandom()
    {
        StudentKind kind = PickKind();
        return Spawn(kind);
    }

    public Student Spawn(StudentKind kind)
    {
        if (slots == null || slots.Length == 0) return null;

        StudentProfile profile = game.GetProfile(kind);
        if (profile == null) profile = StudentProfile.Create(kind);

        Vector3 spawnPos = doorPoint != null ? doorPoint.position : new Vector3(-6f, -3.5f, 0f);
        Student s = null;

        if (estudiantePrefab != null)
        {
            GameObject go = Instantiate(estudiantePrefab, spawnPos, Quaternion.identity);
            s = go.GetComponent<Student>();
            if (s == null) s = go.AddComponent<Student>();
            s.SetupFromProfile(profile, game, this);
        }
        else
        {
            s = Student.Create(profile, spawnPos, game, this);
        }

        students.Add(s);
        int index = students.Count - 1;
        Vector3 target = SlotPosition(index);
        s.WalkTo(target, 5.5f);

        game.RegisterArrival(s);
        return s;
    }

    public void RemoveStudent(Student s)
    {
        if (s == null) return;
        if (!students.Remove(s)) return;

        for (int i = 0; i < students.Count; i++)
        {
            if (students[i] == null) continue;
            if (students[i].state == StudentState.InQueue)
                students[i].WalkTo(SlotPosition(i), 6.5f);
        }
    }

    public Vector3 SlotPosition(int index)
    {
        if (slots == null || slots.Length == 0) return Vector3.zero;
        int i = Mathf.Clamp(index, 0, slots.Length - 1);
        return slots[i] != null ? slots[i].position : Vector3.zero;
    }

    StudentKind PickKind()
    {
        float total = weightLector + weightTesis + weightGrupo + weightDespistado;
        if (total <= 0f) return StudentKind.Lector;

        float r = Random.value * total;
        StudentKind kind;
        if (r < weightLector) kind = StudentKind.Lector;
        else
        {
            r -= weightLector;
            if (r < weightTesis) kind = StudentKind.Tesis;
            else
            {
                r -= weightTesis;
                kind = r < weightGrupo ? StudentKind.GrupoRuidoso : StudentKind.Despistado;
            }
        }

        // Limite de despistados simultaneos: si ya hay el maximo, llega otro tipo.
        if (kind == StudentKind.Despistado && maxDistracted > 0 && CountDistracted() >= maxDistracted)
            kind = PickNonDistracted();

        return kind;
    }

    StudentKind PickNonDistracted()
    {
        float total = weightLector + weightTesis + weightGrupo;
        if (total <= 0f) return StudentKind.Lector;

        float r = Random.value * total;
        if (r < weightLector) return StudentKind.Lector;
        r -= weightLector;
        if (r < weightTesis) return StudentKind.Tesis;
        return StudentKind.GrupoRuidoso;
    }

    /// <summary>Despistados vivos entre la fila y las mesas.</summary>
    int CountDistracted()
    {
        int n = 0;

        for (int i = 0; i < students.Count; i++)
        {
            Student s = students[i];
            if (IsCountableDistracted(s)) n++;
        }

        if (game != null)
        {
            for (int i = 0; i < game.seats.Count; i++)
            {
                Seat st = game.seats[i];
                if (st == null) continue;
                for (int j = 0; j < st.occupants.Count; j++)
                {
                    if (IsCountableDistracted(st.occupants[j])) n++;
                }
            }
        }

        return n;
    }

    static bool IsCountableDistracted(Student s)
    {
        if (s == null || s.profile == null) return false;
        if (s.profile.kind != StudentKind.Despistado) return false;
        if (s.state == StudentState.Leaving || s.state == StudentState.Gone) return false;
        return true;
    }
}
