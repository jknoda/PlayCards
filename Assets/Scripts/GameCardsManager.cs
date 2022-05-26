using Newtonsoft.Json;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class GameCardsManager : MonoBehaviourPunCallbacks
{
    [SerializeField]
    private GameObject _pnDebug;
    [SerializeField]
    private GameObject _msgDebug;
    [SerializeField]
    private GameObject _msgPlacar;
    [SerializeField]
    private GameObject _msgStatus;
    [SerializeField]
    private GameObject _msg;
    [SerializeField]
    private GameObject _msgQuadro;
    [SerializeField]
    private GameObject _txtQdeMonte;

    [SerializeField]
    private MapaJogo _mapaJogo;

    private bool _jogadaFinalizada;

    public static GameCardsManager Instancia { get; private set; }
    private void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            SetEstatistica(0, "INICIAR", 0);
            SetInicializaMapaRPCIN(true);
            SetProximoJogarRPCIN();
            MostraQuemJoga();
            Instancia = this;
        }
    }

    #region Start
    /// <summary>
    /// Iniciliza Mapa - já é chamado em PUNRPC
    /// </summary>
    /// <param name="zerar"></param>
    public void SetInicializaMapaRPCIN(bool zerar)
    {
        if (_mapaJogo == null || zerar)
        {
            _mapaJogo = new MapaJogo
            {
                seqJogadorAtual = GestorDeRede.Instancia.JogadorInicial,
                cartasJogo = new List<CartaJogo>()
            };
        }
        _mapaJogo.Vul = new int[4] { 0, 0, 0, 0 };
        if (GestorDeRede.Instancia.Placar01 >= 1500)
        {
            _mapaJogo.Vul[GestorDeRede.Instancia.Dupla01.Item1 - 1] = 75;
            _mapaJogo.Vul[GestorDeRede.Instancia.Dupla01.Item2 - 1] = 75;
        }
        if (GestorDeRede.Instancia.Placar02 >= 1500)
        {
            _mapaJogo.Vul[GestorDeRede.Instancia.Dupla02.Item1 - 1] = 75;
            _mapaJogo.Vul[GestorDeRede.Instancia.Dupla02.Item2 - 1] = 75;
        }

        _mapaJogo.pegouMorto = new bool[4] { false, false, false, false };
        _mapaJogo.fimRodada = false;
        _mapaJogo.baixou = new bool[4] { false, false, false, false };
        _jogadaFinalizada = false;
        MsgPlacar();
    }

    public void SetInicializaJogada()
    {
        photonView.RPC("SetInicializaJogadaRPC", RpcTarget.All);
        MostraQuemJoga();
    }
    [PunRPC]
    private void SetInicializaJogadaRPC()
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        if (localActor > 4)
            return;
        if (!GameManager.Instancia.ZoomOn)
            GetJogadorObjeto().GetComponent<Jogador>().InicializaJogada();
        SetProximoJogarRPCIN();
    }

    /// <summary>
    /// Atualiza proximo jogador - já é chamado em PUNRPC
    /// </summary>
    public void SetProximoJogarRPCIN()
    {
        //_mapaJogo.seqJogadorAtual++;
        //int nQdePlayer = PhotonNetwork.PlayerList.Length;
        //if (nQdePlayer > 4) nQdePlayer = 4;
        //if (_mapaJogo.seqJogadorAtual > nQdePlayer)
        //    _mapaJogo.seqJogadorAtual = 1;

        _mapaJogo.seqJogadorAtual--;
        int nQdePlayer = PhotonNetwork.PlayerList.Length;
        if (nQdePlayer > 4) nQdePlayer = 4;
        if (_mapaJogo.seqJogadorAtual <= 0)
            _mapaJogo.seqJogadorAtual = nQdePlayer;

    }
    #endregion Start

    #region Mensagens
    public void MsgPlacar()
    {
        string jog01 = "", jog02 = "";
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.ActorNumber <= 4)
            {
                int playerActorNumber = (int)player.CustomProperties["ID"];
                if (GetDupla(playerActorNumber) == 1)
                    jog01 += (jog01.Length > 0 ? " / " : "") + player.NickName;
                if (GetDupla(playerActorNumber) == 2)
                    jog02 += (jog02.Length > 0 ? " / " : "") + player.NickName;
            }
        }
        jog01 = string.IsNullOrEmpty(jog01) ? "01" : jog01;
        jog02 = string.IsNullOrEmpty(jog02) ? "02" : jog02;
        _msgPlacar.GetComponent<Text>().text = jog01.PadRight(15, '.') + " = " + GestorDeRede.Instancia.Placar01.ToString() + "\n" + jog02.PadRight(15, '.') + " = " + GestorDeRede.Instancia.Placar02.ToString();
    }

    private void MsgStatus()
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        if (localActor > 4)
            _msgStatus.GetComponent<Text>().text = "Convidado";
        else
        {
            _msgStatus.GetComponent<Text>().text =
                "Morto: " + (GetPegouMorto(localActor) ? "S" : "N") + "\n" +
                "Vul: " + GetVulPontos(localActor).ToString();
        }

    }

    [PunRPC]
    public void MostraStatus()
    {
        MsgStatus();
    }

    public void MostraQuemJoga(int jogadorNumber = -1)
    {
        photonView.RPC("MostraQuemJogaRPC", RpcTarget.All, jogadorNumber);
    }

    [PunRPC]
    public void MostraQuemJogaRPC(int jogadorNumber)
    {
        int localActor = PhotonNetwork.LocalPlayer.ActorNumber;
        if (localActor > 4)
        {
            _msgQuadro.GetComponent<Text>().text = "";
            return;
        }
        if (jogadorNumber != -1)
            _mapaJogo.seqJogadorAtual = jogadorNumber; // para setar novamente a jogada
        // Salva Status do Jogo
        _msgQuadro.GetComponent<Text>().text = "";
        int jogadorAtual = GetJogadorAtual(); // _mapaJogo.jogadorAtual;
        int nQdePlayer = PhotonNetwork.PlayerList.Length;
        if (nQdePlayer > 4)
            nQdePlayer = 4;
        if (jogadorAtual != 0 && !GetEhFimRodada() && jogadorAtual <= nQdePlayer)
        {
            if (jogadorAtual == localActor)
            {
                _msgQuadro.GetComponent<Text>().text = "SUA VEZ DE JOGAR";
                _msg.GetComponent<Text>().text = "";
                if (GameManager.Instancia != null)
                {
                    if (GestorDeRede.Instancia.InicialOk == 0)
                    {
                        //GestorDeRede.Instancia.InicialOk = jogadorAtual;
                        GestorDeRede.Instancia.SetInicialOk(jogadorAtual);
                    }
                    GameManager.Instancia.MostraMsgMain("SUA VEZ DE JOGAR", false, "suaVez", false, 0);
                    if (GameCardsManager.Instancia.IsBot())
                    {
                        int actorNumber = GetJogadorAtual();
                        //Debug.Log("sua vez");
                        BotManager.Instancia.finalizado = true;
                        BotManager.Instancia.Jogar(actorNumber, false, false);
                    }
                }
                else
                    SoundManager.Instancia.PlaySound("suaVez");
            }
            else
            {
                string jogador = PhotonNetwork.PlayerList.First(x => x.ActorNumber == jogadorAtual).NickName;
                _msgQuadro.GetComponent<Text>().text = jogador.ToUpper() + " JOGANDO...";
            }
        }
    }

    /// <summary>
    /// Mostrtar mensagem na área de mensagem
    /// </summary>
    /// <param name="msg">Mensagem</param>
    /// <param name="quem">ActorNumber(opcional) 0=todos 99=Todos menos DONO </param>
    public void MsgMsg(string msg, int quem, bool paraBot = false)
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        if (quem == localActor)
            _msg.GetComponent<Text>().text = msg;
        else
            photonView.RPC("MostraMsg", RpcTarget.All, msg, quem, paraBot);

    }

    /// <summary>
    /// Mostrtar mensagem na área de quem joga
    /// </summary>
    /// <param name="msg">Mensagem</param>
    /// <param name="quem">ActorNumber(opcional) 0=todos 99=Todos menos DONO</param>
    public void MsgQuemJoga(string msg, int quem = 0)
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        if (quem == localActor)
            _msgQuadro.GetComponent<Text>().text = msg;
        else
            photonView.RPC("MostraMsgQuemJoga", RpcTarget.All, msg, quem);

    }
    public void MsgDebugDesativa()
    {
        _pnDebug.SetActive(false);
    }
    public void MsgDebug(string msg, int actorNumber = 0)
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        msg = localActor.ToString() + ": " + msg + "  [" + DateTime.Now.ToString() + "]";
        if (actorNumber == 0)
            photonView.RPC("MostraMsgDebug", RpcTarget.All, msg);
        else if (actorNumber == localActor)
        {
            _pnDebug.SetActive(true);
            _msgDebug.GetComponent<Text>().text = msg;
        }
    }

    [PunRPC]
    private void MostraMsgDebug(string msg)
    {
        _pnDebug.SetActive(true);
        _msgDebug.GetComponent<Text>().text = msg;
    }

    [PunRPC]
    private void MostraMsg(string msg, int quem, bool paraBot)
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        if (quem == 0 || quem == localActor || (quem == 99 && localActor != 1))
            _msg.GetComponent<Text>().text = msg;
        if (paraBot)
        {
            BotManager.Instancia.jogando = false;
        }
    }
    [PunRPC]
    private void MostraMsgQuemJoga(string msg, int quem)
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        if (quem == 0 || quem == localActor || (quem == 99 && localActor != 1))
            _msgQuadro.GetComponent<Text>().text = msg;
    }

    [PunRPC]
    public void MostraQdeMonte(string qdeCartas)
    {
        if (string.IsNullOrEmpty(qdeCartas))
            _txtQdeMonte.GetComponent<Text>().text = GetCartasNoMonte().ToString();
        else
            _txtQdeMonte.GetComponent<Text>().text = qdeCartas;
    }

    public void FogosOn()
    {
        photonView.RPC("FogosOnRPC", RpcTarget.All);
    }
    [PunRPC]
    private void FogosOnRPC()
    {
        Fogos.Instancia.Play();
    }

    #endregion Mensagens

    #region GET
    public int GetLocalActor()
    {
        return (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
    }
    public bool IsBot(int localActor = 0)
    {
        if (localActor == 0)
            localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        if (localActor > 4)
            return false;
        if (GameManager.Instancia.ZoomOn)
            return false;
        var jogador = GetJogadorObjeto(localActor);
        if (jogador != null)
        {
            return GetJogadorObjeto().GetComponent<Jogador>().Bot;
        }
        else
            return false;
    }
    public CartaJogo GetCarta(int idCarta)
    {
        return _mapaJogo.cartasJogo[idCarta];
    }
    public int GetJogadorAtual()
    {
        int ret = GestorDeRede.Instancia.SeqJogadores[_mapaJogo.seqJogadorAtual - 1];
        return ret;
    }
    public string GetNomeDupla(int idDupla, bool abreviado = false)
    {
        string nome = "";
        Tuple<int, int> dupla;
        if (idDupla == 1)
            dupla = GestorDeRede.Instancia.Dupla01;
        else
            dupla = GestorDeRede.Instancia.Dupla02;

        if (dupla.Item1 == 1)
            nome = GameCardsManager.Instancia.GetNome(1, abreviado);
        else if (dupla.Item1 == 2)
            nome = GameCardsManager.Instancia.GetNome(2, abreviado);
        else if (dupla.Item1 == 3)
            nome = GameCardsManager.Instancia.GetNome(3, abreviado);
        else if (dupla.Item1 == 4)
            nome = GameCardsManager.Instancia.GetNome(4, abreviado);

        if (dupla.Item2 == 1)
            nome += GameCardsManager.Instancia.GetNome(1, abreviado) == "" ? "" : "/" + GameCardsManager.Instancia.GetNome(1, abreviado);
        else if (dupla.Item2 == 2)
            nome += GameCardsManager.Instancia.GetNome(2, abreviado) == "" ? "" : "/" + GameCardsManager.Instancia.GetNome(2, abreviado);
        else if (dupla.Item2 == 3)
            nome += GameCardsManager.Instancia.GetNome(3, abreviado) == "" ? "" : "/" + GameCardsManager.Instancia.GetNome(3, abreviado);
        else if (dupla.Item2 == 4)
            nome += GameCardsManager.Instancia.GetNome(4, abreviado) == "" ? "" : "/" + GameCardsManager.Instancia.GetNome(4, abreviado);

        return nome;
    }

    public int GetDupla(int actorNumber = 0)
    {
        if (actorNumber == 0)
            actorNumber = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];

        if (actorNumber > 4)
            return 0;

        int ret;
        if (actorNumber == GestorDeRede.Instancia.Dupla01.Item1 || actorNumber == GestorDeRede.Instancia.Dupla01.Item2)
            ret = 1;
        else
            ret = 2;
        return ret;
    }
    public string GetNome(int actorNumber, bool abreviado = false)
    {
        if (actorNumber == 0)
            actorNumber = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        string nome = "";
        int nQdePlayer = PhotonNetwork.PlayerList.Length;
        for (int i = 0; i < nQdePlayer; i++)
        {
            if (PhotonNetwork.PlayerList[i].ActorNumber == actorNumber)
                nome = PhotonNetwork.PlayerList[i].NickName;
        }
        if (abreviado)
            nome = (nome + ".....").Substring(0, 4);
        return nome.Trim();
    }
    public bool GetJogadaFinalizada()
    {
        return _jogadaFinalizada;
    }

    public bool GetJogadorPegouMorto(int actorNumber)
    {
        if (actorNumber > 4)
            return false;
        bool ret = _mapaJogo.pegouMorto[actorNumber - 1];
        return ret;
    }
    public bool GetPegouMorto(int actorNumber = 0)
    {
        if (actorNumber == 0)
            actorNumber = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        if (actorNumber > 4)
            return false;
        Tuple<int, int> dupla;
        if (GetDupla(actorNumber) == 1)
        {
            dupla = GestorDeRede.Instancia.Dupla01;
        }
        else
        {
            dupla = GestorDeRede.Instancia.Dupla02;
        }
        bool ret1 = _mapaJogo.pegouMorto[dupla.Item1 - 1];
        bool ret2 = _mapaJogo.pegouMorto[dupla.Item2 - 1];
        return (ret1 || ret2);
    }

    public int GetCartasNoMonte()
    {
        return _mapaJogo.cartasJogo.Count(x => x.Portador == "MONTE");
    }

    public int GetCartasNaMao()
    {
        string jogadorTag = GameCardsManager.Instancia.GetJogador();
        return _mapaJogo.cartasJogo.Count(x => x.Portador == jogadorTag);
    }

    public int GetCartasNoLixo()
    {
        string jogadorTag = GameCardsManager.Instancia.GetJogador();
        return _mapaJogo.cartasJogo.Count(x => x.Portador == "LIXO");
    }

    public bool GetTemCanastraLimpa(int actorNumber)
    {
        if (actorNumber > 4)
            return true;

        if (actorNumber == 0)
            actorNumber = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];

        bool ret = false;
        string portador;
        if (GetDupla(actorNumber) == 1)
            portador = "AREA01";
        else
            portador = "AREA02";
        //AREA01-01S
        int id = 0, ct = 0;
        GameCardsManager.Instancia.GetListaCartasJogo().Where(x => x.Portador.Contains(portador)).OrderBy(x => x.Portador).ToList().
        ForEach(carta =>
        {
            if (carta.Portador.Length == 9) // limpo AREAjj-nn (limpo) / AREAjj-nnS (sujo)
            {
                int idAux = Convert.ToInt32(carta.Portador.Substring(7, 2));
                if (idAux != id)
                {
                    id = idAux;
                    ct = 1;
                }
                else
                    ct++;
                if (ct == 7) // canastra
                {
                    ret = true;
                }
            }
        });

        return ret;
    }

    public int GetPontuacaoCanastras(int actorNumber)
    {
        if (actorNumber > 4)
            return 0;

        if (actorNumber == 0)
            actorNumber = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];

        string portador;
        if (GetDupla(actorNumber) == 1)
            portador = "AREA01";
        else
            portador = "AREA02";
        //AREA01-01S
        int ct = 0, pontos = 0;
        string qPortador = "xxx";
        bool sujo = false;
        int AS = 0;
        GameCardsManager.Instancia.GetListaCartasJogo().Where(x => x.Portador.Contains(portador)).OrderBy(x => x.Portador).ToList().
        ForEach(carta =>
        {
            if (carta.Portador != qPortador)
            {
                if (carta.Portador.Contains("S")) //AREA01-01 / AREA01-01S
                    sujo = true;
                else
                    sujo = false;
                qPortador = carta.Portador;
                ct = 1;
                AS = 0;
            }
            else
                ct++;
            if (carta.Valor == 1 || carta.Valor == 14)
                AS++;
            if (ct == 7) // canastra
            {
                if (AS > 3) // jogo de AS
                    pontos += sujo ? 250 : 500;
                else
                    pontos += sujo ? 100 : 200;
            }
            else if (ct == 14)
            {
                if (sujo)
                {
                    pontos -= 100;
                    pontos += 500;
                }
                else
                {
                    pontos -= 200;
                    pontos += 1000;
                }
            }
        });
        return pontos;
    }

    public bool GetTemJogadaLimpa(int actorNumber)
    {
        if (actorNumber > 4)
            return true;

        if (actorNumber == 0)
            actorNumber = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];

        string portador;
        if (GetDupla(actorNumber) == 1)
            portador = "AREA01";
        else
            portador = "AREA02";
        //AREA01-01S
        int id = 0;
        bool sujo = true;
        GameCardsManager.Instancia.GetListaCartasJogo().Where(x => x.Portador.Contains(portador)).OrderBy(x => x.Portador).ToList().
        ForEach(carta =>
        {
            int idAux = Convert.ToInt32(carta.Portador.Substring(7, 2));
            if (idAux != id)
            {
                if (!carta.Portador.Contains("S")) sujo = false;
                id = idAux;
            }
        });
        return !sujo;
    }

    public int GetPontuacaoArea(int actorNumber)
    {
        if (actorNumber > 4)
            return 0;

        int ret = 0;
        string portador;
        if (GetDupla(actorNumber) == 1)
            portador = "AREA01";
        else
            portador = "AREA02";

        GameCardsManager.Instancia.GetListaCartasJogo().Where(x => x.Portador != "MONTE").ToList().
        ForEach(carta =>
        {
            // pontuação das cartas
            if (carta.Portador.Contains(portador)) ret += carta.Pontos;
        });
        return ret;
    }

    public bool GetJaBaixou(int actorNumber)
    {
        if (actorNumber > 4)
            return true;

        if (actorNumber == 0)
            actorNumber = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];

        bool ret = _mapaJogo.baixou[actorNumber - 1];
        return ret;
    }

    public GameObject GetJogadorObjeto(int actorNumber = 0)
    {
        if (actorNumber > 4)
            return null; // actorNumber = 1; // revisar
        string jogadorTag = GameCardsManager.Instancia.GetJogador(actorNumber);
        if (jogadorTag == "")
            return null;
        GameObject.FindGameObjectWithTag(jogadorTag);
        return GameObject.FindGameObjectWithTag(jogadorTag);
    }

    public bool GetMinhaVez()
    {
        return (GetJogadorAtual() == (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"]);
    }

    public int GetQdeCartaPortador(string portador)
    {
        return _mapaJogo.cartasJogo.Count(x => x.Portador.Contains(portador));
    }


    public Tuple<int, int> GetAdversarios(int actorNumber = 0)
    {
        if (actorNumber == 0)
            actorNumber = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        if (GestorDeRede.Instancia.Dupla01.Item1 == actorNumber || GestorDeRede.Instancia.Dupla01.Item2 == actorNumber)
        {
            return GestorDeRede.Instancia.Dupla02;
        }
        else
        {
            return GestorDeRede.Instancia.Dupla01;
        }
    }

    public int GetMeuParceiro(int actorNumber = -1)
    {
        int ret = 0;

        if (actorNumber == -1)
            actorNumber = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];

        if (GestorDeRede.Instancia.Dupla01.Item1 == actorNumber)
            ret = GestorDeRede.Instancia.Dupla01.Item2;
        if (GestorDeRede.Instancia.Dupla01.Item2 == actorNumber)
            ret = GestorDeRede.Instancia.Dupla01.Item1;

        if (GestorDeRede.Instancia.Dupla02.Item1 == actorNumber)
            ret = GestorDeRede.Instancia.Dupla02.Item2;
        if (GestorDeRede.Instancia.Dupla02.Item2 == actorNumber)
            ret = GestorDeRede.Instancia.Dupla02.Item1;

        return ret;
    }

    public string GetJogador(int actorNumber = 0)
    {
        if (actorNumber == 0)
            actorNumber = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"]; // PhotonNetwork.LocalPlayer.ActorNumber;
        if (actorNumber > 4)
            return "";
        return "JOGADOR" + actorNumber.ToString().Trim().PadLeft(2, '0');
    }

    public string GetPortador(int idCarta)
    {
        return _mapaJogo.cartasJogo[idCarta].Portador;
    }

    public int GetValorCarta(int idCarta)
    {
        return _mapaJogo.cartasJogo[idCarta].Valor;
    }

    /// <summary>
    /// Retorna valor pelo peso da carta
    /// </summary>
    /// <param name="idCarta"></param>
    /// <returns></returns>
    public int GetValorPeso(int idCarta)
    {
        int peso = _mapaJogo.cartasJogo[idCarta].Peso;
        int valor = Convert.ToInt32(peso.ToString().Substring(1, 2));
        if (valor < 1 || valor > 14)
            valor = _mapaJogo.cartasJogo[idCarta].Valor;
        return valor;
    }

    public int GetNaipe(int idCarta)
    {
        return _mapaJogo.cartasJogo[idCarta].Naipe;
    }

    public int GetNaipeLista(List<int> cartas)
    {
        int naipe = 0;
        cartas.ForEach(id =>
        {
            if (GetValorCarta(id) != 2 && GetValorCarta(id) != 99 && naipe == 0)
                naipe = GetNaipe(id);
        });

        return naipe;
    }
    public int GetPesoCarta(int idCarta)
    {
        return _mapaJogo.cartasJogo[idCarta].Peso;
    }

    public int GetPesoCartaGer(int valor, int naipe)
    {
        int peso = naipe * 1000 + valor * 10;
        return peso;
    }

    /// <summary>
    /// Busca primeiro ator da dupla
    /// </summary>
    /// <param name="dupla"></param>
    /// <returns></returns>
    public int GetActorDupla(int dupla)
    {
        if (GetDupla(1) == dupla)
            return 1;
        else if (GetDupla(2) == dupla)
            return 2;
        else if (GetDupla(3) == dupla)
            return 3;
        else if (GetDupla(4) == dupla)
            return 4;
        return 1;
    }
    public int GetVulPontos(int actorNumber)
    {
        int ret;
        if (actorNumber > 4)
            ret = 0;
        else
            ret = _mapaJogo.Vul[actorNumber - 1];
        return ret;
    }

    public bool GetEhLixo(int idCarta)
    {
        return _mapaJogo.cartasJogo[idCarta].Lixo;
    }

    public bool GetEhNeutro2(int idCarta)
    {
        return _mapaJogo.cartasJogo[idCarta].Neutro2;
    }

    public bool GetEhCoringa(int idCarta)
    {
        return _mapaJogo.cartasJogo[idCarta].Coringa;
    }

    public bool GetEhProvisorio(int idCarta)
    {
        return _mapaJogo.cartasJogo[idCarta].Provisorio;
    }

    public string GetTipoDaJogada(List<CartaJogo> cartas)
    {
        string ret = "";

        bool sujo = cartas[0].Portador.Contains("S");
        bool AS = cartas.Count(x => x.Valor == 1 || x.Valor == 14) > 4;

        if (cartas.Count >= 7 && cartas.Count != 14) // Canastra
        {
            if (sujo)
            {
                if (AS)
                    ret = "AS"; // AS Sujo
                else
                    ret = "CS"; // Canastra Suja
            }
            else
            {
                if (AS)
                    ret = "AL"; // AS Limpo
                else
                    ret = "CL"; // Canastra Limpa
            }
        }
        else if (cartas.Count == 14)
        {
            if (sujo)
                ret = "RS"; // Real Sujo
            else
                ret = "RL"; // Real Limpo
        }
        return ret;
    }

    public bool GetEhFimRodada()
    {
        return _mapaJogo.fimRodada;
    }

    public Tuple<string, string> GetPlacarDet()
    {
        return new Tuple<string, string>(GestorDeRede.Instancia.Placar01Det, GestorDeRede.Instancia.Placar02Det);
    }
    public int GetPontosJogo(int actorNumber)
    {
        if (actorNumber > 4)
            return 0;
        else
        {
            if (GetDupla(actorNumber) == 1)
                return GestorDeRede.Instancia.Placar01;
            else
                return GestorDeRede.Instancia.Placar02;
        }
    }

    public Tuple<int, int> GetPlacar()
    {
        return new Tuple<int, int>(GestorDeRede.Instancia.Placar01, GestorDeRede.Instancia.Placar02);
    }

    public string GetVerCartaDebug(int idCarta)
    {
        CartaJogo carta = _mapaJogo.cartasJogo[idCarta];
        string ret = "Id=" + idCarta.ToString() + "/Pt:" + carta.Portador + "/Np:" + carta.Naipe.ToString() + "/Vl:" + carta.Valor.ToString() + "\n";
        ret += "Ps:" + carta.Peso.ToString() + "/Co:" + GetEhCoringa(idCarta).ToString() + "/Pr:" + GetEhProvisorio(idCarta).ToString() + "/Ne:" + GetEhNeutro2(idCarta).ToString();
        return ret;
    }

    public List<CartaJogo> GetListaCartasJogo(string portador = "")
    {
        if (portador == "")
            return _mapaJogo.cartasJogo;
        else
            return _mapaJogo.cartasJogo.Where(x => x.Portador.Contains(portador)).ToList();
    }

    public List<CartaJogo> GetListaClone(string portador = "")
    {
        List<CartaJogo> ret = new List<CartaJogo>();
        if (portador == "")
            _mapaJogo.cartasJogo.ForEach(item =>
            {
                CartaJogo novoItem = new CartaJogo();
                novoItem = item;
                novoItem.BotTag = "";
                novoItem.PesoGen = 0;
                ret.Add(novoItem);
            });
        else
            _mapaJogo.cartasJogo.Where(x => x.Portador.Contains(portador)).ToList().ForEach(item =>
            {
                CartaJogo novoItem = new CartaJogo();
                novoItem = item;
                novoItem.BotTag = "";
                novoItem.PesoGen = 0;
                ret.Add(novoItem);
            });
        return ret;
    }


    public int GetCartaId(int index)
    {
        if (index >= 0 && index < _mapaJogo.cartasJogo.Count)
            return _mapaJogo.cartasJogo[index].Id;
        else
            return -1;
    }

    public void GetLixo()
    {
        Baralho.Instancia.LimparSelecionados();
        _mapaJogo.cartasJogo.Where(x => x.Portador == "LIXO").ToList().
        ForEach(item =>
        {
            Baralho.Instancia.Selecionar(item.Id);
        });
    }
    public int GetActorAtPos(int posicao)
    {
        int ret = 0;
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        if (localActor > 4)
            localActor = GestorDeRede.Instancia.SoVerNumber; // ok
        int euInd = GestorDeRede.Instancia.SeqJogadores.ToList().FindIndex(x => x == localActor);
        int nQdePlayer = PhotonNetwork.PlayerList.Length;
        if (nQdePlayer > 4) nQdePlayer = 4;
        int i;
        for (i = 1; i <= nQdePlayer; i++)
        {
            euInd++;
            if (euInd > nQdePlayer)
                euInd = 1;
            if (i == posicao)
                break;
        }
        ret = GestorDeRede.Instancia.SeqJogadores[euInd - 1];
        return ret;
    }

    public bool GetNovaJogada(int idCarta)
    {
        return _mapaJogo.cartasJogo[idCarta].NovoJogada;
    }
    #endregion GET

    #region SET
    public void SetJogadaFinalizada(bool valor)
    {
        GestorDeRede.Instancia.SetPrimeiraJogada(false);
        photonView.RPC("SetJogadaFinalizadaRPC", RpcTarget.All, valor);
    }
    [PunRPC]
    private void SetJogadaFinalizadaRPC(bool valor)
    {
        _jogadaFinalizada = valor;
    }

    public void SetBaixou(int actorNumber, bool valor = true)
    {
        if (actorNumber > 4)
            return;
        photonView.RPC("SetBaixouRPC", RpcTarget.All, actorNumber, valor);
    }
    [PunRPC]
    private void SetBaixouRPC(int actorNumber, bool valor)
    {
        if (actorNumber > 4)
            return;
        _mapaJogo.baixou[actorNumber - 1] = valor;
    }

    public void SetReorganizaMao(bool RPC = true)
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"]; // 
        if (localActor > 4)
            return;
        GameCardsManager.Instancia.MsgMsg("", 0);
        SoundManager.Instancia.PlaySound("reorganizar");
        string portador = GetJogador();
        if (RPC)
            photonView.RPC("SetReorganizaMaoRPC", RpcTarget.All, portador);
        else
        {
            SetReorganizaMaoCPL(portador);
            return;
        }

        if (GetListaCartasJogo().FindIndex(x => x.SeqFixo != 9000 && x.Portador == portador) != -1)
        {
            List<CartaJogo> listaAux = new List<CartaJogo>();
            _mapaJogo.cartasJogo.Where(x => x.Portador == portador).OrderBy(x => x.Seq).ToList()
            .ForEach(item =>
            {
                if (item.SeqFixo != 9000)
                    item.Seq = item.SeqFixo;
                listaAux.Add(item);
            });
            int ctCartas = listaAux.Count();
            for (int iSeq = 0; iSeq < ctCartas; iSeq++)
            {
                int indAux1 = listaAux.FindIndex(x => x.SeqFixo == iSeq && x.Portador == portador);
                int indAux2 = listaAux.FindIndex(x => x.SeqFixo == 9000 && x.Portador == portador);
                if (indAux1 != -1)
                {
                    SetSeqCarta(listaAux[indAux1].Id, iSeq, true);
                }
                else if (indAux2 != -1)
                {
                    SetSeqCarta(listaAux[indAux2].Id, iSeq, true);
                    listaAux[indAux2].SeqFixo = -1;
                }
            }
        }

        Baralho.Instancia.LimparSelecionados();
        GameObject jogador = GameObject.FindGameObjectWithTag(portador);
        jogador.GetComponent<Jogador>().RemoverCarta(0, -1, ""); // redraw jogador
    }
    [PunRPC]
    private void SetReorganizaMaoRPC(string portador)
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"]; // 
        SetReorganizaMaoCPL(portador);
        FrontManager.Instancia.RedrawOthers(localActor);
    }
    public void SetReorganizaMaoCPL(string portador)
    {
        int seq = 0;
        _mapaJogo.cartasJogo.Where(x => x.Portador == portador).OrderBy(x => x.Peso).ToList()
        .ForEach(item =>
        {
            item.Seq = seq;
            seq++;
        });
    }

    public void SetTrocaPosicao(int idCarta)
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"]; // 
        if (_mapaJogo.cartasJogo[idCarta].SeqFixo != 9000)
        {
            _mapaJogo.cartasJogo[idCarta].SeqFixo = 9000;
            Baralho.Instancia.cartas[idCarta].GetComponent<Carta>().Cor = GestorDeRede.Instancia.GetCor("padrao");  //Color.white;
        }

        string jogadorTag = GameCardsManager.Instancia.GetJogador();
        int SeqAlvo = GetListaCartasJogo().Find(x => x.Id == idCarta).Seq;
        for (int ind = Baralho.Instancia.cartasSel.Count - 1; ind >= 0; ind--)
        {
            int idCartaAux = Baralho.Instancia.cartasSel[ind];
            var carta = GetListaCartasJogo().Find(x => x.Id == idCartaAux);
            int SeqCarta = carta.Seq;
            if (SeqAlvo < SeqCarta)
            {
                for (int i = SeqCarta - 1; i > SeqAlvo; i--)
                {
                    var cartaAux = GetListaCartasJogo().Find(x => x.Seq == i && x.Portador == jogadorTag);
                    cartaAux.Seq = i + 1;
                }
                carta.Seq = SeqAlvo + 1;
            }
            if (SeqAlvo > SeqCarta)
            {
                for (int i = SeqCarta; i <= SeqAlvo - 1; i++)
                {
                    var cartaAux = GetListaCartasJogo().FindLast(x => x.Seq == i + 1 && x.Portador == jogadorTag);
                    cartaAux.Seq = i;
                }
                carta.Seq = SeqAlvo;
            }
        };
        Baralho.Instancia.LimparSelecionados();
        FrontManager.Instancia.RedrawJogador(localActor, false);
    }

    public void SetCartasJogador(int idCarta, string portador, bool novoNaMao, int seq)
    {
        photonView.RPC("SetCartasJogadorRPC", RpcTarget.All, idCarta, portador, novoNaMao, seq);
    }
    [PunRPC]
    private void SetCartasJogadorRPC(int idCarta, string portador, bool novoNaMao, int seq)
    {
        _mapaJogo.cartasJogo[idCarta].Portador = portador;
        _mapaJogo.cartasJogo[idCarta].NovoNaMao = novoNaMao;
        _mapaJogo.cartasJogo[idCarta].NovoJogada = novoNaMao;
        _mapaJogo.cartasJogo[idCarta].Seq = seq;
    }

    public void SetSeqCarta(int idCarta, int seq, bool local = false)
    {
        if (local)
            _mapaJogo.cartasJogo[idCarta].Seq = seq;
        else
            photonView.RPC("SetSeqCartaRPC", RpcTarget.All, idCarta, seq);
    }
    [PunRPC]
    private void SetSeqCartaRPC(int idCarta, int seq)
    {
        _mapaJogo.cartasJogo[idCarta].Seq = seq;
    }

    public void SetSeqFixoCarta(int idCarta, int seq, bool local = false)
    {
        if (local)
            _mapaJogo.cartasJogo[idCarta].SeqFixo = seq;
        else
            photonView.RPC("SetSeqFixoCartaRPC", RpcTarget.All, idCarta, seq);
    }
    [PunRPC]
    private void SetSeqFixoCartaRPC(int idCarta, int seq)
    {
        _mapaJogo.cartasJogo[idCarta].SeqFixo = seq;
    }

    public void SetSeqLixo()
    {
        photonView.RPC("SetSeqLixoRPC", RpcTarget.All);
    }
    [PunRPC]
    private void SetSeqLixoRPC()
    {
        int seq = 2;
        _mapaJogo.cartasJogo.Where(x => x.Portador == "LIXO").OrderBy(x => x.Seq).ToList()
           .ForEach(carta =>
           {
               carta.Seq = seq;
               seq++;
           });
    }

    public void SetFimRodada()
    {
        photonView.RPC("SetFimRodadaRPC", RpcTarget.All);
    }
    [PunRPC]
    private void SetFimRodadaRPC()
    {
        _mapaJogo.fimRodada = true;
        MsgPlacar();
        var placar = GetPlacar();
        if (placar.Item1 < 3000 && placar.Item2 < 3000)
        {
            if (GameCardsManager.Instancia.IsBot() && GestorDeRede.Instancia.DonoDaSala())
            {
                GestorDeRede.Instancia.ComecaJogo("Principal", false);
            }
        }
    }

    public void SetValor(int idCarta, int valor)
    {
        photonView.RPC("SetValorRPC", RpcTarget.All, idCarta, valor);
    }
    [PunRPC]
    private void SetValorRPC(int idCarta, int valor)
    {
        _mapaJogo.cartasJogo[idCarta].Valor = valor;
    }

    public void SetPeso(int idCarta, int peso)
    {
        photonView.RPC("SetPesoRPC", RpcTarget.All, idCarta, peso);
    }
    [PunRPC]
    private void SetPesoRPC(int idCarta, int peso)
    {
        _mapaJogo.cartasJogo[idCarta].Peso = peso;
    }

    public void SetPesoOriginal(int idCarta)
    {
        photonView.RPC("SetPesoOriginalRPC", RpcTarget.All, idCarta);
    }
    [PunRPC]
    private void SetPesoOriginalRPC(int idCarta)
    {
        _mapaJogo.cartasJogo[idCarta].Peso = _mapaJogo.cartasJogo[idCarta].Naipe * 1000 + _mapaJogo.cartasJogo[idCarta].Valor * 10;
    }


    public void SetTrocaPortador(string portadorAtual, string portadorNovo)
    {
        photonView.RPC("SetTrocaPortadorRPC", RpcTarget.All, portadorAtual, portadorNovo);
    }
    [PunRPC]
    private void SetTrocaPortadorRPC(string portadorAtual, string portadorNovo)
    {
        _mapaJogo.cartasJogo.Where(x => x.Portador.Contains(portadorAtual)).ToList().
        ForEach(item =>
        {
            item.Portador = portadorNovo;
        });
    }

    public void SetPortador(int idCarta, string portador)
    {
        photonView.RPC("SetPortadorRPC", RpcTarget.All, idCarta, portador);
    }
    [PunRPC]
    private void SetPortadorRPC(int idCarta, string portador)
    {
        Vector3 scale = Baralho.Instancia.scale;
        Baralho.Instancia.cartas[idCarta].transform.localScale = scale;
        _mapaJogo.cartasJogo[idCarta].Portador = portador;
        if (_mapaJogo.cartasJogo[idCarta].SeqFixo != 9000)
        {
            _mapaJogo.cartasJogo[idCarta].SeqFixo = 9000;
            Baralho.Instancia.cartas[idCarta].GetComponent<Carta>().Cor = GestorDeRede.Instancia.GetCor("padrao");  //Color.white;
        }
    }

    public void SetCoringa(int idCarta, bool valor)
    {
        photonView.RPC("SetCoringaRPC", RpcTarget.All, idCarta, valor);
    }
    [PunRPC]
    private void SetCoringaRPC(int idCarta, bool valor)
    {
        _mapaJogo.cartasJogo[idCarta].Coringa = valor;
    }

    public void SetCoringaProvisorio(int idCarta, bool valor)
    {
        photonView.RPC("SetCoringaProvisorioRPC", RpcTarget.All, idCarta, valor);
    }
    [PunRPC]
    private void SetCoringaProvisorioRPC(int idCarta, bool valor)
    {
        _mapaJogo.cartasJogo[idCarta].Provisorio = valor;
    }

    public void SetNeutro2(int idCarta, bool valor)
    {
        photonView.RPC("SetNeutro2RPC", RpcTarget.All, idCarta, valor);
    }
    [PunRPC]
    private void SetNeutro2RPC(int idCarta, bool valor)
    {
        _mapaJogo.cartasJogo[idCarta].Neutro2 = valor;
    }
    public void SetLixo(int idCarta, bool valor)
    {
        photonView.RPC("SetLixoRPC", RpcTarget.All, idCarta, valor);
    }
    [PunRPC]
    private void SetLixoRPC(int idCarta, bool valor)
    {
        _mapaJogo.cartasJogo[idCarta].Lixo = valor;
    }

    public void SetVul(int actorNumber, int pontos)
    {
        if (actorNumber > 4)
            return;
        photonView.RPC("SetVulRPC", RpcTarget.All, actorNumber, pontos);
    }
    [PunRPC]
    private void SetVulRPC(int actorNumber, int pontos)
    {
        if (actorNumber > 4)
            return;
        _mapaJogo.Vul[actorNumber - 1] = pontos;
    }

    public void SetClearCartasJogo()
    {
        photonView.RPC("SetClearCartasJogoRPC", RpcTarget.All);
    }
    [PunRPC]
    private void SetClearCartasJogoRPC()
    {
        _mapaJogo.cartasJogo.Clear();
    }

    public void SetAddCartasJogo(CartaJogo carta)
    {
        SetAddCartasJogoCPL(carta);
    }
    private void SetAddCartasJogoCPL(CartaJogo carta)
    {
        if (_mapaJogo.cartasJogo.FindIndex(x => x.Id == carta.Id) == -1)
            _mapaJogo.cartasJogo.Add(carta);
    }

    public void SetPegueiMorto(int actorNumber)
    {
        if (actorNumber > 4)
            return;
        photonView.RPC("SetPegueiMortoRPC", RpcTarget.All, actorNumber);
    }
    [PunRPC]
    private void SetPegueiMortoRPC(int actorNumber)
    {
        if (actorNumber > 4)
            return;
        SoundManager.Instancia.PlaySound("morto");
        _mapaJogo.pegouMorto[actorNumber - 1] = true;
        _mapaJogo.baixou[actorNumber - 1] = false;
    }

    public void SetMortoParaMonte(int ind)
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        if (localActor > 4)
            return;
        string morto = "MORTO" + ind.ToString().PadLeft(2, '0');
        photonView.RPC("SetMortoParaMonteRPC", RpcTarget.All, morto);
        photonView.RPC("MostraQdeMonte", RpcTarget.All, "");
    }
    [PunRPC]
    private void SetMortoParaMonteRPC(string portador)
    {
        // Jogar o Morto no Monte
        int mesa = 0;
        float _xStep = Baralho.Instancia.xStep;
        GameObject baralho = GameObject.FindGameObjectWithTag("BARALHO");
        Vector3 objPos;
        GameCardsManager.Instancia.GetListaCartasJogo().Where(x => x.Portador == portador).ToList()
        .ForEach(cartaItem =>
        {
            objPos = new Vector3(baralho.transform.position.x + mesa * _xStep, baralho.transform.position.y, 0);
            Baralho.Instancia.cartas[cartaItem.Id].transform.position = objPos;
            cartaItem.Portador = "MONTE";
            mesa++;
        });
    }
    public void SetCor(int idCarta, float R, float G, float B, bool imagem, bool jogada = false)
    {
        if (!imagem)
            photonView.RPC("SetCorRPC", RpcTarget.All, idCarta, R, G, B, jogada);
        else
            photonView.RPC("SetCorImgRPC", RpcTarget.All, idCarta, R, G, B, jogada);
    }

    [PunRPC]
    private void SetCorRPC(int idCarta, float R, float G, float B, bool jogada)
    {
        // RGB (0.984f, 0.964f, 0.674f);
        GameObject carta = Baralho.Instancia.cartas.Find(x => x.GetComponent<Carta>().Id == idCarta);
        carta.GetComponent<Carta>().Cor = new Color(R, G, B);
        if (jogada)
            carta.GetComponent<Carta>().CorJogada = new Color(R, G, B);
    }
    [PunRPC]
    private void SetCorImgRPC(int idCarta, float R, float G, float B, bool jogada)
    {
        // RGB (0.984f, 0.964f, 0.674f);
        GameObject carta = Baralho.Instancia.cartas.Find(x => x.GetComponent<Carta>().Id == idCarta);
        carta.GetComponent<Image>().color = new Color(R, G, B);
        if (jogada)
            carta.GetComponent<Carta>().CorJogada = new Color(R, G, B);
    }

    public void SetColorArea(int actorNumber)
    {
        if (actorNumber > 4)
            return;
        photonView.RPC("SetColorAreaRPC", RpcTarget.All, actorNumber);
    }
    [PunRPC]
    public void SetColorAreaRPC(int actorNumber)
    {
        if (actorNumber > 4)
            return;
        string area = "AREA" + GetDupla(actorNumber).ToString().PadLeft(2, '0');
        _mapaJogo.cartasJogo.Where(x => x.Portador.Contains(area)).ToList().
        ForEach(item =>
        {
            var carta = Baralho.Instancia.cartas.Find(x => x.GetComponent<Carta>().Id == item.Id);
            carta.GetComponent<Carta>().CorJogada = carta.GetComponent<Carta>().Cor;
            carta.GetComponent<Image>().color = carta.GetComponent<Carta>().Cor;
            _mapaJogo.cartasJogo[item.Id].NovoJogada = false;
        });
    }

    public void SetNovaJogada(int idCarta, bool valor)
    {
        photonView.RPC("SetNovaJogadaRPC", RpcTarget.All, idCarta, valor);
    }
    [PunRPC]
    private void SetNovaJogadaRPC(int idCarta, bool valor)
    {
        _mapaJogo.cartasJogo[idCarta].NovoJogada = valor;
    }

    public void SetVisible(int idCarta, bool valor)
    {
        Baralho.Instancia.cartas[idCarta].GetComponentInChildren<Image>().enabled = valor;
    }

    public void SetHumor()
    {
        int actorOrigem = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        // alterar avatar
        var jogador = this.GetJogadorObjeto(actorOrigem);
        string avatar = GestorDeRede.Instancia.GetAvatar(actorOrigem, false);
        int number = jogador.GetComponent<Jogador>().AvatarNumber;
        number++;
        string numAux = "";
        if (number > 0)
        {
            numAux = "_" + number.ToString().PadLeft(2, '0');
        }
        if (Resources.Load<Sprite>(avatar + numAux) != null)
        {
            jogador.GetComponent<Jogador>().AvatarNumber++;
        }
        else
        {
            jogador.GetComponent<Jogador>().AvatarNumber = 0;
            numAux = "";
        }
        string newAvatar = avatar + numAux;
        jogador.GetComponent<Image>().sprite = Resources.Load<Sprite>(newAvatar);
    }

    private void Humor(int actor)
    {
        var jogador = this.GetJogadorObjeto(actor);
        string avatar = GestorDeRede.Instancia.GetAvatar(actor, false);
        int number = jogador.GetComponent<Jogador>().AvatarNumber;
        //number++;
        string numAux = "";
        if (number > 0)
        {
            numAux = "_" + number.ToString().PadLeft(2, '0');
        }
        //if (Resources.Load<Sprite>(avatar + numAux) != null)
        //{
        //    jogador.GetComponent<Jogador>().AvatarNumber++;
        //}
        //else
        //{
        //    jogador.GetComponent<Jogador>().AvatarNumber = 0;
        //    numAux = "";
        //}
        string newAvatar = avatar + numAux;
        jogador.GetComponent<Image>().sprite = Resources.Load<Sprite>(newAvatar);
        photonView.RPC("HumorRPC", RpcTarget.All, actor, newAvatar);
    }
    [PunRPC]
    private void HumorRPC(int actor, string avatar)
    {
        if (GameManager.Instancia.ZoomOn)
            return;
        var jogador = this.GetJogadorObjeto(actor);
        if (Resources.Load<Sprite>(avatar) != null)
        {
            jogador.GetComponent<Image>().sprite = Resources.Load<Sprite>(avatar);
            SoundManager.Instancia.PlaySound("humor");
            MoverManager.Instancia.MoverHumor(actor, avatar);
        }
    }

    public void SetCutucar(int actorPosicao, int actorNumberAuto, bool self = false)
    {
        int actorOrigem = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        string nomeOrigem = PhotonNetwork.LocalPlayer.NickName;
        if (actorPosicao == 1 && actorOrigem <= 4)
        {
            if (actorOrigem <= 4)
            {
                // auto cutucou
                this.Humor(actorOrigem);
            }
            return;
        }
        int actorNumber = GetActorAtPos(actorPosicao);
        if (actorNumberAuto != 0)
            actorNumber = actorNumberAuto;
        if (GameManager.Instancia.PodeCutucar(-1))
        {
            photonView.RPC("SetCutucarRPC", RpcTarget.All, nomeOrigem, actorNumber, actorOrigem);
        }
        else
        {
            int ret = UnityEngine.Random.Range(1, 6);
            string msg = ret switch
            {
                1 => "Pessoal já cansou das cutucadas!",
                2 => "Chega de cutucadas!",
                3 => "Zerou suas cutucadas!",
                4 => "Agora espere alguém te cutucar!",
                _ => "Você está proibido de cutucar!",
            };
            GameManager.Instancia.MostraMsgMain(msg, false, "msg", false, 0);
        }
    }
    [PunRPC]
    private void SetCutucarRPC(string nomeOrigem, int actorNumber, int actorOrigem)
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        if (localActor == actorNumber)
        {
            MoverManager.Instancia.MoverJogador(actorOrigem, actorNumber);
            if (IsBot())
            {
                BotManager.Instancia.Cutucou(actorOrigem, GetNome(actorOrigem));
            }
            string msg = nomeOrigem + " está te cutucando !!!";
            GameManager.Instancia.PodeCutucar(1);
            GameManager.Instancia.MostraMsgMain(msg, true, "cutucar", false, 0, true);
        }
    }

    public void SetEstatistica(int actorNumber, string campo, int valor)
    {
        if (campo == "INICIAR")
        {
            GestorDeRede.Instancia.StatisticUsuario = new List<GameCardsManager.Estatistica>();
            for (int i = 0; i <= 5; i++)
            {
                int avatar = 0;
                if (i > 0 && i <= PhotonNetwork.PlayerList.Length)
                {
                    avatar = GestorDeRede.Instancia.GetAvatarNumber(i);
                }
                GestorDeRede.Instancia.StatisticUsuario.Add(new GameCardsManager.Estatistica()
                {
                    jogador = avatar,
                    cartasBaixadas = 0,
                    cartasLixadas = 0,
                    cartasPuxadas = 0,
                    pontosNaMao = 0

                });
            }
        }
        else
        {
            photonView.RPC("SetEstatisticaRPC", RpcTarget.All, actorNumber, campo, valor);
        }
    }
    [PunRPC]
    private void SetEstatisticaRPC(int actorNumber, string campo, int valor)
    {
        if (campo == "LIXADAS")
            GestorDeRede.Instancia.StatisticUsuario[actorNumber].cartasLixadas += valor;
        else if (campo == "PUXADAS")
            GestorDeRede.Instancia.StatisticUsuario[actorNumber].cartasPuxadas += valor;
        else if (campo == "BAIXADAS")
            GestorDeRede.Instancia.StatisticUsuario[actorNumber].cartasBaixadas += valor;
        else if (campo == "NAMAO")
        {
            int actor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
            string jogador = "JOGADOR" + actor.ToString().Trim().PadLeft(2, '0');
            int pontos = 0;
            GameCardsManager.Instancia.GetListaCartasJogo().Where(x => x.Portador != "MONTE").ToList().
            ForEach(carta =>
            {
                // pontuação das cartas
                if (carta.Portador == jogador) pontos += carta.Pontos;
            });
            GestorDeRede.Instancia.StatisticUsuario[actor].pontosNaMao += pontos;
        }
    }

    #endregion SET

    #region recall

    public void SetGameRecall()
    {
        if ((int)PhotonNetwork.LocalPlayer.CustomProperties["ID"] > 4)
            return;
        if (!GestorDeRede.Instancia.Recall)
        {
            // gravar dados em tabela
            int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
            string saveGame = SetGameRecallCPL(localActor, false);
            if (!string.IsNullOrEmpty(saveGame))
            {
                int sala = Convert.ToInt32(PhotonNetwork.CurrentRoom.Name.Substring(4, 2));
                WWWForm form = new WWWForm();
                form.AddField("sala", sala);
                form.AddField("dados", saveGame);
                form.AddField("jogador", localActor);
                StartCoroutine(Post("/oapi/play/update", form, false, 0, ""));
            }

            photonView.RPC("SetGameRecallRPC", RpcTarget.All);
        }
    }

    [PunRPC]
    private void SetGameRecallRPC()
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        if (localActor > 4)
            return;
        string saveGame = SetGameRecallCPL(localActor, false);
        if (string.IsNullOrEmpty(saveGame))
            return;
        string fileName = Application.persistentDataPath + "/SALA_" + PhotonNetwork.CurrentRoom.Name.ToUpper() + "_" + localActor.ToString().PadLeft(2, '0') + ".json";
        StreamWriter arquivo = new StreamWriter(fileName);
        arquivo.WriteLine(saveGame);
        arquivo.Close();
        if (GestorDeRede.Instancia.CtrlRecallFront)
            GestorDeRede.Instancia.CtrlRecallFront = false;
    }

    private string SetGameRecallCPL(int actorNumber, bool ver)
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        if (localActor > 4)
            return "";
        if (ver)
        {
            if (actorNumber != localActor)
                return "";
        }
        string tag = "JOGADOR" + actorNumber.ToString().Trim().PadLeft(2, '0');
        GameObject jogador = GameObject.FindGameObjectWithTag(tag);
        if (jogador == null)
            return "";
        if (GameManager.Instancia.ZoomOn)
            return "";
        SaveGame SG = new SaveGame
        {
            // GestorDeRedes
            GameIdf = GestorDeRede.Instancia.GameIdf,
            GameRodada = GestorDeRede.Instancia.GameRodada,
            VulOkA = GestorDeRede.Instancia.VulOkA,
            VulOkB = GestorDeRede.Instancia.VulOkB,
            InicialOk = GestorDeRede.Instancia.InicialOk,
            Placar01Det = GestorDeRede.Instancia.Placar01Det,
            Placar02Det = GestorDeRede.Instancia.Placar02Det,
            Placar01 = GestorDeRede.Instancia.Placar01,
            Placar02 = GestorDeRede.Instancia.Placar02,
            PlacarGeral01 = GestorDeRede.Instancia.PlacarGeral01,
            PlacarGeral02 = GestorDeRede.Instancia.PlacarGeral02,
            JogadorInicial = GestorDeRede.Instancia.JogadorInicial,
            PrimeiraJogada = GestorDeRede.Instancia.PrimeiraJogada,
            Dupla01 = GestorDeRede.Instancia.Dupla01,
            Dupla02 = GestorDeRede.Instancia.Dupla02,
            SeqJogadores = GestorDeRede.Instancia.SeqJogadores,
            MsgHistorico = GestorDeRede.Instancia.MsgHistorico,
            HoraInicio = GestorDeRede.Instancia.HoraInicio

        };
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            int playerActorNumber = (int)player.CustomProperties["ID"];
            if (playerActorNumber <= 4)
            {
                try
                {
                    int avatar = (int)PhotonNetwork.PlayerList[playerActorNumber - 1].CustomProperties["Avatar"];
                    SG.AvatarJogadores[playerActorNumber - 1] = avatar;
                }
                catch
                {
                    int avatar = GestorDeRede.Instancia.AvatarJogadores[playerActorNumber - 1];
                    if (avatar != -1)
                    {
                        GestorDeRede.Instancia.SetAvatar(playerActorNumber, avatar);
                        SG.AvatarJogadores[playerActorNumber - 1] = avatar;
                    }
                }
            }
        }
        SG.Volume = GestorDeRede.Instancia.VolumeFundo;
        SG.VolumeEfeito = GestorDeRede.Instancia.VolumeEfeitos;
        SG.SoundOn = GestorDeRede.Instancia.SoundOn;
        SG.MsgHistorico = GestorDeRede.Instancia.MsgHistorico;
        SG.HoraInicio = GestorDeRede.Instancia.HoraInicio;
        // GameCardsManager
        SG.MapaJogo = _mapaJogo;
        SG.JogadaFinalizada = _jogadaFinalizada;
        // NickName
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            int playerActorNumber = (int)player.CustomProperties["ID"];
            if (playerActorNumber <= 4)
                SG.NickName[playerActorNumber - 1] = player.NickName;
        }
        // Jogador
        SG.PuxouDoMonte = jogador.GetComponent<Jogador>().PuxouDoMonte;
        SG.PegouLixo = jogador.GetComponent<Jogador>().PegouLixo;
        SG.Descartou = jogador.GetComponent<Jogador>().Descartou;
        SG.FinalizouJogada = jogador.GetComponent<Jogador>().FinalizouJogada;
        SG.IniciouJogada = jogador.GetComponent<Jogador>().IniciouJogada;
        SG.JaBaixou = jogador.GetComponent<Jogador>().JaBaixou;
        SG.JogadorPrimeiraJogada = jogador.GetComponent<Jogador>().PrimeiraJogada;
        SG.QdeCartasNaMao = jogador.GetComponent<Jogador>().QdeCartasNaMao;
        SG.QdeCartasNoLixo = jogador.GetComponent<Jogador>().QdeCartasNoLixo;
        SG.UltimaJogada = jogador.GetComponent<Jogador>().UltimaJogada;

        //string saveGame = JsonUtility.ToJson(SG); // JsonConvert.SerializeObject(SG);
        string saveGame = JsonConvert.SerializeObject(SG);
        return saveGame;
    }

    public void GetGameVer()
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        photonView.RPC("GetGameVerRPC", RpcTarget.All, localActor, GestorDeRede.Instancia.SoVerNumber);
    }
    public void GetGameVer2(string saveGame)
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"]; // PhotonNetwork.LocalPlayer.ActorNumber;
        GetGameRecall("99", localActor, saveGame);
        Baralho.Instancia.GetGameVerBaralho();
        FrontManager.Instancia.RedrawJogador(1, true);
        FrontManager.Instancia.RedrawJogada(1, "-");
        FrontManager.Instancia.RedrawOthers(1);

        GameCardsManager.Instancia.MsgPlacar();

        string msg = GameCardsManager.Instancia.GetNome(localActor) + " entrou.";
        GameManager.Instancia.MostraMsgMainAll(msg, false, "msg", 0);
    }

    [PunRPC]
    private void GetGameVerRPC(int actorNumberVer, int soVerNumber)
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        if (localActor == soVerNumber) // carregar dados do jogador associado
        {
            string saveGame = SetGameRecallCPL(localActor, true);
            photonView.RPC("SetJsonGamerVerRPC", RpcTarget.All, saveGame, actorNumberVer);
        }
    }

    [PunRPC]
    private void SetJsonGamerVerRPC(string saveGame, int actorNumberVer)
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        if (localActor == actorNumberVer)
        {
            GetGameVer2(saveGame);
        }
    }


    public void GetGameRecall(string sala, int actorNumberVer = 0, string saveGameVer = "")
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        string saveGame = "";
        if (actorNumberVer == 0)
        {
            if (localActor > 4)
                return;

            // Ler JSON
            string fileName = Application.persistentDataPath + "/SALA_" + sala.ToUpper() + "_" + localActor.ToString().PadLeft(2, '0') + ".json";
            if (!File.Exists(fileName))
                return;
            StreamReader arquivo = new StreamReader(fileName);
            saveGame = arquivo.ReadToEnd();
            arquivo.Close();
            this.GetGameRecallCB(saveGame, actorNumberVer);
            Baralho.Instancia.GerarCartasCB();
            // Fim Ler JSON

            //// ler dados em tabela
            //int salaParm = Convert.ToInt32(PhotonNetwork.CurrentRoom.Name.Substring(4, 2));
            //WWWForm form = new WWWForm();
            //form.AddField("sala", salaParm);
            //StartCoroutine(Post("/oapi/play/find", form, true, actorNumberVer, sala));
            //// Fim ler dados em tabela
        }
        else
        {
            saveGame = saveGameVer;
            //photonView.RPC("GetGameRecallRPC", RpcTarget.All, saveGame, actorNumberVer); // Ler tabela
        }
        photonView.RPC("GetGameRecallRPC", RpcTarget.All, saveGame, actorNumberVer); // Ler JSON
    }

    public void GetGameRecallCB(string saveGame, int actorNumberVer = 0)
    {
        photonView.RPC("GetGameRecallRPC", RpcTarget.All, saveGame, actorNumberVer);
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        GestorDeRede.Instancia.SetDadosRodada(localActor, 0, "recall", 1);
    }

    [PunRPC]
    private void GetGameRecallRPC(string saveGame, int actorNumberVer)
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"]; // PhotonNetwork.LocalPlayer.ActorNumber;
        if (actorNumberVer != 0 && actorNumberVer != localActor)
            return;

        //SaveGame SG = JsonUtility.FromJson<SaveGame>(saveGame); // JsonConvert.DeserializeObject<SaveGame>(saveGame);
        SaveGame SG = JsonConvert.DeserializeObject<SaveGame>(saveGame);
        string fileName = Application.persistentDataPath + "/SALA_" + PhotonNetwork.CurrentRoom.Name.ToUpper() + "_" + localActor.ToString().PadLeft(2, '0') + ".json";
        if (File.Exists(fileName))
        {
            string saveGameAux;
            StreamReader arquivo = new StreamReader(fileName);
            saveGameAux = arquivo.ReadToEnd();
            arquivo.Close();
            //SaveGame SGAux = JsonUtility.FromJson<SaveGame>(saveGameAux); //JsonConvert.DeserializeObject<SaveGame>(saveGame);
            SaveGame SGAux = JsonConvert.DeserializeObject<SaveGame>(saveGame);
            SG.AvatarJogadores = SGAux.AvatarJogadores;
        }
        // GestorDeRede
        GestorDeRede.Instancia.GameIdf = SG.GameIdf;
        GestorDeRede.Instancia.GameRodada = SG.GameRodada;
        GestorDeRede.Instancia.VulOkA = SG.VulOkA;
        GestorDeRede.Instancia.VulOkB = SG.VulOkB;
        GestorDeRede.Instancia.InicialOk = SG.InicialOk;
        GestorDeRede.Instancia.Placar01Det = SG.Placar01Det;
        GestorDeRede.Instancia.Placar02Det = SG.Placar02Det;
        GestorDeRede.Instancia.Placar01 = SG.Placar01;
        GestorDeRede.Instancia.Placar02 = SG.Placar02;
        GestorDeRede.Instancia.PlacarGeral01 = SG.PlacarGeral01;
        GestorDeRede.Instancia.PlacarGeral02 = SG.PlacarGeral02;
        GestorDeRede.Instancia.JogadorInicial = SG.JogadorInicial;
        if (GestorDeRede.Instancia.JogadorInicial > PhotonNetwork.PlayerList.Length)
            GestorDeRede.Instancia.JogadorInicial = 1;
        if (GestorDeRede.Instancia.JogadorInicial < 1)
            GestorDeRede.Instancia.JogadorInicial = PhotonNetwork.PlayerList.Length;
        GestorDeRede.Instancia.PrimeiraJogada = SG.PrimeiraJogada;
        GestorDeRede.Instancia.Dupla01 = SG.Dupla01;
        GestorDeRede.Instancia.Dupla02 = SG.Dupla02;
        GestorDeRede.Instancia.SeqJogadores = SG.SeqJogadores;
        GestorDeRede.Instancia.AvatarJogadores = SG.AvatarJogadores;
        GestorDeRede.Instancia.VolumeFundo = SG.Volume;
        GestorDeRede.Instancia.VolumeEfeitos = SG.VolumeEfeito;
        GestorDeRede.Instancia.SoundOn = SG.SoundOn;
        GestorDeRede.Instancia.MsgHistorico = SG.MsgHistorico;
        GestorDeRede.Instancia.HoraInicio = SG.HoraInicio;
        // GameCardsManager        
        _mapaJogo = SG.MapaJogo;
        //_mapaJogo.seqJogadorAtual += 1;
        //if (_mapaJogo.seqJogadorAtual > 4)
        //    _mapaJogo.seqJogadorAtual = 1;
        _mapaJogo.seqJogadorAtual -= 1;
        if (_mapaJogo.seqJogadorAtual <= 0)
            _mapaJogo.seqJogadorAtual = 4;
        _jogadaFinalizada = SG.JogadaFinalizada;

        #region jogador
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            int playerActorNumber = (int)player.CustomProperties["ID"];
            //if (player.ActorNumber <= 4)
            if (playerActorNumber <= 4)
            {
                string tagPlayer = "JOGADOR" + playerActorNumber.ToString().Trim().PadLeft(2, '0');

                GameObject jogadorPlayer = GameObject.FindGameObjectWithTag(tagPlayer);
                if (jogadorPlayer != null)
                {
                    string avatar = SG.AvatarJogadores[playerActorNumber - 1].ToString().PadLeft(2, '0');
                    //string avatar = GestorDeRede.Instancia.GetAvatar(playerActorNumber, true); // player.ActorNumber, true);
                    jogadorPlayer.GetComponent<Image>().sprite = Resources.Load<Sprite>(avatar);
                    jogadorPlayer.transform.Find("Nome").GetComponent<Text>().text = SG.NickName[playerActorNumber - 1];
                    player.NickName = SG.NickName[playerActorNumber - 1];
                    ExitGames.Client.Photon.Hashtable hash = new ExitGames.Client.Photon.Hashtable
                    {
                        { "Avatar", avatar }
                    };
                    player.SetCustomProperties(hash);
                }
            }
        }
        #endregion jogador
        SoundManager.Instancia.PlayONOFF(false);
        GameCardsManager.Instancia.MsgPlacar();
    }
    #endregion recall


    #region servico
    private IEnumerator Post(string servico, WWWForm dados, bool ret, int actorNumberVer, string sala)
    {
        // Request and wait for the desired page.
        UnityWebRequest webRequest = UnityWebRequest.Post(GestorDeRede.urlService + servico, dados);
        yield return webRequest.SendWebRequest();

        string[] pages = servico.Split('/');
        int page = pages.Length - 1;
        bool erro = false;
        switch (webRequest.result)
        {
            case UnityWebRequest.Result.ConnectionError:
            case UnityWebRequest.Result.DataProcessingError:
                //Debug.LogError(pages[page] + ": Error: " + webRequest.error);
                erro = true;
                break;
            case UnityWebRequest.Result.ProtocolError:
                //Debug.LogError(pages[page] + ": HTTP Error: " + webRequest.error);
                erro = true;
                break;
            case UnityWebRequest.Result.Success:
                //Debug.Log(pages[page] + ":\nReceived: " + webRequest.downloadHandler.text);
                if (ret)
                {
                    //FileSave fs = JsonUtility.FromJson<FileSave>(webRequest.downloadHandler.text); // JsonConvert.DeserializeObject<FileSave>(webRequest.downloadHandler.text);
                    FileSave fs = JsonConvert.DeserializeObject<FileSave>(webRequest.downloadHandler.text);
                    this.GetGameRecallCB(fs.dados, actorNumberVer);
                    Baralho.Instancia.GerarCartasCB();
                }
                break;
        }
        if (erro)
        {
            int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
            string fileName = Application.persistentDataPath + "/SALA_" + sala.ToUpper() + "_" + localActor.ToString().PadLeft(2, '0') + ".json";
            if (File.Exists(fileName))
            {
                StreamReader arquivo = new StreamReader(fileName);
                string saveGame = arquivo.ReadToEnd();
                arquivo.Close();
                photonView.RPC("GetGameRecallRPC", RpcTarget.All, saveGame, actorNumberVer);
            }
        }
    }
    #endregion servico

    #region Classe Entidades
    [Serializable]
    public class FileSave
    {
        public int sala { get; set; }
        public string dados { get; set; }
        public int jogador { get; set; }
    }
    //[Serializable]
    //public class FileRead
    //{
    //    public int sala { get; set; }
    //}

    [Serializable]
    public class SaveGame
    {
        // GestorDeRede
        public int GameIdf { get; set; }
        public int GameRodada { get; set; }
        public bool VulOkA { get; set; }
        public bool VulOkB { get; set; }
        public int InicialOk { get; set; }
        public string Placar01Det { get; set; }
        public string Placar02Det { get; set; }
        public int Placar01 { get; set; }
        public int Placar02 { get; set; }
        public int PlacarGeral01 { get; set; }
        public int PlacarGeral02 { get; set; }

        public int JogadorInicial { get; set; }
        public bool PrimeiraJogada { get; set; }
        public Tuple<int, int> Dupla01 { get; set; }
        public Tuple<int, int> Dupla02 { get; set; }
        public int[] SeqJogadores = new int[4];
        public int[] AvatarJogadores = new int[4];
        public string[] NickName = new string[4];
        public float Volume { get; set; }
        public float VolumeEfeito { get; set; }
        public bool SoundOn { get; set; }
        public string MsgHistorico { get; set; }
        public DateTime HoraInicio { get; set; }

        // GameCardsManager
        public MapaJogo MapaJogo { get; set; }
        public bool JogadaFinalizada { get; set; }
        // Jogador
        public bool PuxouDoMonte { get; set; }
        public bool PegouLixo { get; set; }
        public bool Descartou { get; set; }
        public bool FinalizouJogada { get; set; }
        public bool IniciouJogada { get; set; }
        public bool JaBaixou { get; set; }
        public bool JogadorPrimeiraJogada { get; set; }
        public int QdeCartasNaMao { get; set; }
        public int QdeCartasNoLixo { get; set; }
        public List<int> UltimaJogada { get; set; }
    }

    [Serializable]
    public class MapaJogo
    {
        public int seqJogadorAtual;
        public bool[] pegouMorto;
        public int[] Vul; //01; // vulneravel inicio = 75 pontos
        public bool fimRodada;
        public bool[] baixou; //01;
        public List<CartaJogo> cartasJogo;
    }

    [Serializable]
    public class CartaJogo
    {
        public short Deck;
        /// <summary>
        /// Começa no 0 (zero)
        /// </summary>
        public int Seq;
        public int SeqFixo;
        public int Id;
        public string Portador;
        public int Peso;
        public int Valor;
        public int Naipe;
        public bool Coringa = false;
        public bool Provisorio = false;
        public bool Neutro2 = false;
        public bool Lixo = false;
        public int Pontos;
        public bool NovoNaMao = false;
        public string BotTag = "";
        public int PesoGen; // Peso generico
        public string ctr = "";
        public bool NovoJogada = false;
    }

    [Serializable]
    public class Estatistica
    {
        public int jogador { get; set; }
        public int cartasLixadas { get; set; }
        public int cartasPuxadas { get; set; }
        public int cartasBaixadas { get; set; }
        public int pontosNaMao { get; set; }

    }
    #endregion Classe Entidades
}
