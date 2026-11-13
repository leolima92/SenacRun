#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;

// ============================================================================
//  SENAC GAME - Gerador de prefabs e da Fase 1 (Estação Jurubatuba -> Senac)
//  Fica em Assets/Editor/.  Menu: Tools > Senac Game > Gerar tudo
//  Pode rodar quantas vezes quiser: apaga e recria a fase.
// ============================================================================
public static class SenacLevelBuilder
{
    const string Root = "Assets/SenacGame";
    const string MatDir = Root + "/Materials";
    const string PrefabDir = Root + "/Prefabs";
    const float HalfWidth = 15f;
    const float PlayLimit = 11f;   // o aluno anda de -11 a 11 (carros nascem em ±12)
    const float LeftYaw = -90f;   // construção do lado esquerdo, fachada virada pra pista
    const float RightYaw = 90f;   // construção do lado direito

    static int obstacleLayer;
    static System.Random rng;

    static GameObject[] carPrefabs, busPrefabs, truckPrefabs, bikePrefabs;
    static GameObject taxiPrefab;

    static Material mAsfalto, mAsfaltoOnibus, mAsfaltoEscuro, mCalcada, mGuia, mPraca, mGrama, mCiclovia,
        mFaixaBranca, mFaixaAmarela, mVidro, mPneu, mFarol, mLanterna, mPele, mCabelo, mCamiseta, mCalca,
        mTenis, mMochila, mPreto, mTronco, mFolha, mTerra, mPoste, mLuz, mLixeira, mLaranja, mAzulPlaca,
        mBranco, mCinzaClaro, mChao, mBege, mTijolo, mConcreto, mVerdePlaca, mTapume, mParedeClara,
        mVermelho, mVerdePosto, mAmareloMercado, mAzulMercado, mVerdeGrade, mBau, mQuadroBike,
        mCamisetaCiclista, mCapacete, mLuzAmarela, mLuzVerde;

    // ------------------------------------------------------------------------
    [MenuItem("Tools/Senac Game/Gerar tudo (prefabs + fase 1)")]
    public static void BuildAll()
    {
        rng = new System.Random(2026); // sempre gera igual

        EnsureFolder("Assets", "SenacGame");
        EnsureFolder(Root, "Materials");
        EnsureFolder(Root, "Prefabs");

        EnsureTag("Vehicle");
        EnsureTag("Goal");
        obstacleLayer = EnsureLayer("Obstacle");

        CreateMaterials();

        GameObject student = BuildStudent();

        carPrefabs = new[]
        {
            BuildCar("Carro_Vermelho", C(0.85f, 0.15f, 0.15f)),
            BuildCar("Carro_Azul",     C(0.15f, 0.35f, 0.85f)),
            BuildCar("Carro_Branco",   C(0.92f, 0.92f, 0.92f)),
            BuildCar("Carro_Prata",    C(0.60f, 0.62f, 0.65f)),
            BuildCar("Carro_Preto",    C(0.12f, 0.12f, 0.14f)),
        };
        taxiPrefab = BuildCar("Taxi", C(0.95f, 0.95f, 0.95f), true);

        busPrefabs = new[]
        {
            BuildBus("Onibus_Verde", C(0.10f, 0.55f, 0.30f)),
            BuildBus("Onibus_Azul",  C(0.10f, 0.30f, 0.65f)),
        };

        truckPrefabs = new[]
        {
            BuildTruck("Caminhao_Vermelho", C(0.75f, 0.15f, 0.12f)),
            BuildTruck("Caminhao_Azul",     C(0.15f, 0.30f, 0.60f)),
        };

        bikePrefabs = new[] { BuildBike("Ciclista") };

        BuildLevel(student);

        AssetDatabase.SaveAssets();
        AddSceneToBuild();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("<b>Senac Game:</b> trajeto Jurubatuba -> Senac gerado! Dá Play.");
    }

    // ======================================================================
    //  LAYOUT DA FASE  —  EDITE AQUI!
    //  Cada item é uma faixa no eixo Z (o 1º é z=0, o 2º é z=1...).
    //  Obstáculos: 9 caracteres = x de -4 a 4   (deixe sempre 1 espaço livre!)
    //     T árvore  P poste  L lixeira  S semáforo  K carrinho de compras
    //     C carro estacionado  X táxi estacionado  J mureta de concreto  . livre
    //  Calcada/Grama(obstáculos, decorEsquerda, decorDireita)
    //  Rua(daEsquerda, velocidade, intervaloMin, intervaloMax, pesoCarro, pesoOnibus)
    //  Onibus(daEsquerda, vel)   Marginal(daEsquerda, vel, pesoCaminhao)
    //  Ciclovia(daEsquerda, vel)  Estacionamento(obstáculos)  Mureta(obstáculos)
    //  O ÚLTIMO item tem que ser Praca() (chegada).
    //  ATENÇÃO: os prédios (estação, posto, mercado...) ficam em BuildLandmarks();
    //  se mudar a ordem das faixas, ajuste os z de lá também.
    // ======================================================================
    static RowDef[] Fase1() => new[]
    {
        // --- Estação Jurubatuba (estação à esquerda, ponto de ônibus à direita) ---
        Calcada(".........", false, false),         //  0  saída da estação (largada)
        Calcada("......L..", false, false),         //  1
        Calcada("P.......T", false, false),         //  2

        // --- Av. Octalles Marcondes Ferreira ---
        Estacionamento("C..C...C."),                 //  3  carros estacionados
        Rua(true, 4.0f, 1.6f, 3.0f),                 //  4
        Rua(true, 5.0f, 1.4f, 2.8f),                 //  5
        Estacionamento("XX..X..XX"),                 //  6  ponto de táxi
        Calcada("...P.....", false, true),           //  7  tapume da obra à esquerda
        Grama("T..T...T.", false, true),             //  8
        Calcada(".........", false, true),           //  9

        // --- Esquina: lanchonete (esq.) e posto (dir.) ---
        Calcada("S.......L", false, false),          // 10
        Calcada(".........", false, false),          // 11
        Calcada("..L...S..", false, false),          // 12

        // --- Avenida de mão dupla ---
        Rua(false, 5.5f, 1.3f, 2.6f),                // 13
        Rua(false, 6.5f, 1.2f, 2.4f),                // 14
        Rua(true,  6.0f, 1.2f, 2.4f),                // 15
        Rua(true,  5.0f, 1.4f, 2.8f, 2f, 1f),        // 16  passa ônibus
        Calcada("T...P...T"),                        // 17
        Grama("TT.T..TT."),                          // 18
        Calcada(".........", false, true),           // 19  placa da Marginal

        // --- MARGINAL ---
        Marginal(true,  7.0f),                       // 20
        Marginal(true,  9.0f),                       // 21
        Marginal(true,  6.0f, 1f),                   // 22  caminhões
        Mureta("JJ..J.JJJ"),                         // 23  respiro no meio
        Marginal(false, 6.0f, 1f),                   // 24  caminhões
        Marginal(false, 9.0f),                       // 25
        Marginal(false, 7.5f),                       // 26
        Grama("T.T..T.T."),                          // 27
        Calcada(),                                   // 28

        // --- Ciclovia + rua do mercado ---
        Ciclovia(true,  4.5f),                       // 29
        Ciclovia(false, 4.5f),                       // 30
        Grama("..T...T.."),                          // 31
        Rua(true,  5.5f, 1.3f, 2.6f),                // 32
        Rua(false, 5.5f, 1.3f, 2.6f),                // 33

        // --- Mercado (à direita) com estacionamento ---
        Calcada("K...L...K", false, false),          // 34
        Estacionamento("C..C.C..C"),                 // 35
        Calcada("..K...K..", false, false),          // 36
        Estacionamento(".C..C..C."),                 // 37
        Calcada("K.......K", false, false),          // 38

        // --- Rua do Senac (estreita e arborizada) ---
        Rua(true,  3.5f, 2.0f, 3.5f),                // 39
        Rua(false, 3.5f, 2.0f, 3.5f),                // 40
        Calcada("T..T...T."),                        // 41

        // --- Avenida do Senac ---
        Estacionamento("C.C...C.C"),                 // 42
        Rua(true,  5.0f, 1.4f, 2.8f),                // 43
        Rua(false, 5.5f, 1.3f, 2.6f, 2f, 1f),        // 44
        Grama("T...T..T."),                          // 45
        Ciclovia(false, 4.0f),                       // 46
        Calcada("S.......S"),                        // 47
        Praca(),                                     // 48  CHEGADA
    };

