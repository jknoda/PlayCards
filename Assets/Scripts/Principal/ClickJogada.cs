using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ClickJogada : MonoBehaviour, IPointerDownHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        if (Input.GetKeyDown(KeyCode.Mouse0)) // left click
        {
            int indAux = eventData.hovered.ToList().FindIndex(x => x.name.Contains("AreaJogador"));
            if (indAux != -1)
            {
                int number = Convert.ToInt32(eventData.hovered[indAux].name.Replace("AreaJogador", ""));
                //Debug.Log("Cutucar: " + eventData.hovered[indAux].name + " number: " + number.ToString());
                GameCardsManager.Instancia.SetCutucar(number);
            }
            else if (GameCardsManager.Instancia.GetLocalActor() > 4)
                return;

            if (!eventData.hovered.ToList().Exists(x => x.name == "AreaJogador01")) // self cutucar
                return;
            if (Baralho.Instancia.cartasSel.Count > 0)
            {
                if (Baralho.Instancia.primeiraSelecao.Contains("MORTO"))
                {
                    if (!GameRules.Instancia.TratarClick(9300, Baralho.Instancia.cartasSel[0], false))
                        return;
                }
                else
                {
                    if (!GameRules.Instancia.TratarClick(9100, 0, false)) return;
                }
            }
        }
    }
}
