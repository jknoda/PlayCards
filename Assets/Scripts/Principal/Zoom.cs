using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Zoom : MonoBehaviourPunCallbacks
{
    [SerializeField]
    private GameObject _maoJogador;

    [SerializeField]
    private GameObject _txtSomar;

    private int _somaCarta;


    public static Zoom Instancia { get; private set; }
    private void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instancia = this;
        }
    }

    public void InicializaZoom()
    {
        DesligarSoma();

        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"]; // PhotonNetwork.LocalPlayer.ActorNumber;
        int outroActorNumber;
        if (GestorDeRede.Instancia.Dupla01.Item1 != localActor)
            outroActorNumber = GestorDeRede.Instancia.Dupla01.Item1;
        else
            outroActorNumber = GestorDeRede.Instancia.Dupla02.Item1;
        string portador = "JOGADOR" + localActor.ToString().PadLeft(2, '0');
        _maoJogador.tag = portador;
        GameCardsManager.Instancia.GetListaCartasJogo()
        .ForEach(cartaItem =>
        {
            if (!cartaItem.Portador.Contains(portador) && !cartaItem.Portador.Contains("AREA"))
                GameCardsManager.Instancia.SetVisible(cartaItem.Id, false);
        });
        FrontManager.Instancia.RedrawJogador(localActor, false);
        FrontManager.Instancia.RedrawJogada(localActor, "");
        FrontManager.Instancia.RedrawJogada(outroActorNumber, "");
    }

    public void CartaSomar(int idCarta, bool selecionar)
    {
        int pontos = GameCardsManager.Instancia.GetCarta(idCarta).Pontos;
        if (!selecionar)
            pontos *= (-1);
        Somar(pontos);
    }

    public void Somar(int valor)
    {
        _txtSomar.SetActive(true);
        _somaCarta += valor;
        _txtSomar.transform.GetComponent<Text>().text = _somaCarta.ToString();
    }

    public void DesligarSoma()
    {
        Baralho.Instancia.LimparSelecionados();
        GameManager.Instancia.ZoomSomar = false;
        _somaCarta = 0;
        _txtSomar.SetActive(false);
    }
}