    // Construções grandes nas laterais (posições presas ao layout acima).
    static void BuildLandmarks(Transform p, int goalZ)
    {
        BuildEstacao(p, V(-5.2f, 0, 0f), LeftYaw);          // faixas 0-2
        BuildPontoOnibus(p, V(5.4f, 0, 1f), RightYaw);
        BuildTapume(p, -5.6f, 8f, 3f);                       // faixas 7-9
        BuildLanchonete(p, V(-5.4f, 0, 11f), LeftYaw);       // faixas 10-12
        BuildPosto(p, V(5.4f, 0, 11f), RightYaw);
        BuildPlacaMarginal(p, 19f);                          // faixa 19
        BuildMercado(p, V(5.6f, 0, 36f), RightYaw);          // faixas 34-38
        foreach (float z in new[] { 35f, 37f })              // estacionamento à esquerda
            foreach (float x in new[] { -7f, -9.5f, -12f })
                ParkedCar(p, x, z, false);
        BuildSenac(p, goalZ + 2f);
    }

    // ======================================================================
    //  TIPOS DE FAIXA
    // ======================================================================
    enum RowType { Calcada, Grama, Rua, Onibus, Marginal, Ciclovia, Estacionamento, Mureta, Praca }

    class RowDef
    {
        public RowType type;
        public bool fromLeft;
        public float speed, minI, maxI;
        public float carW, taxiW, busW, truckW, bikeW;
        public string obstacles = ".........";
        public bool decorL = true, decorR = true;
    }

    static RowDef Calcada(string obs = ".........", bool decorL = true, bool decorR = true) =>
        new RowDef { type = RowType.Calcada, obstacles = obs, decorL = decorL, decorR = decorR };

    static RowDef Grama(string obs = ".........", bool decorL = true, bool decorR = true) =>
        new RowDef { type = RowType.Grama, obstacles = obs, decorL = decorL, decorR = decorR };

    static RowDef Estacionamento(string obs) =>
        new RowDef { type = RowType.Estacionamento, obstacles = obs, decorL = false, decorR = false };

    static RowDef Mureta(string obs) =>
        new RowDef { type = RowType.Mureta, obstacles = obs, decorL = false, decorR = false };

    static RowDef Rua(bool fromLeft, float speed, float minI, float maxI, float carW = 1f, float busW = 0f) =>
        new RowDef { type = RowType.Rua, fromLeft = fromLeft, speed = speed, minI = minI, maxI = maxI,
                     carW = carW, taxiW = 0.15f * carW, busW = busW };

    static RowDef Onibus(bool fromLeft, float speed) =>
        new RowDef { type = RowType.Onibus, fromLeft = fromLeft, speed = speed, minI = 2.5f, maxI = 4f,
                     carW = 1f, busW = 2f };

    static RowDef Marginal(bool fromLeft, float speed, float truckW = 0f) =>
        new RowDef { type = RowType.Marginal, fromLeft = fromLeft, speed = speed,
                     minI = truckW > 0 ? 1.6f : 1.0f, maxI = truckW > 0 ? 3.0f : 2.0f,
                     carW = 1f, truckW = truckW };

    static RowDef Ciclovia(bool fromLeft, float speed) =>
        new RowDef { type = RowType.Ciclovia, fromLeft = fromLeft, speed = speed, minI = 1.5f, maxI = 3.5f,
                     bikeW = 1f };

    static RowDef Praca() => new RowDef { type = RowType.Praca };

    static bool IsAsphalt(RowDef r) =>
        r.type is RowType.Rua or RowType.Onibus or RowType.Marginal or RowType.Estacionamento;

    static bool IsMoving(RowDef r) =>
        r.type is RowType.Rua or RowType.Onibus or RowType.Marginal or RowType.Ciclovia;

    static Material RowMaterial(RowType t) => t switch
    {
        RowType.Rua            => mAsfalto,
        RowType.Estacionamento => mAsfalto,
        RowType.Onibus         => mAsfaltoOnibus,
        RowType.Marginal       => mAsfaltoEscuro,
        RowType.Ciclovia       => mCiclovia,
        RowType.Grama          => mGrama,
        RowType.Mureta         => mConcreto,
        RowType.Praca          => mPraca,
        _                      => mCalcada,
    };

    // ======================================================================
    //  MONTAGEM DA FASE
    // ======================================================================
    static void BuildLevel(GameObject studentPrefab)
    {
        var old = GameObject.Find("Fase1_Senac");
        if (old != null) Object.DestroyImmediate(old);

        var rows = Fase1();
        int goalZ = rows.Length - 1;

        var level   = new GameObject("Fase1_Senac").transform;
        var ground  = Child(level, "Chao");
        var props   = Child(level, "Cenario");
        var traffic = Child(level, "Transito");

        Box(ground, V(0, -0.2f, goalZ / 2f), V(60f, 0.1f, goalZ + 40f), mChao);
        for (int z = -4; z < 0; z++) Tile(ground, z, mCalcada);

        for (int z = 0; z < rows.Length; z++)
        {
            var r = rows[z];
            Tile(ground, z, RowMaterial(r.type));

            if (IsMoving(r)) Spawner(traffic, z, r);

            if (r.type is RowType.Calcada or RowType.Grama or RowType.Estacionamento or RowType.Mureta)
                PlaceObstacles(props, z, r.obstacles);

            if (r.type is RowType.Calcada or RowType.Grama)
                Decorate(props, z, r.decorL, r.decorR);

            if (z + 1 < rows.Length) PaintEdge(ground, z + 0.5f, r, rows[z + 1]);
        }

        for (int z = goalZ + 1; z <= goalZ + 9; z++) Tile(ground, z, mPraca);

        BuildLandmarks(props, goalZ);
        BuildPlayBounds(ground, goalZ);

        var goal = new GameObject("Chegada_Senac");
        goal.tag = "Goal";
        goal.transform.SetParent(level, false);
        goal.transform.localPosition = V(0, 0.5f, goalZ);
        var gc = goal.AddComponent<BoxCollider>();
        gc.isTrigger = true;
        gc.size = V(12f, 1f, 0.8f);

        var st = (GameObject)PrefabUtility.InstantiatePrefab(studentPrefab);
        st.transform.SetParent(level, false);
        st.transform.localPosition = Vector3.zero;

        var gmGo = new GameObject("GameManager");
        gmGo.transform.SetParent(level, false);
        var gm = gmGo.AddComponent<GameManager>();
        var gso = new SerializedObject(gm);
        gso.FindProperty("student").objectReferenceValue = st.GetComponent<StudentController>();
        gso.FindProperty("goal").objectReferenceValue = goal.transform;
        gso.ApplyModifiedPropertiesWithoutUndo();

        SetupCamera(st.transform);
        SetupLight();
    }

