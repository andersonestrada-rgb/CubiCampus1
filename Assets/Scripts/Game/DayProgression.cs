using System;
using UnityEngine;

/// <summary>
/// Configuracion de un dia de la campana: umbral, duracion del turno, ritmo de
/// llegada de alumnos, frecuencia de apartados, paciencia y tipos disponibles.
/// </summary>
[Serializable]
public class DayConfig
{
    public int dia;
    public string titulo;

    [Header("Objetivo")]
    public float umbralEficiencia = 60f;

    [Header("Turno")]
    public float turnDuration = 150f;
    public float rushLastSeconds = 25f;

    [Header("Llegada de alumnos")]
    public float spawnInterval = 9.5f;
    public float firstSpawnDelay = 1.5f;

    [Header("Apartados")]
    public float apartadoMin = 42f;
    public float apartadoMax = 58f;

    [Header("Paciencia")]
    [Tooltip("Multiplicador sobre la paciencia base de cada perfil. Los primeros dias son mas tolerantes.")]
    public float patienceMultiplier = 1.35f;

    [Header("Sala")]
    [Tooltip("Mesas activas: 4 en el tutorial (cubiculos), 8 desde el dia 3.")]
    public int activeSeats = 4;

    [Header("Mezcla de alumnos")]
    public float weightLector = 0.58f;
    public float weightTesis = 0.22f;
    public float weightGrupo = 0f;
    public float weightDespistado = 0.20f;

    [Tooltip("Maximo de despistados a la vez (fila + mesas).")]
    public int maxDistracted = 1;
}

/// <summary>
/// Campana de 10 dias en la misma sala. La dificultad sube de forma escalada:
/// mas alumnos, apartados mas frecuentes, rush mas largo y desde el dia 8 dos
/// despistados simultaneos. El umbral pasa de 60% (tutorial) a 75%.
///
/// El dia actual se guarda en PlayerPrefs con la clave "DiaActual" para que el
/// menu ofrezca continuar donde se quedo el jugador.
/// </summary>
public static class DayProgression
{
    public const string KeyDiaActual = "DiaActual";

