using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class ClickChatConvidado : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] GameObject pnChatView;

    private bool timerOn = false;
    private DateTime tempoInicial;
    public void OnPointerDown(PointerEventData eventData)
    {
        if (Input.GetKeyDown(KeyCode.Mouse0)) // left click
        {
            FecharConversa();
        }
    }

    private void FecharConversa()
    {
        pnChatView.SetActive(false);
    }

    private void Update()
    {
        if (pnChatView.activeSelf)
        {
            if (!timerOn || GameManager.Instancia.TempoChat)
            {
                GameManager.Instancia.TempoChat = false;
                timerOn = true;
                tempoInicial = DateTime.Now;
            }
        }
        if (timerOn)
        {
            TimeSpan intervalo = DateTime.Now - tempoInicial;
            if (Input.GetKeyDown(KeyCode.Escape) || (intervalo.Seconds >= GestorDeRede.Instancia.TempoChat))
            {
                timerOn = false;
                FecharConversa();
            }
        }
    }
}