    static void PaintEdge(Transform g, float edge, RowDef a, RowDef b)
    {
        bool asA = IsAsphalt(a), asB = IsAsphalt(b);

        if (asA && asB)
        {
            if (IsMoving(a) && IsMoving(b))
            {
                if (a.fromLeft == b.fromLeft) Dashed(g, edge, mFaixaBranca);
                else
                {
                    SolidLine(g, edge - 0.07f, mFaixaAmarela);
                    SolidLine(g, edge + 0.07f, mFaixaAmarela);
                }
            }
            else SolidLine(g, edge, mFaixaBranca);   // borda da faixa de estacionamento
        }
        else if (a.type == RowType.Ciclovia && b.type == RowType.Ciclovia) Dashed(g, edge, mFaixaBranca);
        else if (a.type == RowType.Ciclovia || b.type == RowType.Ciclovia) SolidLine(g, edge, mFaixaBranca);
        else if (asA != asB) Box(g, V(0, 0.03f, edge), V(HalfWidth * 2, 0.06f, 0.12f), mGuia); // meio-fio
    }

    static void PlaceObstacles(Transform p, int z, string obs)
    {
        if (string.IsNullOrEmpty(obs)) return;
        for (int i = 0; i < obs.Length && i < 9; i++)
        {
            float x = i - 4;
            switch (char.ToUpperInvariant(obs[i]))
            {
                case 'T': Tree(p, x, z); break;
                case 'P': Lamp(p, x, z); break;
                case 'L': Bin(p, x, z); break;
                case 'S': TrafficLight(p, x, z); break;
                case 'K': Cart(p, x, z); break;
                case 'C': ParkedCar(p, x, z, false); break;
                case 'X': ParkedCar(p, x, z, true); break;
                case 'J': Jersey(p, x, z); break;
            }
        }
    }

    static void Decorate(Transform p, int z, bool left, bool right)
    {
        if (left)  { Tree(p, -10f, z); Tree(p, -6.5f, z); if (z % 2 == 0) Lamp(p, -8f, z); }
        if (right) { Tree(p,  6.5f, z); Tree(p, 10f, z);  if (z % 2 == 0) Lamp(p,  8f, z); }
    }

    // ======================================================================
    //  MATERIAIS
    // ======================================================================
    static void CreateMaterials()
    {
        mAsfalto         = Mat("Asfalto",         C(0.22f, 0.22f, 0.24f));
        mAsfaltoOnibus   = Mat("AsfaltoOnibus",   C(0.48f, 0.20f, 0.20f));
        mAsfaltoEscuro   = Mat("AsfaltoMarginal", C(0.16f, 0.16f, 0.18f));
        mCalcada         = Mat("Calcada",         C(0.72f, 0.72f, 0.70f));
        mGuia            = Mat("Guia",            C(0.86f, 0.86f, 0.82f));
        mPraca           = Mat("Praca",           C(0.82f, 0.76f, 0.64f));
        mGrama           = Mat("Grama",           C(0.35f, 0.60f, 0.28f));
        mCiclovia        = Mat("Ciclovia",        C(0.72f, 0.25f, 0.22f));
        mFaixaBranca     = Mat("FaixaBranca",     C(0.95f, 0.95f, 0.95f));
        mFaixaAmarela    = Mat("FaixaAmarela",    C(0.95f, 0.78f, 0.10f));
        mVidro           = Mat("Vidro",           C(0.15f, 0.25f, 0.35f), 0.9f);
        mPneu            = Mat("Pneu",            C(0.08f, 0.08f, 0.08f));
        mFarol           = Mat("Farol",           C(1.00f, 0.95f, 0.70f), 0.5f, true);
        mLanterna        = Mat("Lanterna",        C(0.90f, 0.10f, 0.10f), 0.5f, true);
        mPele            = Mat("Pele",            C(0.87f, 0.68f, 0.55f));
        mCabelo          = Mat("Cabelo",          C(0.20f, 0.13f, 0.08f));
        mCamiseta        = Mat("Camiseta",        C(0.15f, 0.45f, 0.75f));
        mCalca           = Mat("Calca",           C(0.15f, 0.20f, 0.40f));
        mTenis           = Mat("Tenis",           C(0.95f, 0.95f, 0.95f));
        mMochila         = Mat("Mochila",         C(0.95f, 0.50f, 0.10f));
        mPreto           = Mat("Preto",           C(0.05f, 0.05f, 0.05f));
        mTronco          = Mat("Tronco",          C(0.40f, 0.26f, 0.15f));
        mFolha           = Mat("Folha",           C(0.25f, 0.60f, 0.25f));
        mTerra           = Mat("Terra",           C(0.35f, 0.25f, 0.18f));
        mPoste           = Mat("Poste",           C(0.30f, 0.32f, 0.35f), 0.4f);
        mLuz             = Mat("Luz",             C(1.00f, 0.90f, 0.60f), 0.5f, true);
        mLixeira         = Mat("Lixeira",         C(0.95f, 0.55f, 0.10f));
        mLaranja         = Mat("Laranja",         C(0.96f, 0.50f, 0.10f));
        mAzulPlaca       = Mat("AzulPlaca",       C(0.05f, 0.25f, 0.55f));
        mBranco          = Mat("Branco",          C(0.98f, 0.98f, 0.98f));
        mCinzaClaro      = Mat("CinzaClaro",      C(0.70f, 0.70f, 0.70f));
        mChao            = Mat("Chao",            C(0.40f, 0.42f, 0.38f));
        mBege            = Mat("Bege",            C(0.82f, 0.76f, 0.62f));
        mTijolo          = Mat("Tijolo",          C(0.66f, 0.30f, 0.22f));
        mConcreto        = Mat("Concreto",        C(0.62f, 0.62f, 0.60f));
        mVerdePlaca      = Mat("VerdePlaca",      C(0.00f, 0.50f, 0.35f));
        mTapume          = Mat("Tapume",          C(0.10f, 0.35f, 0.85f));
        mParedeClara     = Mat("ParedeClara",     C(0.92f, 0.90f, 0.86f));
        mVermelho        = Mat("Vermelho",        C(0.80f, 0.12f, 0.10f));
        mVerdePosto      = Mat("VerdePosto",      C(0.10f, 0.55f, 0.35f));
        mAmareloMercado  = Mat("AmareloMercado",  C(0.95f, 0.75f, 0.10f));
        mAzulMercado     = Mat("AzulMercado",     C(0.10f, 0.25f, 0.60f));
        mVerdeGrade      = Mat("VerdeGrade",      C(0.10f, 0.40f, 0.20f), 0.5f);
        mBau             = Mat("Bau",             C(0.90f, 0.90f, 0.88f));
        mQuadroBike      = Mat("QuadroBike",      C(0.20f, 0.60f, 0.90f), 0.6f);
        mCamisetaCiclista= Mat("CamisetaCiclista",C(0.90f, 0.30f, 0.40f));
        mCapacete        = Mat("Capacete",        C(0.95f, 0.85f, 0.10f), 0.6f);
        mLuzAmarela      = Mat("LuzAmarela",      C(1.00f, 0.80f, 0.10f), 0.5f, true);
        mLuzVerde        = Mat("LuzVerde",        C(0.10f, 0.90f, 0.30f), 0.5f, true);
    }

