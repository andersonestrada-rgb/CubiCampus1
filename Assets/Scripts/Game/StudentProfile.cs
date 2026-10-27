using System;
using UnityEngine;

public enum StudentKind
{
    Lector,
    Tesis,
    GrupoRuidoso,
    Despistado
}

/// <summary>
/// Datos de balance de cada tipo de estudiante (base para el puzzle).
/// Todos los valores son editables desde el inspector del GameManager.
/// </summary>
[Serializable]
public class StudentProfile
{
    public StudentKind kind;
    public string displayName;
    public int basePoints;
    public float studyTime;
    public float patience;
    public Color color;
    public bool requiresOutlet;
    public bool requiresSilence;
    public bool requiresNoise;
    public bool isGroup;
    public bool distracted; // Despistado: se distrae a mitad del estudio

    public static StudentProfile Create(StudentKind kind)
    {
        StudentProfile p = new StudentProfile();
        p.kind = kind;

        switch (kind)
        {
            case StudentKind.Lector:
                p.displayName = "Lector";
                p.basePoints = 100;
                p.studyTime = 30f;
                p.patience = 22f;
                p.color = new Color(0.35f, 0.68f, 1f);
                p.requiresSilence = true;
                break;

            case StudentKind.Tesis:
                p.displayName = "Tesis";
                p.basePoints = 200;
                p.studyTime = 60f;
                p.patience = 30f;
                p.color = new Color(0.72f, 0.52f, 1f);
                p.requiresSilence = true;
                p.requiresOutlet = true;
                break;

            case StudentKind.GrupoRuidoso:
                p.displayName = "Grupo";
                p.basePoints = 120;
                p.studyTime = 40f;
                p.patience = 18f;
                p.color = new Color(1f, 0.58f, 0.26f);
                p.requiresNoise = true;
                p.isGroup = true;
                break;

            case StudentKind.Despistado:
                p.displayName = "Despistado";
                p.basePoints = 80;
                p.studyTime = 25f;
                p.patience = 16f;
                p.color = new Color(0.45f, 0.88f, 0.55f);
                p.distracted = true;
                break;
        }

        return p;
    }

    public StudentProfile Clone()
    {
        return (StudentProfile)MemberwiseClone();
    }
}
