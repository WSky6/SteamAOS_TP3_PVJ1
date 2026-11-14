using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractuar : MonoBehaviour
{
    [Header("Configuración del Raycast")]
    [Tooltip("Cámara principal desde donde se lanza la vista en primera persona.")]
    public Camera playerCamera;
    [Tooltip("Distancia máxima a la que el jugador puede interactuar.")]
    public float interactionDistance = 3f;
    [Tooltip("Capa (Layer) para optimizar el Raycast y que solo detecte interactuables.")]
    public LayerMask interactableLayer;

    [Header("Input System")]
    [Tooltip("Referencia a la acción de interacción (ej. tecla E).")]
    public InputActionReference interactAction;

    // Referencia al objeto interactuable actual que estamos mirando
    private IInteractable currentInteractable;

    private void OnEnable()
    {
        if (interactAction != null)
        {
            interactAction.action.Enable();
            interactAction.action.started += OnInteractPerformed;
        }
    }

    private void OnDisable()
    {
        if (interactAction != null)
        {
            interactAction.action.started -= OnInteractPerformed;
            interactAction.action.Disable();
        }
    }

    private void Update()
    {
        CheckForInteractable();
    }

    void CheckForInteractable()
    {
        if (playerCamera == null) return;

        // Lanzamos un rayo desde el centro de la cámara hacia adelante
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        // Lanzamos el raycast
        if (Physics.Raycast(ray, out hit, interactionDistance, interactableLayer))
        {
            // Intentamos obtener la interfaz del objeto impactado
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                // Si es un objeto interactuable nuevo
                if (currentInteractable != interactable)
                {
                    currentInteractable = interactable;
                    Debug.Log("Mirando: " + currentInteractable.PromptMessage);
                    // [ AQUÍ PUEDES ACTIVAR TU UI Y MOSTRAR currentInteractable.PromptMessage ]
                }
                return;
            }
        }

        // Si no estamos mirando nada o dejamos de mirar el objeto anterior
        if (currentInteractable != null)
        {
            Debug.Log("Dejó de mirar el objeto.");
            // [ AQUÍ PUEDES OCULTAR TU UI DE INTERACCIÓN ]
            currentInteractable = null;
        }
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        // Si presionamos la tecla y estamos mirando un objeto válido, interactuamos
        if (currentInteractable != null)
        {
            Debug.Log("");
            currentInteractable.Interact();
        }
    }
}
