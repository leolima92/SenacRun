using UnityEngine;
using UnityEngine.SceneManagement;

// Controla o estado da partida e desenha a interface (HUD + telas de fim).
// Usa a interface imediata do Unity (OnGUI): não precisa de Canvas, EventSystem nem TextMeshPro.
public class GameManager : MonoBehaviour
{
    [SerializeField] StudentController student;
    [SerializeField] Transform goal;
    [SerializeField] float endScreenDelay = 0.6f;   // tempinho pra ver o boneco achatado

    enum State { Playing, Lost, Won }
    State state = State.Playing;

    float startTime;
    float finishTime;
    float endedAt;
    float bestTime = -1f;
    bool newRecord;

    const string BestKey = "SenacGame_MelhorTempo";

    // ---- estilos da interface ----
    bool stylesReady;
    float styleScale;
    GUIStyle titleStyle, textStyle, buttonStyle, hudStyle, hintStyle, hintCenterStyle;
    Texture2D texOverlay, texPanel, texButton, texButtonHover, texBarBg, texBarFill, texHud;

    int GoalRow => goal != null ? Mathf.Max(1, Mathf.RoundToInt(goal.position.z)) : 1;
    float Elapsed => state == State.Playing ? Time.time - startTime : finishTime;
    float Progress => student != null ? Mathf.Clamp01((float)student.MaxRow / GoalRow) : 0f;

    void Awake()
    {
        if (student == null) student = FindAnyObjectByType<StudentController>();
        if (goal == null)
        {
            var g = GameObject.FindWithTag("Goal");
            if (g != null) goal = g.transform;
        }
        bestTime = PlayerPrefs.GetFloat(BestKey, -1f);
        startTime = Time.time;
    }

    void Update()
    {
        if (student == null) return;

        if (state == State.Playing)
        {
            if (student.IsDead) End(State.Lost);
            else if (student.HasWon) End(State.Won);
        }

        if (Input.GetKeyDown(KeyCode.R)) Restart();

        bool endScreenVisible = state != State.Playing && Time.time - endedAt > endScreenDelay;
        if (endScreenVisible && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)))
            Restart();
    }

    void End(State s)
    {
        state = s;
        finishTime = Time.time - startTime;
        endedAt = Time.time;

        if (s == State.Won && (bestTime < 0f || finishTime < bestTime))
        {
            bestTime = finishTime;
            newRecord = true;
            PlayerPrefs.SetFloat(BestKey, bestTime);
            PlayerPrefs.Save();
        }
    }

    public void Restart()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.buildIndex >= 0)
        {
            SceneManager.LoadScene(scene.buildIndex);
            return;
        }