    // ======================================================================
    //  ALUNO
    // ======================================================================
    static GameObject BuildStudent()
    {
        var root = new GameObject("Aluno");
        var t = root.transform;

        var col = root.AddComponent<BoxCollider>();
        col.center = V(0, 0.7f, 0);
        col.size = V(0.6f, 1.4f, 0.6f);

        var rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        var ctrl = root.AddComponent<StudentController>();
        var so = new SerializedObject(ctrl);
        so.FindProperty("obstacleMask").intValue = 1 << obstacleLayer;
        so.FindProperty("minX").floatValue = -PlayLimit;
        so.FindProperty("maxX").floatValue = PlayLimit;
        so.ApplyModifiedPropertiesWithoutUndo();

        foreach (float s in new[] { -1f, 1f })
        {
            Box(t, V(0.11f * s, 0.05f, 0.03f),  V(0.20f, 0.10f, 0.28f), mTenis);
            Box(t, V(0.11f * s, 0.30f, 0f),     V(0.18f, 0.40f, 0.20f), mCalca);
            Box(t, V(0.32f * s, 0.77f, 0f),     V(0.14f, 0.42f, 0.18f), mCamiseta);
            Box(t, V(0.32f * s, 0.50f, 0f),     V(0.12f, 0.12f, 0.14f), mPele);
            Box(t, V(0.08f * s, 1.22f, 0.185f), V(0.06f, 0.07f, 0.02f), mPreto);
            Box(t, V(0.15f * s, 0.78f, 0.155f), V(0.06f, 0.44f, 0.02f), mPreto);
        }

        Box(t, V(0, 0.75f, 0),      V(0.50f, 0.50f, 0.30f), mCamiseta);
        Box(t, V(0, 1.20f, 0),      V(0.38f, 0.38f, 0.36f), mPele);
        Box(t, V(0, 1.42f, -0.02f), V(0.40f, 0.10f, 0.40f), mCabelo);
        Box(t, V(0, 1.25f, -0.17f), V(0.40f, 0.30f, 0.06f), mCabelo);
        Box(t, V(0, 0.78f, -0.23f), V(0.40f, 0.44f, 0.16f), mMochila);

        return SavePrefab(root, "Aluno");
    }

    // ======================================================================
    //  VEÍCULOS  (frente = +Z local)
    // ======================================================================
    static GameObject VehicleRoot(string name, Vector3 colCenter, Vector3 colSize)
    {
        var root = new GameObject(name);
        root.tag = "Vehicle";
        var col = root.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.center = colCenter;
        col.size = colSize;
        root.AddComponent<Vehicle>();
        return root;
    }

    static GameObject BuildCar(string name, Color paint, bool taxi = false)
    {
        var lataria = Mat("Lataria_" + name, paint, 0.6f);
        var root = VehicleRoot(name, V(0, 0.5f, 0), V(0.8f, 0.9f, 1.6f));
        var t = root.transform;

        Box(t, V(0, 0.35f, 0),     V(0.90f, 0.45f, 1.70f), lataria);
        Box(t, V(0, 0.74f, -0.1f), V(0.82f, 0.32f, 0.95f), mVidro);
        Box(t, V(0, 0.93f, -0.1f), V(0.86f, 0.08f, 0.99f), lataria);

        foreach (float x in new[] { -0.45f, 0.45f })
            foreach (float z in new[] { -0.55f, 0.55f })
                Cyl(t, V(x, 0.17f, z), V(0.34f, 0.06f, 0.34f), mPneu, V(0, 0, 90));

        foreach (float x in new[] { -0.3f, 0.3f })
        {
            Box(t, V(x, 0.42f,  0.86f), V(0.18f, 0.10f, 0.04f), mFarol);
            Box(t, V(x, 0.42f, -0.86f), V(0.18f, 0.10f, 0.04f), mLanterna);
        }

        if (taxi)
        {
            Box(t, V(0, 1.03f, -0.1f), V(0.40f, 0.12f, 0.22f), mBranco);   // luminoso
            Box(t, V(0, 1.03f, -0.1f), V(0.42f, 0.04f, 0.24f), mVermelho);
            Box(t, V(0, 0.36f, 0),     V(0.92f, 0.06f, 1.40f), mVermelho);  // faixa lateral
        }

        return SavePrefab(root, name);
    }

    static GameObject BuildBus(string name, Color paint)
    {
        var lataria = Mat("Lataria_" + name, paint, 0.5f);
        var root = VehicleRoot(name, V(0, 0.8f, 0), V(0.9f, 1.5f, 3.5f));
        var t = root.transform;

        Box(t, V(0, 0.85f, 0),       V(1.10f, 1.25f, 3.60f), lataria);
        Box(t, V(0, 0.45f, 0),       V(1.12f, 0.20f, 3.62f), mBranco);
        Box(t, V(0, 1.15f, -0.2f),   V(1.12f, 0.45f, 2.90f), mVidro);
        Box(t, V(0, 1.50f, 0),       V(1.00f, 0.08f, 3.40f), mCinzaClaro);
        Box(t, V(0, 1.00f, 1.81f),   V(1.00f, 0.60f, 0.04f), mVidro);
        Box(t, V(0, 1.39f, 1.815f),  V(0.80f, 0.16f, 0.04f), mLaranja);
        Box(t, V(0.56f, 0.75f, 1.2f),V(0.04f, 0.90f, 0.60f), mVidro);

        foreach (float x in new[] { -0.55f, 0.55f })
            foreach (float z in new[] { -1.2f, 1.2f })
                Cyl(t, V(x, 0.22f, z), V(0.44f, 0.07f, 0.44f), mPneu, V(0, 0, 90));

        foreach (float x in new[] { -0.38f, 0.38f })
        {
            Box(t, V(x, 0.50f,  1.81f), V(0.18f, 0.12f, 0.04f), mFarol);
            Box(t, V(x, 0.50f, -1.81f), V(0.18f, 0.12f, 0.04f), mLanterna);
        }

        return SavePrefab(root, name);
    }

