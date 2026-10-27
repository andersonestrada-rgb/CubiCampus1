using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// HUD del turno: reloj, puntos, eficiencia, aforo, combo, bono y pausa.
/// </summary>
public class HUDGame : MonoBehaviour
{
    [Header("Referencias")]
    public GameManager game;

    [Header("Textos")]
    public Text txtPuntos;
    public Text txtTiempo;
    public Text txtEficiencia;
    public Text txtAforo;
    public Text txtAbandonos;
    public Text txtCombo;
    public Text txtRush;
    public Text txtHint;
    public Text txtEstado;
    public Text txtBono;

    [Header("Barras")]
    public Image barraTiempo;
    public Image barraEficiencia;
    public Image overlayAlerta;

    [Header("Botones")]
    public Button btnBono;
    public Button btnPausa;
    public Button btnReanudar;
    public Button btnMenu;

    [Header("Paneles")]
    public GameObject panelPausa;

    float alertPulse;

    void Awake()
    {
        if (btnBono != null) btnBono.onClick.AddListener(OnBono);
        if (btnPausa != null) btnPausa.onClick.AddListener(delegate { SetPause(true); });
        if (btnReanudar != null) btnReanudar.onClick.AddListener(delegate { SetPause(false); });
        if (btnMenu != null) btnMenu.onClick.AddListener(VolverAlMenu);

        if (panelPausa != null) panelPausa.SetActive(false);
        if (txtEstado != null) txtEstado.gameObject.SetActive(false);
    }

    void Update()
    {
        if (game == null) return;

        UpdateClock();
        UpdateScore();
        UpdateEfficiency();
        UpdateAforo();
        UpdateCombo();
        UpdateHint();
        UpdateBono();
        UpdateAlert();
        UpdateInput();
    }

    void UpdateClock()
    {
        float t = game.TimeLeft;
        int minutes = Mathf.FloorToInt(t / 60f);
        int seconds = Mathf.FloorToInt(t % 60f);

        if (txtTiempo != null)
        {
            txtTiempo.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            txtTiempo.color = t <= 30f ? new Color(1f, 0.55f, 0.35f) : new Color(0.95f, 0.95f, 0.98f);
        }

        if (barraTiempo != null)
        {
            barraTiempo.fillAmount = game.TurnDuration > 0f ? Mathf.Clamp01(t / game.TurnDuration) : 0f;
            barraTiempo.color = t <= 30f ? new Color(1f, 0.45f, 0.30f) : new Color(0.35f, 0.70f, 1f);
        }

        if (txtRush != null)
        {
            bool rush = game.IsRush && game.IsPlaying;
            txtRush.gameObject.SetActive(rush);
            if (rush)
            {
                float a = 0.6f + Mathf.PingPong(Time.unscaledTime * 3f, 0.4f);
                txtRush.color = new Color(1f, 0.55f, 0.25f, a);
            }
        }
    }

    void UpdateScore()
    {
        if (txtPuntos != null) txtPuntos.text = "PUNTOS  " + game.Score;
    }

    void UpdateEfficiency()
    {
        float eff = game.Efficiency;

        if (txtEficiencia != null) txtEficiencia.text = "EFICIENCIA  " + Mathf.RoundToInt(eff) + "%";

        Color c;
        if (eff >= 75f) c = new Color(0.35f, 0.90f, 0.45f);
        else if (eff >= 40f) c = new Color(0.98f, 0.78f, 0.25f);
        else c = new Color(1f, 0.32f, 0.32f);

        if (barraEficiencia != null)
        {
            barraEficiencia.fillAmount = Mathf.Clamp01(eff / 100f);
            barraEficiencia.color = c;
        }

        if (txtEficiencia != null) txtEficiencia.color = c;
    }

    void UpdateAforo()
    {
        if (txtAforo != null)
            txtAforo.text = "AFORO  " + game.CapacityUsed + " / " + game.CapacityTotal;

        if (txtAbandonos != null)
            txtAbandonos.text = "ATENDIDOS " + game.Happy + "   ABANDONOS " + game.Abandoned +
                                "   REUBICACION " + (game.RelocationAvailable ? "SI" : "USADA");
    }

    void UpdateCombo()
    {
        if (txtCombo == null) return;

        if (game.ComboMultiplier > 1)
        {
            txtCombo.text = "COMBO x2  " + Mathf.CeilToInt(game.ComboLeft) + "s";
            txtCombo.color = new Color(1f, 0.85f, 0.30f);
        }
        else
        {
            txtCombo.text = "RACHA " + game.PerfectStreak + " / 3";
            txtCombo.color = new Color(0.70f, 0.74f, 0.85f);
        }
    }

    void UpdateHint()
    {
        if (txtHint == null) return;

        string msg = game.Message;
        if (string.IsNullOrEmpty(msg))
            msg = "Arrastra estudiantes a cualquier mesa  |  Clic repetido para que salgan  |  Manten clic y mueve para raspar manchas";

        txtHint.text = msg;
    }

    void UpdateBono()
    {
        bool ready = game.BonoAvailable;

        if (btnBono != null)
        {
            btnBono.interactable = ready;
            Image img = btnBono.targetGraphic as Image;
            if (img != null) img.color = ready ? new Color(0.22f, 0.62f, 0.95f) : new Color(0.22f, 0.24f, 0.30f);
        }

        if (txtBono != null) txtBono.text = ready ? "BONO SILENCIO" : "BONO USADO";
    }

    void UpdateAlert()
    {
        if (overlayAlerta == null) return;

        bool alert = game.IsAlert && game.IsPlaying;
        if (alert)
        {
            alertPulse += Time.unscaledDeltaTime * 2.5f;
            float a = 0.10f + Mathf.PingPong(alertPulse, 0.12f);
            overlayAlerta.gameObject.SetActive(true);
            overlayAlerta.color = new Color(1f, 0.15f, 0.15f, a);
        }
        else
        {
            overlayAlerta.gameObject.SetActive(false);
        }
    }

    void UpdateInput()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.escapeKey.wasPressedThisFrame && game.IsPlaying)
            SetPause(!game.IsPaused);

        if (kb.spaceKey.wasPressedThisFrame && !game.IsPaused && game.BonoAvailable)
            OnBono();
    }

    void OnBono()
    {
        if (game != null) game.ActivateBono();
    }

    void SetPause(bool paused)
    {
        if (game == null || !game.IsPlaying) return;
        game.SetPaused(paused);
        if (panelPausa != null) panelPausa.SetActive(paused);
    }

    void VolverAlMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MenuScene");
    }

    public void ShowEnd()
    {
        if (panelPausa != null) panelPausa.SetActive(false);
        if (txtEstado != null)
        {
            txtEstado.gameObject.SetActive(true);
            txtEstado.text = "TURNO FINALIZADO\nEficiencia " + Mathf.RoundToInt(game.Efficiency) + "%  |  " + game.Score + " pts";
        }
        if (btnBono != null) btnBono.interactable = false;
        if (btnPausa != null) btnPausa.interactable = false;
    }
}
