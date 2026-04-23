using UnityEngine;

public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private TimeManager timeManager;
    [SerializeField] private InventorySystem inventorySystem;

    public TimeManager Time => timeManager;
    public InventorySystem Inventory => inventorySystem;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Reset()
    {
        timeManager = GetComponent<TimeManager>();
        inventorySystem = GetComponent<InventorySystem>();
    }
}