    static GameObject BuildTruck(string name, Color cabColor)
    {
        var cab = Mat("Cabine_" + name, cabColor, 0.5f);
        var root = VehicleRoot(name, V(0, 0.95f, 0), V(0.9f, 1.8f, 4.0f));
        var t = root.transform;

        Box(t, V(0, 0.35f, 0),      V(1.00f, 0.20f, 4.20f), mPneu);     // chassi
        Box(t, V(0, 0.85f, 1.6f),   V(1.10f, 1.30f, 1.00f), cab);       // cabine
        Box(t, V(0, 1.15f, 2.11f),  V(0.95f, 0.50f, 0.04f), mVidro);    // para-brisa
        Box(t, V(0, 1.15f, 1.75f),  V(1.12f, 0.40f, 0.55f), mVidro);    // janelas
        Box(t, V(0, 1.15f, -0.55f), V(1.15f, 1.60f, 3.00f), mBau);      // baú
        Box(t, V(0, 0.90f, -0.55f), V(1.17f, 0.15f, 3.02f), cab);       // faixa no baú

        foreach (float x in new[] { -0.5f, 0.5f })
            foreach (float z in new[] { 1.5f, -0.9f, -1.6f })
                Cyl(t, V(x, 0.24f, z), V(0.48f, 0.07f, 0.48f), mPneu, V(0, 0, 90));

        foreach (float x in new[] { -0.38f, 0.38f })
        {
            Box(t, V(x, 0.55f,  2.11f), V(0.18f, 0.12f, 0.04f), mFarol);
            Box(t, V(x, 0.55f, -2.06f), V(0.18f, 0.12f, 0.04f), mLanterna);
        }

        return SavePrefab(root, name);
    }

    static GameObject BuildBike(string name)
    {
        var root = VehicleRoot(name, V(0, 0.6f, 0), V(0.4f, 1.2f, 0.9f));
        var t = root.transform;

        foreach (float z in new[] { -0.38f, 0.38f })
            Cyl(t, V(0, 0.3f, z), V(0.55f, 0.02f, 0.55f), mPneu, V(0, 0, 90));

        Box(t, V(0, 0.45f, 0),      V(0.05f, 0.05f, 0.75f), mQuadroBike);
        Box(t, V(0, 0.60f, -0.12f), V(0.05f, 0.30f, 0.05f), mQuadroBike);
        Box(t, V(0, 0.62f, 0.30f),  V(0.04f, 0.35f, 0.04f), mQuadroBike);
        Box(t, V(0, 0.80f, 0.30f),  V(0.40f, 0.04f, 0.04f), mPreto);
        Box(t, V(0, 0.76f, -0.12f), V(0.12f, 0.04f, 0.20f), mPreto);   // banco

        foreach (float s in new[] { -1f, 1f })
        {
            Box(t, V(0.10f * s, 0.60f, -0.05f), V(0.10f, 0.35f, 0.12f), mCalca);
            Box(t, V(0.17f * s, 0.95f, 0.15f),  V(0.08f, 0.08f, 0.35f), mCamisetaCiclista);
        }
        Box(t, V(0, 1.00f, -0.02f), V(0.36f, 0.45f, 0.22f), mCamisetaCiclista);
        Box(t, V(0, 1.38f, 0.02f),  V(0.26f, 0.26f, 0.26f), mPele);
        Box(t, V(0, 1.53f, 0.02f),  V(0.28f, 0.08f, 0.30f), mCapacete);

        return SavePrefab(root, name);
    }

    // ======================================================================
    //  OBSTÁCULOS (layer "Obstacle": bloqueiam o aluno)
    // ======================================================================
    static Transform Obstacle(Transform p, string name, float x, float z, Vector3 colSize)
    {
        var go = new GameObject(name);
        go.layer = obstacleLayer;
        go.transform.SetParent(p, false);
        go.transform.localPosition = V(x, 0, z);
        var c = go.AddComponent<BoxCollider>();
        c.size = colSize;
        c.center = V(0, colSize.y / 2f, 0);
        return go.transform;
    }

    static void Tree(Transform p, float x, float z)
    {
        var t = Obstacle(p, "Arvore", x, z, V(0.8f, 2f, 0.8f));
        Box(t, V(0, 0.05f, 0), V(0.8f, 0.1f, 0.8f), mTerra);
        Cyl(t, V(0, 0.6f, 0), V(0.18f, 0.6f, 0.18f), mTronco);
        Sph(t, V(0, 1.5f, 0), V(1.1f, 1.0f, 1.1f), mFolha);
        Sph(t, V(0.15f, 2.0f, -0.1f), V(0.7f, 0.7f, 0.7f), mFolha);
    }

    static void Lamp(Transform p, float x, float z)
    {
        var t = Obstacle(p, "Poste", x, z, V(0.6f, 2f, 0.6f));
        Cyl(t, V(0, 0.05f, 0), V(0.22f, 0.05f, 0.22f), mPoste);
        Cyl(t, V(0, 1.2f, 0), V(0.08f, 1.2f, 0.08f), mPoste);
        Box(t, V(0.25f, 2.35f, 0), V(0.5f, 0.06f, 0.06f), mPoste);
        Box(t, V(0.5f, 2.30f, 0), V(0.28f, 0.10f, 0.18f), mPoste);
        Box(t, V(0.5f, 2.24f, 0), V(0.22f, 0.03f, 0.14f), mLuz);
    }

    static void Bin(Transform p, float x, float z)
    {
        var t = Obstacle(p, "Lixeira", x, z, V(0.6f, 1f, 0.6f));
        Cyl(t, V(0, 0.4f, 0), V(0.4f, 0.4f, 0.4f), mLixeira);
        Cyl(t, V(0, 0.82f, 0), V(0.42f, 0.03f, 0.42f), mPreto);
    }

    static void TrafficLight(Transform p, float x, float z)
    {
        var t = Obstacle(p, "Semaforo", x, z, V(0.5f, 2f, 0.5f));
        Cyl(t, V(0, 1.3f, 0), V(0.1f, 1.3f, 0.1f), mPoste);
        Box(t, V(0, 2.4f, 0.05f), V(0.3f, 0.8f, 0.25f), mPreto);
        Sph(t, V(0, 2.65f, -0.08f), V(0.15f, 0.15f, 0.08f), mLanterna);
        Sph(t, V(0, 2.40f, -0.08f), V(0.15f, 0.15f, 0.08f), mLuzAmarela);
        Sph(t, V(0, 2.15f, -0.08f), V(0.15f, 0.15f, 0.08f), mLuzVerde);
    }

    static void Cart(Transform p, float x, float z)
    {
        var t = Obstacle(p, "Carrinho", x, z, V(0.6f, 1f, 0.7f));
        t.localEulerAngles = V(0, rng.Next(360), 0);
        Box(t, V(0, 0.65f, 0), V(0.45f, 0.35f, 0.6f), mCinzaClaro);
        Box(t, V(0, 0.95f, -0.32f), V(0.45f, 0.04f, 0.04f), mVermelho);
        foreach (float s in new[] { -1f, 1f })
            Box(t, V(0.2f * s, 0.3f, 0), V(0.03f, 0.5f, 0.55f), mPoste);
        foreach (float sx in new[] { -0.18f, 0.18f })
            foreach (float sz in new[] { -0.24f, 0.24f })
                Cyl(t, V(sx, 0.06f, sz), V(0.12f, 0.02f, 0.12f), mPreto, V(0, 0, 90));
    }

    static void Jersey(Transform p, float x, float z)
    {
        var t = Obstacle(p, "Mureta", x, z, V(0.9f, 0.8f, 0.6f));
        Box(t, V(0, 0.15f, 0), V(0.95f, 0.30f, 0.60f), mConcreto);
        Box(t, V(0, 0.55f, 0), V(0.95f, 0.50f, 0.30f), mConcreto);
        Box(t, V(0, 0.65f, 0), V(0.97f, 0.08f, 0.32f), mFaixaAmarela);
    }

