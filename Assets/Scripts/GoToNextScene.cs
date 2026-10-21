using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GoToNextScene : MonoBehaviour
{
    [SerializeField] private string nextSceneName;
    [SerializeField] private float fadeDuration = 1f;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        Debug.Log($"Player enter warp! Loading next scene: {nextSceneName}");
        StartCoroutine(FadeOut());
    }

    IEnumerator FadeOut()
    {
        Image fadeImg = UIManager.Instance.fadeImg;
        fadeImg.gameObject.SetActive(true);
        Color color = fadeImg.color;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            color.a = Mathf.Lerp(0f, 1f, t / fadeDuration);
            fadeImg.color = color;
            yield return null;
        }
        UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
    }
}
