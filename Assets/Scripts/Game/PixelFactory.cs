using UnityEngine;

/// <summary>
/// Fabrica de sprites geometricos. Todo el juego usa cuadrados 1x1 tintados
/// (SpriteRenderer) para que un artista pueda reemplazarlos despues.
/// </summary>
public static class PixelFactory
{
    static Sprite white;

    public static Sprite White
    {
        get
        {
            if (white == null)
            {
                // Sprite placeholder del proyecto (reemplazable por arte final).
                white = Resources.Load<Sprite>("Blanco");
            }

            if (white == null)
            {
                Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                tex.SetPixel(0, 0, Color.white);
                tex.filterMode = FilterMode.Point;
                tex.Apply();
                tex.hideFlags = HideFlags.HideAndDontSave;

                white = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
                white.hideFlags = HideFlags.HideAndDontSave;
            }
            return white;
        }
    }

    public static SpriteRenderer Quad(string name, Transform parent, Vector3 localPos, Vector2 size, Color color, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = White;
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

    public static void SetSize(SpriteRenderer sr, Vector2 size)
    {
        if (sr == null) return;
        sr.transform.localScale = new Vector3(size.x, size.y, 1f);
    }

    public static void SetAlpha(SpriteRenderer sr, float alpha)
    {
        if (sr == null) return;
        Color c = sr.color;
        c.a = alpha;
        sr.color = c;
    }
}
