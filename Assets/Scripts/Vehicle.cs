using UnityEngine;

// Serve pra carro E ônibus: o que muda é o prefab (tamanho, cor).
// Requisitos no prefab: Collider com Is Trigger marcado + Tag "Vehicle".
public class Vehicle : MonoBehaviour
{
    [HideInInspector] public float speed = 5f;
    [HideInInspector] public float limitX = 15f;

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
        if (Mathf.Abs(transform.position.x) > limitX) Destroy(gameObject);
    }
}