    static void ParkedCar(Transform p, float x, float z, bool taxi)
    {
        var prefab = taxi ? taxiPrefab : carPrefabs[rng.Next(carPrefabs.Length)];
        var go = Object.Instantiate(prefab, p);
        go.name = taxi ? "Taxi_Estacionado" : "Carro_Estacionado";
        go.transform.localPosition = V(x, 0, z);
        go.transform.localRotation = Quaternion.Euler(0, rng.Next(2) == 0 ? 90f : -90f, 0);

        Object.DestroyImmediate(go.GetComponent<Vehicle>());   // parado não anda nem mata
        go.tag = "Untagged";
        go.layer = obstacleLayer;
        var col = go.GetComponent<BoxCollider>();
        col.isTrigger = false;
        col.center = V(0, 0.5f, 0);
        col.size = V(0.8f, 0.9f, 0.9f);                       // bloqueia só a casinha dele
    }

    // ======================================================================
    //  CONSTRUÇÕES  (modeladas com a fachada virada pra -Z local)
    // ======================================================================
    static Transform Landmark(Transform p, string name, Vector3 pos, float yaw)
    {
        var t = Child(p, name);
        t.localPosition = pos;
        t.localEulerAngles = V(0, yaw, 0);
        return t;
    }

    // Colisor invisível que impede o aluno de atravessar uma construção
    static void Solid(Transform t, Vector3 center, Vector3 size)
    {
        var go = new GameObject("Colisor");
        go.layer = obstacleLayer;
        go.transform.SetParent(t, false);
        var c = go.AddComponent<BoxCollider>();
        c.center = center;
        c.size = size;
    }

    static void BuildEstacao(Transform p, Vector3 pos, float yaw)
    {
        var t = Landmark(p, "Estacao_Jurubatuba", pos, yaw);
        Solid(t, V(0, 2.25f, 2.5f), V(5f, 4.5f, 5f));

        Box(t, V(0, 2.25f, 2.5f), V(5f, 4.5f, 5f), mBege);
        Box(t, V(0, 4.6f, 2.5f), V(5.2f, 0.2f, 5.2f), mConcreto);

        foreach (float x in new[] { -2.3f, -0.8f, 0.8f, 2.3f })
            Box(t, V(x, 2.25f, -0.05f), V(0.35f, 4.5f, 0.15f), mTijolo);   // pilares de tijolo

        foreach (float x in new[] { -1.55f, 0f, 1.55f })
            Box(t, V(x, 3.6f, -0.03f), V(1.0f, 0.8f, 0.06f), mVidro);

        Box(t, V(0, 1.1f, -0.03f), V(1.6f, 2.2f, 0.06f), mPreto);           // entrada

        // marquise de concreto + placa verde
        Box(t, V(0, 2.7f, -0.75f), V(5.4f, 0.18f, 1.5f), mConcreto);
        foreach (float x in new[] { -2.4f, 2.4f })
            Box(t, V(x, 1.3f, -1.4f), V(0.15f, 2.6f, 0.15f), mConcreto);
        Box(t, V(0, 3.05f, -1.42f), V(5.3f, 0.6f, 0.08f), mVerdePlaca);
        BuildWord(t, "JURUBATUBA", V(0, 3.05f, -1.48f), 0.085f, mBranco);

        // catracas
        foreach (float x in new[] { -0.5f, 0f, 0.5f })
            Box(t, V(x, 0.45f, -0.6f), V(0.12f, 0.9f, 0.5f), mPoste);
    }

    static void BuildPontoOnibus(Transform p, Vector3 pos, float yaw)
    {
        var t = Landmark(p, "Ponto_Onibus", pos, yaw);
        Box(t, V(0, 2.2f, 0.45f), V(2.4f, 0.08f, 1.0f), mVidro);
        Box(t, V(0, 1.2f, 0.92f), V(2.4f, 1.8f, 0.05f), mVidro);
        foreach (float x in new[] { -1.15f, 1.15f })
            Box(t, V(x, 1.1f, 0.9f), V(0.08f, 2.2f, 0.08f), mPoste);
        Box(t, V(0, 0.45f, 0.65f), V(2.0f, 0.08f, 0.4f), mTronco);
        Box(t, V(1.6f, 1.25f, 0f), V(0.06f, 2.5f, 0.06f), mPoste);
        Box(t, V(1.6f, 2.4f, -0.03f), V(0.4f, 0.4f, 0.04f), mAzulPlaca);
    }

    static void BuildTapume(Transform p, float x, float z, float length)
    {
        var t = Child(p, "Obra_Tapume");
        Solid(t, V(x, 1.1f, z), V(0.3f, 2.2f, length));
        Box(t, V(x, 1.1f, z), V(0.12f, 2.2f, length), mTapume);
        foreach (float px in new[] { x - 1.5f, x - 5.5f })
            foreach (float pz in new[] { z - 1f, z + 1f })
                Box(t, V(px, 1.6f, pz), V(0.35f, 3.2f, 0.35f), mConcreto);
        Box(t, V(x - 3.5f, 3.25f, z), V(5f, 0.2f, length - 0.4f), mConcreto);
    }

    static void BuildLanchonete(Transform p, Vector3 pos, float yaw)
    {
        var t = Landmark(p, "Lanchonete", pos, yaw);
        Solid(t, V(0, 1.6f, 2f), V(3f, 3.2f, 4f));
        Box(t, V(0, 1.6f, 2f), V(3f, 3.2f, 4f), mParedeClara);
        Box(t, V(0, 3.0f, 1.95f), V(3.1f, 0.5f, 4.1f), mVermelho);
        Box(t, V(0, 1.2f, -0.03f), V(2.2f, 1.5f, 0.06f), mVidro);
        Box(t, V(0, 2.2f, -0.4f), V(3.2f, 0.1f, 0.8f), mVermelho);
        BuildWord(t, "LANCHES", V(0, 3.0f, -0.14f), 0.07f, mBranco);
        foreach (float x in new[] { -0.9f, 0.9f })
        {
            Cyl(t, V(x, 0.72f, -0.6f), V(0.6f, 0.02f, 0.6f), mBranco);
            Cyl(t, V(x, 0.36f, -0.6f), V(0.06f, 0.36f, 0.06f), mPoste);
        }
    }

    static void BuildPosto(Transform p, Vector3 pos, float yaw)
    {
        var t = Landmark(p, "Posto", pos, yaw);
        Solid(t, V(0, 1.4f, 5.5f), V(3f, 2.8f, 2f));     // loja de conveniência

        Box(t, V(0, 3.2f, 2f), V(3f, 0.35f, 4.5f), mBranco);             // cobertura
        Box(t, V(0, 3.05f, 2f), V(3.05f, 0.12f, 4.55f), mVerdePosto);
        foreach (float x in new[] { -1.1f, 1.1f })
            foreach (float z in new[] { 0.5f, 3.5f })
                Box(t, V(x, 1.55f, z), V(0.2f, 3.1f, 0.2f), mBranco);

        foreach (float z in new[] { 1.2f, 2.8f })                          // bombas
        {
            Box(t, V(0, 0.6f, z), V(0.5f, 1.2f, 0.35f), mVerdePosto);
            Box(t, V(0, 0.95f, z - 0.18f), V(0.3f, 0.2f, 0.02f), mPreto);
        }

        Box(t, V(0, 1.4f, 5.5f), V(3f, 2.8f, 2f), mParedeClara);           // conveniência
        Box(t, V(0, 1.1f, 4.48f), V(2.2f, 1.4f, 0.05f), mVidro);

        Box(t, V(0, 1.8f, -0.8f), V(0.15f, 3.6f, 0.15f), mPoste);          // totem
        Box(t, V(0, 3.4f, -0.8f), V(2.5f, 0.6f, 0.15f), mVerdePosto);
        BuildWord(t, "POSTO", V(0, 3.4f, -0.91f), 0.08f, mBranco);
    }

