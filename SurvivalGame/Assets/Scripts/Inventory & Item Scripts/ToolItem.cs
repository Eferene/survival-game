using UnityEngine;

[CreateAssetMenu(menuName = "New Item/Tool Item")]
public class ToolItem : ItemData
{
    public float efficiency;
    public string[] effectiveTags;

    public override ItemType itemType => ItemType.Tool;
}