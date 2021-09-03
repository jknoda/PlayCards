using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class ClickChat : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] GameObject pnDireita;
    [SerializeField] GameObject pnEsquerda;
    [SerializeField] GameObject pnDireitaBaixo;
    [SerializeField] GameObject pnEsquerdaBaixo;

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
        try
        {
            pnDireita.SetActive(false);
            pnEsquerda.SetActive(false);
            pnDireitaBaixo.SetActive(false);
            pnEsquerdaBaixo.SetActive(false);
        }
        catch
        {
            timerOn = false;
        }
    }

    private void Update()
    {
        if (pnDireita.activeSelf || pnEsquerda.activeSelf || pnDireitaBaixo.activeSelf || pnEsquerdaBaixo.activeSelf)
        {
            if (!timerOn || GameManager.Instancia.TempoChat)
            {
                GameManager.Instancia.TempoChat = false;
                tempoInicial = DateTime.Now;
                timerOn = true;
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