    static readonly DayConfig[] dias =
    {
        // dia 1 - tutorial: 4 cubiculos, sin grupos, paciencia muy alta
        new DayConfig
        {
            dia = 1, titulo = "Primer turno",
            umbralEficiencia = 60f,
            turnDuration = 150f, rushLastSeconds = 25f,
            spawnInterval = 9.5f, firstSpawnDelay = 1.5f,
            apartadoMin = 42f, apartadoMax = 58f,
            patienceMultiplier = 1.35f, activeSeats = 4,
            weightLector = 0.58f, weightTesis = 0.22f, weightGrupo = 0f, weightDespistado = 0.20f,
            maxDistracted = 1
        },
        // dia 2 - sigue el tutorial, algo mas de gente
        new DayConfig
        {
            dia = 2, titulo = "Ritmo de biblioteca",
            umbralEficiencia = 60f,
            turnDuration = 155f, rushLastSeconds = 28f,
            spawnInterval = 9f, firstSpawnDelay = 1.4f,
            apartadoMin = 39f, apartadoMax = 54f,
            patienceMultiplier = 1.25f, activeSeats = 4,
            weightLector = 0.50f, weightTesis = 0.28f, weightGrupo = 0f, weightDespistado = 0.22f,
            maxDistracted = 1
        },
        // dia 3 - se abren las 8 mesas y llegan los grupos ruidosos
        new DayConfig
        {
            dia = 3, titulo = "Sala completa",
            umbralEficiencia = 75f,
            turnDuration = 160f, rushLastSeconds = 32f,
            spawnInterval = 8.5f, firstSpawnDelay = 1.3f,
            apartadoMin = 35f, apartadoMax = 50f,
            patienceMultiplier = 1.15f, activeSeats = 8,
            weightLector = 0.40f, weightTesis = 0.20f, weightGrupo = 0.20f, weightDespistado = 0.20f,
            maxDistracted = 1
        },
        // dia 4
        new DayConfig
        {
            dia = 4, titulo = "Semana de examenes",
            umbralEficiencia = 75f,
            turnDuration = 165f, rushLastSeconds = 34f,
            spawnInterval = 8f, firstSpawnDelay = 1.2f,
            apartadoMin = 32f, apartadoMax = 46f,
            patienceMultiplier = 1.08f, activeSeats = 8,
            weightLector = 0.38f, weightTesis = 0.20f, weightGrupo = 0.22f, weightDespistado = 0.20f,
            maxDistracted = 1
        },
        // dia 5
        new DayConfig
        {
            dia = 5, titulo = "Punto medio",
            umbralEficiencia = 75f,
            turnDuration = 170f, rushLastSeconds = 36f,
            spawnInterval = 7.5f, firstSpawnDelay = 1.2f,
            apartadoMin = 29f, apartadoMax = 42f,
            patienceMultiplier = 1f, activeSeats = 8,
            weightLector = 0.35f, weightTesis = 0.20f, weightGrupo = 0.24f, weightDespistado = 0.21f,
            maxDistracted = 1
        },
        // dia 6
        new DayConfig
        {
            dia = 6, titulo = "Aforo tensionado",
            umbralEficiencia = 75f,
            turnDuration = 172f, rushLastSeconds = 38f,
            spawnInterval = 7f, firstSpawnDelay = 1.1f,
            apartadoMin = 27f, apartadoMax = 39f,
            patienceMultiplier = 0.96f, activeSeats = 8,
            weightLector = 0.32f, weightTesis = 0.21f, weightGrupo = 0.25f, weightDespistado = 0.22f,
            maxDistracted = 1
        },
        // dia 7
        new DayConfig
        {
            dia = 7, titulo = "Vigilia de biblioteca",
            umbralEficiencia = 75f,
            turnDuration = 175f, rushLastSeconds = 40f,
            spawnInterval = 6.5f, firstSpawnDelay = 1.1f,
            apartadoMin = 24f, apartadoMax = 36f,
            patienceMultiplier = 0.92f, activeSeats = 8,
            weightLector = 0.30f, weightTesis = 0.21f, weightGrupo = 0.26f, weightDespistado = 0.23f,
            maxDistracted = 1
        },
        // dia 8 - dos despistados a la vez
        new DayConfig
        {
            dia = 8, titulo = "Dos despistados",
            umbralEficiencia = 75f,
            turnDuration = 177f, rushLastSeconds = 42f,
            spawnInterval = 6.2f, firstSpawnDelay = 1f,
            apartadoMin = 22f, apartadoMax = 34f,
            patienceMultiplier = 0.88f, activeSeats = 8,
            weightLector = 0.28f, weightTesis = 0.20f, weightGrupo = 0.28f, weightDespistado = 0.24f,
            maxDistracted = 2
        },
        // dia 9
        new DayConfig
        {
            dia = 9, titulo = "Casi al limite",
            umbralEficiencia = 75f,
            turnDuration = 179f, rushLastSeconds = 43f,
            spawnInterval = 5.9f, firstSpawnDelay = 1f,
            apartadoMin = 20f, apartadoMax = 31f,
            patienceMultiplier = 0.85f, activeSeats = 8,
            weightLector = 0.26f, weightTesis = 0.20f, weightGrupo = 0.30f, weightDespistado = 0.24f,
            maxDistracted = 2
        },
        // dia 10 - final
        new DayConfig
        {
            dia = 10, titulo = "Aforo al limite",
            umbralEficiencia = 75f,
            turnDuration = 180f, rushLastSeconds = 45f,
            spawnInterval = 5.6f, firstSpawnDelay = 0.9f,
            apartadoMin = 18f, apartadoMax = 28f,
            patienceMultiplier = 0.82f, activeSeats = 8,
            weightLector = 0.24f, weightTesis = 0.20f, weightGrupo = 0.32f, weightDespistado = 0.24f,
            maxDistracted = 2
        }
    };

    public static int TotalDias { get { return dias.Length; } }

    /// <summary>Configuracion del dia indicado (1..10), con recorte automatico.</summary>
    public static DayConfig Get(int dia)
    {
        int i = Mathf.Clamp(dia, 1, dias.Length) - 1;
        return dias[i];
    }

    public static DayConfig PrimerDia { get { return dias[0]; } }

    public static int SiguienteDia(int dia)
    {
        return Mathf.Clamp(dia + 1, 1, dias.Length);
    }

    public static int DiaGuardado()
    {
        return Mathf.Clamp(PlayerPrefs.GetInt(KeyDiaActual, 1), 1, dias.Length);
    }

    public static void GuardarDia(int dia)
    {
        PlayerPrefs.SetInt(KeyDiaActual, Mathf.Clamp(dia, 1, dias.Length));
        PlayerPrefs.Save();
    }

    public static void Reiniciar()
    {
        GuardarDia(1);
    }

    /// <summary>Texto corto para HUD y menu: "DIA 3 / 10 - META 75%".</summary>
    public static string Etiqueta(int dia)
    {
        DayConfig c = Get(dia);
        return "DIA " + c.dia + " / " + dias.Length + "  -  META " + Mathf.RoundToInt(c.umbralEficiencia) + "%";
    }
}
