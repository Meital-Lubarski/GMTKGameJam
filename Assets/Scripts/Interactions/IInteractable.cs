public interface IInteractable
{
    /// <summary>
    /// Whether interacting right now would actually do something.
    /// The prompt on screen is driven by this, so what the player is told he
    /// can do and what pressing the key really does stay the same thing.
    /// </summary>
    bool CanInteract { get; }

    void Interact();
}
