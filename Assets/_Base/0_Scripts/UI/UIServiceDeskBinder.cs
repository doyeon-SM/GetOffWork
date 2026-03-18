using UnityEngine;

public class UIServiceDeskBinder : MonoBehaviour
{
    [SerializeField] private ServiceDeskManager serviceDeskManager;

    public void AskSubmitID()
    {
        serviceDeskManager.SubmitQuestion("submit_id");
    }

    public void AskProxy()
    {
        serviceDeskManager.SubmitQuestion("ask_proxy");
    }

    public void AskCheckPhoto()
    {
        serviceDeskManager.SubmitQuestion("check_photo");
    }

    public void AskCheckIDInfo()
    {
        serviceDeskManager.SubmitQuestion("check_idinfo");
    }

    public void AskCheckAddress()
    {
        serviceDeskManager.SubmitQuestion("check_address");
    }

    public void AskPrint()
    {
        serviceDeskManager.SubmitQuestion("ask_print");
    }

    public void AskMobile()
    {
        serviceDeskManager.SubmitQuestion("ask_mobile");
    }

    public void AskInputPhone()
    {
        serviceDeskManager.SubmitQuestion("input_phone");
    }

    public void AskInputEmail()
    {
        serviceDeskManager.SubmitQuestion("input_email");
    }

    public void AskRetrySubmit()
    {
        serviceDeskManager.SubmitQuestion("retry_submit");
    }
}