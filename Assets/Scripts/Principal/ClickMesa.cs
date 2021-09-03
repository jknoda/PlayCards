using UnityEngine;
using UnityEngine.EventSystems;

public class ClickMesa : MonoBehaviour, IPointerDownHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        if (GameCardsManager.Instancia.GetLocalActor() > 4)
            return;

        var jogador = GameCardsManager.Instancia.GetJogadorObjeto().GetComponent<Jogador>();
        if (Baralho.Instancia.cartasSel.Count > 0 && Input.GetKeyDown(KeyCode.Mouse0)) // left click
        {
            //if (!eventData.hovered.ToList().Exists(x => x.name == "AreaJogador01")) return;
            if (Baralho.Instancia.primeiraSelecao.Contains("MORTO"))
            {
                if (!GameRules.Instancia.TratarClick(9300, Baralho.Instancia.cartasSel[0], false))
                    return;
            }
            else if (Baralho.Instancia.primeiraSelecao.Contains("LIXO"))
            {
                if (!jogador.PuxouDoMonte)
                {
                    if (!GameRules.Instancia.TratarClick(9500, Baralho.Instancia.cartasSel[0], false))
                        return;
                }
            }
            else if (Baralho.Instancia.cartasSel.Count == 1 && (jogador.PuxouDoMonte || jogador.PegouLixo))
            {
                return;
            }
            else
            {
                // baixar
                if (!GameRules.Instancia.TratarClick(9100, 0, false))
                    return;
            }
        }
    }
}
