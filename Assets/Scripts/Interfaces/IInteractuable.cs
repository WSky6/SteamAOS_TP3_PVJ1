public interface IInteractable
{
    // Propiedad para obtener el texto que se mostrará en pantalla (ej. "Presiona E para abrir")
    string PromptMessage { get; }

    // Método que se ejecutará cuando el jugador interactúe
    void Interact();
}
