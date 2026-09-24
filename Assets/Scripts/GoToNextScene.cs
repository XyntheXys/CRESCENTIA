using UnityEngine;

public class GoToNextScene : MonoBehaviour
{
    [SerializeField] private string nextSceneName;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        Debug.Log($"Player enter warp! Loading next scene: {nextSceneName}");
        UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
    }
}
