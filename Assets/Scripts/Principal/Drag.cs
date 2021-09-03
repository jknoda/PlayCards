using UnityEngine;
using UnityEngine.EventSystems;

public class Drag : MonoBehaviour, IPointerClickHandler, IPointerDownHandler
{
    // private string _portador;
    // private string _jogador;
    // private Vector3 _posInicial;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (GameCardsManager.Instancia.GetLocalActor() > 4)
            return;

        int cartaInd = this.GetComponent<Carta>().Id;
        string portador = GameCardsManager.Instancia.GetPortador(cartaInd);
        string primeiroPortador = Baralho.Instancia.primeiraSelecao;
        if ((Input.GetKeyUp(KeyCode.Mouse0) || eventData.clickCount == 2) && portador == "MONTE")
        {
            Baralho.Instancia.LimparSelecionados();
            if (!GameRules.Instancia.TratarClick(9200, cartaInd, false)) return;
        }
        else if (Input.GetKeyUp(KeyCode.Mouse0)) // left click
        {
            if (GameManager.Instancia.ZoomOn && GameManager.Instancia.ZoomSomar)
                return;
            else if (Baralho.Instancia.cartasSel.Count > 0 && (!string.IsNullOrEmpty(primeiroPortador) && primeiroPortador != portador))
            {
                if (!GameRules.Instancia.TratarClick(0, cartaInd, false))
                    return;
            }
            else if (GameManager.Instancia.ZoomOn && Baralho.Instancia.cartasSel.Count > 0 && portador.Contains("JOGADOR") && primeiroPortador == portador)
            {
                if (Baralho.Instancia.cartasSel[0] != cartaInd)
                {
                    int index = Baralho.Instancia.cartasSel.Find(x => x == cartaInd);
                    if (index != 1) 
                        Baralho.Instancia.cartasSel.Remove(index);
                    if (Baralho.Instancia.cartasSel.Count > 0)
                        if (!GameRules.Instancia.TratarClick(0, cartaInd, false)) 
                            return;
                }
            }
        }
        else if (eventData.clickCount == 2 && portador.Contains("MORTO"))
        {
            if (!GameRules.Instancia.TratarClick(9300, cartaInd, false)) 
                return;
        }
        else if (eventData.clickCount == 2 && portador.Contains("JOGADOR"))
        {
            var jogador = GameCardsManager.Instancia.GetJogadorObjeto().GetComponent<Jogador>();
            if ((jogador.PuxouDoMonte || jogador.PegouLixo) && !jogador.Descartou)
            {
                if (!GameRules.Instancia.TratarClick(9400, cartaInd, false)) 
                    return;
            }
        }
        else if (eventData.clickCount == 2 && portador.Contains("LIXO"))
        {
            var jogador = GameCardsManager.Instancia.GetJogadorObjeto().GetComponent<Jogador>();
            if (!jogador.PuxouDoMonte)
            {
                if (!GameRules.Instancia.TratarClick(9500, cartaInd, false)) 
                    return;
            }
        }
        try
        {
            Debug.Log(
                "OnPointerClick - Carta ID:" + this.GetComponent<Carta>().Id.ToString() +
                " Seq: " + GameCardsManager.Instancia.GetListaCartasJogo().Find(x => x.Id == this.GetComponent<Carta>().Id).Seq.ToString() +
                " Pes: " + GameCardsManager.Instancia.GetListaCartasJogo().Find(x => x.Id == this.GetComponent<Carta>().Id).Peso.ToString() +
                " Por: " + GameCardsManager.Instancia.GetListaCartasJogo().Find(x => x.Id == this.GetComponent<Carta>().Id).Portador
            );
        }
        catch
        {
            Debug.Log("Error");
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (GameCardsManager.Instancia.GetLocalActor() > 4)
            return;

        int idCarta = this.GetComponent<Carta>().Id;
        string portador = GameCardsManager.Instancia.GetPortador(idCarta);
        if ((Input.GetKeyDown(KeyCode.Mouse2) && GameManager.Instancia.selClick == 2) || (Input.GetKeyDown(KeyCode.Mouse0) && GameManager.Instancia.selClick == 0)) // midlle click // left click
        {
            if (GameRules.Instancia.SelecionarValido(portador, idCarta))
            {
                Baralho.Instancia.Selecionar(this.GetComponent<Carta>().Id);
            }
        }
    }
}