    static void BuildPlacaMarginal(Transform p, float z)
    {
        var t = Child(p, "Placa_Marginal");
        Box(t, V(-11.6f, 2.0f, z), V(0.2f, 4f, 0.2f), mPoste);
        Box(t, V(-5.4f, 2.0f, z), V(0.2f, 4f, 0.2f), mPoste);
        Box(t, V(-8.5f, 3.6f, z - 0.15f), V(7.6f, 1.2f, 0.1f), mVerdePlaca);
        BuildWord(t, "MARGINAL", V(-8.5f, 3.6f, z - 0.24f), 0.15f, mBranco);
    }

    static void BuildMercado(Transform p, Vector3 pos, float yaw)
    {
        var t = Landmark(p, "Mercado", pos, yaw);
        Solid(t, V(0, 2.6f, 4f), V(5f, 5.2f, 8f));
        Box(t, V(0, 2.6f, 4f), V(5f, 5.2f, 8f), mAmareloMercado);
        Box(t, V(0, 5.25f, 4f), V(5.1f, 0.1f, 8.1f), mCinzaClaro);
        Box(t, V(0, 4.4f, -0.04f), V(5f, 0.9f, 0.08f), mAzulMercado);
        BuildWord(t, "MERCADO", V(0, 4.4f, -0.12f), 0.1f, mBranco);
        Box(t, V(0, 1.2f, -0.03f), V(2.4f, 2.2f, 0.06f), mVidro);
        Box(t, V(0, 2.5f, -0.5f), V(3f, 0.12f, 1.0f), mAzulMercado);
    }

    static void BuildSenac(Transform p, float z)
    {
        var s = Landmark(p, "Predio_Senac", V(0, 0, z), 0f);

        // muro baixo + gradil verde, com o portão aberto no meio
        foreach (float side in new[] { -1f, 1f })
        {
            Box(s, V(side * 5.35f, 0.25f, -1f), V(7.3f, 0.5f, 0.25f), mConcreto);
            Box(s, V(side * 5.35f, 1.75f, -1f), V(7.3f, 0.06f, 0.06f), mVerdeGrade);
            for (float x = 1.9f; x <= 9.0f; x += 0.25f)
                Box(s, V(side * x, 1.15f, -1f), V(0.04f, 1.3f, 0.04f), mVerdeGrade);
            Box(s, V(side * 1.75f, 1.0f, -1f), V(0.18f, 2.0f, 0.18f), mVerdeGrade);
        }

        // prédio de tijolo
        Box(s, V(0, 3f, 3.5f), V(18f, 6f, 6f), mTijolo);
        Box(s, V(0, 6.12f, 3.5f), V(18.2f, 0.25f, 6.2f), mConcreto);
        Box(s, V(0, 2.8f, 0.45f), V(18.1f, 0.25f, 0.12f), mConcreto);

        for (int i = 0; i < 12; i++)
        {
            float x = -8.25f + i * 1.5f;
            if (Mathf.Abs(x) > 2.6f) Box(s, V(x, 1.5f, 0.47f), V(1.1f, 1.1f, 0.08f), mVidro);
            if (Mathf.Abs(x) > 3.6f) Box(s, V(x, 4.0f, 0.47f), V(1.1f, 1.1f, 0.08f), mVidro);
        }

        // entrada
        Box(s, V(0, 1.1f, 0.44f), V(2.4f, 2.2f, 0.1f), mVidro);
        Box(s, V(0, 1.1f, 0.38f), V(0.05f, 2.2f, 0.04f), mPoste);
        Box(s, V(0, 2.5f, -0.1f), V(3.6f, 0.15f, 1.2f), mLaranja);
        Cyl(s, V(-1.5f, 1.25f, -0.55f), V(0.12f, 1.25f, 0.12f), mBranco);
        Cyl(s, V( 1.5f, 1.25f, -0.55f), V(0.12f, 1.25f, 0.12f), mBranco);

        // placa
        Box(s, V(0, 4.6f, 0.42f), V(6.8f, 1.4f, 0.12f), mAzulPlaca);
        BuildWord(s, "SENAC", V(0, 4.6f, 0.33f), 0.22f, mBranco);
    }

    // ======================================================================
    //  LETREIROS (fonte de bloquinhos 5x5)
    // ======================================================================
    static readonly Dictionary<char, string[]> PixelFont = new Dictionary<char, string[]>
    {
        { 'A', new[] { ".###.", "#...#", "#####", "#...#", "#...#" } },
        { 'B', new[] { "####.", "#...#", "####.", "#...#", "####." } },
        { 'C', new[] { ".####", "#....", "#....", "#....", ".####" } },
        { 'D', new[] { "####.", "#...#", "#...#", "#...#", "####." } },
        { 'E', new[] { "#####", "#....", "####.", "#....", "#####" } },
        { 'F', new[] { "#####", "#....", "####.", "#....", "#...." } },
        { 'G', new[] { ".####", "#....", "#..##", "#...#", ".###." } },
        { 'H', new[] { "#...#", "#...#", "#####", "#...#", "#...#" } },
        { 'I', new[] { "#####", "..#..", "..#..", "..#..", "#####" } },
        { 'J', new[] { "..###", "...#.", "...#.", "#..#.", ".##.." } },
        { 'L', new[] { "#....", "#....", "#....", "#....", "#####" } },
        { 'M', new[] { "#...#", "##.##", "#.#.#", "#...#", "#...#" } },
        { 'N', new[] { "#...#", "##..#", "#.#.#", "#..##", "#...#" } },
        { 'O', new[] { ".###.", "#...#", "#...#", "#...#", ".###." } },
        { 'P', new[] { "####.", "#...#", "####.", "#....", "#...." } },
        { 'R', new[] { "####.", "#...#", "####.", "#..#.", "#...#" } },
        { 'S', new[] { "#####", "#....", "#####", "....#", "#####" } },
        { 'T', new[] { "#####", "..#..", "..#..", "..#..", "..#.." } },
        { 'U', new[] { "#...#", "#...#", "#...#", "#...#", ".###." } },
        { 'V', new[] { "#...#", "#...#", "#...#", ".#.#.", "..#.." } },
    };

