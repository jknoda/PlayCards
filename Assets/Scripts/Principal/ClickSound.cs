using UnityEngine;
using UnityEngine.EventSystems;

public class ClickSound : MonoBehaviour, IPointerDownHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        if (Input.GetKeyDown(KeyCode.Mouse0)) // left click
        {
            SoundManager.Instancia.PlayONOFF();
        }
    }
}
