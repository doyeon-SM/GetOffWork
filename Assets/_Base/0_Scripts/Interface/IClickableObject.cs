using UnityEngine;

public interface IClickableObject
{
    void OnClicked();
    string GetDisplayName();
    void OnHoverEnter();
    void OnHoverExit();
}