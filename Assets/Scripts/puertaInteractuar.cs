using UnityEngine;
using System.Collections;

public class Door : MonoBehaviour, IInteractable
{
    [Header("Configuración de Texto")]
    [SerializeField] private string promptMessageOpen = "Presiona E para abrir";
    [SerializeField] private string promptMessageClose = "Presiona E para cerrar";

    [Header("Configuración de Movimiento")]
    [Tooltip("Distancia que se moverá hacia la izquierda.")]
    [SerializeField] private float moveDistance = 1.75f;
    [Tooltip("Velocidad a la que se mueve la puerta.")]
    [SerializeField] private float moveSpeed = 5f;

    public string PromptMessage => isOpen ? promptMessageClose : promptMessageOpen;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private bool isOpen = false;
    private Coroutine moveCoroutine;

    private void Start()
    {
        // Guardamos la posición local inicial
        startPosition = transform.localPosition;
    }

    public void Interact()
    {
        isOpen = !isOpen;

        if (isOpen)
        {
            // Calculamos el desplazamiento hacia la izquierda (-X) respetando la rotación de la puerta
            Vector3 localOffset = Vector3.left * moveDistance;
            targetPosition = startPosition + (transform.localRotation * localOffset);
        }
        else
        {
            targetPosition = startPosition;
        }

        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }

        moveCoroutine = StartCoroutine(MoveDoorRoutine(targetPosition));
    }

    private IEnumerator MoveDoorRoutine(Vector3 destination)
    {
        while (Vector3.Distance(transform.localPosition, destination) > 0.001f)
        {
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, destination, moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.localPosition = destination;
    }
}
