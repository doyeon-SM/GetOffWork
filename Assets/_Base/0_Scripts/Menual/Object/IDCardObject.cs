using UnityEngine;

public class IDCardObject : ClickableWorldObject
{
    public override void OnClicked()
    {
        base.OnClicked();
        Debug.Log("[IDCardObject] 신분증 오브젝트 클릭");
    }
}