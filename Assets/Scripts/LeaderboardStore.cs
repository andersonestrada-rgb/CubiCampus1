using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Entrada de un marcador diario.
/// </summary>
[Serializable]
public class LeaderboardEntry
{
    public string nombre;
    public int score;
    public float eficiencia;
    public bool actual;
}

/// <summary>
/// Marcadores locales separados por dia. Cada dia guarda su propio top 20 en
/// PlayerPrefs para que la campana de 10 dias tenga 10 tablas independientes.
/// Tambien centraliza el nombre del jugador elegido en el menu.
/// </summary>
public static class LeaderboardStore
{
    public const int MaxDias = 10;
    public const int MaxEntradas = 20;
    public const int MaxNombre = 16;
    public const string KeyNombreJugador = "NombreJugador";
    public const string DefaultNombre = "ALEX MONITOR";

    public struct ResumenRegistro
    {
        public int puesto;
        public bool nuevoRecord;
        public bool entro;
    }

    // ------------------------------------------------------------------ nombre

    public static string NombreJugador()
    {
        string guardado = PlayerPrefs.GetString(KeyNombreJugador, "");
        if (string.IsNullOrEmpty(guardado)) return DefaultNombre;
        return Normalizar(guardado);
    }

    public static void GuardarNombre(string nombre)
    {
        string limpio = Normalizar(nombre);
        PlayerPrefs.SetString(KeyNombreJugador, limpio);
        PlayerPrefs.Save();
    }

    /// <summary>Recorta, quita espacios repetidos y limita a 16 caracteres.</summary>
    public static string Normalizar(string nombre)
    {
        if (string.IsNullOrEmpty(nombre)) return DefaultNombre;

        string recortado = nombre.Trim();
        StringBuilder sb = new StringBuilder(MaxNombre);
        bool espacioPrevio = false;

        for (int i = 0; i < recortado.Length && sb.Length < MaxNombre; i++)
        {
            char c = recortado[i];

            if (c == ' ')
            {
                if (sb.Length == 0 || espacioPrevio) continue;
                sb.Append(' ');
                espacioPrevio = true;
            }
            else
            {
                sb.Append(c);
                espacioPrevio = false;
            }
        }

        string limpio = sb.ToString().Trim();
        return string.IsNullOrEmpty(limpio) ? DefaultNombre : limpio;
    }

    // ------------------------------------------------------------------ tablas

    static string Clave(int dia, int indice, string campo)
    {
        return "Rank_D" + Mathf.Clamp(dia, 1, MaxDias) + "_" + indice + "_" + campo;
    }

    /// <summary>Top del dia pedido, ya ordenado por puntos y eficiencia.</summary>
    public static List<LeaderboardEntry> Top(int dia, int limite)
    {
        int tope = Mathf.Clamp(limite, 0, MaxEntradas);
        List<LeaderboardEntry> lista = Cargar(dia);

        if (lista.Count > tope) lista.RemoveRange(tope, lista.Count - tope);
        return lista;
    }

    /// <summary>
    /// Registra una partida en la tabla del dia y devuelve el puesto obtenido
    /// (puede ser mayor que MaxEntradas si no entra en el top guardado).
    /// </summary>
    public static ResumenRegistro Registrar(int dia, int score, float eficiencia)
    {
        return Registrar(dia, NombreJugador(), score, eficiencia);
    }

    public static ResumenRegistro Registrar(int dia, string nombre, int score, float eficiencia)
    {
        List<LeaderboardEntry> lista = Cargar(dia);
        int mejorPrevio = lista.Count > 0 ? lista[0].score : int.MinValue;

        lista.Add(new LeaderboardEntry
        {
            nombre = Normalizar(nombre),
            score = Mathf.Max(0, score),
            eficiencia = Mathf.Clamp(eficiencia, 0f, 100f),
            actual = true
        });

        lista.Sort(Comparar);

        int puesto = 1;
        for (int i = 0; i < lista.Count; i++)
        {
            if (lista[i].actual)
            {
                puesto = i + 1;
                break;
            }
        }

        bool nuevoRecord = puesto == 1 && score > mejorPrevio;

        GuardarLista(dia, lista);

        return new ResumenRegistro
        {
            puesto = puesto,
            nuevoRecord = nuevoRecord,
            entro = puesto <= MaxEntradas
        };
    }

    /// <summary>Mejor puesto del nombre indicado dentro del top guardado del dia.</summary>
    public static int MejorPuesto(int dia, string nombre)
    {
        if (string.IsNullOrEmpty(nombre)) return 0;

        List<LeaderboardEntry> lista = Cargar(dia);
        for (int i = 0; i < lista.Count; i++)
        {
            if (string.Equals(lista[i].nombre, nombre, StringComparison.OrdinalIgnoreCase))
                return i + 1;
        }

        return 0;
    }

    static int Comparar(LeaderboardEntry a, LeaderboardEntry b)
    {
        int cmp = b.score.CompareTo(a.score);
        if (cmp != 0) return cmp;

        cmp = b.eficiencia.CompareTo(a.eficiencia);
        if (cmp != 0) return cmp;

        // Empate exacto: la partida recien jugada queda debajo de las anteriores.
        if (a.actual != b.actual) return a.actual ? 1 : -1;
        return 0;
    }

    static List<LeaderboardEntry> Cargar(int dia)
    {
        List<LeaderboardEntry> lista = new List<LeaderboardEntry>();

        for (int i = 0; i < MaxEntradas; i++)
        {
            string nombre = PlayerPrefs.GetString(Clave(dia, i, "Nombre"), "");
            if (string.IsNullOrEmpty(nombre)) continue;

            lista.Add(new LeaderboardEntry
            {
                nombre = nombre,
                score = PlayerPrefs.GetInt(Clave(dia, i, "Score"), 0),
                eficiencia = PlayerPrefs.GetFloat(Clave(dia, i, "Eficiencia"), 0f)
            });
        }

        lista.Sort(Comparar);
        return lista;
    }

    static void GuardarLista(int dia, List<LeaderboardEntry> lista)
    {
        int total = Mathf.Min(MaxEntradas, lista.Count);

        for (int i = 0; i < total; i++)
        {
            PlayerPrefs.SetString(Clave(dia, i, "Nombre"), lista[i].nombre);
            PlayerPrefs.SetInt(Clave(dia, i, "Score"), lista[i].score);
            PlayerPrefs.SetFloat(Clave(dia, i, "Eficiencia"), lista[i].eficiencia);
        }

        for (int i = total; i < MaxEntradas; i++)
        {
            PlayerPrefs.DeleteKey(Clave(dia, i, "Nombre"));
            PlayerPrefs.DeleteKey(Clave(dia, i, "Score"));
            PlayerPrefs.DeleteKey(Clave(dia, i, "Eficiencia"));
        }

        PlayerPrefs.Save();
    }
}
