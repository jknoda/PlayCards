using UnityEngine;
using UnityEngine.EventSystems;

public class ClickMensagem : MonoBehaviour, IPointerDownHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        if (Input.GetKeyDown(KeyCode.Mouse0)) // left click
        {
            GameManager.Instancia.DesativaPainelMsg();
        }
    }
}
