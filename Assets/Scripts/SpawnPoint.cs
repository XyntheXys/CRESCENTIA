using UnityEngine;
using UnityEngine.InputSystem;

public class SpawnPoint : MonoBehaviour
{
    [SerializeField] private bool isActive = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (!isActive) return;
        PlayerController.Instance.transform.position = transform.position;
    }

    //// Update is called once per frame
    //void Update()
    //{
    //    if (Keyboard.current.qKey.isPressed)
    //    {
    //        PlayerController.Instance.transform.position = transform.position;
    //    }
    //}
}
