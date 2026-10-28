using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SfxEntry
{
    public Sfx tipo;
    public AudioClip clip;
    [Range(0f, 1f)] public float volumen = 1f;
    [Range(0.5f, 2f)] public float pitch = 1f;
    public bool usarPitchAleatorio;
    [Range(0f, 0.2f)] public float variacionPitch = 0.05f;
}

[CreateAssetMenu(fileName = "AudioConfig", menuName = "CubiCampus/Audio/Config")]
public class AudioConfig : ScriptableObject
{
    public static AudioConfig Instance;
    public List<SfxEntry> entradas = new List<SfxEntry>();
    public AudioClip musicaMenu;
    public AudioClip musicaJuego;
    public AudioClip musicaResultados;

    public SfxEntry Get(Sfx tipo)
    {
        for (int i = 0; i < entradas.Count; i++)
        {
            if (entradas[i] != null && entradas[i].tipo == tipo)
                return entradas[i];
        }
        return null;
    }
}
