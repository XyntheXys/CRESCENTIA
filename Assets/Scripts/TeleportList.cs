using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TeleportList : MonoBehaviour
{
    [SerializeField] private TMPro.TMP_Dropdown dropdown;
    [SerializeField] private GameObject teleportParent;
    public Image fadeImg;

    private List<Transform> tpList = new List<Transform>();
    private Transform selectedPoint = null;

    public static TeleportList Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: Persist across scenes
        }
    }

    private void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    private void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Debug.Log("Dropdown options count: " + dropdown.options.Count);
        LoadTeleportPoints();
    }

    void LoadTeleportPoints()
    {
        teleportParent = GameObject.FindGameObjectWithTag("TeleportParent");
        if (teleportParent == null) return;

        // Clear existing options
        dropdown.ClearOptions();
        tpList.Clear();

        // Add new options based on teleportPoints
        foreach (Transform child in teleportParent.transform)
        {
            dropdown.options.Add(new TMPro.TMP_Dropdown.OptionData(child.name));
            tpList.Add(child);
        }

        // Set the first point as the selected point by default
        if (tpList.Count > 0)
        {
            selectedPoint = tpList[0];
            dropdown.value = 0; // Set the dropdown to the first option
        }

        // Refresh the dropdown to show the updated options
        dropdown.RefreshShownValue();
    }

    public void OnDropdownValueChanged(int index)
    {
        if (index < 0 || index >= tpList.Count) return;

        selectedPoint = tpList[index];
        Debug.Log($"Selected Point: {selectedPoint.name} | Position: {selectedPoint.position}");
    }

    public void Teleport()
    {
        Debug.Log($"Teleporting to: {selectedPoint?.name} | Position: {selectedPoint?.position}");
        if (selectedPoint == null) return;
        PlayerController.Instance.transform.position = selectedPoint.position;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        //Debug.Log($"Loaded Scene: {scene.name} (Mode: {mode})");
        fadeImg.gameObject.SetActive(false); 
        LoadTeleportPoints(); // Reload teleport points for the new scene
    }
}
