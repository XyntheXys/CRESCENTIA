using UnityEngine;
using UnityEngine.UI;

public class UpdateHpUI : MonoBehaviour
{
    private PlayerController player => PlayerController.Instance;

    [SerializeField] private Slider realHpBar;
    [SerializeField] private Slider rallyHpBar;

    void Start()
    {
        if (player == null) return;
        UpdateBars(player.Health, player.RallyHealth, player.MaxHealth);
    }

    private void OnEnable() { if (player) player.OnHealthChanged += UpdateBars; }
    private void OnDisable() { if (player) player.OnHealthChanged -= UpdateBars; }

    void UpdateBars(float currentHealth, float currentRallyHealth, float maxHealth)
    {
        realHpBar.value = currentHealth / maxHealth;
        rallyHpBar.value = currentRallyHealth / maxHealth;
    }
}
