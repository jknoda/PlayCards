using Photon.Pun;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ClickJogada : MonoBehaviour, IPointerDownHandler, IBeginDragHandler,
      IDragHandler, IEndDragHandler
{

    private int _number = 0;
    public void OnBeginDrag(PointerEventData eventData)
    {
    }

    public void OnDrag(PointerEventData eventData)
    {
    }

    //int tap = 0;
    public void OnEndDrag(PointerEventData eventData)
    {
        if (_number != -1)
        {
            GameCardsManager.Instancia.SetCutucar(_number, 0);
        }
        return;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        //if (Input.GetKeyDown(KeyCode.Escape)) // Esc
        //{
        //    int indAux = eventData.hovered.ToList().FindIndex(x => x.name.Contains("AreaJogador"));
        //    if (indAux != -1)
        //    {
        //        int number = Convert.ToInt32(eventData.hovered[indAux].name.Replace("AreaJogador", ""));
        //        GameCardsManager.Instancia.SetCutucar(number,0);
        //    }
        //    return;
        //}

        //Debug.Log("tecla: " + eventData.ToString());

        if (Input.GetKeyDown(KeyCode.Mouse2)) // middle click
        {
            if (_number == 0) _number = -1;
            if (_number != -1)
            {
                GameCardsManager.Instancia.SetCutucar(_number, 0);
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.Mouse0)) // left click
        {
            _number = -1;
            int indAux = eventData.hovered.ToList().FindIndex(x => x.name.Contains("AreaJogador"));
            if (indAux != -1)
            {
                int number = Convert.ToInt32(eventData.hovered[indAux].name.Replace("AreaJogador", ""));
                _number = number;
                //Debug.Log("Cutucar: " + eventData.hovered[indAux].name + " number: " + number.ToString());
                int actorOrigem = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
                if (number == 1 && actorOrigem <= 4)
                {
                    if (actorOrigem <= 4)
                    {
                        GameCardsManager.Instancia.SetHumor();
                    }
                }
                else
                {
                    GameCardsManager.Instancia.SetCutucar(number, 0);
                }
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
