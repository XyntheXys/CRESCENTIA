using UnityEngine;

public interface IInteractable
{
    public bool IsInteractable { get; set; }
    public void Interact();
}

public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    [SerializeField] private bool isInteractable = true;
    public bool IsInteractable
    {
        get => isInteractable;
        set => isInteractable = value;
    }

    public abstract void Interact();

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        PlayerController p = PlayerController.Instance;
        if (p != null)
        {
            p.SetInteractable(this);
            Debug.Log($"Set {gameObject.name} as interactable");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        PlayerController p = PlayerController.Instance;
        if (p != null)
        {
            p.SetInteractable(null);
            Debug.Log($"Removed {gameObject.name} as interactable");
        }
    }
}
