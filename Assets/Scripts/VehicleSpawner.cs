using System.Collections;
using UnityEngine;

// Coloque num objeto vazio no meio de uma faixa de rua (o Z dele define a faixa).
// Cada faixa tem UMA velocidade, assim um carro nunca atravessa o ônibus da frente.
public class VehicleSpawner : MonoBehaviour
{
    [System.Serializable]
    public class VehicleOption
    {
        public GameObject prefab;
        [Min(0f)] public float weight = 1f;   // chance relativa (ex.: carro 3, ônibus 1)
    }

    [SerializeField] VehicleOption[] vehicles;
    [SerializeField] float speed = 5f;
    [SerializeField] float minInterval = 1.5f;
    [SerializeField] float maxInterval = 3f;
    [SerializeField] bool fromLeft = true;
    [SerializeField] float spawnX = 12f;

    IEnumerator Start()
    {
        // começa em tempo aleatório pra faixas não ficarem sincronizadas
        yield return new WaitForSeconds(Random.Range(0f, maxInterval));
        while (true)
        {
            Spawn();
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
        }
    }

    void Spawn()
    {
        GameObject prefab = PickVehicle();
        if (prefab == null) return;

        float x = fromLeft ? -spawnX : spawnX;
        Vector3 pos = new Vector3(x, 0f, transform.position.z);
        Quaternion rot = Quaternion.LookRotation(fromLeft ? Vector3.right : Vector3.left);

        GameObject obj = Instantiate(prefab, pos, rot);
        Vehicle v = obj.GetComponent<Vehicle>();
        v.speed = speed;
        v.limitX = spawnX + 3f; // folga pro ônibus, que é comprido
    }

    GameObject PickVehicle()
    {
        float total = 0f;
        foreach (var o in vehicles) total += o.weight;
        if (total <= 0f) return null;

        float r = Random.Range(0f, total);
        foreach (var o in vehicles)
        {
            if (r < o.weight) return o.prefab;
            r -= o.weight;
        }
        return vehicles[vehicles.Length - 1].prefab;
    }
}
