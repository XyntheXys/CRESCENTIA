using UnityEngine;

public interface IInteractable
{
    public bool IsInteractable { get; set; }
    public void Interact();
}

public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    [SerializeField] private bool isInteractable = true;
    [SerializeField] private bool printDebugLogs = true;
    public bool IsInteractable
    {
        get => isInteractable;
        set => isInteractable = value;
    }

    public abstract void Interact();

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isInteractable || !collision.CompareTag("Player")) return;
        PlayerController p = PlayerController.Instance;
        if (p != null)
        {
            p.SetInteractable(this);
            if (printDebugLogs) Debug.Log($"Set {gameObject.name} as interactable");
        }
    }

    protected virtual void OnTriggerExit2D(Collider2D collision)
    {
        if (!isInteractable || !collision.CompareTag("Player")) return;
        PlayerController p = PlayerController.Instance;
        if (p != null)
        {
            p.SetInteractable(null);
            if (printDebugLogs) Debug.Log($"Removed {gameObject.name} as interactable");
        }
    }
}
