using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIIDCardView : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text idText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text addressText;
    private void Awake()
    {
        Hide();    
    }

    public void Show(UserRecordData record)
    {
        if (record == null)
            return;

        if (portraitImage != null)
            portraitImage.sprite = record.portrait;

        if (idText != null)
            idText.text = record.recordId;

        if (nameText != null)
            nameText.text = record.fullName;

        if (addressText != null)
            addressText.text = record.address;

        if (root != null)
            root.SetActive(true);
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
    }
}