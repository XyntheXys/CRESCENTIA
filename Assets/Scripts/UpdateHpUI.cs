using UnityEngine;
using UnityEngine.UI;

public class UpdateHpUI : MonoBehaviour
{
    private PlayerController player => PlayerController.Instance;

    [SerializeField] private Slider realHpBar;
    [SerializeField] private Slider rallyHpBar;

    private bool isSubscribed = false;

    void Start()
    {
        if (player == null) return;
        TrySubscribe();
    }

    private void OnEnable() { TrySubscribe(); }
    private void OnDisable() { Unsubscribe(); }

    private void TrySubscribe()
    {
        if (isSubscribed) return;

        var player = PlayerController.Instance;
        if (player != null)
        {
            player.OnHealthChanged += UpdateBars;
            isSubscribed = true;

            UpdateBars(player.RealHp, player.RallyHp, player.MaxHp);
        }
    }

    private void Unsubscribe()
    {
        if (!isSubscribed) return;

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.OnHealthChanged -= UpdateBars;
        }
        isSubscribed = false;
    }

    void UpdateBars(float currentHealth, float currentRallyHealth, float maxHealth)
    {
        //Debug.Log($"Update hp bar: real {currentHealth} rally {currentRallyHealth} max {maxHealth}");
        realHpBar.value = currentHealth / maxHealth;
        rallyHpBar.value = currentRallyHealth / maxHealth;
    }
}