    static void BuildWord(Transform p, string word, Vector3 center, float px, Material m)
    {
        var w = Child(p, "Letreiro_" + word);
        float letterW = 5 * px, gap = px;
        float total = word.Length * letterW + (word.Length - 1) * gap;
        float startX = center.x - total / 2f;

        for (int i = 0; i < word.Length; i++)
        {
            if (!PixelFont.TryGetValue(char.ToUpperInvariant(word[i]), out var rows)) continue;
            for (int r = 0; r < 5; r++)
                for (int c = 0; c < 5; c++)
                {
                    if (rows[r][c] != '#') continue;
                    float x = startX + i * (letterW + gap) + c * px + px / 2f;
                    float y = center.y + (2 - r) * px;
                    Box(w, V(x, y, center.z), V(px * 0.95f, px * 0.95f, 0.06f), m);
                }
        }
    }

    // ======================================================================
    //  SPAWNERS
    // ======================================================================
    static void Spawner(Transform p, int row, RowDef r)
    {
        var go = new GameObject($"Spawner_Faixa{row}_{r.type}");
        go.transform.SetParent(p, false);
        go.transform.localPosition = V(0, 0, row);

        var options = new List<(GameObject prefab, float w)>();
        AddOptions(options, carPrefabs, r.carW);
        if (r.taxiW > 0f) options.Add((taxiPrefab, r.taxiW));
        AddOptions(options, busPrefabs, r.busW);
        AddOptions(options, truckPrefabs, r.truckW);
        AddOptions(options, bikePrefabs, r.bikeW);

        var sp = go.AddComponent<VehicleSpawner>();
        var so = new SerializedObject(sp);
        so.FindProperty("speed").floatValue = r.speed;
        so.FindProperty("minInterval").floatValue = r.minI;
        so.FindProperty("maxInterval").floatValue = r.maxI;
        so.FindProperty("fromLeft").boolValue = r.fromLeft;
        so.FindProperty("spawnX").floatValue = 12f;

        var arr = so.FindProperty("vehicles");
        arr.arraySize = options.Count;
        for (int i = 0; i < options.Count; i++)
        {
            var el = arr.GetArrayElementAtIndex(i);
            el.FindPropertyRelative("prefab").objectReferenceValue = options[i].prefab;
            el.FindPropertyRelative("weight").floatValue = options[i].w;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void AddOptions(List<(GameObject, float)> list, GameObject[] arr, float weight)
    {
        if (weight <= 0f || arr == null || arr.Length == 0) return;
        foreach (var a in arr) list.Add((a, weight / arr.Length));
    }

    // ======================================================================
    //  CÂMERA, LUZ, BUILD
    // ======================================================================
    static void SetupCamera(Transform target)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            cam = go.AddComponent<Camera>();
        }

        var follow = cam.GetComponent<CameraFollow>();
        if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();

        var so = new SerializedObject(follow);
        so.FindProperty("target").objectReferenceValue = target;
        so.FindProperty("offset").vector3Value = V(0f, 10f, -6f);   // câmera centralizada
        so.ApplyModifiedPropertiesWithoutUndo();

        Vector3 offset = so.FindProperty("offset").vector3Value;
        cam.transform.position = target.position + offset;
        cam.transform.rotation = Quaternion.LookRotation(-offset);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = C(0.55f, 0.75f, 0.95f);
    }

    static void SetupLight()
    {
        if (GameObject.Find("Directional Light") != null) return;
        var l = new GameObject("Directional Light").AddComponent<Light>();
        l.type = LightType.Directional;
        l.shadows = LightShadows.Soft;
        l.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    static void AddSceneToBuild()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(scene.path)) return;
        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (list.Exists(s => s.path == scene.path)) return;
        list.Add(new EditorBuildSettingsScene(scene.path, true));
        EditorBuildSettings.scenes = list.ToArray();
    }

    // ======================================================================
    //  HELPERS
    // ======================================================================
    static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
    static Color C(float r, float g, float b) => new Color(r, g, b);

    static Transform Child(Transform parent, string name)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        return t;
    }

    static GameObject Part(Transform parent, PrimitiveType type, Vector3 pos, Vector3 scale,
                           Material mat, Vector3? euler = null)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = type.ToString();
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        if (euler.HasValue) go.transform.localEulerAngles = euler.Value;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    static GameObject Box(Transform p, Vector3 pos, Vector3 scale, Material m) =>
        Part(p, PrimitiveType.Cube, pos, scale, m);
    static GameObject Cyl(Transform p, Vector3 pos, Vector3 scale, Material m, Vector3? euler = null) =>
        Part(p, PrimitiveType.Cylinder, pos, scale, m, euler);
    static GameObject Sph(Transform p, Vector3 pos, Vector3 scale, Material m) =>
        Part(p, PrimitiveType.Sphere, pos, scale, m);

    static void Tile(Transform p, int z, Material m) =>
        Box(p, V(0, -0.05f, z), V(HalfWidth * 2, 0.1f, 1f), m);

    static void Dashed(Transform p, float z, Material m)
    {
        for (float x = -14f; x <= 14f; x += 2f)
            Box(p, V(x, 0.01f, z), V(1f, 0.02f, 0.08f), m);
    }

    // Faixa zebrada amarela/preta marcando o limite lateral da área jogável (x = -4..4)
    static void BuildPlayBounds(Transform g, int goalZ)
    {
        var b = Child(g, "Limite_Area_Jogavel");
        foreach (float x in new[] { -PlayLimit - 0.55f, PlayLimit + 0.55f })
            for (int z = -4; z <= goalZ; z++)
            {
                Box(b, V(x, 0.025f, z - 0.25f), V(0.12f, 0.03f, 0.5f), mFaixaAmarela);
                Box(b, V(x, 0.025f, z + 0.25f), V(0.12f, 0.03f, 0.5f), mPreto);
            }
    }

    static void SolidLine(Transform p, float z, Material m) =>
        Box(p, V(0, 0.01f, z), V(HalfWidth * 2, 0.02f, 0.06f), m);

    static Shader LitShader()
    {
        if (GraphicsSettings.currentRenderPipeline != null)
        {
            var s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("HDRP/Lit");
            if (s != null) return s;
        }
        return Shader.Find("Standard");
    }

    static Material Mat(string name, Color c, float smooth = 0.15f, bool emissive = false)
    {
        string path = $"{MatDir}/{name}.mat";
        var shader = LitShader();
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, path);
        }
        else if (m.shader != shader) m.shader = shader;

        m.color = c;
        if (m.HasProperty("_BaseColor"))  m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
        if (emissive && m.HasProperty("_EmissionColor"))
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * 1.5f);
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    static GameObject SavePrefab(GameObject go, string name)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabDir}/{name}.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder($"{parent}/{name}"))
            AssetDatabase.CreateFolder(parent, name);
    }

    static SerializedObject TagManager() =>
        new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

    static void EnsureTag(string tag)
    {
        var tm = TagManager();
        var tags = tm.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
            if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;
        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        tm.ApplyModifiedPropertiesWithoutUndo();
    }

    static int EnsureLayer(string name)
    {
        int existing = LayerMask.NameToLayer(name);
        if (existing != -1) return existing;

        var tm = TagManager();
        var layers = tm.FindProperty("layers");
        for (int i = 8; i < layers.arraySize; i++)
        {
            var sp = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(sp.stringValue))
            {
                sp.stringValue = name;
                tm.ApplyModifiedPropertiesWithoutUndo();
                return i;
            }
        }
        Debug.LogError("Não há layers livres para criar 'Obstacle'.");
        return 0;
    }
}
#endif
