using Photon.Pun;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static GameCardsManager;

public class FrontManager : MonoBehaviourPunCallbacks
{
    [SerializeField] GameObject _marcador;

    private const float _xStep = 30;
    public List<Sprite> lixoLista;

    private bool _reorganizar;

    public static FrontManager Instancia { get; private set; }
    private void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _reorganizar = false;
            lixoLista = new List<Sprite>();
            Instancia = this;
        }
    }


    #region Redraw Cartas
    public void RedrawJogador(int actorNumber, bool others, bool redrawOthers = true)
    {
        photonView.RPC("RedrawJogadorRPC", RpcTarget.All, actorNumber, others, redrawOthers);
    }

    [PunRPC]
    private void RedrawJogadorRPC(int actorNumber, bool others, bool redrawOthers)
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        if (localActor > 4)
        {
            localActor = GestorDeRede.Instancia.SoVerNumber;
            actorNumber = localActor;
            redrawOthers = false;
        }
        if (actorNumber != 0 && actorNumber != localActor)
        {
            return;
        }

        float xStepInicial = 45;
        float xStep;
        string tag = "JOGADOR" + localActor.ToString().Trim().PadLeft(2, '0');
        Vector3 scale;
        float scaleAlter;

        Vector3 posInicial;
        int nCartasMax;

        GameObject jogador;
        float cartaX = 70f;
        float widthCanvas = GameObject.Find("Canvas").transform.GetComponent<RectTransform>().rect.width;
        float yAux = 0;

        if (others)
        {
            jogador = GameManager.Instancia.Spawn(1, 1).Item1.gameObject;
            posInicial = jogador.transform.position;
            scale = Baralho.Instancia.scale * 1.1f; // 10% maior
            scaleAlter = 1f;
            xStep = _xStep + 5;
            nCartasMax = (int)((widthCanvas - xStepInicial) / ((cartaX + xStep) / 2));
        }
        else
        {
            // zoom
            jogador = GameObject.FindGameObjectWithTag(tag);
            posInicial = jogador.transform.position;
            scale = Baralho.Instancia.scale * 1.4f;
            scaleAlter = 1.2f;
            xStep = _xStep + 10;
            nCartasMax = (int)((widthCanvas - xStepInicial) / ((cartaX + xStep) / 2));
            int nCartas = GameCardsManager.Instancia.GetQdeCartaPortador(tag);
            yAux = 0;
        }

        int ct = GameCardsManager.Instancia.GetListaCartasJogo().Where(x => x.Portador == tag).Count();
        float x = posInicial.x + xStepInicial + 10, y = posInicial.y + yAux;
        if (GameCardsManager.Instancia.GetQdeCartaPortador(tag) > nCartasMax)
        {
            scale = Baralho.Instancia.scale * scaleAlter;
            xStep = _xStep;
        }
        int seq = 0;
        GameCardsManager.Instancia.GetListaCartasJogo().Where(x => x.Portador == tag).OrderBy(x => x.Seq).ToList()
        .ForEach(item =>
        {
            if (item.SeqFixo == -1)
                GameCardsManager.Instancia.SetSeqFixoCarta(item.Id, 9000, true);
            GameCardsManager.Instancia.SetSeqCarta(item.Id, seq);
            seq++;
            GameObject carta = Baralho.Instancia.cartas.Find(x => x.GetComponent<Carta>().Id == item.Id);
            carta.transform.localScale = scale;
            // restabelecer valor de A
            if (GameCardsManager.Instancia.GetValorCarta(item.Id) == 14)
            {
                GameCardsManager.Instancia.SetValor(item.Id, 1);
            }
            // restabelecer peso das cartas na mão do jogador
            GameCardsManager.Instancia.SetPeso(item.Id, GameCardsManager.Instancia.GetNaipe(item.Id) * 1000 + GameCardsManager.Instancia.GetValorCarta(item.Id) * 10);
            carta.transform.SetAsLastSibling();
            float xAux = 0;
            float yAux = y;
            if (GameCardsManager.Instancia.GetListaCartasJogo().FirstOrDefault(x => x.Id == item.Id).NovoNaMao)
            {
                GameCardsManager.Instancia.GetListaCartasJogo().FirstOrDefault(x => x.Id == item.Id).NovoNaMao = false;
                yAux = y + 20f;
            }
            x += xStep;
            xAux = x;
            carta.transform.position = new Vector3(xAux, yAux, 0);
            carta.GetComponent<Carta>().PosicaoInicial = carta.transform.position;

            if (GameManager.Instancia.ZoomOn)
                carta.GetComponent<Carta>().MostraCarta(true); // carta.GetComponent<Carta>().Nome);
            else
            {
                if (GestorDeRede.Instancia.BotDebug || GestorDeRede.Instancia.SoVer)
                    carta.GetComponent<Carta>().MostraCarta(true); // carta.GetComponent<Carta>().Nome);
                else
                {
                    if (!GestorDeRede.Instancia.BotDebug && GameCardsManager.Instancia.GetJogadorObjeto(localActor).GetComponent<Jogador>().Bot)
                        carta.GetComponent<Carta>().MostraCarta(false); // carta.GetComponent<Carta>().Verso);
                    else
                        carta.GetComponent<Carta>().MostraCarta(true); // carta.GetComponent<Carta>().Nome);
                }
            }
            if (item.SeqFixo != 9000)
                carta.GetComponent<Image>().color = GestorDeRede.Instancia.GetCor("fixar");
        });
        if (others)
        {
            var jogadorAux = GameCardsManager.Instancia.GetJogadorObjeto(localActor);
            if (jogadorAux != null)
                jogadorAux.transform.Find("Qde").GetComponent<Text>().text = "";
            if (redrawOthers)
                this.RedrawOthers(localActor);
            GameCardsManager.Instancia.photonView.RPC("MostraStatus", RpcTarget.All);
        }
    }

    public void RedrawLixo(int idCarta, string jogadorTag)
    {
        GameObject jogador;
        if (idCarta != -1)
        {
            jogador = GameObject.FindGameObjectWithTag(jogadorTag);
            jogador.GetComponent<Jogador>().RemoverCarta(idCarta, 0, "LIXO");
        }

        photonView.RPC("RedrawLixoRPC", RpcTarget.All, idCarta, jogadorTag);
    }
    [PunRPC]
    public void RedrawLixoRPC(int idCarta, string jogadorTag)
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"]; // PhotonNetwork.LocalPlayer.ActorNumber;
        if (localActor > 4)
            localActor = GestorDeRede.Instancia.SoVerNumber;
        RedrawLixoCPL(idCarta, jogadorTag);
        RedrawOthers(localActor);
    }
    public void RedrawLixoCPL(int idCarta, string jogadorTag)
    {
        if (GameManager.Instancia.ZoomOn)
            return;

        GameObject lixo = GameObject.Find("Lixo");
        float x = lixo.transform.position.x - 20, y = lixo.transform.position.y;
        float yAux = 0, xAux = 0;
        lixoLista.Clear();
        GameCardsManager.Instancia.GetListaCartasJogo().Where(x => x.Portador == "LIXO").OrderBy(x => x.Seq).ToList()
        .ForEach(item =>
        {
            GameObject carta = Baralho.Instancia.cartas.Find(x => x.GetComponent<Carta>().Id == item.Id);
            if (carta != null)
            {
                carta.transform.localScale = Baralho.Instancia.scale * (0.9f);
                carta.transform.SetAsFirstSibling();
                x -= 24f;
                if (GestorDeRede.Instancia.LixoBaguncado)
                {
                    switch (UnityEngine.Random.Range(1, 4))
                    {
                        case 1:
                            yAux = 0;
                            xAux = 0;
                            break;
                        case 2:
                            yAux = 10;
                            xAux = UnityEngine.Random.Range(3, 6);
                            break;
                        case 3:
                            yAux = -10;
                            xAux = UnityEngine.Random.Range(3, 6) * (-1);
                            break;
                    }
                }
                carta.transform.position = new Vector3(x + xAux, y + yAux, 0);
                carta.GetComponent<Carta>().PosicaoInicial = carta.transform.position;
                carta.GetComponent<Carta>().MostraCarta(true); // carta.GetComponent<Carta>().Nome);
                Sprite img = Resources.Load<Sprite>("Images/Cards/" + carta.GetComponent<Carta>().Nome);
                lixoLista.Add(img);
            }
        });
    }

    public void RedrawJogada(int actorNumber, string endDrag)
    {
        photonView.RPC("RedrawJogadaRPC", RpcTarget.All, actorNumber, endDrag);
    }
    [PunRPC]
    private void RedrawJogadaRPC(int actorNumber, string endDrag)
    {
        RedrawJogadaCPL(actorNumber, endDrag);
    }

    public void RedrawJogadaCPL(int actorNumber, string endDrag)
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        if (localActor > 4)
            localActor = GestorDeRede.Instancia.SoVerNumber;

        float yStepInit;
        float yStepSalto;
        float xStep;
        float xStepSalto;
        //float widthCanvas;
        Vector3 scale;

        scale = Baralho.Instancia.scale;
        yStepInit = 10f;
        yStepSalto = 80f;
        xStep = 30f;
        xStepSalto = 36f;

        if (GameManager.Instancia.ZoomOn)
        {
            float perc = 1.2f;
            scale = Baralho.Instancia.scale * perc;
            yStepInit = -10f;
        }
        bool parceiro = false;
        if (actorNumber == localActor)
            parceiro = true;
        if (GameCardsManager.Instancia.GetDupla(localActor) == GameCardsManager.Instancia.GetDupla(actorNumber))
            parceiro = true;
        string tag, portador;

        if (parceiro)
            tag = "JOGADA01";
        else
            tag = "JOGADA02";

        if (GameCardsManager.Instancia.GetDupla(actorNumber) == 1)
            portador = "AREA01";
        else
            portador = "AREA02";
        GameObject jogada = GameObject.FindGameObjectWithTag(tag);
        float x = jogada.transform.position.x, y = jogada.transform.position.y;
        if (parceiro)
            y += (yStepInit + yStepSalto);
        else
            y -= yStepInit;

        string areaInd = "X";

        if (endDrag.Contains("-"))
        {
            areaInd = (endDrag + "XXXXXXXXXX").Substring(6, 3); // -01 para não confundir com AREA01
        }

        float maxX;

        maxX = _marcador.transform.position.x;
        var portadores = GameCardsManager.Instancia.GetListaCartasJogo().OrderBy(x => x.Portador).Where(x => x.Portador.Contains(portador)).Select(x => x.Portador).Distinct().ToList();

        if (_reorganizar)
        {
            _reorganizar = false;
            //Reorganizar(portadores, portador); // suspenso por enquanto
        }

        foreach (string itemPortador in portadores)
        {
            List<CartaJogo> cartas = GameCardsManager.Instancia.GetListaCartasJogo().Where(x => x.Portador == itemPortador).OrderBy(x => x.Peso).ToList();

            string tipoCanastra = GameCardsManager.Instancia.GetTipoDaJogada(cartas);
            GameObject ultimaCarta = null;

            bool jogadaAtual = itemPortador.Contains(areaInd);
            bool played = false;
            bool first = true;
            float xCalc = x + cartas.Count * xStep;
            if (xCalc > maxX)
            {
                _reorganizar = true;
                x = jogada.transform.position.x;
                if (parceiro)
                    y += yStepSalto * (-1);
                else
                    y -= yStepSalto;
            }
            foreach (CartaJogo item in cartas)
            {
                GameObject carta = Baralho.Instancia.cartas.Find(x => x.GetComponent<Carta>().Id == item.Id);
                if (carta != null)
                {
                    carta.transform.localScale = scale;
                    carta.transform.SetAsLastSibling();
                    if (first)
                    {
                        first = false;
                        carta.transform.position = new Vector3(x, y, 0);
                    }
                    x += xStep;
                    carta.transform.position = new Vector3(x, y, 0);
                    if (carta.GetComponent<Carta>().Cor != GestorDeRede.Instancia.GetCor("padrao")
                        && carta.GetComponent<Carta>().Cor != GestorDeRede.Instancia.GetCor("2coringa")
                        && carta.GetComponent<Carta>().Cor != GestorDeRede.Instancia.GetCor("2lixo"))
                    {
                        played = true;
                        if (tipoCanastra != "")
                            carta.GetComponent<Carta>().Cor = GestorDeRede.Instancia.GetCor("padrao");
                    }

                    if (item.Portador.Contains("S") && item.Coringa && item.Valor == 2)
                        carta.GetComponent<Carta>().Cor = GestorDeRede.Instancia.GetCor("2coringa");

                    carta.GetComponent<Carta>().PosicaoInicial = carta.transform.position;
                    carta.GetComponent<Carta>().MostraCarta(true); // carta.GetComponent<Carta>().Nome);
                    ultimaCarta = carta;
                }
            }
            x += xStepSalto;
            float qdeCutucar = 0;
            bool trocaCanastra = false; // troca canastra por RL ou RS para estatistica
            if (ultimaCarta != null)
            {
                switch (tipoCanastra)
                {
                    case "AS":
                        ultimaCarta.GetComponent<Carta>().Cor = GestorDeRede.Instancia.GetCor("AS"); // new Color(1f, 0.8f, 0.8f);
                        break;
                    case "AL":
                        qdeCutucar = 1.5f;
                        if (jogadaAtual && !played)
                        {
                            jogadaAtual = false;
                            SoundManager.Instancia.PlaySound("asAs");
                            GameCardsManager.Instancia.FogosOn();
                            BotManager.Instancia.FezCanastra(actorNumber, tipoCanastra);
                        }
                        ultimaCarta.GetComponent<Carta>().Cor = GestorDeRede.Instancia.GetCor("AL"); // new Color(0.839f, 0.960f, 0.839f);
                        break;
                    case "CS":
                        ultimaCarta.GetComponent<Carta>().Cor = GestorDeRede.Instancia.GetCor("CS"); // new Color(1f, 0.6f, 0.6f);
                        break;
                    case "CL":
                        qdeCutucar = 1;
                        ultimaCarta.GetComponent<Carta>().Cor = GestorDeRede.Instancia.GetCor("CL"); // new Color(0.6f, 1f, 0.6f);
                        break;
                    case "RS":
                        if (jogadaAtual)
                        {
                            jogadaAtual = false;
                            SoundManager.Instancia.PlaySound("asAs");
                            GameCardsManager.Instancia.FogosOn();
                            trocaCanastra = true;
                            BotManager.Instancia.FezCanastra(actorNumber, tipoCanastra);
                            qdeCutucar = 1.5f;
                        }
                        ultimaCarta.GetComponent<Carta>().Cor = GestorDeRede.Instancia.GetCor("RS"); // new Color(0.901f, 0.6f, 1f);
                        break;
                    case "RL":
                        if (jogadaAtual)
                        {
                            jogadaAtual = false;
                            SoundManager.Instancia.PlaySound("asAs");
                            GameCardsManager.Instancia.FogosOn();
                            trocaCanastra = true;
                            BotManager.Instancia.FezCanastra(actorNumber, tipoCanastra);
                            qdeCutucar = 2;
                        }
                        ultimaCarta.GetComponent<Carta>().Cor = GestorDeRede.Instancia.GetCor("RL"); // new Color(0.6f, 0.6f, 1f);
                        break;
                }

                if (!string.IsNullOrEmpty(tipoCanastra))
                {
                    if (!played || trocaCanastra)
                    {
                        if (!GestorDeRede.Instancia.CtrlRecallFront)
                            GestorDeRede.Instancia.SetDadosRodada(actorNumber, GameCardsManager.Instancia.GetDupla(actorNumber), tipoCanastra.ToLower(), 1);
                    }
                    ultimaCarta.GetComponent<Carta>().MostraCarta(true); // ultimaCarta.GetComponent<Carta>().Nome);
                    if (jogadaAtual && !played)
                    {
                        SoundManager.Instancia.PlaySound("canastra");
                        BotManager.Instancia.FezCanastra(actorNumber, tipoCanastra);
                        GameManager.Instancia.PodeCutucar(qdeCutucar, actorNumber);
                        int actorParceiro = GameCardsManager.Instancia.GetMeuParceiro(actorNumber);
                        GameManager.Instancia.PodeCutucar((qdeCutucar / 2), actorParceiro);
                    }
                }
            }
        }
    }

    public void RedrawOthers(int actorNumber)
    {
        photonView.RPC("RedrawOthersRPC", RpcTarget.All, actorNumber);
    }
    [PunRPC]
    private void RedrawOthersRPC(int actorNumber)
    {
        RedrawOthersCPL(actorNumber);
    }

    public void RedrawOthersCPL(int actorNumber)
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"]; // PhotonNetwork.LocalPlayer.ActorNumber;
        if (localActor > 4)
            localActor = GestorDeRede.Instancia.SoVerNumber;

        if (actorNumber == localActor)
            return;

        if (GameManager.Instancia.ZoomOn)
            return;

        if (Baralho.Instancia.cartas.Count == 0) return;

        string tag = "JOGADOR" + actorNumber.ToString().Trim().PadLeft(2, '0');

        var ret = GameManager.Instancia.Spawn(localActor, actorNumber);

        Transform spawn = ret.Item1;

        int ind = ret.Item2;

        int inversor = ind <= 1 ? 1 : -1; // spawns 0 e 1 posX positivo, 2 e 3 posX negativo

        float x = spawn.position.x + 45 * (inversor), y = spawn.position.y;

        // Cartas dos outros jogadores
        Vector3 scale = Baralho.Instancia.scale * 0.6f;
        GameCardsManager.Instancia.GetListaCartasJogo().Where(x => x.Portador == tag).OrderBy(x => x.Seq).ToList()
        .ForEach(item =>
        {
            GameObject carta = Baralho.Instancia.cartas.Find(x => x.GetComponent<Carta>().Id == item.Id);
            carta.transform.localScale = scale;
            carta.transform.SetAsLastSibling();
            x += 0.5f * inversor;
            carta.transform.position = new Vector3(x, y, 0);
            carta.GetComponent<Carta>().MostraCarta(false); // carta.GetComponent<Carta>().Verso);
        });
        var jogadorAux = GameCardsManager.Instancia.GetJogadorObjeto(actorNumber);
        if (jogadorAux != null)
            jogadorAux.transform.Find("Qde").GetComponent<Text>().text = GameCardsManager.Instancia.GetQdeCartaPortador(tag).ToString();
    }
    #endregion Redraw Cartas
}
