using UnityEngine;

[CreateAssetMenu(menuName = "New Item/Tool Item")]
public class ToolItem : ItemData
{
    public int minEfficiency;
    public int maxEfficiency;
    public string[] effectiveTags;

    public override ItemType itemType => ItemType.Tool;
}