#if UNITY_EDITOR
        // cena fora da lista de build: recarrega direto pelo editor
        UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
            scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#endif
    }

    // =====================================================================
    //  INTERFACE
    // =====================================================================
    void OnGUI()
    {
        EnsureStyles();
        DrawHud();
        if (state != State.Playing && Time.time - endedAt > endScreenDelay)
            DrawEndScreen();
    }

    void DrawHud()
    {
        float s = styleScale;

        Rect box = new Rect(20 * s, 20 * s, 360 * s, 112 * s);
        GUI.DrawTexture(box, texHud);

        GUI.Label(new Rect(box.x + 16 * s, box.y + 8 * s, box.width - 32 * s, 36 * s),
                  $"Rumo ao Senac   {Mathf.RoundToInt(Progress * 100)}%", hudStyle);

        Rect bar = new Rect(box.x + 16 * s, box.y + 48 * s, box.width - 32 * s, 16 * s);
        GUI.DrawTexture(bar, texBarBg);
        GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * Progress, bar.height), texBarFill);

        GUI.Label(new Rect(box.x + 16 * s, box.y + 70 * s, box.width - 32 * s, 34 * s),
                  $"Tempo: {FormatTime(Elapsed)}", hudStyle);

        GUI.Label(new Rect(Screen.width - 380 * s, 20 * s, 360 * s, 30 * s),
                  "WASD / setas  •  R reinicia", hintStyle);
    }

    void DrawEndScreen()
    {
        float s = styleScale;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), texOverlay);

        float w = 640 * s, h = 380 * s;
        Rect panel = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
        GUI.DrawTexture(panel, texPanel);

        bool won = state == State.Won;
        titleStyle.normal.textColor = won ? new Color(0.35f, 0.9f, 0.45f) : new Color(1f, 0.35f, 0.3f);

        string title = won ? "CHEGOU NO SENAC!" : "ATROPELADO!";
        string text;
        if (won)
        {
            text = $"Presença garantida!\nTempo: {FormatTime(finishTime)}";
            if (newRecord) text += "\nNOVO RECORDE!";
            else if (bestTime > 0f) text += $"\nRecorde: {FormatTime(bestTime)}";
        }
        else
        {
            text = $"Faltou na aula...\nVocê fez {Mathf.RoundToInt(Progress * 100)}% do caminho.";
        }

        // título encolhe até caber numa linha só
        titleStyle.fontSize = Mathf.RoundToInt(58 * s);
        var titleContent = new GUIContent(title);
        while (titleStyle.fontSize > 10 && titleStyle.CalcSize(titleContent).x > w - 40 * s)
            titleStyle.fontSize--;

        GUI.Label(new Rect(panel.x, panel.y + 28 * s, w, 80 * s), titleContent, titleStyle);
        GUI.Label(new Rect(panel.x + 30 * s, panel.y + 110 * s, w - 60 * s, 130 * s), text, textStyle);

        Rect btn = new Rect(panel.x + (w - 340 * s) / 2f, panel.y + h - 120 * s, 340 * s, 72 * s);
        if (GUI.Button(btn, "JOGAR DE NOVO", buttonStyle)) Restart();

        GUI.Label(new Rect(panel.x, panel.y + h - 40 * s, w, 26 * s), "ou aperte Enter", hintCenterStyle);
    }

    void EnsureStyles()
    {
        float scale = Mathf.Max(0.3f, Screen.height / 1080f);
        if (stylesReady && Mathf.Approximately(scale, styleScale)) return;
        styleScale = scale;
        stylesReady = true;

        if (texOverlay == null)
        {
            texOverlay     = Tex(new Color(0f, 0f, 0f, 0.55f));
            texPanel       = Tex(new Color(0.08f, 0.12f, 0.20f, 0.95f));
            texButton      = Tex(new Color(0.96f, 0.50f, 0.10f));
            texButtonHover = Tex(new Color(1.00f, 0.62f, 0.25f));
            texBarBg       = Tex(new Color(1f, 1f, 1f, 0.25f));
            texBarFill     = Tex(new Color(0.96f, 0.50f, 0.10f));
            texHud         = Tex(new Color(0f, 0f, 0f, 0.45f));
        }

        int F(float size) => Mathf.RoundToInt(size * scale);

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = F(58), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
            wordWrap = false
        };

        textStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = F(28), alignment = TextAnchor.MiddleCenter, wordWrap = true
        };
        textStyle.normal.textColor = Color.white;

        hudStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = F(24), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft
        };
        hudStyle.normal.textColor = Color.white;

        hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = F(18), alignment = TextAnchor.UpperRight
        };
        hintStyle.normal.textColor = new Color(1f, 1f, 1f, 0.85f);

        hintCenterStyle = new GUIStyle(hintStyle) { alignment = TextAnchor.MiddleCenter };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = F(30), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter
        };
        buttonStyle.normal.background = texButton;
        buttonStyle.hover.background = texButtonHover;
        buttonStyle.active.background = texButtonHover;
        buttonStyle.normal.textColor = Color.white;
        buttonStyle.hover.textColor = Color.white;
        buttonStyle.active.textColor = Color.white;
    }

    static Texture2D Tex(Color c)
    {
        var t = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
        t.SetPixel(0, 0, c);
        t.Apply();
        return t;
    }

    static string FormatTime(float seconds)
    {
        int min = Mathf.FloorToInt(seconds / 60f);
        float sec = seconds - min * 60f;
        return $"{min:00}:{sec:00.0}";
    }
}
