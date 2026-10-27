using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Efectos de sonido disponibles en el juego.</summary>
public enum Sfx
{
    UiClick,
    Pickup,
    Pack,
    Assign,
    Happy,
    Attention,
    Distraction,
    Abandon,
    EarlyLeave,
    Error,
    Combo,
    Bono,
    Apartado,
    Rush,
    Tick,
    Whistle,
    Clean,
    Victory,
    Defeat,
    DayStart
}

/// <summary>
/// Audio 100% procedural: genera en runtime los efectos y la musica de ambiente
/// (sin archivos externos) y los reproduce con fuentes 2D. Persiste entre escenas
/// para que la musica no se corte al cambiar de sala. El slider de volumen del
/// menu controla el volumen maestro via AudioListener.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public const int Frecuencia = 22050;
    const float Tau = Mathf.PI * 2f;

    public static AudioManager Instance { get; private set; }

    [Header("Mezcla")]
    [Range(0f, 1f)] public float volumenMusica = 0.30f;
    [Range(0f, 1f)] public float volumenSfx = 0.90f;

    AudioSource fuenteMusica;
    AudioSource fuenteSfx;
    AudioClip musica;
    readonly Dictionary<Sfx, AudioClip> banco = new Dictionary<Sfx, AudioClip>();
    float nivelEscena = 0.85f;
    float duck = 1f;
    bool construido;

    // ------------------------------------------------------------------ ciclo

    public static AudioManager Ensure()
    {
        if (!Application.isPlaying) return null;
        if (Instance != null) return Instance;

        GameObject go = new GameObject("AudioManager");
        return go.AddComponent<AudioManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat("Volumen", 0.8f));

        if (UnityEngine.Object.FindAnyObjectByType<AudioListener>() == null)
            gameObject.AddComponent<AudioListener>();

        CrearFuentes();
        Construir();
        ReproducirMusica();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void CrearFuentes()
    {
        GameObject gm = new GameObject("Musica");
        gm.transform.SetParent(transform, false);
        fuenteMusica = gm.AddComponent<AudioSource>();
        fuenteMusica.playOnAwake = false;
        fuenteMusica.loop = true;
        fuenteMusica.spatialBlend = 0f;
        fuenteMusica.volume = 0f;

        GameObject gs = new GameObject("Efectos");
        gs.transform.SetParent(transform, false);
        fuenteSfx = gs.AddComponent<AudioSource>();
        fuenteSfx.playOnAwake = false;
        fuenteSfx.loop = false;
        fuenteSfx.spatialBlend = 0f;
    }

    void ReproducirMusica()
    {
        if (fuenteMusica == null || musica == null) return;

        fuenteMusica.clip = musica;
        AplicarVolumenMusica();
        fuenteMusica.Play();
    }

    void AplicarVolumenMusica()
    {
        if (fuenteMusica != null)
            fuenteMusica.volume = Mathf.Clamp01(volumenMusica * nivelEscena * duck);
    }

    // ------------------------------------------------------------------ API

    public static void Play(Sfx sfx)
    {
        Play(sfx, 1f, 1f);
    }

    public static void Play(Sfx sfx, float volumen, float tono)
    {
        if (!Application.isPlaying) return;

        AudioManager am = Ensure();
        if (am != null) am.Sonar(sfx, volumen, tono);
    }

    void Sonar(Sfx sfx, float volumen, float tono)
    {
        AudioClip clip;
        if (fuenteSfx == null || !banco.TryGetValue(sfx, out clip) || clip == null) return;

        fuenteSfx.pitch = Mathf.Clamp(tono, 0.4f, 2.2f);
        fuenteSfx.PlayOneShot(clip, Mathf.Clamp01(volumen) * volumenSfx);
    }

    /// <summary>Volumen relativo de la musica segun la escena (menu, partida, resultados).</summary>
    public static void SetMusicLevel(float nivel)
    {
        if (!Application.isPlaying) return;

        AudioManager am = Ensure();
        if (am == null) return;

        am.nivelEscena = Mathf.Clamp01(nivel);
        am.AplicarVolumenMusica();
    }

    /// <summary>Baja la musica momentaneamente (por ejemplo al entrar en rush).</summary>
    public static void SetMusicDuck(bool activo)
    {
        if (!Application.isPlaying) return;

        AudioManager am = Ensure();
        if (am == null) return;

        am.duck = activo ? 0.6f : 1f;
        am.AplicarVolumenMusica();
    }

    public static void SetMasterVolume(float valor)
    {
        AudioListener.volume = Mathf.Clamp01(valor);
    }

    // ------------------------------------------------------------------ sintesis

    void Construir()
    {
        if (construido) return;
        construido = true;

        banco[Sfx.UiClick] = Crear("sfx_ui", 0.06f, delegate (float t)
        {
            return Seno(t, 1050f) * Exp(t, 65f) * 0.55f;
        });

        banco[Sfx.Pickup] = Crear("sfx_pickup", 0.09f, delegate (float t)
        {
            return Chirp(t, 460f, 720f, 0.09f) * Exp(t, 35f) * 0.38f;
        });

        banco[Sfx.Pack] = Crear("sfx_pack", 0.05f, delegate (float t)
        {
            return Tri(t, 390f) * Exp(t, 80f) * 0.38f;
        });

        banco[Sfx.Assign] = Crear("sfx_assign", 0.16f, delegate (float t)
        {
            return (Chirp(t, 560f, 860f, 0.14f) + Seno(t, 1720f) * 0.12f) * Exp(t, 18f) * 0.55f;
        });

        banco[Sfx.Happy] = Crear("sfx_happy", 0.55f, delegate (float t)
        {
            float v = 0f;
            float[] notas = { 659.25f, 880f, 1174.66f };
            for (int i = 0; i < notas.Length; i++)
            {
                float lt = t - i * 0.075f;
                if (lt < 0f) continue;
                v += Seno(lt, notas[i]) * Exp(lt, 9f) * 0.42f;
            }
            return v * Exp(t, 2.2f);
        });

        banco[Sfx.Attention] = Crear("sfx_attention", 0.26f, delegate (float t)
        {
            if (t < 0.11f) return Seno(t, 1250f) * Exp(t, 28f) * 0.5f;
            float lt = t - 0.11f;
            return Seno(lt, 1650f) * Exp(lt, 24f) * 0.5f;
        });

        banco[Sfx.Distraction] = Crear("sfx_distraction", 0.36f, delegate (float t)
        {
            float v = 0f;
            if (t < 0.1f) v = Sq(t, 880f) * Exp(t, 16f) * 0.28f;
            else if (t > 0.16f && t < 0.28f)
            {
                float lt = t - 0.16f;
                v = Sq(lt, 820f) * Exp(lt, 16f) * 0.28f;
            }
            return v;
        });

        banco[Sfx.Abandon] = Crear("sfx_abandon", 0.5f, delegate (float t)
        {
            float f = Mathf.Lerp(340f, 88f, Mathf.Clamp01(t / 0.45f));
            return (Sq(t, f) * 0.55f + Seno(t, f * 0.5f) * 0.45f) * Exp(t, 5.5f) * 0.36f;
        });

        banco[Sfx.EarlyLeave] = Crear("sfx_earlyleave", 0.42f, delegate (float t)
        {
            float f = Mathf.Lerp(520f, 170f, Mathf.Clamp01(t / 0.38f));
            return Tri(t, f) * Exp(t, 7f) * 0.32f;
        });

        banco[Sfx.Error] = Crear("sfx_error", 0.28f, delegate (float t)
        {
            return (Sq(t, 150f) * 0.6f + Seno(t, 95f) * 0.4f) * Exp(t, 11f) * 0.4f;
        });

        banco[Sfx.Combo] = Crear("sfx_combo", 0.62f, delegate (float t)
        {
            float v = 0f;
            float[] notas = { 660f, 880f, 1320f };
            for (int i = 0; i < notas.Length; i++)
            {
                float lt = t - i * 0.075f;
                if (lt < 0f) continue;
                v += Seno(lt, notas[i]) * Exp(lt, 7.5f) * 0.4f;
            }
            v += Seno(t, 1760f) * Mathf.Clamp01((t - 0.2f) / 0.05f) * Exp(Mathf.Max(0f, t - 0.2f), 6f) * 0.18f;
            return v;
        });

        banco[Sfx.Bono] = Crear("sfx_bono", 0.85f, delegate (float t)
        {
            float env = Mathf.Clamp01(t / 0.12f) * Mathf.Clamp01((0.85f - t) / 0.25f);
            float brillo = Seno(t, 2100f) * 0.14f * (1f + Seno(t, 7f)) * 0.5f;
            return (Chirp(t, 280f, 1400f, 0.7f) * 0.5f + brillo) * env * 0.55f;
        });

        banco[Sfx.Apartado] = Crear("sfx_apartado", 0.42f, delegate (float t)
        {
            float ruido = t < 0.07f ? UnityEngine.Random.Range(-1f, 1f) * Exp(t, 40f) * 0.5f : 0f;
            return Seno(t, 96f) * Exp(t, 8f) * 0.6f + ruido;
        });

        banco[Sfx.Rush] = Crear("sfx_rush", 0.68f, delegate (float t)
        {
            if (t < 0.22f) return Sq(t, 790f) * Exp(t, 6f) * 0.34f;
            if (t > 0.36f && t < 0.6f)
            {
                float lt = t - 0.36f;
                return Sq(lt, 620f) * Exp(lt, 6f) * 0.34f;
            }
            return 0f;
        });

        banco[Sfx.Tick] = Crear("sfx_tick", 0.07f, delegate (float t)
        {
            return Seno(t, 1500f) * Exp(t, 85f) * 0.45f;
        });

        banco[Sfx.Whistle] = Crear("sfx_whistle", 1.2f, delegate (float t)
        {
            float env = Mathf.Clamp01(t / 0.04f) * Mathf.Clamp01((1.2f - t) / 0.3f);
            float f = 1850f + Mathf.Sin(Tau * 7f * t) * 55f;
            return (Seno(t, f) * 0.5f + Seno(t, f * 0.5f) * 0.12f) * env * 0.55f;
        });

        banco[Sfx.Clean] = Crear("sfx_clean", 0.32f, delegate (float t)
        {
            float ruido = UnityEngine.Random.Range(-1f, 1f) * Exp(t, 15f) * 0.32f;
            float tono = Seno(t, 1300f) * Exp(t, 26f) * 0.32f;
            float cola = Mathf.Clamp01((0.32f - t) / 0.06f);
            return (ruido + tono) * cola;
        });

        banco[Sfx.Victory] = Crear("sfx_victory", 1.6f, delegate (float t)
        {
            float v = 0f;
            float[] notas = { 523.25f, 659.25f, 783.99f, 1046.5f };
            for (int i = 0; i < notas.Length; i++)
            {
                float lt = t - i * 0.13f;
                if (lt < 0f) continue;
                v += Seno(lt, notas[i]) * Exp(lt, 4.5f) * 0.36f;
            }

            float lc = t - 0.55f;
            if (lc > 0f)
                v += (Seno(lc, 523.25f) + Seno(lc, 659.25f) + Seno(lc, 783.99f)) * Exp(lc, 3.4f) * 0.12f;

            return v * Mathf.Clamp01(t / 0.005f);
        });

        banco[Sfx.Defeat] = Crear("sfx_defeat", 1.5f, delegate (float t)
        {
            float v = 0f;
            float[] notas = { 392f, 329.63f, 261.63f, 196f };
            for (int i = 0; i < notas.Length; i++)
            {
                float lt = t - i * 0.18f;
                if (lt < 0f) continue;
                v += Tri(lt, notas[i]) * Exp(lt, 4f) * 0.34f;
            }
            return v * Mathf.Clamp01(t / 0.005f);
        });

        banco[Sfx.DayStart] = Crear("sfx_daystart", 0.65f, delegate (float t)
        {
            float v = 0f;
            float[] notas = { 523.25f, 783.99f };
            for (int i = 0; i < notas.Length; i++)
            {
                float lt = t - i * 0.13f;
                if (lt < 0f) continue;
                v += (Seno(lt, notas[i]) + Seno(lt, notas[i] * 2f) * 0.15f) * Exp(lt, 4.2f) * 0.4f;
            }
            return v;
        });

        musica = CrearMusica();
    }

    static AudioClip Crear(string nombre, float duracion, Func<float, float> muestra)
    {
        int n = Mathf.Max(1, Mathf.CeilToInt(duracion * Frecuencia));
        float[] datos = new float[n];

        for (int i = 0; i < n; i++)
            datos[i] = Mathf.Clamp(muestra(i / (float)Frecuencia), -1f, 1f);

        AudioClip clip = AudioClip.Create(nombre, n, 1, Frecuencia, false);
        clip.SetData(datos, 0);
        return clip;
    }

    static float Seno(float t, float f)
    {
        return Mathf.Sin(Tau * f * t);
    }

    static float Tri(float t, float f)
    {
        float p = t * f;
        p -= Mathf.Floor(p);
        return 4f * Mathf.Abs(p - 0.5f) - 1f;
    }

    static float Sq(float t, float f)
    {
        float p = t * f;
        p -= Mathf.Floor(p);
        return p < 0.5f ? 1f : -1f;
    }

    static float Exp(float t, float k)
    {
        return Mathf.Exp(-t * k);
    }

    /// <summary>Barrido lineal de frecuencia con fase continua.</summary>
    static float Chirp(float t, float f0, float f1, float duracion)
    {
        float u = Mathf.Clamp01(t / duracion);
        float fase = Tau * (f0 * t + (f1 - f0) * t * u * 0.5f);
        return Mathf.Sin(fase);
    }

    // ------------------------------------------------------------------ musica

    static AudioClip CrearMusica()
    {
        const float duracion = 16f;
        int n = Mathf.RoundToInt(duracion * Frecuencia);
        float[] datos = new float[n];

        // Am - F - C - G: pad suave de 4 s por acorde.
        float[][] acordes =
        {
            new float[] { 110f, 164.81f, 220f, 261.63f },
            new float[] { 87.31f, 130.81f, 174.61f, 220f },
            new float[] { 130.81f, 196f, 261.63f, 329.63f },
            new float[] { 98f, 146.83f, 196f, 246.94f }
        };

        // Frecuencias cuantizadas a ciclos enteros del loop: el empalme no chasquea.
        for (int c = 0; c < acordes.Length; c++)
            for (int k = 0; k < acordes[c].Length; k++)
                acordes[c][k] = Mathf.Round(acordes[c][k] * duracion) / duracion;

        float duracionAcorde = duracion / acordes.Length;

        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Frecuencia;
            float v = 0f;

            for (int c = 0; c < acordes.Length; c++)
            {
                float local = t - c * duracionAcorde;
                if (local < 0f || local >= duracionAcorde) continue;

                float env = Mathf.Clamp01(local / 0.45f) * Mathf.Clamp01((duracionAcorde - local) / 0.7f);
                float tremolo = 0.85f + 0.15f * Mathf.Sin(Tau * 0.25f * t);

                for (int k = 0; k < acordes[c].Length; k++)
                {
                    float f = acordes[c][k];
                    float onda = Mathf.Sin(Tau * f * t)
                               + 0.35f * Mathf.Sin(Tau * 2f * f * t)
                               + 0.12f * Mathf.Sin(Tau * 3f * f * t);
                    v += onda * env * tremolo * 0.055f;
                }
            }

            datos[i] = v;
        }

        // Campanitas suaves sobre el pad; ninguna cruza el final del loop.
        float[] campanas = { 523.25f, 659.25f, 783.99f, 987.77f, 587.33f, 880f };

        for (int k = 0; k < 14; k++)
        {
            float inicio = 0.9f + k * 1.05f;
            float f = Mathf.Round(campanas[k % campanas.Length] * duracion) / duracion;
            int desde = Mathf.RoundToInt(inicio * Frecuencia);
            int largo = Mathf.RoundToInt(1.4f * Frecuencia);

            for (int j = 0; j < largo && desde + j < n; j++)
            {
                float lt = j / (float)Frecuencia;
                float env = Mathf.Clamp01(lt / 0.01f) * Mathf.Exp(-lt * 2.6f);
                datos[desde + j] += Mathf.Sin(Tau * f * lt) * env * 0.05f;
            }
        }

        float max = 0.0001f;
        for (int i = 0; i < n; i++) max = Mathf.Max(max, Mathf.Abs(datos[i]));

        float escala = 0.9f / max;
        for (int i = 0; i < n; i++) datos[i] = Mathf.Clamp(datos[i] * escala, -1f, 1f);

        AudioClip clip = AudioClip.Create("musica_ambiente", n, 1, Frecuencia, false);
        clip.SetData(datos, 0);
        return clip;
    }

    /// <summary>Volumen maestro actual, para pruebas y para el menu.</summary>
    public static float MasterVolume()
    {
        return AudioListener.volume;
    }
}
