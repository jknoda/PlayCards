using Photon.Pun;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameRules : MonoBehaviourPunCallbacks
{
    private List<int> _cartasJogada;
    private List<int> _iguaisValidos;
    private bool _jogoIguais; // jogada com valores iguais (Ex: AS)
    private bool _novaJogada;
    private int _idCartaComprada;

    private bool _soCheck;

    private bool _botEmbaralhar;

    private bool _2coringa = false;

    private int indA1para14;
    /// <summary>
    /// Item01 = idCarta, Item02 = peso;
    /// </summary>
    private Tuple<int, int> ind2TempVoltar;

    public bool botPegueMorto;

    public bool First { get; set; }
    public static GameRules Instancia { get; private set; }
    private void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            botPegueMorto = false;
            Instancia = this;
            _idCartaComprada = 0;
            _botEmbaralhar = false;
            CarregaIguaisValidos();
        }
    }

    private void CarregaIguaisValidos()
    {
        _iguaisValidos = new List<int>
        {
            1, // AS
            14 // AS
        };
    }

    #region Rules
    public Tuple<bool, string> VerJogadaFinalizada(bool somenteMsg = false)
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"]; // PhotonNetwork.LocalPlayer.ActorNumber;
        if (localActor > 4)
            return new Tuple<bool, string>(false, "---");
        bool desfazer = false;
        botPegueMorto = false;
        string msg = "";
        bool finalizou = true;
        var jogador = GameCardsManager.Instancia.GetJogadorObjeto().GetComponent<Jogador>();
        bool pegouMorto = GameCardsManager.Instancia.GetPegouMorto(localActor);
        int indMorto = GameCardsManager.Instancia.GetListaCartasJogo().FindIndex(x => x.Portador.Contains("MORTO")); // (x.Portador + "00000").Substring(0, 5) == "MORTO");
        if (indMorto == -1) // não tem mais morto para pegar
            pegouMorto = true;
        // regras de finalização
        // trocou carta do lixo bater
        if (GameCardsManager.Instancia.GetCartasNaMao() == 0 && pegouMorto && jogador.PegouLixo && jogador.QdeCartasNoLixo == 1 && jogador.QdeCartasNaMao == 1 && jogador.Descartou)
        {
            finalizou = false;
            desfazer = true;
            msg = "Não pode bater trocando carta do lixo";
        }
        else if (GameCardsManager.Instancia.GetCartasNaMao() == 0 && !pegouMorto && !(jogador.PegouLixo && GameCardsManager.Instancia.GetCartasNoLixo() > 1))
        {
            finalizou = false;
            msg = "Pegue o morto";
            if (GameCardsManager.Instancia.IsBot())
            {
                botPegueMorto = true;
            }
        }
        else if (!jogador.PegouLixo && !jogador.PuxouDoMonte)
        {
            finalizou = false;
            if (GameCardsManager.Instancia.GetMinhaVez())
                msg = "Puxe uma carta ou pegue lixo";
        }
        else if (jogador.PegouLixo && GameCardsManager.Instancia.GetCartasNoLixo() == 1)
        {
            finalizou = true;
        }
        else if (jogador.PegouLixo && GameCardsManager.Instancia.GetCartasNaMao() == 0 && GameCardsManager.Instancia.GetCartasNoLixo() <= 1)
        {
            finalizou = true;
        }
        else if (jogador.PuxouDoMonte && GameCardsManager.Instancia.GetCartasNaMao() == 0)
        {
            finalizou = true;
        }
        else if (jogador.Descartou)
        {
            if (jogador.PegouLixo && GameCardsManager.Instancia.GetCartasNoLixo() != 1)
            {
                finalizou = false;
                msg = "Deixe uma carta no lixo";
            }
        }
        else
        {
            finalizou = false;
            msg = "Descarte uma carta";
        }
        if (finalizou && somenteMsg)
        {
            msg = "Finalize a jogada";
        }
        if (finalizou)
        {
            if (GameCardsManager.Instancia.GetCartasNoMonte() == 0)
            {
                // Ver se tem carta no morto
                int indMorto01 = GameCardsManager.Instancia.GetListaCartasJogo().FindIndex(x => x.Portador.Contains("MORTO01"));
                int indMorto02 = GameCardsManager.Instancia.GetListaCartasJogo().FindIndex(x => x.Portador.Contains("MORTO02"));
                if (indMorto01 != -1 || indMorto02 != -1)
                {
                    GameCardsManager.Instancia.SetMortoParaMonte(indMorto01 != -1 ? 1 : 2);
                }
            }

            // ver vul
            if (jogador.GetComponent<Jogador>().PrimeiraJogada)
            {
                int actorNumber = GameCardsManager.Instancia.GetJogadorAtual();
                jogador.GetComponent<Jogador>().PrimeiraJogada = false;
                // Ver VUL
                var ret = VulOk(actorNumber);
                if (!ret.Item1)
                {
                    msg = ret.Item2;
                    desfazer = true;
                }
                else if (ret.Item2 == "<0>") // não baixou
                {
                    jogador.GetComponent<Jogador>().PrimeiraJogada = true;
                }
            }
        }
        if (finalizou && !somenteMsg)
        {
            GameCardsManager.Instancia.GetJogadorObjeto().GetComponent<Jogador>().FinalizaJogada();
            GameCardsManager.Instancia.SetGameRecall();
        }

        if (desfazer)
        {
            finalizou = false;
            DesfazerUltimaJogada(jogador.gameObject);
            jogador.GetComponent<Jogador>().PrimeiraJogada = true;
            jogador.GetComponent<Jogador>().FinalizouJogada = false;
        }


        return new Tuple<bool, string>(finalizou, msg);
    }

    public void JogadaFinalizada()
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        if (localActor > 4)
            return;
        bool fimJogada = false;
        bool fimRodada = false;
        bool erro = false;
        string msgFim = "";
        Tuple<bool, string> retFim = VerJogadaFinalizada();
        string msg = retFim.Item2;
        fimJogada = retFim.Item1;
        GameManager.Instancia.MostraMsgMain(msg, false, "0", false, 0);
        GameObject jogador = GameCardsManager.Instancia.GetJogadorObjeto();
        int cartasNaMao = GameCardsManager.Instancia.GetCartasNaMao();
        if (jogador.GetComponent<Jogador>().FinalizouJogada || (cartasNaMao == 0 && fimJogada))
        {
            // Verificar se é fim do jogo
            bool temCanastra = GameCardsManager.Instancia.GetTemCanastraLimpa(localActor);
            int id = GameCardsManager.Instancia.GetDupla(localActor); // == 1 ? id = 1 : id = 2;
            bool pegouMorto = GameCardsManager.Instancia.GetPegouMorto(localActor);
            if (cartasNaMao == 0)
            {
                int indMorto = GameCardsManager.Instancia.GetListaCartasJogo().FindIndex(x => x.Portador.Contains("MORTO"));
                if (indMorto == -1) // não tem mais morto para pegar
                    pegouMorto = true;
                if (temCanastra)
                {
                    if (pegouMorto)
                    {
                        msgFim = ValidarBatida(localActor);
                        if (msgFim == "OK")
                        {
                            if (GameCardsManager.Instancia.IsBot(localActor))
                            {
                                BotManager.Instancia.PararBot();
                                ChatManager.Instancia.EnviarBot("Bati!");
                            }
                            fimJogada = true;
                            fimRodada = true;
                            Totalizar(localActor); // bot ok
                            msgFim = "";
                        }
                        else
                        {
                            fimJogada = false;
                            fimRodada = false;
                        }

                    }
                }
                else
                {
                    if (pegouMorto)
                    {
                        msgFim = "Não tem canastra limpa";
                        erro = true;
                        fimJogada = false;
                        if (jogador.GetComponent<Jogador>().Descartou)
                            jogador.GetComponent<Jogador>().Descartou = false;
                        if (jogador.GetComponent<Jogador>().FinalizouJogada)
                            jogador.GetComponent<Jogador>().FinalizouJogada = false;
                    }
                }
            }
            if (fimJogada)
            {
                if (fimRodada)
                {
                    if (GameCardsManager.Instancia.GetPlacar().Item1 >= 3000 || GameCardsManager.Instancia.GetPlacar().Item2 > 3000)
                    {
                        Final(GameCardsManager.Instancia.GetPlacar().Item1, GameCardsManager.Instancia.GetPlacar().Item2);
                    }
                    else
                    {
                        MsgFinalizar(false, "");
                        if (GameCardsManager.Instancia.IsBot(1)) // se bot é master
                        {
                            _botEmbaralhar = true;
                        }
                    }
                    GameCardsManager.Instancia.SetFimRodada();
                }
                else
                {
                    GameCardsManager.Instancia.SetInicializaJogada();
                    GameCardsManager.Instancia.SetReorganizaMao();
                }
            }
            else
            {
                if (string.IsNullOrEmpty(msgFim))
                    msgFim = msg;
                if (erro)
                {
                    // desfazer ultima jogada
                    DesfazerUltimaJogada(jogador);
                }
                GameManager.Instancia.MostraMsgMain(msgFim, false, "0", false, 0, true);
                if (GameCardsManager.Instancia.IsBot())
                {
                    //Debug.Log("falta limpa");
                    BotManager.Instancia.finalizado = true;
                    BotManager.Instancia.Jogar(localActor, true, false);
                }
            }
        }
        if (botPegueMorto)
        {
            botPegueMorto = false;
            bool finalizou = GameCardsManager.Instancia.GetJogadorObjeto().GetComponent<Jogador>().FinalizouJogada;
            BotManager.Instancia.PegueMorto(localActor, finalizou);
        }
        else if (_botEmbaralhar)
        {
            _botEmbaralhar = false;
            BotManager.Instancia.Embaralhar();
        }
    }

    public string ValidarBatida(int actorNumber = 0)
    {
        //if (actorNumber == 0)
        //    actorNumber = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"]; // PhotonNetwork.LocalPlayer.ActorNumber;
        //bool ZerouMonte = GameCardsManager.Instancia.GetCartasNoMonte() == 0;
        string ret = "OK";
        return ret;
    }

    public void DesfazerUltimaJogada(GameObject jogador, int idCartaLixo = -1)
    {
        jogador.GetComponent<Jogador>().Descartou = false;
        jogador.GetComponent<Jogador>().IncluirCarta(jogador.GetComponent<Jogador>().UltimaJogada, true);
        GameCardsManager.Instancia.MsgMsg("JOGADA DESFEITA", 0);
        if (idCartaLixo != -1)
        {
            jogador.GetComponent<Jogador>().PuxouDoMonte = false;
            jogador.GetComponent<Jogador>().PegouLixo = false;
            GameCardsManager.Instancia.SetPortador(idCartaLixo, "LIXO");
            FrontManager.Instancia.RedrawLixo(idCartaLixo, GameCardsManager.Instancia.GetJogador());
        }
        if (GameCardsManager.Instancia.IsBot())
        {
            Baralho.Instancia.LimparSelecionados();
            BotManager.Instancia.desfeita = true;
        }
    }

    private void Final(int placar01, int placar02)
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        if (localActor <= 4)
        {
            if (placar01 > placar02)
            {
                if (GameCardsManager.Instancia.GetDupla(localActor) == 1)
                {
                    GestorDeRede.Instancia.SetPlacarGeral(1);
                }
            }
            else if (placar02 > placar01)
            {
                if (GameCardsManager.Instancia.GetDupla(localActor) == 2)
                {
                    GestorDeRede.Instancia.SetPlacarGeral(2);
                }
            }
            else
            {
                GestorDeRede.Instancia.SetPlacarGeral(1);
                GestorDeRede.Instancia.SetPlacarGeral(2);
            }
        }
        photonView.RPC("FinalRPC", RpcTarget.All, placar01, placar02);
    }
    [PunRPC]
    private void FinalRPC(int placar01, int placar02)
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        string msgResultado = "";
        if (localActor <= 4)
        {
            if (placar01 > placar02)
            {
                if (GameCardsManager.Instancia.GetDupla(localActor) == 1)
                {
                    msgResultado = "VENCEDOR!!!";
                    GameManager.Instancia.MostraMsgMain(msgResultado, true, "win", false, localActor, true);
                }
                else
                {
                    msgResultado = "PERDEDOR!!!";
                    GameManager.Instancia.MostraMsgMain(msgResultado, true, "lose", false, localActor, true);
                }
            }
            else if (placar02 > placar01)
            {
                if (GameCardsManager.Instancia.GetDupla(localActor) == 2)
                {
                    msgResultado = "VENCEDOR!!!";
                    GameManager.Instancia.MostraMsgMain(msgResultado, true, "win", false, localActor, true);
                }
                else
                {
                    msgResultado = "PERDEDOR!!!";
                    GameManager.Instancia.MostraMsgMain(msgResultado, true, "lose", false, localActor, true);
                }
            }
            else
            {
                msgResultado = "EMPATE!!!";
                GameManager.Instancia.MostraMsgMain(msgResultado, true, "msg", false, localActor, true);
            }
        }
        GameCardsManager.Instancia.MsgPlacar();
        MsgFinalizar(true, msgResultado, localActor);
    }

    private Tuple<bool, string> VulOk(int actorNumber)
    {
        bool ret;
        string msg = "<0>";
        int pontos = GameCardsManager.Instancia.GetPontuacaoCanastras(actorNumber) + GameCardsManager.Instancia.GetPontuacaoArea(actorNumber);
        if (GameCardsManager.Instancia.GetVulPontos(actorNumber) == 0 || pontos == 0)
        {
            ret = true;
            msg = "<0>";
        }
        else
        {
            bool temLimpo = GameCardsManager.Instancia.GetTemJogadaLimpa(actorNumber);
            bool jaBaixou = GameCardsManager.Instancia.GetJogadorObjeto().GetComponent<Jogador>().JaBaixou;
            bool jaPegouMorto = GameCardsManager.Instancia.GetPegouMorto();
            if (jaBaixou || jaPegouMorto)
            {
                ret = true;
                msg = "";
            }
            else
            {
                if (temLimpo)
                {
                    if (pontos >= GameCardsManager.Instancia.GetVulPontos(actorNumber))
                    {
                        ret = true;
                        msg = "";
                    }
                    else
                    {
                        ret = false;
                        GameCardsManager.Instancia.SetVul(actorNumber, GameCardsManager.Instancia.GetVulPontos(actorNumber) + 15);
                        msg = "Vul ! Baixou " + pontos.ToString() + " pontos. Agora precisa de " + GameCardsManager.Instancia.GetVulPontos(actorNumber).ToString() + " pontos";
                    }
                }
                else
                {
                    ret = false;
                    GameCardsManager.Instancia.SetVul(actorNumber, GameCardsManager.Instancia.GetVulPontos(actorNumber) + 15);
                    msg = "Vul ! Não tem jogo limpo. Agora precisa de " + GameCardsManager.Instancia.GetVulPontos(actorNumber).ToString() + " pontos";
                }
            }
            GameCardsManager.Instancia.MsgMsg(msg, 0);
        }
        return new Tuple<bool, string>(ret, msg);
    }

    public void Totalizar(int actorBateu)
    {
        GameCardsManager.Instancia.SetJogadaFinalizada(true);

        string msgResultado = "FINAL DA RODADA";
        GameManager.Instancia.MostraMsgMainAll(msgResultado, true, "fimRodada", 0);
        GameManager.Instancia.MostraMsgMainAll(msgResultado + "\nClique em Embaralhar", true, "0", 1, true);

        int pt01 = GameCardsManager.Instancia.GetPontosJogo(GameCardsManager.Instancia.GetActorDupla(1));
        int pt02 = GameCardsManager.Instancia.GetPontosJogo(GameCardsManager.Instancia.GetActorDupla(2));

        Tuple<string, string> placarDet = GameCardsManager.Instancia.GetPlacarDet();

        int qdeJog = PhotonNetwork.PlayerList.Length;
        if (qdeJog > 4) qdeJog = 4;

        int[] ptBatida = new int[4];
        ptBatida[0] = actorBateu == 1 ? 100 : 0;
        ptBatida[1] = actorBateu == 2 ? 100 : 0;
        ptBatida[2] = actorBateu == 3 ? 100 : 0;
        ptBatida[3] = actorBateu == 4 ? 100 : 0;

        bool[] baixou = new bool[4];
        baixou[0] = GameCardsManager.Instancia.GetJaBaixou(1);
        baixou[1] = qdeJog >= 2 ? GameCardsManager.Instancia.GetJaBaixou(2) : true;
        baixou[2] = qdeJog >= 3 ? GameCardsManager.Instancia.GetJaBaixou(3) : true;
        baixou[3] = qdeJog >= 4 ? GameCardsManager.Instancia.GetJaBaixou(4) : true;

        int[] mao = new int[4];
        for (int i = 0; i < 4; i++)
        {
            if (!baixou[i] && qdeJog > i)
                mao[i] += 100;
            else
                mao[i] = 0;
        }

        GameCardsManager.Instancia.GetListaCartasJogo().Where(x => x.Portador != "MONTE").ToList().
        ForEach(carta =>
        {
            string portador = carta.Portador + "000000000";
            // pontuação das cartas
            if (carta.Portador == "JOGADOR01" && baixou[0]) mao[0] += carta.Pontos;
            if (carta.Portador == "JOGADOR02" && baixou[1]) mao[1] += carta.Pontos;
            if (carta.Portador == "JOGADOR03" && baixou[2]) mao[2] += carta.Pontos;
            if (carta.Portador == "JOGADOR04" && baixou[3]) mao[3] += carta.Pontos;
        });

        int jogada01 = 0, jogada02 = 0;
        jogada01 = GameCardsManager.Instancia.GetPontuacaoArea(GameCardsManager.Instancia.GetActorDupla(1));
        jogada02 = GameCardsManager.Instancia.GetPontuacaoArea(GameCardsManager.Instancia.GetActorDupla(2));

        int morto01 = 0, morto02 = 0;
        morto01 = GameCardsManager.Instancia.GetPegouMorto(GameCardsManager.Instancia.GetActorDupla(1)) ? 0 : 100;
        morto02 = GameCardsManager.Instancia.GetPegouMorto(GameCardsManager.Instancia.GetActorDupla(2)) ? 0 : 100;

        // pontuação das canastras
        int canastra01 = GameCardsManager.Instancia.GetPontuacaoCanastras(GameCardsManager.Instancia.GetActorDupla(1));
        int canastra02 = GameCardsManager.Instancia.GetPontuacaoCanastras(GameCardsManager.Instancia.GetActorDupla(2));

        int saida01 = canastra01 - morto01 + ptBatida[GestorDeRede.Instancia.Dupla01.Item1 - 1] + ptBatida[GestorDeRede.Instancia.Dupla01.Item2 - 1];
        int saida02 = canastra02 - morto02 + ptBatida[GestorDeRede.Instancia.Dupla02.Item1 - 1] + ptBatida[GestorDeRede.Instancia.Dupla02.Item2 - 1];

        int pontos01 = 0, pontos02 = 0;

        int mao01 = (mao[GestorDeRede.Instancia.Dupla01.Item1 - 1] + mao[GestorDeRede.Instancia.Dupla01.Item2 - 1]);
        int mao02 = (mao[GestorDeRede.Instancia.Dupla02.Item1 - 1] + mao[GestorDeRede.Instancia.Dupla02.Item2 - 1]);

        jogada01 -= mao01;
        jogada02 -= mao02;

        if ((jogada01 < 0 && jogada02 < 0) || (saida01 < 0 && saida02 < 0)) // já foi totalizado
            return;

        pontos01 = pt01 + saida01 + jogada01;
        pontos02 = pt02 + saida02 + jogada02;

        string cAux = pontos01.ToString();
        if (cAux.Substring(cAux.Length - 1, 1) == "5")
        {
            jogada01 += 5;
            pontos01 += 5;
        }
        cAux = pontos02.ToString();
        if (cAux.Substring(cAux.Length - 1, 1) == "5")
        {
            jogada02 += 5;
            pontos02 += 5;
        }

        string pt01Aux = placarDet.Item1 + saida01.ToString("##,##0") + "\n" + jogada01.ToString("##,##0") + "\n" + "--------\n" + pontos01.ToString("##,##0") + "\n";
        string pt02Aux = placarDet.Item2 + saida02.ToString("##,##0") + "\n" + jogada02.ToString("##,##0") + "\n" + "--------\n" + pontos02.ToString("##,##0") + "\n";

        GestorDeRede.Instancia.SetPlacarDet(pt01Aux, pt02Aux);
        GestorDeRede.Instancia.SetPlacar(pontos01, pontos02);
        GameCardsManager.Instancia.SetFimRodada();
        GameCardsManager.Instancia.SetGameRecall();
    }

    public bool SelecionarValido(string portador, int indCartaSel)
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"]; // 
        if (localActor > 4)
            return false;
        string jogador = GameCardsManager.Instancia.GetJogador();
        bool ret = true;
        if (Baralho.Instancia.cartasSel.Count > 1 && Baralho.Instancia.primeiraSelecao != portador) ret = false;
        if (ret && portador == "MONTE" && Baralho.Instancia.cartasSel.Count >= 1) ret = false;
        if (ret && portador.Contains("AREA")) ret = false;
        if (ret && portador.Contains("LIXO")) ret = false;
        if (ret && portador.Contains("JOGADOR") && portador != jogador) ret = false;
        if (ret)
        {
            // Selecao no mesmo portador / mesmo naipe caso portador = JOGADOR
            int naipeSel = GameCardsManager.Instancia.GetNaipe(indCartaSel);
            int valorSel = GameCardsManager.Instancia.GetValorCarta(indCartaSel);
            if (GameCardsManager.Instancia.GetEhNeutro2(indCartaSel))
                valorSel = 0;

            bool jogoIguais = VerJogoIguais(Baralho.Instancia.cartasSel, indCartaSel) == 0; // 0=jogo de iguais valido

            foreach (int ind in Baralho.Instancia.cartasSel)
            {
                if (GameCardsManager.Instancia.GetPortador(ind) != portador)
                {
                    ret = false;
                    break;
                }
                if (portador.Contains("JOGADOR"))
                {
                    // todos do mesmo naipe / coringa 2/joker
                    int valorAtual = GameCardsManager.Instancia.GetValorCarta(ind);
                    if (GameCardsManager.Instancia.GetEhNeutro2(ind))
                        valorAtual = 0;
                    if (valorSel != 2 && valorSel != 99 && valorAtual != 2 && GameCardsManager.Instancia.GetValorCarta(ind) != 99)
                    {
                        if (GameCardsManager.Instancia.GetNaipe(ind) != naipeSel && !jogoIguais && !GameManager.Instancia.ZoomOn)
                        {
                            //Debug.Log("naipe diferente");
                            ret = false;
                            break;
                        }
                    }
                }
            }
        }
        return ret;
    }

    private bool PuxouCarta(int actorNumber)
    {
        bool ret;
        var jogador = GameCardsManager.Instancia.GetJogadorObjeto(actorNumber).GetComponent<Jogador>();
        ret = (jogador.PuxouDoMonte || jogador.PegouLixo);
        return ret;
    }
    /// <summary>
    /// Ver Click na carta ou objeto
    /// </summary>
    /// <param name="idCarta">9000=click no lixo</param>
    /// <returns></returns>
    public bool TratarClick(int Codigo, int idCarta, bool soCheck, string areaAdversaria = "")
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"]; // PhotonNetwork.LocalPlayer.ActorNumber;
        if (localActor > 4)
            return false;
        bool ret = false;
        bool gerarSound = true;
        string portadorInicioDrag = "";
        if (Baralho.Instancia.cartasSel.Count > 0)
            portadorInicioDrag = GameCardsManager.Instancia.GetListaCartasJogo().Find(x => x.Id == Baralho.Instancia.cartasSel[0]).Portador;
        if (Codigo == 0)
        {
            var carta = GameCardsManager.Instancia.GetListaCartasJogo().Find(x => x.Id == idCarta);
            if (soCheck)
            {
                if (portadorInicioDrag.Contains("LIXO"))
                    portadorInicioDrag = GameCardsManager.Instancia.GetJogador();
                List<Collider2D> listaCollider = new List<Collider2D>();
                ret = EndDragValido(idCarta, listaCollider, portadorInicioDrag, carta.Portador, soCheck, areaAdversaria);
                return ret;
            }
            else if (carta.Portador.Contains("AREA")) // arrasta carta para jogada
            {
                if (!PuxouCarta(localActor))
                {
                    GameManager.Instancia.MostraMsgMain("Antes. Puxe ou pegue do lixo", false, "suaVez", false, 0);
                    return false;
                }
                List<Collider2D> listaCollider = new List<Collider2D>();
                ret = EndDragValido(idCarta, listaCollider, portadorInicioDrag, carta.Portador, soCheck);
            }
            else if (carta.Portador.Contains("JOGADOR") && portadorInicioDrag.Contains("JOGADOR")) // Troca posição das cartas da mão
            {
                GameCardsManager.Instancia.SetTrocaPosicao(idCarta);
                ret = true;
            }
            else if (carta.Portador.Contains("JOGADOR") && portadorInicioDrag.Contains("LIXO")) // lixo para mão
            {
                Codigo = 9500;
            }
            else if (carta.Portador.Contains("JOGADOR") && portadorInicioDrag.Contains("MONTE")) // monte para mão
            {
                Codigo = 9200;
            }
            else if (carta.Portador.Contains("JOGADOR") && portadorInicioDrag.Contains("MORTO")) // morto para mão
            {
                Codigo = 9300;
            }
        }

        if (Codigo == 9000) // Descarte no lixo
        {
            ret = true;
            gerarSound = false;
            if (Baralho.Instancia.cartasSel.Count == 0)
                Baralho.Instancia.Selecionar(idCarta);
            int idCartaAux = Baralho.Instancia.cartasSel[0];
            string jogadorTag = GameCardsManager.Instancia.GetJogador();
            GameObject jogador = GameObject.FindGameObjectWithTag(jogadorTag);
            int retDes = Descartar(idCartaAux, jogadorTag, jogador);
            if (retDes == 2)
            {
                DesfazerUltimaJogada(jogador, jogador.GetComponent<Jogador>().IdCartaLixo);
                return false;
            }
            ret = retDes == 0;
        }
        else if (Codigo == 9100) // clique na área de jogada
        {
            if (!PuxouCarta(localActor))
            {
                Baralho.Instancia.LimparSelecionados();
                GameManager.Instancia.MostraMsgMain("Antes. Puxe ou pegue do lixo", false, "suaVez", false, 0);
                return false;
            }
            List<Collider2D> listaCollider = new List<Collider2D>();
            ret = EndDragValido(idCarta, listaCollider, portadorInicioDrag, "-", false);
        }
        else if (Codigo == 9200) // double click no monte
        {
            portadorInicioDrag = "MONTE";
            List<Collider2D> listaCollider = new List<Collider2D>();
            string jogadorTag = GameCardsManager.Instancia.GetJogador();
            ret = EndDragValido(idCarta, listaCollider, portadorInicioDrag, jogadorTag, false);
        }
        else if (Codigo == 9300) // double click no morto
        {
            if (string.IsNullOrEmpty(portadorInicioDrag))
                portadorInicioDrag = GameCardsManager.Instancia.GetListaCartasJogo().Find(x => x.Id == idCarta).Portador;
            List<Collider2D> listaCollider = new List<Collider2D>();
            string jogadorTag = GameCardsManager.Instancia.GetJogador();
            ret = EndDragValido(idCarta, listaCollider, portadorInicioDrag, jogadorTag, false);
        }
        else if (Codigo == 9400) // double click para descarte
        {
            if (string.IsNullOrEmpty(portadorInicioDrag))
                portadorInicioDrag = GameCardsManager.Instancia.GetListaCartasJogo().Find(x => x.Id == idCarta).Portador;
            List<Collider2D> listaCollider = new List<Collider2D>();
            ret = EndDragValido(idCarta, listaCollider, portadorInicioDrag, "LIXO", false);
        }
        else if (Codigo == 9500) // double click pegar lixo
        {
            portadorInicioDrag = "LIXO";
            string jogadorTag = GameCardsManager.Instancia.GetJogador();
            List<Collider2D> listaCollider = new List<Collider2D>();
            ret = EndDragValido(idCarta, listaCollider, portadorInicioDrag, jogadorTag, false);
        }

        if (ret)
        {
            if (gerarSound)
                SoundManager.Instancia.PlaySound("endDrag");
        }
        Baralho.Instancia.LimparSelecionados();
        return ret;
    }

    /// <summary>
    /// Verificar se tem cartas repetidas. Ex: jogada de AS
    /// </summary>
    /// <param name="listaCartas"></param>
    /// <param name="idCarta"></param>
    /// <returns>-1=não tem repetido, 0=repetido valido, 1=repetido invalido</returns>
    public short VerJogoIguais(List<int> listaCartas, int idCarta = -1)
    {
        bool jogoIguais = false;
        // Verificar se está fazendo jogo de cartas iguais
        int valor = -1;
        int igual = 0;
        int coringa = 0;
        int incluido = -1;
        if (idCarta != -1)
        {
            listaCartas.Add(idCarta);
            incluido = listaCartas.Count - 1;
        }
        listaCartas.OrderBy(x => GameCardsManager.Instancia.GetValorCarta(x)).ToList()
        .ForEach(item =>
        {
            //int itemValor = GameCardsManager.Instancia.GetValorCarta(item);
            //if (itemValor == 14)
            //    itemValor = 1;
            _2coringa = GameCardsManager.Instancia.GetValorCarta(item) == 2 && !GameCardsManager.Instancia.GetEhLixo(item);
            if (_2coringa || GameCardsManager.Instancia.GetValorCarta(item) == 99)
                coringa++;
            else if (GameCardsManager.Instancia.GetValorCarta(item) != valor)
            {
                valor = GameCardsManager.Instancia.GetValorCarta(item);
                if (valor == 14)
                    valor = 1;
            }
            else
            {
                if (igual == 0)
                    igual++; // somando o anterior tambem, pois anterior é igual ao atual
                igual++;
            }
        });
        jogoIguais = (listaCartas.Count - coringa) == igual;
        if (coringa > 1)
            jogoIguais = false;
        if (incluido != -1)
            listaCartas.RemoveAt(incluido);

        short ret = -1;
        if (jogoIguais && _iguaisValidos.Contains(valor)) ret = 0;
        if (jogoIguais && !_iguaisValidos.Contains(valor)) ret = 1;

        return ret;
    }

    public bool EndDragValido(int idCarta, List<Collider2D> listaCollider, string portadorInicioDrag, string endDrag, bool SoCheck, string areaAdversaria = "")
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"]; // PhotonNetwork.LocalPlayer.ActorNumber;
        if (localActor > 4)
            return false;

        _soCheck = SoCheck;
        _jogoIguais = true;

        bool gravaJogada = false;

        indA1para14 = -1;
        ind2TempVoltar = new Tuple<int, int>(-1, -1);
        if (!GameCardsManager.Instancia.GetMinhaVez())
            return false;
        bool ret = false;
        string msgRet = "";
        string jogadorTag = GameCardsManager.Instancia.GetJogador();
        GameObject jogador = GameObject.FindGameObjectWithTag(jogadorTag);
        string portadorEndDrag = "";
        bool arrastouParaMao;
        if (endDrag == "")
            arrastouParaMao = listaCollider.FindLastIndex(x => x.CompareTag(jogadorTag)) >= 0; // Arrastou para suas cartas (mão)
        else
            arrastouParaMao = endDrag == jogadorTag; // Arrastou para suas cartas (mão)
        if (portadorInicioDrag.Contains("MORTO"))
        {
            arrastouParaMao = true;
        }
        else if (listaCollider.Count > 0 || endDrag != "")
        {
            if (endDrag == "")
            {
                int ind = listaCollider.FindLastIndex(x => x.CompareTag("CARTA"));
                for (int indAux = 0; indAux < listaCollider.Count; indAux++)
                {
                    if (listaCollider[indAux].CompareTag("CARTA"))
                    {
                        int idAux = listaCollider[indAux].GetComponent<Carta>().Id;
                        if (idAux >= 0)
                            if ((GameCardsManager.Instancia.GetPortador(idAux).Contains("AREA")))
                                ind = indAux;
                    }
                }

                if (ind >= 0)
                {
                    int idAux = listaCollider[ind].GetComponent<Carta>().Id;
                    portadorEndDrag = idAux >= 0 ? GameCardsManager.Instancia.GetPortador(idAux) : "";
                    if (!arrastouParaMao && portadorEndDrag == jogadorTag && portadorInicioDrag != jogadorTag)
                    {
                        arrastouParaMao = true;
                    }
                }
            }
            else
            {
                portadorEndDrag = endDrag;
                if (!arrastouParaMao && portadorEndDrag == jogadorTag && portadorInicioDrag != jogadorTag)
                {
                    arrastouParaMao = true;
                }
            }
        }

        if (portadorInicioDrag == "MONTE" && !arrastouParaMao)
            return false;

        if (arrastouParaMao)
        {
            #region Compra de carta ou lixo
            bool ok = true;
            if (ok && portadorInicioDrag == "MONTE")
            {
                if (jogador.GetComponent<Jogador>().PuxouDoMonte
                    || jogador.GetComponent<Jogador>().PegouLixo) // ja puxou a carta
                    ok = false;
                else
                {
                    _idCartaComprada = idCarta;
                    jogador.GetComponent<Jogador>().PuxouDoMonte = true;
                    GameCardsManager.Instancia.SetColorArea(localActor);
                    gravaJogada = true;
                }
            }
            if (ok && portadorInicioDrag == "LIXO")
            {
                if (jogador.GetComponent<Jogador>().PuxouDoMonte) // ja puxou a carta
                    ok = false;
                else
                {
                    jogador.GetComponent<Jogador>().PegouLixo = true;
                    GameCardsManager.Instancia.SetColorArea(localActor);
                    gravaJogada = true;
                    BotManager.Instancia.SetLixadas(Baralho.Instancia.cartasSel, true);
                }
            }
            if (portadorInicioDrag.Contains("MORTO")) // pegou o morto
            {
                if (GameCardsManager.Instancia.GetCartasNaMao() > 0)
                    ok = false;
                if (jogador.GetComponent<Jogador>().PegouLixo && GameCardsManager.Instancia.GetCartasNoLixo() > 1)
                    ok = false;
            }

            if (!ok)
                return false;
            #endregion Compra de carta ou lixo

            if (portadorInicioDrag.Contains("MORTO")) // arrastou morto
            {
                if (GameCardsManager.Instancia.GetPegouMorto(localActor))
                {
                    ret = false;
                    msgRet = "Já pegou o morto";
                }
                else
                {
                    GameCardsManager.Instancia.SetPegueiMorto(localActor);
                    Baralho.Instancia.LimparSelecionados();
                    GameCardsManager.Instancia.GetListaCartasJogo().Where(x => x.Portador == portadorInicioDrag).ToList()
                     .ForEach(item =>
                     {
                         Baralho.Instancia.Selecionar(item.Id);
                     });
                    jogador.GetComponent<Jogador>().IncluirCarta(Baralho.Instancia.cartasSel, false);
                    Baralho.Instancia.LimparSelecionados();
                    GameCardsManager.Instancia.SetReorganizaMao();
                    if (jogador.GetComponent<Jogador>().Descartou && !GameCardsManager.Instancia.GetJogadaFinalizada())
                        JogadaFinalizada();
                    ret = true;
                }
            }
            else
            {
                if (Baralho.Instancia.cartasSel.Count > 0) // arrastou cartas selecionadas (lixo ou suas cartas mesmo)
                {
                    jogador.GetComponent<Jogador>().IncluirCarta(Baralho.Instancia.cartasSel, false);
                    ret = true;
                }
                else // arrastou 1 carta (lixo, monte, ou sua carta)
                {
                    List<int> lista = new List<int>
                    {
                        idCarta
                    };
                    jogador.GetComponent<Jogador>().IncluirCarta(lista, false);
                    ret = true;
                }
            }
        }

        if (portadorInicioDrag.Contains("JOG"))
        {
            if (!ret && listaCollider.FindLastIndex(x => x.name.Contains("JOG")) >= 0)
                ret = false;
        }
        // Descartou para lixo
        if (!ret && (listaCollider.FindLastIndex(x => x.CompareTag("LIXO")) >= 0 || endDrag == "LIXO"))
        {
            if (Baralho.Instancia.cartasSel.Count == 0)
            {
                int retDes = Descartar(idCarta, jogadorTag, jogador);
                if (retDes == 2)
                {
                    DesfazerUltimaJogada(jogador, jogador.GetComponent<Jogador>().IdCartaLixo);
                    return false;
                }
                ret = retDes == 0;
            }
        }

        // Arrastou para jogada
        #region Jogada
        string portador = "0000";
        if (!ret)
        {
            ret = true;
            int actorNumber = localActor;
            if (areaAdversaria == "")
            {
                if (GameCardsManager.Instancia.GetDupla(actorNumber) == 1)
                    portador = "AREA01";
                else
                    portador = "AREA02";
            }
            else
                portador = areaAdversaria;

            _novaJogada = true;
            if (portadorEndDrag.Contains(portador))
            {
                _novaJogada = false;
                if (Baralho.Instancia.cartasSel.Count == 0)
                    Baralho.Instancia.cartasSel.Add(idCarta);
                ret = true;
            }

            if (_novaJogada)
            {
                if (Baralho.Instancia.cartasSel.Count < 3)
                {
                    ret = false;
                    msgRet = "Mínimo de 3 cartas";
                }
                if (ret)
                {
                    var carta = GameCardsManager.Instancia.GetListaCartasJogo().OrderBy(x => x.Portador).ToList().FindLast(x => x.Portador.Contains(portador));
                    if (carta == null)
                        portador += "-01";
                    else
                    {
                        portador = portador + "-" + (Convert.ToInt32(carta.Portador.Substring(7, 2)) + 1).ToString().Trim().PadLeft(2, '0');
                    }
                    portadorEndDrag = portador;
                }
            }
            else
            {
                portador = portadorEndDrag;
            }

            // Validar jogada
            if (ret)
            {
                _cartasJogada = new List<int>();
                int naipe = 0;
                GameCardsManager.Instancia.GetListaCartasJogo(portador)
                .ForEach(item =>
                {
                    _cartasJogada.Add(item.Id);
                    if (!item.Coringa && item.Valor != 2 && naipe == 0)
                        naipe = item.Naipe;
                });
                Baralho.Instancia.cartasSel.ForEach(item =>
                {
                    _cartasJogada.Add(item);
                    var carta = GameCardsManager.Instancia.GetCarta(item);
                    if (!carta.Coringa && (carta.Valor != 2 || carta.Lixo) && carta.Naipe != naipe && naipe != 0 && !_jogoIguais)
                        ret = false;
                });

                bool temJoker = _cartasJogada.FindIndex(x => GameCardsManager.Instancia.GetValorCarta(x) == 99) != -1;
                int indAux = _cartasJogada.FindIndex(x => GameCardsManager.Instancia.GetValorCarta(x) != 99 && GameCardsManager.Instancia.GetValorCarta(x) != 2);
                if (_cartasJogada.Count(x => GameCardsManager.Instancia.GetEhCoringa(x) || GameCardsManager.Instancia.GetEhProvisorio(x)) == 2)
                {
                    indAux = _cartasJogada.FindIndex(x => GameCardsManager.Instancia.GetEhProvisorio(x));
                    if (indAux != -1)
                        GameCardsManager.Instancia.SetPesoOriginal(_cartasJogada[indAux]);
                }
                _cartasJogada = _cartasJogada.OrderBy(x => GameCardsManager.Instancia.GetPesoCarta(x)).ToList();
                int iguais = VerJogoIguais(_cartasJogada);
                _jogoIguais = iguais == 0; // 0=tem iguais validos
                if (iguais == 1 || !ret) // 1=iguais mas invalidos
                {
                    ret = false;
                }
                else if (!_jogoIguais)
                {
                    ret = JogadaValida(portador);
                    if (_soCheck)
                    {
                        return ret;
                    }

                    if (ret)
                    {
                        int ind2 = _cartasJogada.FindIndex(x => GameCardsManager.Instancia.GetValorCarta(x) == 2);
                        if (temJoker && ind2 != -1)
                        {
                            int idCartaAux = _cartasJogada[ind2];
                            GameCardsManager.Instancia.SetPesoOriginal(idCartaAux);
                            GameCardsManager.Instancia.SetCoringaProvisorio(idCartaAux, false);
                            GameCardsManager.Instancia.SetCoringa(idCartaAux, false);
                            GameCardsManager.Instancia.SetNeutro2(idCartaAux, true);
                        }
                    }
                }
                else
                {
                    if (_soCheck)
                    {
                        return true;
                    }

                    string sujo = "";
                    List<Tuple<int, int>> cartasAux = new List<Tuple<int, int>>();
                    foreach (int idCartaItem in _cartasJogada)
                    {
                        int peso = GameCardsManager.Instancia.GetPesoCarta(idCartaItem);
                        cartasAux.Add(new Tuple<int, int>(idCartaItem, peso));
                        _2coringa = GameCardsManager.Instancia.GetValorCarta(idCartaItem) == 2 && !GameCardsManager.Instancia.GetEhLixo(idCartaItem);
                        if (_2coringa)
                            sujo = "S";

                    }
                    EfetivarJogada(ref portador, sujo, ref cartasAux);
                    ret = true;
                }

                if (_soCheck)
                {
                    return ret;
                }

                if (ret)
                {
                    if (portadorInicioDrag == "LIXO")
                        jogador.GetComponent<Jogador>().PegouLixo = true;
                    if (portador.Contains("AREA"))
                    {
                        GameCardsManager.Instancia.SetBaixou(localActor);
                    }
                    GameCardsManager.Instancia.GetListaCartasJogo().Where(x => x.Portador == portador).OrderBy(x => x.Peso).ToList()
                    .ForEach(item =>
                    {
                        jogador.GetComponent<Jogador>().RemoverCarta(item.Id, actorNumber, portador);
                    });
                    jogador.GetComponent<Jogador>().RemoverCarta(0, -1, "");
                    FrontManager.Instancia.RedrawJogada(actorNumber, portadorEndDrag);
                }
            }
        }
        if (_soCheck)
        {
            GameManager.Instancia.MostraMsgMain("Socheck sem volta", true, "msg", false, 0);
            return ret;
        }
        if (!ret)
        {
            GameCardsManager.Instancia.MsgMsg(msgRet, localActor);
        }
        else
        {
            if (gravaJogada)
                GravarJogada(jogadorTag);
            if (GameCardsManager.Instancia.GetCartasNaMao() == 0 && !GameCardsManager.Instancia.GetJogadaFinalizada())
                JogadaFinalizada();
            string qdeCartas = GameCardsManager.Instancia.GetCartasNoMonte().ToString();
            GameCardsManager.Instancia.photonView.RPC("MostraQdeMonte", RpcTarget.All, qdeCartas);
        }
        #endregion Jogada
        return ret;
    }

    private void GravarJogada(string jogadorTag)
    {
        if (_soCheck)
            return;
        else
        {
            GameObject jogador = GameObject.FindGameObjectWithTag(jogadorTag);
            GameCardsManager.Instancia.GetListaCartasJogo(jogadorTag).
            ForEach(item =>
            {
                jogador.GetComponent<Jogador>().UltimaJogada.Add(item.Id);
            });
        }
    }

    public int Descartar(int idCarta, string jogadorTag, GameObject jogador)
    {
        int ret;
        List<int> listaAux = new List<int>();
        listaAux.Add(idCarta);
        BotManager.Instancia.SetLixadas(listaAux, false);
        if (!jogador.GetComponent<Jogador>().PegouLixo && !jogador.GetComponent<Jogador>().PuxouDoMonte)
        {
            return 1;
        }
        else if (jogador.GetComponent<Jogador>().PegouLixo && jogador.GetComponent<Jogador>().QdeCartasNoLixo == 1 && jogador.GetComponent<Jogador>().IdCartaLixo == idCarta && !GameCardsManager.Instancia.IsBot())
        {
            GameManager.Instancia.MostraMsgMain("Lixo devolvido, puxe uma carta", false, "suaVez", false, 0);
            return 2;
        }
        else
        {
            SoundManager.Instancia.PlaySound("lixo");
            MoverManager.Instancia.MoverLixoLimpar();
            if (GameCardsManager.Instancia.GetValorCarta(idCarta) == 2) // 2 não é mais coringa
            {
                GameCardsManager.Instancia.SetPesoOriginal(idCarta);
                GameCardsManager.Instancia.SetLixo(idCarta, true);
                GameCardsManager.Instancia.SetCoringaProvisorio(idCarta, false);
                GameCardsManager.Instancia.SetCoringa(idCarta, false);
                GameCardsManager.Instancia.SetNeutro2(idCarta, true);
                Color corAux = GestorDeRede.Instancia.GetCor("2lixo");
                GameCardsManager.Instancia.SetCor(idCarta, corAux.r, corAux.g, corAux.b, false, true); // 0.984f, 0.964f, 0.674f);
            }
            FrontManager.Instancia.RedrawLixo(idCarta, jogadorTag);
            if (jogador.GetComponent<Jogador>().PuxouDoMonte && _idCartaComprada == idCarta && GestorDeRede.Instancia.PrimeiraJogada && !GameCardsManager.Instancia.IsBot())
            {
                GestorDeRede.Instancia.SetPrimeiraJogada(false); // já não é mais a primeira jogada
                jogador.GetComponent<Jogador>().PuxouDoMonte = false;
                jogador.GetComponent<Jogador>().PegouLixo = false;
                // pode comprar outra carta
                GameManager.Instancia.MostraMsgMain("PUXE OUTRA CARTA", false, "suaVez", false, 0);
            }
            else
            {
                GestorDeRede.Instancia.SetPrimeiraJogada(false);
                jogador.GetComponent<Jogador>().Descartou = true;
                if (!GameCardsManager.Instancia.GetJogadaFinalizada())
                    JogadaFinalizada();
            }
            ret = 0;
        }
        return ret;
    }

    private bool JogadaValida(string portador)
    {
        bool ret = true;

        // Todos com mesmo naipe
        int naipe = -1;
        int coringa2 = 0;
        int coringaJoker = 0;
        bool tem2coringa = false;
        foreach (int idCarta in _cartasJogada)
        {
            int valor = GameCardsManager.Instancia.GetValorCarta(idCarta);
            _2coringa = GameCardsManager.Instancia.GetValorCarta(idCarta) == 2 && !GameCardsManager.Instancia.GetEhLixo(idCarta);
            if (GameCardsManager.Instancia.GetValorCarta(idCarta) == 2
                && (GameCardsManager.Instancia.GetEhCoringa(idCarta) || GameCardsManager.Instancia.GetEhProvisorio(idCarta)))
            {
                ind2TempVoltar = new Tuple<int, int>(GameCardsManager.Instancia.GetCarta(idCarta).Id, GameCardsManager.Instancia.GetCarta(idCarta).Peso);
            }
            if (GameCardsManager.Instancia.GetEhCoringa(idCarta) && _2coringa)
            {
                tem2coringa = true;
            }
            if (naipe == -1 && valor != 2 && valor != 99) naipe = GameCardsManager.Instancia.GetNaipe(idCarta);
            if (_2coringa) coringa2++;
            if (valor == 99) coringaJoker++;
            if (naipe != -1 && naipe != GameCardsManager.Instancia.GetNaipe(idCarta) && !_2coringa && valor != 99 && !_jogoIguais)
            {
                ret = false;
                break;
            }
        }
        // tratamento de coringas
        if (coringaJoker > 1) ret = false;
        else if (coringa2 > 2) ret = false;
        else if (coringaJoker >= 1 && (coringa2 >= 2 || tem2coringa)) ret = false;
        if (_cartasJogada.Count > 14) ret = false;
        if (ret)
        {
            TratarAS();
            // Ver sequencia
            ret = SequenciaValida(portador);
            if (!ret && indA1para14 != -1)
            {
                // Voltar AS
                GameCardsManager.Instancia.SetValor(indA1para14, 1);
                GameCardsManager.Instancia.SetPesoOriginal(indA1para14);
            }
            if (!ret && ind2TempVoltar.Item1 != -1)
            {
                // Voltar 2
                GameCardsManager.Instancia.SetPeso(ind2TempVoltar.Item1, ind2TempVoltar.Item2);
            }
        }

        return ret;
    }

    private void TratarAS()
    {
        int qdeA = 0;
        int idCartaA1 = -1;
        int idCartaA2 = -1;
        bool temCoringa = false;
        bool tem14 = false;
        bool temPara14 = false;
        bool tem2normal = false;
        int peso2como13 = 0;
        foreach (int idCarta in _cartasJogada)
        {
            int valor = GameCardsManager.Instancia.GetValorCarta(idCarta);
            if (valor == 2 && !GameCardsManager.Instancia.GetEhCoringa(idCarta))
                tem2normal = true;
            if (valor == 2 || valor == 99)
                temCoringa = true;
            if (valor == 14)
                tem14 = true;
            if (valor == 13)
                temPara14 = true;
            if (valor == 12 && (!tem2normal || temCoringa))
                temPara14 = true;
            if (peso2como13 == GameCardsManager.Instancia.GetPesoCarta(idCarta))
                temPara14 = true;
            if (valor == 1 || valor == 14)
            {
                if (peso2como13 == 0)
                {
                    peso2como13 = GameCardsManager.Instancia.GetPesoCartaGer(13, GameCardsManager.Instancia.GetNaipe(idCarta)) + 1;
                }
                qdeA++;
                if (qdeA == 1)
                    idCartaA1 = idCarta;
                else if (qdeA == 2)
                    idCartaA2 = idCarta;
                else
                {
                    idCartaA1 = -1;
                    idCartaA2 = -1;
                }
            }
        }
        if (qdeA == 2 && temPara14) // tem 2 A
        {
            if (GameCardsManager.Instancia.GetCarta(idCartaA1).Valor != 14 && GameCardsManager.Instancia.GetCarta(idCartaA2).Valor != 14)
            {
                GameCardsManager.Instancia.SetValor(idCartaA1, 1);
                GameCardsManager.Instancia.SetPesoOriginal(idCartaA1);

                GameCardsManager.Instancia.SetValor(idCartaA2, 14);
                GameCardsManager.Instancia.SetPesoOriginal(idCartaA2);
                if (!tem14)
                    indA1para14 = idCartaA2;
            }
        }
        if (qdeA == 1)
        {
            int ct = _cartasJogada.Count;
            int ultimaCarta = GameCardsManager.Instancia.GetValorCarta(_cartasJogada[ct - 1]);
            if ((ct - 2 >= 0) && (ultimaCarta == 14 || ultimaCarta == 99 || ultimaCarta == 2))
                ultimaCarta = GameCardsManager.Instancia.GetValorCarta(_cartasJogada[ct - 2]);
            if ((ct - 3 >= 0) && (ultimaCarta == 14 || ultimaCarta == 99 || ultimaCarta == 2))
                ultimaCarta = GameCardsManager.Instancia.GetValorCarta(_cartasJogada[ct - 3]);
            int primeiraCarta = GameCardsManager.Instancia.GetValorCarta(_cartasJogada[0]);
            if ((ct >= 2) && (primeiraCarta == 1 || ultimaCarta == 99 || ultimaCarta == 2))
                primeiraCarta = GameCardsManager.Instancia.GetValorCarta(_cartasJogada[1]);
            if ((ct >= 3) && (primeiraCarta == 1 || ultimaCarta == 99 || ultimaCarta == 2))
                primeiraCarta = GameCardsManager.Instancia.GetValorCarta(_cartasJogada[2]);
            bool a14 = false;
            // Verificar se A é 14
            if (ultimaCarta == 13)
            {
                a14 = true;
            }
            else if (temCoringa && (ultimaCarta == 13 || ultimaCarta == 12) && (primeiraCarta != 2 && primeiraCarta != 3))
            {
                a14 = true;
            }
            if (a14)
            {
                //Debug.Log("A=14");
                GameCardsManager.Instancia.SetValor(idCartaA1, 14);
                GameCardsManager.Instancia.SetPesoOriginal(idCartaA1);
                if (!tem14)
                    indA1para14 = idCartaA1;
            }
            else if (!temPara14)
            {
                GameCardsManager.Instancia.SetValor(idCartaA1, 1);
                GameCardsManager.Instancia.SetPesoOriginal(idCartaA1);
            }
        }
    }
    private bool SequenciaValida(string portador)
    {
        bool ret = true;
        bool cartaRepetido = false;

        int neutro2 = -1;
        int idJoker = -1;
        int id2Coringa = -1;
        int id2Provisorio = -1;
        bool provisorioNeutro = false;
        bool tem2lixo = false;

        int naipe = 0;
        // _cartasJogada = lista com código das cartas para analise
        string sujo = "";
        List<int> cartaFaltante = new List<int>();
        bool repetir = _cartasJogada.Count <= 14;
        List<Tuple<int, int>> cartasAux = new List<Tuple<int, int>>();
        int qdeSel = 1;
        while (repetir) // repete caso haja A de 1 para 14
        {
            neutro2 = -1;
            idJoker = -1;
            id2Coringa = -1;
            id2Provisorio = -1;

            cartasAux.Clear();
            cartaFaltante.Clear();

            int id2 = -1;
            // buscar naipe
            foreach (int idCarta in _cartasJogada)
            {
                if (naipe == 0 && GameCardsManager.Instancia.GetValorCarta(idCarta) != 2 && GameCardsManager.Instancia.GetValorCarta(idCarta) != 99)
                {
                    naipe = GameCardsManager.Instancia.GetNaipe(idCarta);
                }
                if (GameCardsManager.Instancia.GetValorCarta(idCarta) == 2)
                {
                    id2 = idCarta;
                }
            }

            // Isolar coringas
            foreach (int idCarta in _cartasJogada)
            {
                int valor = GameCardsManager.Instancia.GetValorCarta(idCarta);
                bool bCoringa = false;
                bool bProvisorio = false;
                _2coringa = GameCardsManager.Instancia.GetValorCarta(idCarta) == 2 && !GameCardsManager.Instancia.GetEhLixo(idCarta);

                if (!tem2lixo)
                    tem2lixo = GameCardsManager.Instancia.GetValorCarta(idCarta) == 2 && GameCardsManager.Instancia.GetEhLixo(idCarta);
                if (_2coringa && GameCardsManager.Instancia.GetNaipe(idCarta) == naipe && !GameCardsManager.Instancia.GetEhNeutro2(idCarta))
                {
                    // restabelecer peso de carta do naipe corrente
                    GameCardsManager.Instancia.SetPesoOriginal(idCarta);
                    if (id2Provisorio == -1 && !GameCardsManager.Instancia.GetEhCoringa(idCarta) && !tem2lixo)
                        bProvisorio = true;
                    else
                        bCoringa = true;
                }
                int peso = GameCardsManager.Instancia.GetPesoCarta(idCarta);
                if (bProvisorio)
                {
                    id2Provisorio = idCarta;
                }
                else if (valor == 99)
                {
                    idJoker = idCarta;
                }
                else if (bCoringa || (_2coringa && (GameCardsManager.Instancia.GetNaipe(idCarta) != naipe || GameCardsManager.Instancia.GetEhCoringa(idCarta))))
                {
                    int idCartaAux = idCarta;
                    if (id2Coringa != -1)
                    {
                        ret = false;
                        if (_2coringa && GameCardsManager.Instancia.GetNaipe(idCarta) != naipe) // tem 2 dois
                        {
                            if (GameCardsManager.Instancia.GetNaipe(id2Coringa) == naipe) // tem 2 com mesmo naipe
                            {
                                ret = true;
                                GameCardsManager.Instancia.SetCoringaProvisorio(id2Coringa, true);
                                id2Provisorio = id2Coringa;
                                id2Coringa = idCarta;
                            }
                        }
                        else if (_2coringa && GameCardsManager.Instancia.GetNaipe(idCarta) == naipe) // tem 2 dois
                        {
                            if (GameCardsManager.Instancia.GetNaipe(id2Coringa) != naipe) // tem 2 com mesmo naipe
                            {
                                ret = true;
                                GameCardsManager.Instancia.SetCoringaProvisorio(idCarta, true);
                                id2Provisorio = idCarta;
                                idCartaAux = id2Coringa;
                            }
                        }

                        if (!ret)
                            break;
                    }
                    id2Coringa = idCartaAux; ;
                }
                else
                {
                    cartasAux.Add(new Tuple<int, int>(idCarta, peso));
                }
            }

            if (!ret) return ret;

            if (idJoker != -1 && id2Provisorio != -1)
            {
                int peso = GameCardsManager.Instancia.GetPesoCarta(id2Provisorio);
                int indAux = cartasAux.FindIndex(x => x.Item1 == id2Provisorio);
                if (indAux == -1)
                    cartasAux.Add(new Tuple<int, int>(id2Provisorio, peso));
                neutro2 = id2Provisorio;
                id2Provisorio = -1;
            }

            if (id2Provisorio != -1 && id2Coringa != -1 && id2Provisorio != id2Coringa)
            {
                int peso = GameCardsManager.Instancia.GetPesoCarta(id2Provisorio);
                cartasAux.Add(new Tuple<int, int>(id2Provisorio, peso));
                provisorioNeutro = true;
                // precisa controlar no fim, se ret = true, o provisorio devera virar neutro
            }

            if (idJoker != -1 && id2 != -1)
            {
                GameCardsManager.Instancia.SetCoringa(id2, false);
                GameCardsManager.Instancia.SetCoringaProvisorio(id2, false);
                GameCardsManager.Instancia.SetNeutro2(id2, true);
            }

            cartasAux = cartasAux.OrderBy(x => x.Item2).ToList();
            repetir = false;
            int indAS = -1;
            int faltante = 0;
            qdeSel = cartasAux.Count;
            for (int index = 0; index < qdeSel - 1; index++)
            {
                int valorAtual = GameCardsManager.Instancia.GetValorCarta(cartasAux[index].Item1);
                _2coringa = GameCardsManager.Instancia.GetValorCarta(cartasAux[index].Item1) == 2 && !GameCardsManager.Instancia.GetEhLixo(cartasAux[index].Item1) && idJoker == -1;
                bool neutro = GameCardsManager.Instancia.GetEhNeutro2(cartasAux[index].Item1);
                if (valorAtual == 1)
                    indAS = index;
                int valorProx = GameCardsManager.Instancia.GetValorCarta(cartasAux[index + 1].Item1);
                if (valorAtual + 1 != valorProx)
                {
                    if (valorAtual == valorProx)
                        cartaRepetido = true;
                    else
                    {
                        for (int falta = 1; falta < (valorProx - valorAtual); falta++)
                        {
                            if (!cartaFaltante.Contains(valorAtual + falta))
                                cartaFaltante.Add(valorAtual + falta);
                        }
                        if (cartaFaltante.Count == 2 && _2coringa && index == 0 && faltante == 0 && id2Provisorio == -1)
                        {
                            faltante++;
                            cartaFaltante.Clear();
                        }
                        if (cartaFaltante.Count > 0)
                        {
                            if (indAS != -1 && cartaFaltante.Count > 1 && cartaFaltante[cartaFaltante.Count - 1] != 2)
                            {
                                GameCardsManager.Instancia.SetValor(cartasAux[indAS].Item1, 14);
                                GameCardsManager.Instancia.SetPesoOriginal(cartasAux[indAS].Item1);
                                indA1para14 = cartasAux[indAS].Item1;
                                repetir = true;
                                break;
                            }
                            faltante++;
                            if (faltante == 2) // faltando 2 faixas
                                return false;
                        }
                    }
                }
            } // EndFor
        } // EndWhie

        sujo = "";

        if (id2Provisorio == id2Coringa)
            id2Provisorio = -1;
        ret = cartaFaltante.Count <= 1 && !cartaRepetido;
        if (idJoker != -1 && (id2Provisorio != -1 || id2Coringa != -1))
            ret = false;

        if (!ret)
            return ret;

        if (provisorioNeutro && id2Provisorio != -1)
        {
            // precisa controlar no fim, se ret = true, o provisorio devera virar neutro
            // tem 2 dois, um deles é provisorio, este deverá se tornar neutro.
            GameCardsManager.Instancia.SetCoringa(id2Provisorio, false);
            GameCardsManager.Instancia.SetCoringaProvisorio(id2Provisorio, false);
            GameCardsManager.Instancia.SetNeutro2(id2Provisorio, true);
            id2Provisorio = -1;
        }

        if (id2Coringa != -1 && id2Provisorio != -1)
        {
            //Debug.Log("coringa/provisorio");
            GameCardsManager.Instancia.SetCoringa(id2Coringa, true);
            GameCardsManager.Instancia.SetCoringaProvisorio(id2Coringa, false);
            GameCardsManager.Instancia.SetNeutro2(id2Coringa, false);

            GameCardsManager.Instancia.SetCoringa(id2Provisorio, false);
            GameCardsManager.Instancia.SetCoringaProvisorio(id2Provisorio, false);
            GameCardsManager.Instancia.SetNeutro2(id2Provisorio, true);
            GameCardsManager.Instancia.SetPesoOriginal(id2Provisorio);
            int peso = GameCardsManager.Instancia.GetPesoCarta(id2Provisorio);
            cartasAux.Add(new Tuple<int, int>(id2Provisorio, peso));
            cartasAux = cartasAux.OrderBy(x => x.Item2).ToList();
            if (cartaFaltante.Count == 1 && cartaFaltante[0] == 2)
                cartaFaltante.Clear();
            id2Provisorio = -1;
        }

        if (cartaFaltante.Count == 0 && (idJoker != -1 || id2Coringa != -1 || id2Provisorio != -1))
        {
            // colocar coringa no fim ou no comeco
            int valor = 0;
            int valorFim = GameCardsManager.Instancia.GetValorCarta(cartasAux[qdeSel - 1].Item1);
            if (valorFim == 99 || valorFim == 2)
                valorFim = GameCardsManager.Instancia.GetValorCarta(cartasAux[qdeSel - 2].Item1);
            int valorInicio = GameCardsManager.Instancia.GetValorCarta(cartasAux[0].Item1);
            bool neutro = GameCardsManager.Instancia.GetEhNeutro2(cartasAux[0].Item1);
            if ((valorInicio == 2 && !neutro) || valorInicio == 99)
                valorInicio = GameCardsManager.Instancia.GetValorCarta(cartasAux[1].Item1);
            if (id2Provisorio == -1 && (14 - valorFim) > valorInicio)
            {
                // colocar no fim
                valor = valorFim + 1;
                if (valor > 14) // ultima carta é AS
                {
                    valor = valorInicio - 1;
                }
            }
            else
            {
                // colocar no começo
                valor = valorInicio - 1;
                if (valor < 1) // primeira carta é AS
                {
                    valor = valorFim + 1;
                }
            }
            if (id2Provisorio != -1 && valorInicio == 1)
                ret = true;
            else
            {
                if (valor < 1 || valor > 14) // primeira carta é AS / ultima carta é AS
                {
                    ret = false;
                }
                else
                    cartaFaltante.Add(valor);
            }
        }

        if (!ret)
            return ret;

        if (cartaFaltante.Count == 1)
        {
            int peso = naipe * 1000 + cartaFaltante[0] * 10 + 1;
            if (idJoker != -1)
            {
                //Debug.Log("joker");
                cartasAux.Add(new Tuple<int, int>(idJoker, peso));
                if (neutro2 != -1)
                {
                    GameCardsManager.Instancia.SetCoringa(neutro2, false);
                    GameCardsManager.Instancia.SetCoringaProvisorio(neutro2, false);
                    GameCardsManager.Instancia.SetNeutro2(neutro2, true);
                }
            }
            if (id2Coringa != -1)
            {
                //Debug.Log("2 coringa");
                cartasAux.Add(new Tuple<int, int>(id2Coringa, peso));
                GameCardsManager.Instancia.SetCoringa(id2Coringa, true);
                GameCardsManager.Instancia.SetCoringaProvisorio(id2Coringa, false);
                GameCardsManager.Instancia.SetNeutro2(id2Coringa, false);
                sujo = "S";
            }
            if (id2Provisorio != -1)
            {
                //Debug.Log("2 provisorio");
                cartasAux.Add(new Tuple<int, int>(id2Provisorio, peso));
                // ultima carta > 8 , 2 é sujo
                int valorFim = GameCardsManager.Instancia.GetValorCarta(cartasAux[qdeSel - 1].Item1);
                if (valorFim == 99 || valorFim == 2)
                    valorFim = GameCardsManager.Instancia.GetValorCarta(cartasAux[qdeSel - 2].Item1);
                if (valorFim > 8)
                {
                    if (cartaFaltante[0] != 2)
                    {
                        sujo = "S";
                        GameCardsManager.Instancia.SetCoringaProvisorio(id2Provisorio, false);
                        GameCardsManager.Instancia.SetCoringa(id2Provisorio, true);
                    }
                    else
                    {
                        GameCardsManager.Instancia.SetCoringaProvisorio(id2Provisorio, true);
                        GameCardsManager.Instancia.SetCoringa(id2Provisorio, false);
                    }
                    GameCardsManager.Instancia.SetNeutro2(id2Provisorio, false);
                }
                else
                {
                    GameCardsManager.Instancia.SetCoringaProvisorio(id2Provisorio, true);
                    GameCardsManager.Instancia.SetCoringa(id2Provisorio, false);
                    GameCardsManager.Instancia.SetNeutro2(id2Provisorio, false);
                }
            }
            if (idJoker == -1 && id2Coringa == -1 && id2Provisorio == -1)
                return false;
        }

        if (_soCheck)
        {
            return true; // Efetivar jogadda
        }
        else
        {
            EfetivarJogada(ref portador, sujo, ref cartasAux);
        }
        return ret;
    }

    private void EfetivarJogada(ref string portador, string sujo, ref List<Tuple<int, int>> cartasAux)
    {
        cartasAux = cartasAux.OrderBy(x => x.Item2).ToList();
        // atualizar mapa e portador
        if (portador.Length < 9)
            portador += "000000";
        portador = portador.Substring(0, 9) + sujo;
        List<int> listaAux = new List<int>();
        foreach (Tuple<int, int> item in cartasAux)
        {
            listaAux.Add(item.Item1);
            GameObject carta = Baralho.Instancia.cartas.Find(x => x.GetComponent<Carta>().Id == item.Item1);
            int idCarta = GameCardsManager.Instancia.GetListaCartasJogo().FindIndex(x => x.Id == item.Item1);
            GameCardsManager.Instancia.SetPeso(idCarta, item.Item2);
            GameCardsManager.Instancia.SetPortador(idCarta, portador);
        }
        BotManager.Instancia.SetLixadas(listaAux, false);
    }

    public void FinalizaJogada()
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"]; // 
        if (localActor > 4)
            return;
        SoundManager.Instancia.PlaySound("finalizar");
        if (GameCardsManager.Instancia.GetEhFimRodada())
        {
            if (GameCardsManager.Instancia.GetPlacar().Item1 >= 3000 || GameCardsManager.Instancia.GetPlacar().Item2 > 3000)
                MsgFinalizar(true, "");
            else
                MsgFinalizar(false, "");
            if (GameCardsManager.Instancia.IsBot(1)) // set bot é master
            {
                BotManager.Instancia.Embaralhar();
            }
        }
        else
        {
            bool fimRodada = false;
            if (GameCardsManager.Instancia.GetMinhaVez())
            {
                string jogadorTag = GameCardsManager.Instancia.GetJogador();
                GameObject jogador = GameObject.FindGameObjectWithTag(jogadorTag);
                if (!jogador.GetComponent<Jogador>().PuxouDoMonte && !jogador.GetComponent<Jogador>().PegouLixo) // nao fez nada ainda
                {
                    int cartasMonte = GameCardsManager.Instancia.GetCartasNoMonte();
                    if (cartasMonte == 0)
                    {
                        string msgFim = GameRules.Instancia.ValidarBatida(localActor);
                        if (msgFim == "OK")
                        {
                            GameRules.Instancia.Totalizar(0);
                            if (GameCardsManager.Instancia.GetPlacar().Item1 >= 3000 || GameCardsManager.Instancia.GetPlacar().Item2 > 3000)
                            {
                                Final(GameCardsManager.Instancia.GetPlacar().Item1, GameCardsManager.Instancia.GetPlacar().Item2);
                            }
                            GameCardsManager.Instancia.SetFimRodada();
                            fimRodada = true;
                        }
                        else
                        {
                            fimRodada = false;
                        }
                    }
                }
            }
            if (!fimRodada && !GameCardsManager.Instancia.GetJogadaFinalizada())
            {
                JogadaFinalizada();
            }
        }
    }

    private static void MsgFinalizar(bool jogoFinalizado, string msgResultado, int actorNumber = 0)
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        string msg = "Rodada finalizada";
        if (jogoFinalizado)
            msg = "Jogo finalizado";
        GameCardsManager.Instancia.MsgQuemJoga(msgResultado, localActor);
        GameCardsManager.Instancia.MsgMsg(msg, 99, true);
        if (!string.IsNullOrEmpty(msgResultado) && actorNumber == 1)
        {
            msgResultado += "\n";
            GameManager.Instancia.MostraMsgMainAll(msgResultado + "Clique em Embaralhar", true, "0", 1, true);
        }
        msg = "Clique em Embaralhar";
        GameCardsManager.Instancia.MsgMsg(msg, 1, true);
        GestorDeRede.Instancia.DesligarConvidados();
    }

    public void NovasCartas()
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        if (localActor > 4)
            return;

        GameCardsManager.Instancia.MsgMsg("", localActor);

        if (GameCardsManager.Instancia.GetPlacar().Item1 >= 3000 || GameCardsManager.Instancia.GetPlacar().Item2 >= 3000)
        {
            GestorDeRede.Instancia.SetPlacar(0, 0, true);
            GestorDeRede.Instancia.ComecaJogo("Principal", false);
        }
        else
        {
            if (GameCardsManager.Instancia.GetEhFimRodada())
            {
                if (GestorDeRede.Instancia.DonoDaSala())
                {
                    GestorDeRede.Instancia.ComecaJogo("Principal", false);
                }
            }
            else
            {
                if (GestorDeRede.Instancia.RodadaDeRecall)
                {
                    int jogador = GestorDeRede.Instancia.JogadorInicial;
                    GestorDeRede.Instancia.photonView.RPC("SetJogadorInicial", RpcTarget.All, jogador);
                    FrontManager.Instancia.RedrawJogada(localActor, "-");
                    FrontManager.Instancia.RedrawOthers(localActor);
                }
                GameCardsManager.Instancia.MostraQuemJoga(GameCardsManager.Instancia.GetJogadorAtual());
            }
        }
    }
    #endregion Rules
}
