using UnityEngine;
using UnityEngine.EventSystems;

public class Lixo : MonoBehaviour, IPointerDownHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        if (GameCardsManager.Instancia.GetLocalActor() > 4)
            return;

        if (Input.GetKeyDown(KeyCode.Mouse0)) // left click
        {
            if (Baralho.Instancia.cartasSel.Count == 1)
            {
                if (!GameRules.Instancia.TratarClick(9000, 0, false)) return;
            }
            else
            {
                // selecionar todas cartas do lixo
                GameCardsManager.Instancia.GetLixo();
            }
        }
    }
}
