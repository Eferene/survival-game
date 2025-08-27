using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[System.Serializable]
public class BuildingSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Slot UI Elements")]
    public Sprite buildingIcon;
    public GameObject building;
    [SerializeField] private Image iconImage;
    [SerializeField] private int slotIndex;
    
    PlayerGeneral playerGeneral;

    void Start()
    {
        playerGeneral = GameObject.FindWithTag("Player").GetComponent<PlayerGeneral>();
        if (iconImage != null && buildingIcon != null)
        {
            iconImage.sprite = buildingIcon;
        }

        if (GetComponent<Image>() != null)
        {
            GetComponent<Image>().alphaHitTestMinimumThreshold = 0.1f;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (building != null)
        {
            Destroy(playerGeneral.previewBuilding);
            playerGeneral.previewBuilding = null;
            playerGeneral.selectedBuilding = building;
        }
        else
        {
            playerGeneral.selectedBuilding = null;
            Debug.LogWarning("Bu slota herhangi bir building eklenmemiş.");
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        playerGeneral.selectedBuilding = null;
    }
}
