using System.Collections;
using UnityEngine;

// Aluno que anda em grid (1 unidade por pulo), com passinho em arco.
// Requisitos no objeto: Collider + Rigidbody (marcar Is Kinematic).
public class StudentController : MonoBehaviour
{
    [Header("Movimento")]
    [SerializeField] float hopDuration = 0.12f;
    [SerializeField] float hopHeight = 0.4f;

    [Header("Limites e obstáculos")]
    [SerializeField] LayerMask obstacleMask;   // layer de árvores, postes, bancos
    [SerializeField] float minX = -4f;
    [SerializeField] float maxX = 4f;

    bool isHopping;
    public bool IsDead { get; private set; }
    public bool HasWon { get; private set; }
    public int MaxRow { get; private set; }

    void Update()
    {
        if (isHopping || IsDead || HasWon) return;

        Vector3 dir = Vector3.zero;
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) dir = Vector3.forward;
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) dir = Vector3.back;
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) dir = Vector3.left;
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) dir = Vector3.right;

        if (dir != Vector3.zero) TryHop(dir);
    }

    void TryHop(Vector3 dir)
    {
        transform.rotation = Quaternion.LookRotation(dir);

        Vector3 target = transform.position + dir;
        target.x = Mathf.Round(target.x);
        target.y = 0f;
        target.z = Mathf.Round(target.z);

        if (target.x < minX || target.x > maxX)
        {
            Debug.Log($"[Aluno] Bloqueado: limite lateral da pista (x={target.x}, limite {minX}..{maxX})");
            return;
        }
        if (target.z < 0) return; // não volta pra trás do ponto de partida

        var hits = Physics.OverlapBox(target + Vector3.up * 0.5f, Vector3.one * 0.4f,
                                      Quaternion.identity, obstacleMask);
        if (hits.Length > 0)
        {
            var h = hits[0];
            Debug.Log($"[Aluno] Bloqueado em ({target.x}, {target.z}) por '{h.name}' " +
                      $"(pai: '{(h.transform.parent ? h.transform.parent.name : "-")}', " +
                      $"layer: {LayerMask.LayerToName(h.gameObject.layer)})", h);
            return;
        }

        StartCoroutine(Hop(target));
    }

    IEnumerator Hop(Vector3 target)
    {
        isHopping = true;
        Vector3 start = transform.position;
        float t = 0f;

        while (t < 1f)
        {
            t = Mathf.Clamp01(t + Time.deltaTime / hopDuration);
            float arc = Mathf.Sin(t * Mathf.PI) * hopHeight;
            transform.position = Vector3.Lerp(start, target, t) + Vector3.up * arc;
            yield return null;
        }

        transform.position = target;
        isHopping = false;

        int row = Mathf.RoundToInt(target.z);
        if (row > MaxRow) MaxRow = row;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Vehicle")) Die();
        else if (other.CompareTag("Goal")) Win();
    }

    public void Die()
    {
        if (IsDead || HasWon) return;
        IsDead = true;
        StopAllCoroutines();
        transform.localScale = new Vector3(1.3f, 0.1f, 1.3f); // atropelado :(
        Debug.Log("Perdeu a aula! Atropelado.");
    }

    void Win()
    {
        if (IsDead || HasWon) return;
        HasWon = true;
        Debug.Log("Chegou no Senac! Presença garantida.");
    }
}
