using Photon.Pun;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static GameCardsManager;

public class BotManager : MonoBehaviourPunCallbacks
{
    private DateTime tempoInicial;
    private int secJogo;
    private int meuId;
    private float rndRate; // rate de random
    private Jogador jogadorObj;
    private bool _naoTemLimpa;
    private bool _ret;
    private bool _first;
    private int step;
    private bool naRotina;
    private int controleLoop;

    private int qdeCartasBaixadas;

    private List<Tuple<int, int>> lixoTemp; // <int,int> 1=id 2=peso
    private bool baixandoVul;
    private int idCartaPuxada;

    private string motivoDebug;
    private string motivoDescarte;

    public bool jogando;

    public int vulPto;
    public int vulCtrl;
    public bool finalizado;
    public bool desfeita;

    private bool lixei;

    private List<int> cartasLixadas;

    private List<CartasLixadas> actorLixadas;
    /// <summary>
    /// Perfil L=Lixeiro C=Conservador B=Busca bater S=Sujão
    /// </summary>
    private char perfil;

    public static BotManager Instancia { get; private set; }
    private void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            IniciarBot();
            Instancia = this;
        }
    }
    private void Update()
    {
        if (jogando)
        {
            TimeSpan intervalo = DateTime.Now - tempoInicial;
            if (intervalo.Seconds >= secJogo && !naRotina)
            {
                jogando = false;
                naRotina = true;
                step++;
                switch (step)
                {
                    case 1: // puxar o lixar
                        idCartaPuxada = -1;
                        if (!jogadorObj.PegouLixo && !jogadorObj.PuxouDoMonte)
                        {
                            motivoDebug += " ***ANALISAR LIXAR/ ";
                            if (!_first && (AnalisarVaiLixar() || (_naoTemLimpa && RandPerc(50))))
                            {
                                lixei = true;
                                //Debug.Log("Lixar");
                                motivoDebug += " ***LIXAR/ ";
                                // lixar
                                _ret = Lixar();
                            }
                            else
                            {
                                lixei = false;
                                //Debug.Log("Puxar");
                                motivoDebug += " ***PUXAR/ ";
                                // puxar carta
                                _ret = Puxar();
                            }
                        }
                        if (!_ret)
                        {
                            motivoDebug += " !RET JOGAR/ ";
                            Jogar((int)PhotonNetwork.LocalPlayer.CustomProperties["ID"], _naoTemLimpa, false);
                        }
                        tempoInicial = DateTime.Now;
                        naRotina = false;
                        VoltaJogar();
                        break;
                    case 2:
                        //Debug.Log("Definir Jogada");
                        motivoDebug += " ***DEFINIR/ ";
                        _ret = DefinirJogada();
                        if (baixandoVul)
                        {
                            baixandoVul = false;
                        }
                        tempoInicial = DateTime.Now;
                        naRotina = false;
                        VoltaJogar();
                        break;
                    case 3:
                        //Debug.Log("Descartar");
                        motivoDebug += " ***DESCARTAR/ ";
                        ControleDescartar();
                        if (desfeita)
                        {
                            ChatManager.Instancia.EnviarBot("Eita!!!!");
                            desfeita = false;
                            ControleDescartar();
                            motivoDebug += " **DESCARTAR DESFEITA/ ";
                        }
                        //Debug.Log("Descartou");
                        tempoInicial = DateTime.Now;
                        naRotina = false;
                        Retornar();
                        break;
                    default:
                        //Debug.Log("Default");
                        motivoDebug += "Default/ ";
                        step = 0;
                        controleLoop++;
                        if (controleLoop >= 2)
                        {
                            motivoDebug += "SAIDA DO LOOP/ ";
                            naRotina = false;
                            Retornar();
                        }
                        break;
                }
                secJogo = 1;
            }
        }
        else
        {
            if (controleLoop >= 2)
            {
                int actorAux = GameCardsManager.Instancia.GetMeuParceiro();
                string nome = GameCardsManager.Instancia.GetNome(0);
                GameManager.Instancia.MostraMsgMainAll("Bot " + nome + " em loop, help!", true, "msg", actorAux);
                GameManager.Instancia.MostraMsgMainAll("Bot " + nome + " em loop, help!", true, "msg", 1);
            }
        }
    }

    private void IniciarBot()
    {
        jogando = false;
        secJogo = 1;
        finalizado = true;
        cartasLixadas = new List<int>();
        rndRate = 1;
        lixoTemp = new List<Tuple<int, int>>();
        actorLixadas = new List<CartasLixadas>();
        for (int i = 0; i < 4; i++)
        {
            CartasLixadas item = new CartasLixadas();
            item.listaIdCarta = new List<int>();
            item.listaIdCarta.Add(-1);
            actorLixadas.Add(item);
        }
        SortPerfil();
    }

    private void SortPerfil()
    {
        // Perfil L=Lixeiro C=Conservador B=Busca bater S=Sujão
        int p = Rand(6);
        perfil = 'L';
        switch (p)
        {
            case 1:
                perfil = 'L';
                break;
            case 2:
                perfil = 'C';
                break;
            case 3:
                perfil = 'B';
                break;
            case 4:
                perfil = 'S';
                break;
            default:
                perfil = 'B';
                break;
        }
        //perfil = 'S'; ////
    }

    /// <summary>
    /// Testar percentual - n% de chance de acontecer
    /// </summary>
    /// <param name="perc"></param>
    /// <returns>true se valor > perc</returns>
    private bool RandPerc(int perc, bool rateOn = true)
    {
        bool ret;
        if (rateOn)
        {
            if (perfil == 'L') // lixeiro
                perc = (int)(perc * 1.20); // Aumentar em 20% - chances de lixar
            else if (perfil == 'C') // conservador
                perc = (int)(perc / 1.20); // Diminuir em 20%
            else if (perfil == 'B') // bater
                perc = (int)(perc / 1.10); // Diminuir em 10%
            ret = Rand(100) > (100 - Math.Round(perc * rndRate));
        }
        else
            ret = Rand(100) > (100 - perc);
        return ret;
    }

    /// <summary>
    /// Numero inteiro random de 1 a final (inclusive)
    /// </summary>
    /// <param name="final">Inclusive</param>
    /// <returns></returns>
    private int Rand(int final)
    {
        int ret = UnityEngine.Random.Range(1, final + 1);
        return ret;
    }

    public void Jogar(int actorNumber, bool naoTemLimpa, bool first, bool puxarNovamente = false)
    {
        if (!GameCardsManager.Instancia.IsBot())
            return;
        if (!finalizado)
            return;
        desfeita = false;
        meuId = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        motivoDebug = " ";
        motivoDescarte = " ";
        qdeCartasBaixadas = 0;
        finalizado = false;
        cartasLixadas.Clear();
        GameCardsManager.Instancia.SetReorganizaMao();
        if (first)
        {
            string msg = "";
            if (GameCardsManager.Instancia.GetPlacar().Item1 > GameCardsManager.Instancia.GetPlacar().Item2 && RandPerc(75, false))
            {
                if (GameCardsManager.Instancia.GetDupla(meuId) == 1)
                    msg = "Parceiro, vamos continuar assim!";
                else
                    msg = "Parceiro, vamos virar esse jogo!";
            }
            else if (RandPerc(10, false))
            {
                msg = "Oi! Estou jogando!";
            }
            else if (RandPerc(80, false))
            {
                int hora = DateTime.Now.Hour;
                if (hora >= 5 && hora <= 12)
                    msg = " Bom dia !";
                else if (hora > 12 && hora <= 18)
                    msg = " Boa tarde !";
                else
                    msg = " Boa noite !";
                msg = "Pessoal...." + msg;
            }
            else if (GameCardsManager.Instancia.GetVulPontos(meuId) >= 75)
            {
                msg = "VUL !!!";
            }
            if (msg != "")
                ChatManager.Instancia.EnviarBot(msg);
        }
        int cartasMonte = GameCardsManager.Instancia.GetCartasNoMonte();
        if (cartasMonte == 0)
        {
            GameRules.Instancia.FinalizaJogada();
            return;
        }
        Baralho.Instancia.LimparSelecionados(false);
        jogadorObj = GameCardsManager.Instancia.GetJogadorObjeto().GetComponent<Jogador>();
        _naoTemLimpa = naoTemLimpa;
        _ret = true;
        _first = first;
        step = 0;
        controleLoop = 0;
        tempoInicial = DateTime.Now;
        secJogo = 1;
        naRotina = false;
        jogando = true; // inicializar update
        if (puxarNovamente)
            Update();
    }
    private void VoltaJogar()
    {
        jogando = true;
    }
    private void Retornar()
    {
        motivoDebug += "Retornar/ ";
        jogando = false;
        MsgDebug();
        return;
    }

    private bool Puxar()
    {
        int iAux = GameCardsManager.Instancia.GetCartasNoMonte();
        if (iAux == 0)
        {
            string msgFim = GameRules.Instancia.ValidarBatida();
            if (msgFim == "OK")
            {
                GameRules.Instancia.Totalizar(0);
            }
            return false;
        }
        else
        {
            idCartaPuxada = GameCardsManager.Instancia.GetListaCartasJogo("MONTE")[iAux - 1].Id;
            return GameRules.Instancia.TratarClick(9200, idCartaPuxada, false);
        }
    }

    #region Rotina para lixar

    private int VerServeNaJogada()
    {
        string jogadorTag = GameCardsManager.Instancia.GetJogador();
        int actorNumber = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"]; // PhotonNetwork.LocalPlayer.ActorNumber;
        string area;
        if (GameCardsManager.Instancia.GetDupla(actorNumber) == 1)
            area = "AREA01";
        else
            area = "AREA02";

        var jogadas = GameCardsManager.Instancia.GetListaCartasJogo(area).OrderBy(x => x.Portador).Select(x => x.Portador).Distinct().ToList();
        var cartasLixo = GameCardsManager.Instancia.GetListaClone("LIXO").OrderBy(x => x.Peso).ToList();
        var cartasMao = GameCardsManager.Instancia.GetListaClone(jogadorTag).OrderBy(x => x.Peso).ToList();

        // Ver se cartas do lixo servem na jogada
        int qdeServe = 0;
        int ctJogadas = jogadas.Count;
        for (int i = 0; i < ctJogadas; i++)
        {
            List<CartaJogo> jogadasLista = GameCardsManager.Instancia.GetListaCartasJogo(jogadas[i]);
            var naipeLista = GameCardsManager.Instancia.GetNaipeLista(jogadasLista.Select(x => x.Id).ToList());
            if (GameRules.Instancia.VerJogoIguais(jogadasLista.Select(x => x.Id).ToList()) == 0)
            {
                continue;
            }
            bool contJogada = true;
            while (contJogada)
            {
                contJogada = false;
                // Ver se Cartas do Lixo servem na jogada
                int ctCLixo = cartasLixo.Count;
                jogadasLista = jogadasLista.OrderBy(x => x.Peso).ToList();
                ServeNaJogada(cartasLixo, ref qdeServe, jogadasLista, naipeLista, ref contJogada, ctCLixo);

                // Ver se Cartas do Lixo servem na jogada
                int ctCMao = cartasMao.Count;
                jogadasLista = jogadasLista.OrderBy(x => x.Peso).ToList();
                ServeNaJogada(cartasMao, ref qdeServe, jogadasLista, naipeLista, ref contJogada, ctCMao);
            } // while contJogada
        } // for i
        Baralho.Instancia.LimparSelecionados(false);
        return qdeServe;
    }

    private void ServeNaJogada(List<CartaJogo> cartasLista, ref int qdeServe, List<CartaJogo> jogadasLista, int naipeLista, ref bool contJogada, int ctCartas)
    {
        for (int j = 0; j < ctCartas; j++)
        {
            int naipe = GameCardsManager.Instancia.GetCarta(cartasLista[j].Id).Naipe;
            int valor = GameCardsManager.Instancia.GetCarta(cartasLista[j].Id).Valor;
            contJogada = false;
            var lista = jogadasLista.Select(x => x.Id).ToList();
            if (naipe == naipeLista)
            {
                int ct = jogadasLista.Count - 1;
                int pValor = GameCardsManager.Instancia.GetValorPeso(jogadasLista[0].Id) - 1;
                int uValor = GameCardsManager.Instancia.GetValorPeso(jogadasLista[ct].Id) + 1;
                int ind = jogadasLista.FindIndex(x => x.Coringa || (x.Valor == 2 && !x.Neutro2));
                int cValor = 0;
                if (ind != -1)
                    cValor = GameCardsManager.Instancia.GetValorPeso(jogadasLista[ind].Id);
                if (valor == pValor || valor == uValor || valor == cValor) // lixo serve na jogada
                {
                    qdeServe++;
                    jogadasLista.Add(cartasLista[j]);
                    contJogada = true;
                    motivoDebug += "QdeS:" + jogadasLista[0].Portador + "-" + valor.ToString() + "/ ";
                }
            }
        } // for j
    }

    private bool AnalisarVaiLixar()
    {
        bool ret = false;
        bool toNoBat = false;

        string jogadorTag = GameCardsManager.Instancia.GetJogador();
        int actorNumber = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"]; // PhotonNetwork.LocalPlayer.ActorNumber;
        string area;
        if (GameCardsManager.Instancia.GetDupla(actorNumber) == 1)
            area = "AREA01";
        else
            area = "AREA02";

        List<int> cartaFaltante = new List<int>(); // peso
        int valAtual = 0;
        int valAnterior = 0;
        int naipe = 0;
        var cartas = GameCardsManager.Instancia.GetListaCartasJogo(jogadorTag).OrderBy(x => x.Peso).ToList();
        // Cartas faltantes na  mao
        for (int i = 0; i < cartas.Count; i++)
        {
            int item;
            var carta = cartas[i];
            if (naipe != carta.Naipe)
            {
                naipe = carta.Naipe;
                valAtual = 0;
                valAnterior = 0;
            }
            valAtual = carta.Valor;
            if (valAtual != valAnterior + 1 && valAnterior != 0 && valAtual > valAnterior + 1)
            {
                if ((valAnterior - 1) > 0)
                {
                    item = GameCardsManager.Instancia.GetPesoCartaGer((valAnterior - 1), naipe);
                    cartaFaltante.Add(item);
                }
                if ((valAtual + 1) < 14)
                {
                    item = GameCardsManager.Instancia.GetPesoCartaGer((valAtual + 1), naipe);
                    cartaFaltante.Add(item);
                }
                if ((valAnterior - 2) > 0)
                {
                    item = GameCardsManager.Instancia.GetPesoCartaGer((valAnterior - 2), naipe);
                    cartaFaltante.Add(item);
                }
                if ((valAtual + 2) < 14)
                {
                    item = GameCardsManager.Instancia.GetPesoCartaGer((valAtual + 2), naipe);
                    cartaFaltante.Add(item);
                }

                if (valAtual - valAnterior <= 4)
                {
                    for (int j = valAnterior + 1; j < valAtual; j++)
                    {
                        item = GameCardsManager.Instancia.GetPesoCartaGer(j, naipe);
                        if (!cartaFaltante.Contains(item))
                        {
                            if (GameCardsManager.Instancia.GetListaCartasJogo(jogadorTag).FindIndex(x => x.Valor == j && x.Naipe == naipe) == -1)
                                cartaFaltante.Add(item); // não tem na mão nem na jogada
                        }
                    }
                }
            }
            valAnterior = valAtual;
        };

        // Ver se carta do lixo serve na jogada ou mão
        var jogadas = GameCardsManager.Instancia.GetListaCartasJogo(area).OrderBy(x => x.Portador).Select(x => x.Portador).Distinct().ToList();
        int ctJogadas = jogadas.Count;
        var cartasLixo = GameCardsManager.Instancia.GetListaCartasJogo("LIXO").OrderBy(x => x.Peso).ToList();
        int qdeLixo = cartasLixo.Count;
        int qdeJogada = 0; // qde de jogadas que carta do lixo serve
        valAtual = 0;
        valAnterior = 0;
        int seq = 0;
        int qdeServeFalta = 0;
        int qdeMao = cartas.Count;
        naipe = 0;
        bool lixarObrigatorio = false;
        for (int i = 0; i < qdeLixo; i++)
        {
            Baralho.Instancia.LimparSelecionados(false);
            // ver se serve na mão
            int idCarta = cartasLixo[i].Id;
            if (cartasLixo[i].Valor != 1 && cartasLixo[i].Valor != 14 && cartas.FindIndex(x => x.Peso == cartasLixo[i].Peso) != -1) // já tem a carta na mão
                continue;

            if (cartaFaltante.Count > 0)
            {
                int cartaLixo = GameCardsManager.Instancia.GetPesoCartaGer(cartasLixo[i].Valor, cartasLixo[i].Naipe);
                bool incluir = true;
                if (GameCardsManager.Instancia.GetListaCartasJogo(jogadorTag).FindIndex(x => x.Valor == cartasLixo[i].Valor && x.Naipe == cartasLixo[i].Naipe) != -1)
                    incluir = false; // ja tem a carta na mão
                if (incluir)
                {
                    if (GameCardsManager.Instancia.GetListaCartasJogo(area).FindIndex(x => x.Valor == cartasLixo[i].Valor && x.Naipe == cartasLixo[i].Naipe) != -1)
                        incluir = false; // ja tem a carta no jogo
                }
                if (incluir && cartaFaltante.Contains(cartaLixo))
                {
                    qdeServeFalta++;
                }
            }

            // ver se serve na jogada
            Baralho.Instancia.Selecionar(idCarta, false);
            if (cartasLixo[i].Naipe != naipe)
            {
                valAnterior = 0;
                naipe = cartasLixo[i].Naipe;
            }
            valAtual = cartasLixo[i].Valor;
            if (valAtual == valAnterior + 1)
                seq++;
            valAnterior = valAtual;
            bool serve = false;
            bool analisaAS = cartasLixo[i].Valor == 1 || cartasLixo[i].Valor == 14;
            bool jogoAS = false;
            for (int j = 0; j < ctJogadas; j++)
            {
                if (analisaAS) // analisando AS
                {
                    // Ver se tem jogo de AS
                    if (GameRules.Instancia.VerJogoIguais(GameCardsManager.Instancia.GetListaCartasJogo(jogadas[j]).Select(x => x.Id).ToList()) == 0)
                    {
                        jogoAS = true;
                    }
                }
                int idCartaJogada = GameCardsManager.Instancia.GetListaCartasJogo(jogadas[j])[0].Id;
                int qdeFuturo = 0;
                if (CartaServe(idCartaJogada, true, "", ref qdeFuturo)) // serve
                {
                    qdeCartasBaixadas++;
                    serve = true;
                    int qdeJogadaAux = GameCardsManager.Instancia.GetQdeCartaPortador(jogadas[j]);
                    bool coringaNaJogada = GameCardsManager.Instancia.GetListaCartasJogo(jogadas[j]).FindIndex(x => x.Coringa || (x.Valor == 2 && !x.Neutro2)) != -1;
                    if ((qdeFuturo >= 11 && qdeJogadaAux > 7) || (qdeFuturo >= 7 && qdeJogadaAux < 7) || qdeJogadaAux == 6 || qdeJogadaAux == 13)
                    {
                        motivoDebug += "<qdeJogada == 6 || qdeJogada == 13 (obrig) : ";
                        if (!jogadas[j].Contains("S")) // canastra limpa
                        {
                            lixarObrigatorio = true;
                            motivoDebug += "!Contains('S')/ ";
                        }
                        else if (!ToNoBati())
                        {
                            lixarObrigatorio = true;
                            motivoDebug += "!TaNoBati()/ ";
                        }
                        motivoDebug += ">/ ";
                    }
                    else if (!ToNoBati() && qdeLixo <= 5 && qdeJogadaAux < 7)
                    {
                        motivoDebug += "!ToNoBati() && qdeLixo <= 5/ ";
                        lixarObrigatorio = true;
                    }
                    else if (coringaNaJogada && qdeMao >= 4 && qdeLixo <= 6 && qdeJogadaAux < 7)
                    {
                        motivoDebug += "coringaNaJogada && qdeMao >= 4 && qdeLixo <= 6/ ";
                        lixarObrigatorio = true;
                    }
                    else if (qdeJogadaAux >= 11)
                    {
                        if (!MilMorto(jogadas[j]))
                        {
                            motivoDebug += "Mil/ ";
                            lixarObrigatorio = true;
                        }
                    }
                    else if (!ToNoBati() && RandPerc(10))
                    {
                        motivoDebug += "!ToNoBati() && 10%/ ";
                        lixarObrigatorio = true;
                    }
                }
            }
            if (analisaAS && jogoAS)
            {
                lixarObrigatorio = true;
                motivoDebug += "lixar AS/ ";
            }
            if (serve)
                qdeJogada++;
        }

        motivoDebug += " serve jog: " + qdeJogada.ToString() + "/ ";

        int qdeBaixar = BaixarSequencia(jogadorTag, true, cartasLixo);
        qdeBaixar += VerServeNaJogada();

        if (qdeBaixar >= 3)
        {
            if (qdeBaixar > 5)
            {
                motivoDebug += "qdeBaixar > 5/ ";
                lixarObrigatorio = true;
            }
            else if (qdeMao > 4 && qdeLixo <= 4)
            {
                motivoDebug += "qdeMao > 4 && qdeLixo <= 4/ ";
                lixarObrigatorio = true;
            }
            else if (qdeLixo <= 3 && RandPerc(40))
            {
                motivoDebug += "qdeLixo <= 3 && 40%/ ";
                lixarObrigatorio = true;
            }
        }

        if (!lixarObrigatorio)
            toNoBat = ToNoBati();

        if (qdeLixo > 1 && !GameCardsManager.Instancia.GetTemCanastraLimpa(actorNumber) && GameCardsManager.Instancia.GetCartasNaMao() <= 4 && GameCardsManager.Instancia.GetPegouMorto(meuId) && RandPerc(80))
        {
            motivoDebug += "semCanastraLimpa, cartasNaMao <= 4, pegouMorto, 80%/ ";
            lixarObrigatorio = true;
        }
        else if (!GameCardsManager.Instancia.GetPegouMorto(meuId) && GameCardsManager.Instancia.GetCartasNaMao() - qdeCartasBaixadas == 0)
        {
            motivoDebug += "lixar para bater/ ";
            lixarObrigatorio = true;
        }

        if (lixarObrigatorio)
        {
            ret = true;
            motivoDebug += "=obrigatorio/ ";
        }
        else if (toNoBat && RandPerc(90))
        {
            ret = false;
            motivoDebug += "NAO PEGAR taNoBati && RandPerc(90)/ ";
        }
        else if (toNoBat && qdeJogada == 0)
        {
            ret = false;
            motivoDebug += "NAO PEGAR taNoBati && qdeJogada == 0/ ";
        }
        else if (qdeMao > 6 && qdeJogada > 0 && RandPerc(30))
        {
            ret = true;
            motivoDebug += "qdeMao > 6 && qdeJogada > 0  && 40%/ ";
        }
        else if (qdeMao > 3 && qdeJogada > 1 && RandPerc(40))
        {
            ret = true;
            motivoDebug += "qdeMao > 3 && qdeJogada > 1 && 40%/ ";
        }
        else if (qdeMao > 5 && seq >= 2 && qdeLixo <= 6 && RandPerc(60))
        {
            ret = true;
            motivoDebug += "qdeMao > 5 && seq >= 2 && qdeLixo <= 6 && 60%/ ";
        }
        else if (qdeServeFalta >= 2 && qdeLixo <= 6 && RandPerc(50))
        {
            ret = true;
            motivoDebug += "qdeServeFalta >= 3 && qdeLixo <= 6 && 50%/ ";
        }
        else if (qdeServeFalta >= 1 && qdeLixo <= 4 && RandPerc(40))
        {
            ret = true;
            motivoDebug += "qdeServeFalta >= 1 && qdeLixo <= 4 && 40%/ ";
        }
        if (!ret)
        {
            if (qdeMao > 4 && qdeServeFalta > 0 && RandPerc(20))
            {
                ret = true;
                motivoDebug += "qdeMao > 4 && qdeServeFalta > 0 && 20%/ ";
            }
            else if (qdeMao > 4 && RandPerc(30) && qdeLixo <= 5 && (seq > 1 || qdeServeFalta > 0 || qdeJogada > 0))
            {
                ret = true;
                motivoDebug += "qdeMao > 4 && 30% && qdeLixo <= 5 && (seq > 1 || qdeServeFalta > 0 || qdeJogada > 0/ ";
            }
            else if (perfil == 'L' && RandPerc(5, false))
            {
                ret = true;
                motivoDebug += "perfil L/ ";
            }
        }
        if (!lixarObrigatorio && ret)
        {
            if (qdeMao > 15 && RandPerc(70))
            {
                ret = false;
                motivoDebug += "N.PEGA: !lixarObrigatorio && qdeMao > 15 && 70%/ ";
            }
            if (toNoBat && RandPerc(70))
            {
                ret = false;
                motivoDebug += "N.PEGA: taNoBati && 70% && !lixarObrigatorio/ ";
            }
            if (perfil == 'B' && RandPerc(90))
            {
                ret = false;
                motivoDebug += "N.PEGA: perfil B/ ";
            }
            if (qdeMao == 1)
            {
                ret = false;
                motivoDebug += "N.PEGA: 1x1/ ";
            }
        }
        Baralho.Instancia.LimparSelecionados(false);
        return ret;
    }
    private bool Lixar()
    {
        string jogadorTag = GameCardsManager.Instancia.GetJogador();
        GameObject jogador = GameObject.FindGameObjectWithTag(jogadorTag);
        jogador.GetComponent<Jogador>().PuxouDoMonte = false;
        jogador.GetComponent<Jogador>().PegouLixo = false;
        GameCardsManager.Instancia.GetListaCartasJogo("LIXO").
        ForEach(carta =>
        {
            cartasLixadas.Add(carta.Id);
        });
        GameCardsManager.Instancia.GetLixo();
        int cartaInd = GameCardsManager.Instancia.GetListaCartasJogo(jogadorTag)[0].Id;
        return GameRules.Instancia.TratarClick(0, cartaInd, false);
    }

    #endregion Rotina para lixar

    #region Definir Jogada
    private bool DefinirJogada()
    {
        int actorNumber = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        string area;
        if (GameCardsManager.Instancia.GetDupla(actorNumber) == 1)
            area = "AREA01";
        else
            area = "AREA02";

        GameCardsManager.Instancia.SetReorganizaMao();
        Baralho.Instancia.LimparSelecionados(false);

        bool ret = true;
        vulCtrl = GameCardsManager.Instancia.GetVulPontos(actorNumber);
        vulPto = 0;
        if (GameCardsManager.Instancia.GetQdeCartaPortador(area) > 0)
        {
            vulCtrl = 0;
        }
        string jogadorTag = GameCardsManager.Instancia.GetJogador();
        if (GameCardsManager.Instancia.GetQdeCartaPortador(jogadorTag) == 1
            && !GameCardsManager.Instancia.GetTemCanastraLimpa(actorNumber)
            && GameCardsManager.Instancia.GetCartasNaMao() == 2
            && GameCardsManager.Instancia.GetPegouMorto(actorNumber))
        {
            motivoDebug += "Sem canastra/ ";
            return true;
        }

        if (vulCtrl > 0)
            motivoDebug += "Vul " + vulCtrl.ToString() + "/ ";

        // Verifica se baixa cartas com coringa na jogada
        BaixarCoringaJogada(area, jogadorTag);

        // Verifica se alguma carta baixa na jogada
        BaixarNaJogada(area, jogadorTag);

        baixandoVul = false;
        // Verifica se tem alguma sequencia para baixar
        int retCtr = BaixarSequencia(jogadorTag, false);
        return ret;
    }
    private void BaixarCoringaJogada(string area, string jogadorTag)
    {
        var cartasCoringasMao = GameCardsManager.Instancia.GetListaCartasJogo(jogadorTag).Where(x => (x.Coringa || (x.Valor == 2 && x.Neutro2))).OrderBy(x => x.Peso).ToList();
        if (cartasCoringasMao.Count == 0) // não tem coringa para analisar
            return;

        int qdeMao = GameCardsManager.Instancia.GetQdeCartaPortador(jogadorTag);
        var cartasMao = GameCardsManager.Instancia.GetListaCartasJogo(jogadorTag).Where(x => (!x.Coringa && x.Valor != 2) || x.Neutro2).OrderBy(x => x.Peso).ToList();

        var jogadas = GameCardsManager.Instancia.GetListaCartasJogo(area).OrderBy(x => x.Portador).Select(x => x.Portador).Distinct().ToList();

        List<Tuple<string, int>> jogadasPrioridade = new List<Tuple<string, int>>();

        jogadas.ForEach(item =>
        {
            int nQde = GameCardsManager.Instancia.GetQdeCartaPortador(item);
            jogadasPrioridade.Add(new Tuple<string, int>(item, nQde));
        });
        jogadasPrioridade = jogadasPrioridade.OrderByDescending(x => x.Item2).ToList();  // prioridade para jogadas com mais cartas

        int ctCoringa = cartasCoringasMao.Count;
        int ctCartasMao = cartasMao.Count;
        int ctJogadas = jogadasPrioridade.Count;
        if (ctJogadas == 0)
            return;

        List<int> cartasUsadas = new List<int>();
        for (int i = 0; i < ctJogadas; i++)
        {
            int idCartaJogada = GameCardsManager.Instancia.GetListaCartasJogo(jogadasPrioridade[i].Item1).FirstOrDefault().Id;
            int qdeJogada = GameCardsManager.Instancia.GetQdeCartaPortador(jogadasPrioridade[i].Item1);
            bool baixar = false;
            for (int j = 0; j < ctCoringa; j++)
            {
                if (cartasUsadas.Contains(cartasCoringasMao[j].Id))
                    continue;

                for (int k = 0; k < ctCartasMao; k++)
                {
                    if (cartasUsadas.Contains(cartasCoringasMao[j].Id) || cartasUsadas.Contains(cartasMao[k].Id))
                        continue;

                    Baralho.Instancia.LimparSelecionados(false);
                    Baralho.Instancia.Selecionar(cartasCoringasMao[j].Id, false);
                    Baralho.Instancia.Selecionar(cartasMao[k].Id, false);
                    int qdeFuturo = 0;
                    bool joker = cartasCoringasMao[j].Valor == 99;
                    if (CartaServe(idCartaJogada, true, "", ref qdeFuturo))
                    {
                        if (qdeJogada > 3 && qdeJogada < 7 && joker)
                            baixar = true;
                        else if (qdeJogada >= 11 && !MilMorto(jogadasPrioridade[i].Item1) && joker)
                            baixar = true;
                        else if (qdeJogada > 3 && qdeJogada < 7 && !joker && !_naoTemLimpa && (perfil == 'B' || perfil == 'S' || RandPerc(30, false)))
                            baixar = true;
                    }
                    if (baixar)
                    {
                        Baralho.Instancia.LimparSelecionados(false);
                        Baralho.Instancia.Selecionar(cartasCoringasMao[j].Id, false);
                        Baralho.Instancia.Selecionar(cartasMao[k].Id, false);
                        if (CartaServe(idCartaJogada, false, "", ref qdeFuturo))
                        {
                            cartasUsadas.Add(cartasCoringasMao[j].Id);
                            cartasUsadas.Add(cartasMao[k].Id);

                            motivoDebug += "Bx2: " + cartasMao[k].Valor.ToString() + " naipe: " + cartasMao[k].Naipe.ToString() + "/ ";
                        }
                    }
                } // for k
            } // for j
        } // for  i
        Baralho.Instancia.LimparSelecionados(false);
    }

    private int BaixarSequencia(string jogadorTag, bool soVer, List<CartaJogo> cartasLixo = null)
    {
        int actorNumber = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"]; // PhotonNetwork.LocalPlayer.ActorNumber;
        string area;
        if (GameCardsManager.Instancia.GetDupla(actorNumber) == 1)
            area = "AREA01";
        else
            area = "AREA02";
        int qdeJogada = GameCardsManager.Instancia.GetListaCartasJogo(area).Select(x => x.Portador).Distinct().Count();

        int ret = 0;
        #region Baixar sequencia
        int seqAtual = 0;
        int seqAnterior = 0;
        List<int> joker = new List<int>();
        List<int> coringa = new List<int>();
        List<int> AS = new List<int>();
        List<CartaJogo> cartasJogadorCoringa;
        cartasJogadorCoringa = GameCardsManager.Instancia.GetListaCartasJogo(jogadorTag);
        bool repetir = true;
        bool fezJogadaDeAs = false;
        bool taNoVul = vulCtrl > 0;
        bool temLimpaVul = false;
        while (repetir)
        {
            bool jogadaDeAs = false;
            repetir = false;
            joker.Clear();
            coringa.Clear();
            AS.Clear();
            cartasJogadorCoringa.ForEach(carta =>
            {
                if (carta.Portador == jogadorTag)
                {
                    if (carta.Valor == 99)
                        joker.Add(carta.Id);
                    if (carta.Valor == 2 && !carta.Neutro2)
                        coringa.Add(carta.Id);
                    if (carta.Valor == 1 || carta.Valor == 14)
                        AS.Add(carta.Id);
                }
            });

            if (qdeJogada <= 2 && AS.Count >= 3 && (RandPerc(90) || taNoVul))
            {
                fezJogadaDeAs = true;
                jogadaDeAs = true;
            }

            int naipe = 0;
            List<int> listaJogada = new List<int>();
            int pesoAtual = 0;
            int pesoAnterior = 0;
            List<CartaJogo> cartasJogador;
            cartasJogador = GameCardsManager.Instancia.GetListaCartasJogo(jogadorTag).OrderBy(x => x.Peso).ToList();
            if (soVer)
            {
                cartasLixo.ForEach(item =>
                {
                    cartasJogador.Add(item);
                });
            };
            if (!jogadaDeAs)
            {
                if (!fezJogadaDeAs)
                {
                    AS.ForEach(cartaId =>
                    {
                        var item = GameCardsManager.Instancia.GetCarta(cartaId);
                        item.Valor = 14;
                        item.Peso = GameCardsManager.Instancia.GetPesoCartaGer(14, item.Naipe);
                        cartasJogador.Add(item);
                    });
                    cartasJogador = cartasJogador.OrderBy(x => x.Peso).ToList();
                }
                else
                {
                    cartasJogador = cartasJogador.Where(x => x.Valor != 1 && x.Valor != 14).OrderBy(x => x.Peso).ToList();
                }
                bool usouCoringa = false;
                cartasJogador.ForEach(carta =>
                {
                    pesoAtual = carta.Peso; // para excluir cartas repetidas
                    if (pesoAtual != pesoAnterior)
                    {
                        seqAtual = carta.Valor;
                        if (naipe != carta.Naipe)
                        {
                            int aux = carta.Naipe * 1000 + 1 * 10;
                            ret += VerJogadaSeq(ref listaJogada, ref joker, ref coringa, soVer, taNoVul);
                            motivoDebug += "VerSeq Naipe " + ret.ToString() + "/ ";
                            listaJogada.Clear();
                            naipe = carta.Naipe;
                            listaJogada.Add(carta.Id);
                        }
                        else if (seqAtual == seqAnterior + 1)
                        {
                            listaJogada.Add(carta.Id);
                        }
                        else if (seqAtual == seqAnterior + 2 && joker.Count > 0 && !usouCoringa)
                        {
                            listaJogada.Add(joker[0]);
                            joker.RemoveAt(0);
                            listaJogada.Add(carta.Id);
                            usouCoringa = true;
                        }
                        else if (!_naoTemLimpa && perfil == 'S' && seqAtual == seqAnterior + 2 && coringa.Count > 0 && !usouCoringa)
                        {
                            listaJogada.Add(coringa[0]);
                            coringa.RemoveAt(0);
                            listaJogada.Add(carta.Id);
                            usouCoringa = true;
                        }
                        else
                        {
                            ret += VerJogadaSeq(ref listaJogada, ref joker, ref coringa, soVer, taNoVul);
                            motivoDebug += "VerSeq Else " + ret.ToString() + "/ ";
                            listaJogada.Clear();
                            listaJogada.Add(carta.Id);
                        }
                        seqAnterior = carta.Valor;
                    }
                    pesoAnterior = pesoAtual;
                });
            }
            else
            {
                AS.ForEach(cartaId =>
                {
                    listaJogada.Add(cartaId);
                });
            }
            ret += VerJogadaSeq(ref listaJogada, ref joker, ref coringa, soVer, taNoVul);
            if (!temLimpaVul)
            {
                bool tem2 = false;
                listaJogada.ForEach(item =>
                {
                    if (GameCardsManager.Instancia.GetCarta(item).Valor == 2)
                        tem2 = true;

                });
                if (!tem2)
                    temLimpaVul = true;
            }

            if (soVer)
            {
                // verificar se usou carta do lixo em listaJogada
                bool usouLixo = false;
                cartasLixo.ForEach(item =>
                {
                    if (listaJogada.Contains(item.Id))
                        usouLixo = true;
                });
                if (!usouLixo)
                    ret = 0;
            }
            motivoDebug += "VerSeq ret = " + ret.ToString() + "/ ";
            listaJogada.Clear();
            if (vulCtrl > 0 && vulPto >= vulCtrl && temLimpaVul)
            {
                if (vulCtrl > 0)
                    baixandoVul = true;
                vulCtrl = 0;
                vulPto = 0;
                repetir = true;
            }
        }
        #endregion Baixar sequencia
        return ret;
    }

    private void BaixarNaJogada(string area, string jogadorTag)
    {
        #region CartaNaJogada
        bool baixou = true;
        while (baixou)
        {
            // Verificar se alguma carta serve na jogada
            var cartasArea = GameCardsManager.Instancia.GetListaCartasJogo(area);
            var jogadas = GameCardsManager.Instancia.GetListaCartasJogo(area).OrderBy(x => x.Portador).Select(x => x.Portador).Distinct().ToList();

            List<Tuple<string, int>> jogadasPrioridade = new List<Tuple<string, int>>();

            var jogadaAS = "";
            jogadas.ForEach(jog =>
            {
                var cartas = GameCardsManager.Instancia.GetListaCartasJogo(jog).Select(x => x.Id).ToList();
                if (GameRules.Instancia.VerJogoIguais(cartas) == 0)
                    jogadaAS = jog;
            });
            if (jogadaAS != "")
            {
                // baixar na jogada de AS
                jogadasPrioridade.Add(new Tuple<string, int>(jogadaAS, GameCardsManager.Instancia.GetQdeCartaPortador(jogadaAS)));
                int qdeMao = GameCardsManager.Instancia.GetQdeCartaPortador(jogadorTag);
                var cartasMao = GameCardsManager.Instancia.GetListaCartasJogo(jogadorTag).Where(x => (!x.Coringa && x.Valor != 2) || x.Neutro2).OrderBy(x => x.Peso).ToList();
                VerBaixar(jogadasPrioridade, cartasArea, qdeMao, cartasMao);
                jogadasPrioridade.Clear();
            }

            jogadas.ForEach(item =>
            {
                int nQde = GameCardsManager.Instancia.GetQdeCartaPortador(item);
                jogadasPrioridade.Add(new Tuple<string, int>(item, nQde));
            });
            jogadasPrioridade = jogadasPrioridade.OrderByDescending(x => x.Item2).ToList();  // prioridade para jogadas com mais cartas

            AnalisarJogadaPrioridade(ref jogadasPrioridade);

            if (jogadasPrioridade.Count > 0)
            {
                int qdeMao = GameCardsManager.Instancia.GetQdeCartaPortador(jogadorTag);
                var cartasMao = GameCardsManager.Instancia.GetListaCartasJogo(jogadorTag).Where(x => (!x.Coringa && x.Valor != 2) || x.Neutro2).OrderBy(x => x.Peso).ToList();
                baixou = VerBaixar(jogadasPrioridade, cartasArea, qdeMao, cartasMao);
                if (!baixou)
                {
                    cartasMao = GameCardsManager.Instancia.GetListaCartasJogo(jogadorTag).Where(x => x.Coringa || (x.Valor == 2 && !x.Neutro2)).OrderBy(x => x.Peso).ToList();
                    if (cartasMao.Count > 0)
                    {
                        if (cartasMao.Count <= 4) // para batida, sujar menor jogada
                            jogadasPrioridade = jogadasPrioridade.OrderBy(x => x.Item2).ToList(); // Para sujar a menor jogada
                        baixou = VerBaixar(jogadasPrioridade, cartasArea, qdeMao, cartasMao);
                    }
                }
            }
            else
            {
                baixou = false; // não tem jogo baixado
            }
        } //while

        #endregion CartaNaJogada
    }

    private void AnalisarJogadaPrioridade(ref List<Tuple<string, int>> jogadasPrioridade)
    {
        int ct = jogadasPrioridade.Count;
        if (ct <= 1)
            return;
        // Verificar se não tem jogada repetidada com mais prioridade
        List<Tuple<int, int>> troca = new List<Tuple<int, int>>();
        for (int i = 0; i < ct - 1; i++)
        {
            var item = jogadasPrioridade[i];
            var cartas = GameCardsManager.Instancia.GetListaCartasJogo(item.Item1);
            int naipeA = GameCardsManager.Instancia.GetNaipeLista(cartas.Select(x => x.Id).ToList());
            for (int j = i + 1; j < ct; j++)
            {
                var itemAnalise = jogadasPrioridade[j];
                var cartasAnalise = GameCardsManager.Instancia.GetListaCartasJogo(itemAnalise.Item1);
                int naipeB = GameCardsManager.Instancia.GetNaipeLista(cartasAnalise.Select(x => x.Id).ToList());
                if (naipeA == naipeB)
                {
                    var cartasAux = GameCardsManager.Instancia.GetListaCartasJogo().Where(x => x.Portador == item.Item1 || x.Portador == itemAnalise.Item1).Select(x => x.Peso).Distinct().ToList();
                    if (cartasAux.Count < (cartas.Count + cartasAnalise.Count))
                    {
                        if (cartas.Count >= 7 && cartas.Count <= 11 && cartasAnalise.Count < 7) // dar prioridade ao menor
                        {
                            troca.Add(new Tuple<int, int>(i, j));
                        }
                    }
                }
            }
        }
        if (troca.Count > 0)
        {
            motivoDebug += "Troca prior/ ";
            for (int i = 0; i < troca.Count; i++)
            {
                var aux = jogadasPrioridade[troca[i].Item1];
                jogadasPrioridade[troca[i].Item1] = jogadasPrioridade[troca[i].Item2];
                jogadasPrioridade[troca[i].Item2] = aux;
            }
        }
    }
    private bool VerBaixar(List<Tuple<string, int>> jogadasPrioridade, List<CartaJogo> cartasArea, int qdeMao, List<CartaJogo> cartasMao)
    {

        // Ver se tem jogada com AS
        string jogadaAS = "";
        int nContJogadas = jogadasPrioridade.Count;

        for (int j = 0; j < nContJogadas; j++)
        {
            if (!jogadasPrioridade[j].Item1.Contains("S")) // não está sujo
            {
                var cartas = GameCardsManager.Instancia.GetListaCartasJogo(jogadasPrioridade[j].Item1).Select(x => x.Id).ToList();
                if (GameRules.Instancia.VerJogoIguais(cartas) == 0)
                {
                    // Ver se já não está morto
                    if (!AsMorto(jogadasPrioridade[j].Item1) && cartas.Count <= 6)
                        jogadaAS = jogadasPrioridade[j].Item1;
                }
                else if (cartas.Count > 12)
                {
                    if (!MilMorto(jogadasPrioridade[j].Item1))
                        jogadaAS = jogadasPrioridade[j].Item1;
                }
                //if (jogadaAS != "") // verificar se já não tem joker
                //{
                //    if (GameCardsManager.Instancia.GetListaCartasJogo(jogadasPrioridade[j].Item1).FindIndex(x => x.Valor == 99) != -1)
                //    {
                //        jogadaAS = "";
                //    }
                //}
            }
        }

        bool baixou = false;
        for (int i = 0; i < cartasMao.Count; i++)
        {
            var cartaMao = cartasMao[i];
            int idCartaMao = cartaMao.Id;
            string portadorAux = "";

            bool jogoAS = false;
            if (jogadaAS != "" && (cartaMao.Valor == 99 || cartaMao.Valor == 1 || cartaMao.Valor == 14)) // priorizar joker para jogo de AS ou o proprio AS no jogo de AS
            {
                jogoAS = true;
                portadorAux = jogadaAS;
            }

            for (int j = 0; j < nContJogadas; j++)
            {
                if (portadorAux == "" || portadorAux == jogadasPrioridade[j].Item1)
                {
                    bool jogarOK = true;
                    if (jogadasPrioridade[j].Item1.Contains("S")) // já é sujo
                    {
                        motivoDebug += jogadasPrioridade[j].Item1 + "/ ";
                        jogarOK = true;
                    }
                    else
                    {
                        jogarOK = ValidaCarta(
                            GameCardsManager.Instancia.GetListaCartasJogo(jogadasPrioridade[j].Item1).OrderBy(X => X.Peso).ToList(),
                            cartaMao, qdeMao, jogoAS);
                    }
                    if (jogarOK && qdeMao == 2)
                    {
                        // Verificar se parceiro pegou morto e não baixou
                        int parceiro = GameCardsManager.Instancia.GetMeuParceiro();
                        if (GameCardsManager.Instancia.GetPegouMorto() && !GameCardsManager.Instancia.GetJaBaixou(parceiro))
                        {
                            motivoDebug += "parc.pegou morto/ ";
                            jogarOK = false;
                        }
                    }
                    if (jogarOK)
                    {
                        int idCartaJogada = GameCardsManager.Instancia.GetListaCartasJogo(jogadasPrioridade[j].Item1).FirstOrDefault().Id;
                        if (cartaMao.Valor == 14 || cartaMao.Valor == 1 || cartasArea.FindIndex(x => x.Peso == cartaMao.Peso) == -1) // não tem carta da mão no jogo baixado
                        {
                            Baralho.Instancia.LimparSelecionados(false);
                            Baralho.Instancia.Selecionar(idCartaMao, false);
                            int qdeFuturo = 0;
                            if (CartaServe(idCartaJogada, false, "", ref qdeFuturo))
                            {
                                motivoDebug += "Baixou valor: " + cartaMao.Valor.ToString() + " naipe: " + cartaMao.Naipe.ToString() + "/ ";
                                baixou = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        if (cartaMao.Valor == 2)
                        {
                            motivoDebug += "***voltou 2/ ";
                            // voltar peso / valor original do 2
                            GameCardsManager.Instancia.SetPesoOriginal(cartaMao.Id);
                            GameCardsManager.Instancia.SetNeutro2(cartaMao.Id, false);
                            GameCardsManager.Instancia.SetCoringaProvisorio(cartaMao.Id, false);
                            GameCardsManager.Instancia.SetCoringa(cartaMao.Id, false);
                        }
                    }
                }
            };

            if (portadorAux == jogadaAS)
                jogadaAS = "";

            if (baixou)
                break;
        };
        Baralho.Instancia.LimparSelecionados(false);
        return baixou;
    }

    /// <summary>
    /// Ver se vai sujar, ou baixar a carta analisada
    /// </summary>
    /// <param name="cartasJogada"></param>
    /// <param name="cartaAnalisar"></param>
    /// <param name="qdeMao"></param>
    /// <returns></returns>
    private bool ValidaCarta(List<CartaJogo> cartasJogada, CartaJogo cartaAnalisar, int qdeMao, bool jogoAS)
    {
        bool ret = true;
        int indAux = cartasJogada.FindIndex(x => x.Valor == 2); // já tem 2 na jogada
        // Verificar se não vai sujar canastra limpa
        if (cartasJogada.Count >= 7 && indAux != -1 && !cartasJogada[0].Portador.Contains("S"))
        {
            int ultimaCartaAux = cartasJogada[cartasJogada.Count - 1].Valor;
            if (ultimaCartaAux - cartaAnalisar.Valor == 2)
                return false;
            if (ultimaCartaAux == 12 && (cartaAnalisar.Valor == 1 || cartaAnalisar.Valor == 14))
                return false;
        }

        if (DesceTudo() && cartaAnalisar.Valor != 2 && ret)
        {
            motivoDebug += "tudo/ ";
            return true;
        }

        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        int ultimaCarta;
        int primeiraCarta;
        int qdeJogada = cartasJogada.Count;
        int naipe = 0;
        bool temJoker = false;
        List<int> cartasId = new List<int>();
        cartasJogada.ForEach(item =>
        {
            if (item.Valor == 99)
                temJoker = true;
            cartasId.Add(item.Id);
            if (!item.Coringa && item.Valor != 2 && naipe == 0)
            {
                naipe = item.Naipe;
            }
        });

        ultimaCarta = GameCardsManager.Instancia.GetValorPeso(cartasJogada[cartasJogada.Count - 1].Id);
        primeiraCarta = GameCardsManager.Instancia.GetValorPeso(cartasJogada[0].Id); // Convert.ToInt32(cartasJogada[0].Peso.ToString().Substring(1, 2)); // Usar peso pois pode ser um coringa ex: 1031 03=valor
        indAux = cartasJogada.FindIndex(x => x.Valor == 2); // já tem 2 na jogada
        if (cartaAnalisar.Valor != 2 && cartaAnalisar.Valor != 99
            && indAux != -1 && ultimaCarta <= 8 && cartaAnalisar.Valor > 8
            && cartasJogada[indAux].Naipe == naipe)
        {
            ret = false;
            motivoDebug += cartaAnalisar.Peso.ToString() + " não sujar >= 9/ ";
        }
        if (cartaAnalisar.Valor == 2) // ver se compensa sujar ou 2 serve na jogada
        {
            motivoDebug += "***voltou 2 validar/ ";
            GameCardsManager.Instancia.SetPesoOriginal(cartaAnalisar.Id);
            GameCardsManager.Instancia.SetCoringa(cartaAnalisar.Id, false);
            GameCardsManager.Instancia.SetCoringaProvisorio(cartaAnalisar.Id, false);
            if (GameRules.Instancia.VerJogoIguais(cartasId) == 0) // jogada de AS - não sujar
            {
                motivoDebug += "não sujar AS/ ";
                return false;
            }
            bool vaiSujar = true;
            bool jogadaOk = false;
            if (indAux != -1 && RandPerc(10) && GameCardsManager.Instancia.GetTemCanastraLimpa(0)) // já tem 2 e c.limpa >10%
            {
                jogadaOk = true;
                motivoDebug += "já tem 2 e limpo e 10%/ ";
            }
            else if (indAux == -1 && cartaAnalisar.Naipe != naipe && RandPerc(10) && GameCardsManager.Instancia.GetTemCanastraLimpa(0)) // não tem 2 e tem canastra limpa, >10%
            {
                jogadaOk = true;
                motivoDebug += "sujar tem c.limpo e 10%/ ";
            }
            else if (indAux == -1 && cartaAnalisar.Naipe == naipe && ultimaCarta <= 8 && RandPerc(50)) // não tem 2 então 2 será provisorio e >50%
            {
                vaiSujar = false;
                jogadaOk = true;
                motivoDebug += "sera prov e 50%/ ";
            }
            else if (indAux == -1 && cartasJogada.Count == 6 && cartaAnalisar.Naipe == naipe && ultimaCarta == 8) // 2 faz canastra
            {
                vaiSujar = false;
                jogadaOk = true;
                motivoDebug += "2 faz c.limpa/ ";
            }
            else if (indAux == -1 && cartasJogada.Count > 10 && primeiraCarta == 3 && cartaAnalisar.Naipe == naipe)
            {
                vaiSujar = false;
                jogadaOk = true;
                motivoDebug += "2 pode mil/ ";
            }
            if (vaiSujar) // pretende sujar
            {
                ret = true;
                // não sujar SE...
                if (cartasJogada.Count >= 7)
                    ret = false;
                else if (qdeMao <= 3 && (perfil == 'S' || perfil == 'B' || RandPerc(3)) && RandPerc(70, false)) // para bater
                {
                    ret = true;
                }
                else if (cartasJogada.Count < 7 && !_naoTemLimpa)
                {
                    ret = false;
                }
                else if (cartasJogada.Count >= 4 && RandPerc(90))
                {
                    ret = false;
                    if (perfil == 'S' && RandPerc(60, false))
                        ret = true;
                }
                else if (qdeMao > 9) // tentar usar para baixar
                    ret = false;
                else if (!GameCardsManager.Instancia.GetTemCanastraLimpa(localActor))
                    ret = false;
                else if (cartasJogada.Count < 7 && cartasJogada.FindIndex(x => x.Provisorio) != -1 && cartaAnalisar.Valor >= 9 && !ToNoBati())
                {
                    motivoDebug += "carta>=9 ";
                    ret = false;
                }
                else if (!GameCardsManager.Instancia.GetPegouMorto(localActor) && RandPerc(30))
                {
                    motivoDebug += "n.morto e 30%/ ";
                    ret = false;
                }
                if (!GameCardsManager.Instancia.GetPegouMorto(localActor) && qdeMao == 2 && RandPerc(90))
                {
                    motivoDebug += "sujou p/morto/ ";
                    ret = true;
                }
                if (temJoker && cartasJogada.Count > 10)
                {
                    motivoDebug += "Tem JK pode mil/ ";
                    ret = true;
                }
                if (ret)
                    motivoDebug += "sujou/ ";
                else
                    motivoDebug += "desistiu sujo/ ";
            }
            else
            {
                ret = jogadaOk;
            }
        }
        if (cartaAnalisar.Valor == 99) // usar joker
        {
            ret = false;
            if (jogoAS)
            {
                // guardar joker para AS ou usar quando
                if (qdeJogada >= 5 && qdeJogada < 7)
                    ret = true;
                else if (ToNoBati(true))
                    ret = true;
                if (ret)
                    motivoDebug += "JK mesmo tendo AS/ ";
            }
            else if (qdeJogada == 6)
            {
                motivoDebug += "JK qdeJogada == 6/ ";
                ret = true;
            }
            else if (qdeJogada == 5 && qdeMao > 4)
            {
                ret = true;
                motivoDebug += "JK qdeJogada == 5 && qdeMao > 4/ ";
            }
            else if (qdeJogada < 5 && ToNoBati(true))
            {
                ret = true;
                motivoDebug += "JK qdeJogada < 5 && ToNoBati/ ";
            }
            else if (qdeJogada > 12 && RandPerc(50) && !MilMorto(cartasJogada[0].Portador))
            {
                motivoDebug += "JK qdeJogada > 12 && 50%/ ";
                ret = true;
            }
            else if (!GameCardsManager.Instancia.GetPegouMorto(localActor) && qdeMao == 2 && RandPerc(90))
            {
                motivoDebug += "JK N.Morto 90%/ ";
                ret = true;
            }
            else if (ToNoBati())
            {
                motivoDebug += "ToNoBati/ ";
                ret = true;
            }
            else if (RandPerc(3))
            {
                motivoDebug += "JK 3%/ ";
                ret = true;
            }
        }

        return ret;
    }

    private int VerJogadaSeq(ref List<int> listaJogada, ref List<int> joker, ref List<int> coringa, bool soVer, bool taNoVul)
    {
        int ret = 0;
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"]; // PhotonNetwork.LocalPlayer.ActorNumber;
        if (listaJogada.Count == 2 && joker.Count > 0) // uso do joker para formar trinca
        {
            bool usar = false;
            if (RandPerc(30) || DesceTudo() || ToNoBati())
                usar = true;
            else if (taNoVul)
                usar = true;
            if (usar)
            {
                listaJogada.Add(joker[0]);
                joker.RemoveAt(0);
            }
        }
        else if (listaJogada.Count == 2 && coringa.Count > 0 && !taNoVul && (RandPerc(10) || ToNoBati() || DesceTudo())) // sujar
        {
            if (GameCardsManager.Instancia.GetTemCanastraLimpa(localActor) || RandPerc(1) || (ToNoBati() && !GameCardsManager.Instancia.GetPegouMorto()))
            {
                if (!listaJogada.Contains(coringa[0]))
                {
                    listaJogada.Add(coringa[0]);
                    coringa.RemoveAt(0);
                }
            }
        }
        if (listaJogada.Count >= 3)
        {
            if (!soVer)
            {
                bool temJoker = listaJogada.FindIndex(x => GameCardsManager.Instancia.GetCarta(x).Valor == 99) != -1;
                bool tem2Cor = listaJogada.FindIndex(x => GameCardsManager.Instancia.GetCarta(x).Valor == 2 && !GameCardsManager.Instancia.GetCarta(x).Neutro2) != -1;
                string res;
                if (tem2Cor && temJoker)
                    res = "A";
                else
                    res = AnalisarJogada(listaJogada);
                if (res == "0")
                {
                    ret = listaJogada.Count;
                    if (!soVer && ret >= 3)
                        Jogada(listaJogada);
                }
            }
            else
            {
                ret = listaJogada.Count;
                qdeCartasBaixadas += ret;
            }
        }
        return ret;
    }

    /// <summary>
    /// Analisar jogada - se compensa abaixar o jogo
    /// </summary>
    /// <param name="listaJogada"></param>
    /// <param name="soVer"></param>
    /// <returns>0=Baixar, A=Aguardar</returns>
    private string AnalisarJogada(List<int> listaJogada)
    {
        string ret = "0";
        string jogadorTag = GameCardsManager.Instancia.GetJogador();

        int actorNumber = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"]; // PhotonNetwork.LocalPlayer.ActorNumber;
        string area;
        if (GameCardsManager.Instancia.GetDupla(actorNumber) == 1)
            area = "AREA01";
        else
            area = "AREA02";

        int cartasMao = GameCardsManager.Instancia.GetQdeCartaPortador(jogadorTag);
        int naipe = GameCardsManager.Instancia.GetNaipeLista(listaJogada);
        int valorInit = GameCardsManager.Instancia.GetValorCarta(listaJogada[0]);
        if (valorInit == 2 || valorInit == 99)
        {
            valorInit = GameCardsManager.Instancia.GetValorCarta(listaJogada[1]);
        }
        int valorFim = GameCardsManager.Instancia.GetValorCarta(listaJogada[listaJogada.Count - 1]);
        if (valorFim == 2 || valorFim == 99)
        {
            valorFim = GameCardsManager.Instancia.GetValorCarta(listaJogada[listaJogada.Count - 2]);
        }
        int valorJogInit = 0;
        int valorJogFim = 0;

        var jogadas = GameCardsManager.Instancia.GetListaCartasJogo(area).Where(x => x.Naipe == naipe).OrderBy(x => x.Portador).Select(x => x.Portador).Distinct().ToList();
        int qdeAux = 0;
        string portador = "";
        jogadas.ForEach(item =>
        {
            int i = GameCardsManager.Instancia.GetQdeCartaPortador(item);
            if (i > qdeAux)
            {
                qdeAux = i;
                portador = item;
            }
        });

        var listaCartaJogo = GameCardsManager.Instancia.GetListaCartasJogo(portador); // area).Where(x => x.Naipe == naipe).ToList();

        List<int> cartasId = new List<int>();
        listaCartaJogo.ForEach(item =>
        {
            cartasId.Add(item.Id);
        });

        bool iguais = GameRules.Instancia.VerJogoIguais(cartasId) == 0;

        if (listaCartaJogo != null && listaCartaJogo.Count >= 2)
        {
            valorJogInit = listaCartaJogo[0].Valor;
            if (valorJogInit == 2 || valorJogInit == 99)
                valorJogInit = listaCartaJogo[1].Valor;
            valorJogFim = listaCartaJogo[listaCartaJogo.Count - 1].Valor;
            if (valorJogFim == 2 || valorJogFim == 99)
                valorJogFim = listaCartaJogo[listaCartaJogo.Count - 2].Valor;
            int ind = -1;
            if (valorFim < valorJogInit)
            {
                ind = valorJogInit - valorFim;
            }
            else if (valorInit > valorJogFim)
            {
                ind = valorInit - valorJogFim;
            }
            if (ind > 0 && !baixandoVul)
            {
                if (ind <= 4 && !iguais) // 3 cartas de distancia - aguardar para baixar
                {
                    ret = "A";
                    motivoDebug += "'A' 3 cartas de distancia - aguardar para baixar/ ";
                }
                else if (listaCartaJogo.Count >= 11) // guardar para fazer mil
                {
                    ret = "A";
                    motivoDebug += "'A' guardar para fazer mil/ ";
                }
                else if (ind > 4 && RandPerc(30) || (perfil == 'B' && RandPerc(50, false))) // maior distancia e sort 30%
                {
                    ret = "0";
                    motivoDebug += "'0' maior distancia e sort 30% ou Perfil B/ ";
                }
            }
            if (cartasMao - listaCartaJogo.Count < 3)
            {
                ret = "0";
                motivoDebug += "'0' cartasMao - listaCartaJogo.Count < 3/ ";
            }
            if (ret == "A" && DesceTudo())
            {
                ret = "0";
                motivoDebug += "'A' DesceTudo/ ";
            }
        }

        return ret;
    }

    private void Jogada(List<int> listaJogada)
    {
        int pto = 0;
        listaJogada.ForEach(idCarta =>
        {
            Baralho.Instancia.Selecionar(idCarta, false);
            pto += GameCardsManager.Instancia.GetCarta(idCarta).Pontos;
        });

        vulPto += pto;
        if (vulCtrl == 0)
        {
            bool retCtrl = GameRules.Instancia.TratarClick(9100, 0, false);
        }
        else
        {
            Baralho.Instancia.LimparSelecionados();
        }
    }

    #endregion Definir Jogada

    #region Controle de Descarte
    private void ControleDescartar()
    {
        string jogadorTag = GameCardsManager.Instancia.GetJogador();
        int idCarta = 0;
        if (GameCardsManager.Instancia.GetCartasNaMao() > 0)
            idCarta = GameCardsManager.Instancia.GetListaCartasJogo(jogadorTag)[0].Id;

        if (GameCardsManager.Instancia.GetCartasNaMao() == 1)
        {
            motivoDescarte += "UMA CARTA/ ";
            Descartar(idCarta);
            return;
        }
        int actorNumber = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        if (GameCardsManager.Instancia.GetListaCartasJogo(jogadorTag).Count == 0)
        {
            ChatManager.Instancia.EnviarBot("Bati!");
            PegueMorto(actorNumber, false);
            return;
        }

        SequenciarDescarte(actorNumber, jogadorTag);

        int id = AnalisarDescarte();

        if (lixoTemp.Count == 0)
        {
            motivoDescarte += "zero/ ";
            // Primeira carta do jogador se não tiver nenhuma opção para descarte
            lixoTemp.Add(new Tuple<int, int>(GameCardsManager.Instancia.GetListaCartasJogo(jogadorTag)[0].Id, 0));
        }

        idCarta = lixoTemp[id].Item1;

        motivoDescarte += "Carta " + GameCardsManager.Instancia.GetCarta(idCarta).Peso.ToString() + " selecionada/ ";
        if (idCarta == 0)
        {
            idCarta = GameCardsManager.Instancia.GetListaCartasJogo(jogadorTag).FirstOrDefault().Id;
            motivoDescarte += "Carta " + GameCardsManager.Instancia.GetCarta(idCarta).Peso.ToString() + " caiu no == 0/ ";
        }
        lixoTemp.Clear();

        Descartar(idCarta);

        return;
    }

    private void Descartar(int idCarta)
    {
        int actorNumber = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        Baralho.Instancia.LimparSelecionados(false);
        Baralho.Instancia.Selecionar(idCarta, false);
        bool ret = GameRules.Instancia.TratarClick(9000, idCarta, false);
        finalizado = true;

        GameCardsManager.Instancia.SetReorganizaMao();

        FrontManager.Instancia.RedrawJogada(actorNumber, "-");
        FrontManager.Instancia.RedrawOthers(actorNumber);
    }

    private void SequenciarDescarte(int actorNumber, string jogadorTag)
    {
        lixoTemp.Clear();
        List<int> listaAux = new List<int>();
        listaAux = CartasDuplicadas(actorNumber, jogadorTag);
        if (listaAux.Count > 0)
        {
            listaAux.ForEach(item =>
            {
                if (lixoTemp.FindIndex(x => x.Item1 == item) == -1)
                    lixoTemp.Add(new Tuple<int, int>(item, 0));
            });
        }
        listaAux = CartasPorNaipe(jogadorTag);
        if (listaAux.Count > 0)
        {
            listaAux.ForEach(item =>
            {
                if (lixoTemp.FindIndex(x => x.Item1 == item) == -1)
                    lixoTemp.Add(new Tuple<int, int>(item, 0));
            });
        }
        listaAux = CartasOutras(jogadorTag);
        if (listaAux.Count > 0)
        {
            listaAux.ForEach(item =>
            {
                if (lixoTemp.FindIndex(x => x.Item1 == item) == -1)
                    lixoTemp.Add(new Tuple<int, int>(item, 0));
            });
        }
    }

    private List<int> CartasDuplicadas(int actorNumber, string jogadorTag)
    {
        List<int> ret = new List<int>();
        string area;
        if (GameCardsManager.Instancia.GetDupla(actorNumber) == 1)
            area = "AREA01";
        else
            area = "AREA02";
        List<int> pesoAux = new List<int>();
        pesoAux.Add(0);
        var CartasMao = GameCardsManager.Instancia.GetListaCartasJogo(jogadorTag);
        var CartasJogada = GameCardsManager.Instancia.GetListaCartasJogo().Where(x => x.Portador.Contains(jogadorTag) || x.Portador.Contains(area)).ToList();
        for (int i = 0; i < CartasMao.Count; i++)
        {
            var carta = CartasMao[i];
            if ((!carta.Coringa && carta.Valor != 2) || carta.Neutro2)
            {
                int ctAux = CartasJogada.Count(x => x.Peso == carta.Peso);
                if (ctAux >= 2)
                {
                    if (!pesoAux.Contains(carta.Peso) && carta.Valor != 1 && carta.Valor != 14)
                    {
                        ret.Add(carta.Id);
                        pesoAux.Add(carta.Peso);
                    }
                }
            }
        }
        return ret;
    }
    private List<int> CartasPorNaipe(string jogadorTag)
    {
        List<int> ret = new List<int>();
        List<Tuple<int, int>> qdeNaipe = new List<Tuple<int, int>>
        {
            new Tuple<int, int>(1, GameCardsManager.Instancia.GetListaCartasJogo(jogadorTag).Count(x => x.Naipe == 1)),
            new Tuple<int, int>(2, GameCardsManager.Instancia.GetListaCartasJogo(jogadorTag).Count(x => x.Naipe == 2)),
            new Tuple<int, int>(3, GameCardsManager.Instancia.GetListaCartasJogo(jogadorTag).Count(x => x.Naipe == 3)),
            new Tuple<int, int>(4, GameCardsManager.Instancia.GetListaCartasJogo(jogadorTag).Count(x => x.Naipe == 4))
        }; // Tuple<Naipe,Qde>
        qdeNaipe = qdeNaipe.OrderBy(x => x.Item2).ToList();
        // Por menor qde naipe, que não seja sequencia (parzinho)
        int cartaAtualNaipe = 0;
        int cartaAnteriorNaipe = 0;
        for (int i = 0; i < qdeNaipe.Count; i++)
        {
            cartaAtualNaipe = 0;
            cartaAnteriorNaipe = 0;
            GameCardsManager.Instancia.GetListaCartasJogo(jogadorTag).
            Where(x => x.Naipe == qdeNaipe[i].Item1 && x.Valor != 99 && x.Valor != 2).OrderBy(x => x.Peso).ToList().
            ForEach(item =>
            {
                cartaAtualNaipe = item.Valor;
                if ((cartaAtualNaipe - cartaAnteriorNaipe + 1 > 2))
                {
                    if (!ret.Contains(item.Id) && item.Valor != 1 && item.Valor != 14)
                        ret.Add(item.Id);
                }
                cartaAnteriorNaipe = cartaAtualNaipe;
            });
        }
        return ret;
    }
    private List<int> CartasOutras(string jogadorTag)
    {
        List<int> ret = new List<int>();

        // Outras cartas fora coringa e AS
        GameCardsManager.Instancia.GetListaCartasJogo(jogadorTag).
        ForEach(carta =>
        {
            if ((!carta.Coringa && carta.Valor != 2 && carta.Valor != 1 && carta.Valor != 14) || (carta.Neutro2))
            {
                if (!ret.Contains(carta.Id))
                    ret.Add(carta.Id);
            }
        });
        // Outras cartas fora joker
        GameCardsManager.Instancia.GetListaCartasJogo(jogadorTag).
        ForEach(carta =>
        {
            if (carta.Valor != 99)
            {
                if (!ret.Contains(carta.Id))
                    ret.Add(carta.Id);
            }
        });
        // Restante das cartas
        GameCardsManager.Instancia.GetListaCartasJogo(jogadorTag).
        ForEach(carta =>
        {
            if (!ret.Contains(carta.Id))
                ret.Add(carta.Id);
        });

        return ret;
    }

    private int AnalisarDescarte()
    {
        int actorNumber = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        string jogadorTag = GameCardsManager.Instancia.GetJogador();
        int ret = -1;

        string area;
        if (GameCardsManager.Instancia.GetDupla(actorNumber) == 1)
            area = "AREA01";
        else
            area = "AREA02";

        string areaAdversario;
        if (GameCardsManager.Instancia.GetDupla(actorNumber) == 1)
        {
            areaAdversario = "AREA02";
        }
        else
        {
            areaAdversario = "AREA01";
        }

        var jogadasAdversario = GameCardsManager.Instancia.GetListaCartasJogo(areaAdversario).OrderBy(x => x.Portador).Select(x => x.Portador).Distinct().ToList();
        var jogadaAS = ""; // Adversário tem jogo de AS
        List<Tuple<string, int>> jogadasPrioridade = new List<Tuple<string, int>>();
        jogadasAdversario.ForEach(jog =>
        {
            int ctAux = GameCardsManager.Instancia.GetQdeCartaPortador(jog);
            jogadasPrioridade.Add(new Tuple<string, int>(jog, ctAux));
            var cartas = GameCardsManager.Instancia.GetListaCartasJogo(jog).Select(x => x.Id).ToList();
            if (GameRules.Instancia.VerJogoIguais(cartas) == 0)
                jogadaAS = jog;
        });
        int ctJogadasAdv = jogadasAdversario.Count;

        // Tratar cartas quando tiver somente 2 na mão
        if (GameCardsManager.Instancia.GetCartasNaMao() == 2)
        {
            var carta01 = GameCardsManager.Instancia.GetListaCartasJogo(jogadorTag)[0];
            var carta02 = GameCardsManager.Instancia.GetListaCartasJogo(jogadorTag)[1];
            lixoTemp.Clear();
            if (carta01.Valor == 99 || (carta01.Valor == 2 && !carta01.Neutro2))
            {
                // deixar carta01 como 2a.opção
                lixoTemp.Add(new Tuple<int, int>(carta02.Id, 0));
                lixoTemp.Add(new Tuple<int, int>(carta01.Id, 0));
            }
            if (carta02.Valor == 99 || (carta02.Valor == 2 && !carta02.Neutro2))
            {
                // deixar carta02 como 2a.opção
                lixoTemp.Add(new Tuple<int, int>(carta01.Id, 0));
                lixoTemp.Add(new Tuple<int, int>(carta02.Id, 0));
            }

            // ver se algum serve no adversario
            if (lixoTemp.Count == 0)
            {
                // ver se serve para adversario
                int ct01 = 0;
                int ct02 = 0;
                for (int i = 1; i <= 2; i++)
                {
                    Baralho.Instancia.LimparSelecionados(false);
                    var carta = carta01;
                    if (i == 1)
                        carta = carta01;
                    else
                        carta = carta02;
                    Baralho.Instancia.Selecionar(carta.Id, false);
                    for (int iJogada = 0; iJogada < ctJogadasAdv; iJogada++)
                    {
                        int idCartaJogada = GameCardsManager.Instancia.GetListaCartasJogo(jogadasPrioridade[iJogada].Item1)[0].Id;
                        int qdeJogada = GameCardsManager.Instancia.GetQdeCartaPortador(jogadasPrioridade[iJogada].Item1);
                        int qdeFuturo = 0;
                        if (CartaServe(idCartaJogada, true, areaAdversario, ref qdeFuturo))
                        {
                            if (jogadasPrioridade[iJogada].Item1 == jogadaAS && (carta.Valor == 1 || carta.Valor == 14))
                                qdeJogada = 12;
                            if (i == 1)
                                ct01 = qdeJogada;
                            else
                                ct02 = qdeJogada;
                        }
                    }
                } // for i
                if (ct01 != 0 && ct01 > ct02)
                {
                    lixoTemp.Add(new Tuple<int, int>(carta02.Id, 0));
                    lixoTemp.Add(new Tuple<int, int>(carta01.Id, 0));
                }
                else if (ct02 != 0 && ct02 > ct01)
                {
                    lixoTemp.Add(new Tuple<int, int>(carta01.Id, 0));
                    lixoTemp.Add(new Tuple<int, int>(carta02.Id, 0));
                }
                else
                {
                    if (RandPerc(50, false))
                    {
                        lixoTemp.Add(new Tuple<int, int>(carta01.Id, 0));
                        lixoTemp.Add(new Tuple<int, int>(carta02.Id, 0));
                    }
                    else
                    {
                        lixoTemp.Add(new Tuple<int, int>(carta02.Id, 0));
                        lixoTemp.Add(new Tuple<int, int>(carta01.Id, 0));
                    }
                }
            }

            // ver se é futuro
            if (lixoTemp.Count == 0)
            {
                var jogadas = GameCardsManager.Instancia.GetListaCartasJogo(area).OrderBy(x => x.Portador).Select(x => x.Portador).Distinct().ToList();
                int ctJog = jogadas.Count;
                bool ct01 = false; //, ct02 = false; // pode servir em jogada
                for (int i = 0; i < ctJog; i++)
                {
                    var listAux = GameCardsManager.Instancia.GetListaCartasJogo(jogadas[i]).OrderBy(x => x.Peso).ToList();
                    var cartaInicio = listAux[0];
                    if (cartaInicio.Valor == 99 || (cartaInicio.Valor == 2 && !cartaInicio.Neutro2))
                        cartaInicio = listAux[1];
                    var cartaFim = listAux[listAux.Count - 1];
                    if (cartaFim.Valor == 99 || (cartaFim.Valor == 2 && !cartaFim.Neutro2))
                        cartaFim = listAux[listAux.Count - 2];
                    if (carta01.Naipe == GameCardsManager.Instancia.GetListaCartasJogo(jogadas[i])[0].Naipe)
                    {
                        int aux01 = cartaInicio.Valor - carta01.Valor;
                        int aux02 = carta01.Valor - cartaFim.Valor;
                        if ((aux01 <= 2 && aux01 > 0) || (aux02 <= 2 && aux02 > 0))
                            ct01 = true;

                        aux01 = cartaInicio.Valor - carta02.Valor;
                        aux02 = carta02.Valor - cartaFim.Valor;
                    }
                }
                if (ct01)
                {
                    // deixar carta01 como 2a.opção
                    lixoTemp.Add(new Tuple<int, int>(carta02.Id, 0));
                    lixoTemp.Add(new Tuple<int, int>(carta01.Id, 0));
                }
                else
                {
                    // deixar carta02 como 2a.opção
                    lixoTemp.Add(new Tuple<int, int>(carta01.Id, 0));
                    lixoTemp.Add(new Tuple<int, int>(carta02.Id, 0));
                }
            }

            motivoDescarte += "Duas=>" + GameCardsManager.Instancia.GetCarta(lixoTemp[0].Item1).Peso.ToString() + "/ ";
            return 0;
        }

        // Não descartar carta lixada (quando havia 1 carta no lixo)
        if (cartasLixadas.Count == 1)
        {
            int indAux = lixoTemp.FindIndex(x => x.Item1 == cartasLixadas[0]);
            if (indAux != -1 && lixoTemp.Count > 1)
            {
                motivoDebug += "excluido: lixado /";
                lixoTemp.RemoveAt(indAux);
            }
        }
        cartasLixadas.Clear();

        List<int> Excluir = new List<int>();
        jogadasPrioridade = jogadasPrioridade.OrderBy(x => x.Item2).ToList();
        ctJogadasAdv = jogadasAdversario.Count;
        int ct = lixoTemp.Count;
        int idMenor = 0;
        int idMenorQde = 0;
        bool loop = true;
        int iteracao = 0;
        while (loop)
        {
            loop = false;
            idMenor = 0;
            idMenorQde = 0;
            for (int iLixoTemp = 0; iLixoTemp < ct; iLixoTemp++)
            {
                Baralho.Instancia.LimparSelecionados(false);
                // ver se serve para adversário
                var carta = GameCardsManager.Instancia.GetCarta(lixoTemp[iLixoTemp].Item1);
                int idCarta = carta.Id;
                Baralho.Instancia.Selecionar(idCarta, false);
                bool serve = false;
                for (int iJogada = 0; iJogada < ctJogadasAdv; iJogada++)
                {
                    int idCartaJogada = GameCardsManager.Instancia.GetListaCartasJogo(jogadasPrioridade[iJogada].Item1)[0].Id;
                    int qdeJogada = GameCardsManager.Instancia.GetQdeCartaPortador(jogadasPrioridade[iJogada].Item1);
                    int qdeFuturo = 0;
                    if (CartaServe(idCartaJogada, true, areaAdversario, ref qdeFuturo)) // serve para o adversario
                    {
                        if (jogadaAS == jogadasPrioridade[iJogada].Item1 && (carta.Valor == 1 || carta.Valor == 14) && (qdeJogada > 3 && RandPerc(50)) && !AsMorto(jogadasPrioridade[iJogada].Item1))
                        {
                            // Excluir cartas que servem em jogo de AS
                            motivoDescarte += "AS: " + carta.Peso.ToString() + " na " + jogadasPrioridade[iJogada].Item1 + "/ ";
                            Excluir.Add(idCarta);
                        }
                        else if ((qdeFuturo >= 11 && qdeJogada > 7) || (qdeFuturo >= 7 && qdeJogada < 7) || qdeJogada == 6 || (qdeJogada > 11 && !MilMorto(jogadasPrioridade[iJogada].Item1)))
                        {
                            motivoDescarte += "Canastra ou fut.mil/ ";
                            Excluir.Add(idCarta);
                        }
                        else
                        {
                            motivoDescarte += "SERVE: " + carta.Peso.ToString() + " na " + jogadasPrioridade[iJogada].Item1 + "/ ";
                            if (idMenor == 0)
                            {
                                idMenor = iLixoTemp;
                                idMenorQde = qdeJogada;
                            }
                        }
                        serve = true;
                        break;
                    }
                }
                if (!serve)
                {
                    // Ver se carta é futuro
                    if (iteracao == 0)
                    {
                        if (CartaFuturo(area, carta.Id))
                        {
                            Excluir.Add(carta.Id);
                        }
                        else if (carta.Valor == 1 || carta.Valor == 14 || (carta.Valor == 2 && !carta.Neutro2) || carta.Valor == 99)
                        {
                            Excluir.Add(carta.Id);
                        }
                    }
                    else if (carta.Valor == 99)
                    {
                        Excluir.Add(carta.Id);
                    }
                    else
                    {
                        ret = iLixoTemp;
                        motivoDescarte += "Carta " + GameCardsManager.Instancia.GetCarta(lixoTemp[ret].Item1).Peso.ToString() + " não serve/ ";
                        break;
                    }
                }
            }
            if (ret == -1 && Excluir.Count > 0)
            {
                // Não definiu descarte - volta nova analise, sem os "futuros"
                motivoDescarte += "Retorno excluir/ ";
                Excluir.ForEach(id =>
                {
                    if (!lixoTemp.Any(x => x.Item1 == id))
                        lixoTemp.Add(new Tuple<int, int>(id, 0));
                });
                Excluir.Clear();
                ct = lixoTemp.Count;
                loop = true;
                iteracao++;
                if (iteracao >= 2)
                    loop = false;
            }
            if (!loop && ret == -1 && iteracao < 10)
            {
                iteracao = 10;
                loop = true;
                SequenciarDescarte(actorNumber, jogadorTag);
            }
        } // loop
        if (ret == -1)
        {
            ret = idMenor;
            motivoDescarte += "Carta " + GameCardsManager.Instancia.GetCarta(lixoTemp[ret].Item1).Peso.ToString() + " serve menor jogada/ ";
            string msg = "";
            if (RandPerc(50, false))
            {
                msg = Rand(5) switch
                {
                    1 => "Aproveita o lixo que não é sempre!",
                    2 => "Parceiro, precisei jogar esta carta!",
                    3 => "Eita, não tenho o que jogar!",
                    4 => "Tá ficando complicado!",
                    5 => "Manda uma boa também!",
                    _ => "Sem carta pra jogar!",
                };
                ChatManager.Instancia.EnviarBot(msg);
            }
        }
        Baralho.Instancia.LimparSelecionados(false);

        if (ret == -1)
        {
            ret = 0;
            if (lixoTemp.Count > 0)
            {
                motivoDescarte += "Primeira opcao " + GameCardsManager.Instancia.GetCarta(lixoTemp[ret].Item1).Peso.ToString() + "/ ";
            }
        }
        return ret;
    }

    private bool CartaFuturo(string area, int cartaId)
    {
        bool ret = false;

        var carta = GameCardsManager.Instancia.GetCarta(cartaId);
        string jogadorTag = GameCardsManager.Instancia.GetJogador();
        var cartasJogador = GameCardsManager.Instancia.GetListaCartasJogo(jogadorTag);
        var cartasArea = GameCardsManager.Instancia.GetListaCartasJogo(area);
        // Futuro na mão
        int pesoInicio = GameCardsManager.Instancia.GetPesoCartaGer(carta.Valor - 2, carta.Naipe);
        int pesoFim = GameCardsManager.Instancia.GetPesoCartaGer(carta.Valor + 2, carta.Naipe);
        int indAux = cartasJogador.FindIndex(x => x.Naipe == carta.Naipe && (x.Peso >= pesoInicio && x.Peso <= pesoFim));
        int ctQde = cartasJogador.Count(x => x.Peso == carta.Peso);
        ctQde += cartasArea.Count(x => x.Peso == carta.Peso);
        if (ctQde == 1 && indAux != -1) // qde=1 só tem a carta da mão na jogada
        {
            motivoDebug += "Futuro mão/ ";
            ret = true;
        }

        if (!ret)
        {
            // Futuro em jogadas
            var jogadas = cartasArea.OrderBy(x => x.Portador).Select(x => x.Portador).Distinct().ToList();
            jogadas.ForEach(portador =>
            {
                var cartasJogada = GameCardsManager.Instancia.GetListaCartasJogo(portador);
                int pesoInicio = GameCardsManager.Instancia.GetPesoCartaGer(carta.Valor - 2, carta.Naipe);
                int pesoFim = GameCardsManager.Instancia.GetPesoCartaGer(carta.Valor + 2, carta.Naipe);
                int indAux = cartasJogada.FindIndex(x => x.Naipe == carta.Naipe && (x.Peso >= pesoInicio && x.Peso <= pesoFim));
                int ctQde = cartasJogada.Count(x => x.Peso == carta.Peso);
                ctQde += cartasArea.Count(x => x.Peso == carta.Peso);
                if (ctQde == 1 && indAux != -1)
                {
                    motivoDebug += "Futuro " + portador + "/ ";
                    ret = true;
                }
            });
        }

        return ret;
    }
    #endregion Controle de Descarte

    #region Outras jogadas - controle bot
    public void PegueMorto(int actorNumber, bool finalizouJogada)
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"]; // (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"]; // PhotonNetwork.LocalPlayer.ActorNumber;
        //Debug.Log("bot Pegou Morto");
        if (GameCardsManager.Instancia.GetPegouMorto(actorNumber))
        {
            //Debug.Log("parceiro ja pegou");
            return;
        }
        if (GameCardsManager.Instancia.GetQdeCartaPortador("MORTO") == 0)
        {
            //Debug.Log("bot Não tem morto para pegar");
            return;
        }

        int qdeMorto = 0;
        if (GameCardsManager.Instancia.GetQdeCartaPortador("MORTO01") > 0)
            qdeMorto++;
        if (GameCardsManager.Instancia.GetQdeCartaPortador("MORTO02") > 0)
            qdeMorto++;
        qdeMorto = Rand(qdeMorto + 1);
        string morto = "MORTO" + qdeMorto.ToString().PadLeft(2, '0');
        if (GameCardsManager.Instancia.GetQdeCartaPortador(morto) == 0)
        {
            if (qdeMorto == 1)
                qdeMorto = 2;
            else
                qdeMorto = 1;
            morto = "MORTO" + qdeMorto.ToString().PadLeft(2, '0');
        }
        Baralho.Instancia.LimparSelecionados(false);
        int cartaInd = GameCardsManager.Instancia.GetListaCartasJogo(morto)[0].Id;
        Baralho.Instancia.Selecionar(cartaInd, false);
        if (!GameRules.Instancia.TratarClick(9300, cartaInd, false))
            Debug.Log("Erro ao pegar morto");
        else
            ChatManager.Instancia.EnviarBot("Peguei o morto!");
        if (!finalizouJogada)
        {
            //Debug.Log("bot Pegou Morto direto");
            finalizado = true;
            Jogar(localActor, false, false);
        }
        GameCardsManager.Instancia.MostraQuemJoga();
    }
    public void Embaralhar()
    {
        photonView.RPC("EmbaralharRPC", RpcTarget.All);
    }
    [PunRPC]
    private void EmbaralharRPC()
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        if (localActor == 1 && GameCardsManager.Instancia.IsBot())
        {
            //Debug.Log("Embaralhar");
            GameRules.Instancia.NovasCartas();
        }
    }

    /// <summary>
    /// Batida do bot
    /// </summary>
    public void PararBot()
    {
        photonView.RPC("PararBotRPC", RpcTarget.All);
    }
    [PunRPC]
    private void PararBotRPC()
    {
        jogando = false;
    }
    #endregion Outras jogadas

    #region Controle geral jogadas
    private bool ToNoBati(bool semCoringa = false)
    {
        bool ret = false;

        string jogadorTag = GameCardsManager.Instancia.GetJogador();
        int qdeCartasMao = GameCardsManager.Instancia.GetQdeCartaPortador(jogadorTag);
        bool temCoringa = false;
        int valorAtual = 0;
        int valorAnterior = 99;
        int naipe = 0;
        int maiorSeq = 0;
        int seq = 0;
        GameCardsManager.Instancia.GetListaCartasJogo(jogadorTag).OrderBy(x => x.Peso).ToList().
        ForEach(carta =>
        {
            if ((carta.Valor == 2 || carta.Valor == 99) && !carta.Neutro2)
                temCoringa = true && !semCoringa;
            if (naipe != carta.Naipe)
            {
                if (seq > maiorSeq)
                    maiorSeq = seq;
                naipe = carta.Naipe;
                valorAnterior = 99;
                seq = 0;
            }
            valorAtual = carta.Valor;
            if (valorAtual == valorAnterior + 1)
                seq++;
            else
            {
                if (seq > maiorSeq)
                    maiorSeq = seq;
                seq = 0;
            }
            valorAnterior = valorAtual;
        });
        if (seq > maiorSeq)
            maiorSeq = seq;

        ret = DesceTudo();

        if (qdeCartasMao <= 3)
            ret = true;
        if (temCoringa && qdeCartasMao <= 5)
            ret = true;
        if (qdeCartasMao - maiorSeq <= 2)
            ret = true;
        if (qdeCartasMao - maiorSeq <= 3 && temCoringa)
            ret = true;
        if (!GameCardsManager.Instancia.GetTemCanastraLimpa(0))
            ret = false;

        return ret;
    }

    private bool DesceTudo()
    {
        bool ret = false;

        // ver mao dos adversários
        for (int i = 1; i <= 4; i++)
        {
            if (i != meuId)
            {
                bool morto = GameCardsManager.Instancia.GetPegouMorto(i);
                string jogador = "JOGADOR" + i.ToString().PadLeft(2, '0');
                int nQdeMao = GameCardsManager.Instancia.GetQdeCartaPortador(jogador);
                if ((nQdeMao <= 3 || (nQdeMao <= 5 && RandPerc(70))) && morto)
                {
                    ret = true;
                }
            }
        }

        return ret;
    }

    private bool AsMorto(string jogada)
    {
        int ct = GameCardsManager.Instancia.GetListaCartasJogo("AREA01").Count(x => x.Valor == 1 || x.Valor == 14);
        ct += GameCardsManager.Instancia.GetListaCartasJogo("AREA02").Count(x => x.Valor == 1 || x.Valor == 14);
        return ct == 8; // se tiver 8 AS baixados, jogo já está morto/liquidado
    }

    private bool MilMorto(string jogada)
    {
        bool ret = false;
        int naipe = 0;
        int valorJoker = 0;
        GameCardsManager.Instancia.GetListaCartasJogo(jogada).ForEach(carta =>
        {
            if (naipe == 0 && carta.Valor != 2 && carta.Valor != 99)
                naipe = carta.Naipe;
            // .Naipe * 1000 + Valor * 10
            if (carta.Valor == 99)
            {
                valorJoker = GameCardsManager.Instancia.GetValorPeso(carta.Id);
            }
        });
        // Verificar os AS
        int ctAS = GameCardsManager.Instancia.GetListaCartasJogo("AREA01").Count(x =>
            (x.Valor == 1 || x.Valor == 14)
            && (x.Naipe == naipe)
        );
        ctAS += GameCardsManager.Instancia.GetListaCartasJogo("AREA02").Count(x =>
            (x.Valor == 1 || x.Valor == 14)
            && (x.Naipe == naipe)
        );

        // Verificar Outras cartas
        for (int i = 2; i <= 13; i++)
        {
            int ct = GameCardsManager.Instancia.GetListaCartasJogo("AREA01").Count(x =>
                (x.Valor == i)
                && (x.Naipe == naipe)
                && (x.Portador != jogada)
            );
            ct += GameCardsManager.Instancia.GetListaCartasJogo("AREA02").Count(x =>
                (x.Valor == i)
                && (x.Naipe == naipe)
                && (x.Portador != jogada)
            );
            if (ctAS != 0 && ct == 2)
            {
                ret = true;
                break;
            }
        }
        return ret;
    }

    private bool CartaServe(int idCartaJogada, bool soVer, string areaAdversaria, ref int qdeFuturo)
    {
        string portInicial = GameCardsManager.Instancia.GetPortador(idCartaJogada);

        #region Para voltar jogada
        // valor, peso, coringa, provisorio, neutro2, lixo, portador
        List<Tuple<int, int, bool, bool, bool, bool, string>> ctrl = new List<Tuple<int, int, bool, bool, bool, bool, string>>();
        List<int> ctrlId = new List<int>();
        Tuple<int, int, bool, bool, bool, bool, string> ctrlItem;
        GameCardsManager.Instancia.GetListaCartasJogo(portInicial).ForEach(carta =>
        {
            ctrlId.Add(carta.Id);
            ctrlItem = new Tuple<int, int, bool, bool, bool, bool, string>
            (
                carta.Valor,
                carta.Peso,
                carta.Coringa,
                carta.Provisorio,
                carta.Neutro2,
                carta.Lixo,
                carta.Portador
            );
            ctrl.Add(ctrlItem);
        });
        Baralho.Instancia.cartasSel.
        ForEach(cartaId =>
        {
            var carta = GameCardsManager.Instancia.GetCarta(cartaId);
            ctrlId.Add(carta.Id);
            ctrlItem = new Tuple<int, int, bool, bool, bool, bool, string>
            (
                carta.Valor,
                carta.Peso,
                carta.Coringa,
                carta.Provisorio,
                carta.Neutro2,
                carta.Lixo,
                carta.Portador
            );
            ctrl.Add(ctrlItem);
        });
        #endregion Para voltar jogada

        bool ret = GameRules.Instancia.TratarClick(0, idCartaJogada, soVer, areaAdversaria);
        if (ret)
        {
            // verificar impacto de uma carta que serve
            var cartas = GameCardsManager.Instancia.GetListaCartasJogo(portInicial);
            int qde = cartas.Count;
            int primeiro = cartas[0].Valor;
            if (primeiro == 2 || primeiro == 99)
            {
                primeiro = cartas[1].Valor - 1;
            }
            int ultimo = cartas[qde - 1].Valor;
            if (ultimo == 2 || primeiro == 99)
            {
                ultimo = cartas[qde - 2].Valor + 1;
            }
            int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
            int parceiro = GameCardsManager.Instancia.GetMeuParceiro(localActor);
            var adversario = GameCardsManager.Instancia.GetAdversarios(localActor);
            int qdeServe = 0;
            if (!string.IsNullOrEmpty(areaAdversaria))
            {
                actorLixadas[adversario.Item1 - 1].listaIdCarta.ForEach(item =>
                {
                    qdeServe = VerQdeServe(item, primeiro, ultimo, qdeServe);
                });
                actorLixadas[adversario.Item2 - 1].listaIdCarta.ForEach(item =>
                  {
                      qdeServe = VerQdeServe(item, primeiro, ultimo, qdeServe);
                  });
            }
            else
            {
                actorLixadas[localActor - 1].listaIdCarta.ForEach(item =>
                  {
                      qdeServe = VerQdeServe(item, primeiro, ultimo, qdeServe);
                  });
                actorLixadas[parceiro - 1].listaIdCarta.ForEach(item =>
                  {
                      qdeServe = VerQdeServe(item, primeiro, ultimo, qdeServe);
                  });
            }
            qdeFuturo = qde + qdeServe;
        }
        string portFinal = GameCardsManager.Instancia.GetPortador(idCartaJogada);
        if (portInicial != portFinal)
            motivoDebug += " ***PORTADORES: " + (soVer ? "SOVER " : "") + portInicial + " => " + portFinal + "/ ";
        if (soVer || (!ret && portInicial.Contains("AREA")))
        {
            // Voltar jogada
            for (int i = 0; i < ctrlId.Count; i++)
            {
                int id = ctrlId[i];
                GameCardsManager.Instancia.SetValor(id, ctrl[i].Item1);
                GameCardsManager.Instancia.SetPeso(id, ctrl[i].Item2);
                GameCardsManager.Instancia.SetCoringa(id, ctrl[i].Item3);
                GameCardsManager.Instancia.SetCoringaProvisorio(id, ctrl[i].Item4);
                GameCardsManager.Instancia.SetNeutro2(id, ctrl[i].Item5);
                GameCardsManager.Instancia.SetLixo(id, ctrl[i].Item6);
                if (GameCardsManager.Instancia.GetPortador(id) != ctrl[i].Item7)
                    GameCardsManager.Instancia.SetPortador(id, ctrl[i].Item7);
            };
        }
        return ret;
    }

    private int VerQdeServe(int item, int primeiro, int ultimo, int qdeServe)
    {
        if (item >= 0)
        {
            var carta = GameCardsManager.Instancia.GetCarta(item);
            if (carta.Valor >= primeiro - 2 && carta.Valor <= ultimo + 2)
                qdeServe++;
            if (carta.Valor == 1) // testar com 14
            {
                int aux = 1;
                if (aux >= primeiro - 2 && aux <= ultimo + 2)
                    qdeServe++;
            }
        }

        return qdeServe;
    }
    public void SetLixadas(List<int> cartasId, bool lixou)
    {
        int actorNumber = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        if (cartasId == null || cartasId.Count <= 0 || actorNumber > 4)
            return;
        string json = JsonUtility.ToJson(cartasId); //JsonConvert.SerializeObject(cartasId);
        photonView.RPC("SetLixadasRPC", RpcTarget.All, actorNumber, json, lixou);
    }
    [PunRPC]
    private void SetLixadasRPC(int actoNumber, string json, bool lixou)
    {
        List<int> cartasId = JsonUtility.FromJson<List<int>>(json); // JsonConvert.DeserializeObject<List<int>>(json);
        if (GameCardsManager.Instancia.IsBot())
        {
            if (lixou)
            {
                cartasId.ForEach(id =>
                {
                    actorLixadas[actoNumber - 1].listaIdCarta.Add(id);
                });
            }
            else
            {
                int ind = actorLixadas[actoNumber - 1].listaIdCarta.FindIndex(x => x == cartasId[0]);
                if (ind != -1)
                    actorLixadas[actoNumber - 1].listaIdCarta.RemoveAt(ind);
            }
        }
    }

    private void AnaliseGeral()
    {
        string jogadorTag = GameCardsManager.Instancia.GetJogador();

        int actorNumber = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        string area;
        if (GameCardsManager.Instancia.GetDupla(actorNumber) == 1)
            area = "AREA01";
        else
            area = "AREA02";

        List<CartaJogo> cartasAnalise = new List<CartaJogo>();
        GameCardsManager.Instancia.GetListaCartasJogo(area).ForEach(item =>
        {
            item.ctr = "JOGADA";
            cartasAnalise.Add(item);
        });

        List<int> coringa2, joker;
        coringa2 = new List<int>();
        joker = new List<int>();

        GameCardsManager.Instancia.GetListaCartasJogo(jogadorTag).ForEach(item =>
        {
            if (item.Valor == 99)
                joker.Add(item.Id);
            else
            {
                if (item.Valor == 2 && !item.Neutro2)
                    coringa2.Add(item.Id);
                item.ctr = "MAO";
                cartasAnalise.Add(item);
            }
        });
        GameCardsManager.Instancia.GetListaCartasJogo("LIXO").ForEach(item =>
        {
            item.ctr = "LIXO";
            cartasAnalise.Add(item);
        });
        cartasAnalise = cartasAnalise.OrderBy(x => x.Peso).ToList();
    }

    #endregion Controle geral jogadas

    #region Bot responde
    public void FezCanastra(int actorNumber, string tipoCanastra)
    {
        if (!GameCardsManager.Instancia.IsBot())
            return;
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"]; // 
        /*
            ret = "AS"; // AS Sujo
            ret = "CS"; // Canastra Suja
            ret = "AL"; // AS Limpo
            ret = "CL"; // Canastra Limpa
            ret = "RS"; // Real Sujo
            ret = "RL"; // Real Limpo
        */
        bool parceiro = false;
        bool eu = false;
        if (actorNumber == localActor)
            eu = true;
        if (!eu && GameCardsManager.Instancia.GetDupla(localActor) == GameCardsManager.Instancia.GetDupla(actorNumber))
            parceiro = true;
        //if (eu || parceiro)
        //    Debug.Log("NÓS Tipo canastra " + tipoCanastra);
        //else
        //    Debug.Log("ELES Tipo canastra " + tipoCanastra);
        if (tipoCanastra.Contains("R") || tipoCanastra == "AL")
        {
            if (eu || parceiro)
                ChatManager.Instancia.EnviarBot("Uhuuuu!!!");
            else
                ChatManager.Instancia.EnviarBot("Oh noooooo!!!");
        }
        else
        {
            if (eu || parceiro)
            {
                if (parceiro)
                {
                    if (RandPerc(50, false))
                    {
                        ChatManager.Instancia.EnviarBot("Boa!!!");
                    }
                }
                else if (RandPerc(15, false))
                {
                    if (tipoCanastra != "CS" && tipoCanastra != "AS")
                        ChatManager.Instancia.EnviarBot("Jogo muito, kkkk!!!");
                    else
                        ChatManager.Instancia.EnviarBot("Sujo mas tá valendo, kkkk!!!");
                }
            }
            else
                ChatManager.Instancia.EnviarBot("Aff!!!");
        }
    }

    /// <summary>
    /// Bot foi cutucado
    /// </summary>
    /// <param name="actorNumber"></param>
    /// <param name="nome"></param>
    public void Cutucou(int actorNumber, string nome)
    {
        string msg = "";
        if (actorNumber == GameCardsManager.Instancia.GetMeuParceiro())
        {
            if (RandPerc(90, false))
            {
                msg = Rand(5) switch
                {
                    1 => "Parceiro tá bravo, kkk!",
                    2 => "Calma parceiro, kkk!",
                    3 => "Tudo sob controle, kkk!",
                    4 => "To muito lixeiro ? kkk!",
                    5 => "Deixa comigo parceiro, kkk!",
                    _ => "Chega de lixar, kkk!",
                };
            }
            if (RandPerc(70, false))
            {
                // diminui rate de lixar
                rndRate -= (float)Rand(15) / 100;
                if (rndRate <= 0)
                {
                    msg = "PAROU HEIM !!!";
                    rndRate = 0.5f + ((float)Rand(50) / 100);
                }
            }
            else
            {
                // muda perfil
                msg = "Opa, vamos melhorar !!!";
                SortPerfil();
            }
        }
        else
        {
            msg = Rand(5) switch
            {
                1 => "Ai, qual problema ?",
                2 => "Isso, gasta os cutucos, kkk!",
                3 => "Não sinto nada, kkk!",
                4 => "Cosquinha!!!",
                5 => "Já te cutuco de volta!",
                _ => "Chega..!",
            };
        }
        if (!string.IsNullOrEmpty(msg))
            ChatManager.Instancia.EnviarBot(msg);

        GameManager.Instancia.PodeCutucar(1, actorNumber);
    }

    public void MsgDebug()
    {
        motivoDebug += " << " + motivoDescarte + " >>";
        if (GestorDeRede.Instancia.BotDebug && !string.IsNullOrEmpty(motivoDebug))
        {
            motivoDebug = "'" + perfil + "' " + motivoDebug;
            GameCardsManager.Instancia.MsgDebug(motivoDebug, this.meuId);
        }
    }

    public void LerMsg(string msg, int actorNumber)
    {
        if (GameCardsManager.Instancia.IsBot(actorNumber))
            return;
        photonView.RPC("LerMsgRPC", RpcTarget.All, msg, actorNumber);
    }

    [PunRPC]
    private void LerMsgRPC(string msg, int actorNumber)
    {
        if (!GameCardsManager.Instancia.IsBot())
            return;
        string msgRet = "";
        msg = msg.Replace(" ", ",").ToUpper() + "   ";
        msg = LimpaChar(msg);
        List<string> palavras = msg.Split(',').ToList();
        bool pergunta = msg.Contains("?");
        bool parceiro = actorNumber == GameCardsManager.Instancia.GetMeuParceiro();
        string meuNome = GameCardsManager.Instancia.GetNome(0).ToUpper();
        string seuNome = GameCardsManager.Instancia.GetNome(actorNumber);
        bool paraMim = msg.Contains("BOT") || ((msg.Contains("PARCEIRO") || msg.Contains("PARCA")) && parceiro);
        if (palavras.Contains(meuNome))
            paraMim = true;
        bool responder = true;
        if (pergunta)
        {
            if (paraMim)
            {
                if (
                    (palavras.Any(x => x.Contains("TUDO")) && palavras.Any(x => x.Contains("BEM")))
                    || (palavras.Any(x => x.Contains("COMO")) && palavras.Any(x => x.Contains("VAI")))
                    )
                {
                    msgRet = seuNome + ", tudo bem e vc?";
                }
                else if ((palavras.Any(x => x.Contains("QUAL")) && palavras.Any(x => x.Contains("NOME"))) || (palavras.Any(x => x.Contains("QUEM")) && palavras.Any(x => x.Contains("V"))))
                {
                    msgRet = "Eu sou " + meuNome + "!";
                }
                else if (palavras.Any(x => x.Contains("VAMOS")))
                {
                    msgRet = "Vamos sim parceiro!";
                }
                else if (palavras.Any(x => x.Contains("QUAL")) && palavras.Any(x => x.Contains("PERFIL")))
                {
                    string perfilDes = "Desconhecido";
                    switch (perfil)
                    {
                        case 'L':
                            perfilDes = "Lixeiro";
                            break;
                        case 'B':
                            perfilDes = "Baixar";
                            break;
                        case 'C':
                            perfilDes = "Conservador";
                            break;
                        case 'S':
                            perfilDes = "Sujar";
                            break;
                    }
                    msgRet = "Sou do perfil " + perfilDes + ".";
                }
                else
                {
                    msgRet = "Oi " + seuNome + "! Não entendi a pergunta...";
                }
            }
            else
            {
                if (paraMim && palavras.Any(x => x.Contains("VAMOS")) && palavras.Any(x => x.Contains("LA")))
                {
                    msgRet = "Vamos sim parceiro!";
                }
                else if (palavras.Any(x => x.Contains("TUDO")) && palavras.Any(x => x.Contains("BEM")))
                {
                    msgRet = "Tudo bem e vcs?";
                }
            }
        }
        else
        {
            responder = true; // RandPerc(60, false);
            if (palavras.Any(x => x.Contains("OI")) && (msg.Substring(0, 2) == "OI" || palavras.Any(x => x.Contains("GENTE")) || palavras.Any(x => x.Contains("PESSOAL"))))
            {
                msgRet = "Oi! Pessoal!";
            }
            else if (palavras.Any(x => x.Contains("AFF")) || palavras.Any(x => x.Contains("EITA")))
            {
                if (parceiro)
                    msgRet = "Calma parceiro!";
                else
                    msgRet = "kkkk";
            }
            else if (palavras.Any(x => x.Contains("TUDO")) && palavras.Any(x => x.Contains("BEM")))
            {
                msgRet = "Que bom!";
            }
            else if ((palavras.Any(x => x.Contains("BOA")) || palavras.Any(x => x.Contains("BOM"))) && (palavras.Any(x => x.Contains("DIA")) || palavras.Any(x => x.Contains("TARDE")) || palavras.Any(x => x.Contains("NOITE"))))
            {
                msgRet = msg.ToLower() + "!";
            }
            else if (parceiro && palavras.Any(x => x.Contains("BOA")))
            {
                msgRet = "Valeu!";
            }
        }
        // Genérico Geral
        if (paraMim && (palavras.Any(x => x.Contains("MUDAR")) && palavras.Any(x => x.Contains("DEBUG"))))
        {
            GestorDeRede.Instancia.BotDebug = !GestorDeRede.Instancia.BotDebug;
            if (GestorDeRede.Instancia.BotDebug)
                msgRet = "Ok. Debug ligado!";
            else
                msgRet = "Ok. Debug desligado!";
        }
        if ((palavras.Any(x => x.Contains("QUAL")) && palavras.Any(x => x.Contains("VERSAO"))))
        {
            msgRet = "Estamos usando a versão " + GestorDeRede.Instancia.Versao + ".";
        }
        if (palavras.Any(x => x.Contains("QUE") && palavras.Any(x => x.Contains("HORA"))))
        {
            msgRet = "São " + DateTime.Now.ToString("HH:mm:ss") + " !";
        }
        if (palavras.Any(x => x.Contains("TEMPO")) && palavras.Any(x => x.Contains("JOGO")))
        {
            TimeSpan intervalo = DateTime.Now - GestorDeRede.Instancia.HoraInicio;
            msgRet = "Tempo de jogo: " + intervalo.Hours.ToString() + " horas, " + intervalo.Minutes.ToString() + " minutos e " + intervalo.Seconds.ToString() + " segundos.";
        }
        if (palavras.Any(x => x.Contains("VAMO")) && RandPerc(30, false))
        {
            msgRet = "Onde? kkk";
        }
        if (parceiro && palavras.Any(x => x.Contains("EEE")) && RandPerc(60, false))
        {
            msgRet = msg.Replace(',', ' ').ToLower() + "...";
        }

        if ((palavras.Any(x => x.Contains("ALGUEM")) || palavras.Any(x => x.Contains("QUEM"))) && palavras.Any(x => x.Contains("MORTO")))
        {
            msgRet = "";
            if (palavras.Any(x => x.Contains("NAO")))
            {
                msgRet = "Vou dizer quem pegou.\n";
            }
            else
            {
                msgRet = "Quem pegou morto.\n";
            }
            string morto = "";
            if (GameCardsManager.Instancia.GetJogadorPegouMorto(1))
                morto += (morto.Length > 0 ? "\n" : "") + GameCardsManager.Instancia.GetNome(1) + (GameCardsManager.Instancia.GetJaBaixou(1) ? " (Baixou)" : " (Não baixou)");
            if (GameCardsManager.Instancia.GetJogadorPegouMorto(2))
                morto += (morto.Length > 0 ? "\n" : "") + GameCardsManager.Instancia.GetNome(2) + (GameCardsManager.Instancia.GetJaBaixou(2) ? " (Baixou)" : " (Não baixou)");
            if (GameCardsManager.Instancia.GetJogadorPegouMorto(3))
                morto += (morto.Length > 0 ? "\n" : "") + GameCardsManager.Instancia.GetNome(3) + (GameCardsManager.Instancia.GetJaBaixou(3) ? " (Baixou)" : " (Não baixou)");
            if (GameCardsManager.Instancia.GetJogadorPegouMorto(4))
                morto += (morto.Length > 0 ? "\n" : "") + GameCardsManager.Instancia.GetNome(4) + (GameCardsManager.Instancia.GetJaBaixou(4) ? " (Baixou)" : " (Não baixou)");
            if (string.IsNullOrEmpty(morto))
                morto = "Ninguém";
            msgRet += morto;
        }

        if (
            (palavras.Any(x => x.Contains("QUAL")) && palavras.Any(x => x.Contains("PLACAR")))
            || (palavras.Any(x => x.Contains("QUANTO")) && (palavras.Any(x => x.Contains("JOGO")) || palavras.Any(x => x.Contains("PLACAR"))))
            || (palavras.Any(x => x.Contains("PLACAR")))
        )
        {
            var placarAux = GameCardsManager.Instancia.GetPlacar();
            int itEu = GameCardsManager.Instancia.GetDupla();
            int itPerg = GameCardsManager.Instancia.GetDupla(actorNumber);
            if (itEu == itPerg)
            {
                if (itEu == 1)
                {
                    msgRet = "Nós: " + placarAux.Item1.ToString();
                    msgRet += "\nEles: " + placarAux.Item2.ToString();
                }
                else
                {
                    msgRet = "Nós: " + placarAux.Item2.ToString();
                    msgRet += "\nEles: " + placarAux.Item1.ToString();
                }
            }
            else
            {
                if (itEu == 1)
                {
                    msgRet = "Nós: " + placarAux.Item1.ToString();
                    msgRet += "\nVocês: " + placarAux.Item2.ToString();
                }
                else
                {
                    msgRet = "Nós: " + placarAux.Item2.ToString();
                    msgRet += "\nVocês: " + placarAux.Item1.ToString();
                }
            }
        }

        // Genérico (pode ser pergunta ou não)
        if (palavras.Any(x => x.Contains("LIXEIRO")) || (palavras.Any(x => x.Contains("COMO")) && palavras.Any(x => x.Contains("LIXA"))))
        {
            if (lixei && !parceiro)
                msgRet = "Na próxima eu deixo vc lixar.";
        }
        if (palavras.Any(x => x.Contains("COMPLICA")) || palavras.Any(x => x.Contains("DIFICIL")))
        {
            if (parceiro)
                msgRet = "Parceiro. Deixa que eu resolvo, kkk.";
            else
                msgRet = seuNome + ", continue assim, kkk.";
        }
        if (palavras.Any(x => x.Contains("NAO")) && palavras.Any(x => x.Contains("JOGA")))
        {
            if (parceiro)
                msgRet = "Parceiro. Joga qualquer coisa.";
            else
                msgRet = seuNome + ", manda a boa então, kkk.";
        }
        if (palavras.Any(x => x.Contains("ESQUECI")) && palavras.Any(x => x.Contains("BAIXA")))
        {
            if (parceiro)
                msgRet = "Não esquece na próxima jogada.";
            else
                msgRet = seuNome + ", esquece isso, kkk.";
        }

        if (palavras.Any(x => x.Contains("AZAR")) || palavras.Any(x => x.Contains("PE FRIO")))
        {
            if (parceiro)
                msgRet = "Coloca uma meia pra esquentar o pé.";
            else
                msgRet = "Continue assim, kkk.";
        }
        if (palavras.Any(x => x.Contains("VOU")))
        {
            if (palavras.Any(x => x.Contains("DORMIR")))
                msgRet = "Boa noite!";
            else if (palavras.Any(x => x.Contains("PARA")))
                msgRet = "Ok!";
            else
                msgRet = "Eu também " + msg.Replace(',', ' ').Trim().ToLower().Replace("vou", "quero ir");
        }
        if (palavras.Any(x => x.Contains("BYE")) || palavras.Any(x => x.Contains("TCHAU")) || palavras.Any(x => x.Contains("OYASS")))
        {
            msgRet = "Até mais!";
        }

        if (palavras.Any(x => x.Contains("CHEGA")))
        {
            msgRet = seuNome + " cansou ?";
        }
        if (!parceiro && (palavras.Any(x => x.Contains("EEE")) || palavras.Any(x => x.Contains("EBA"))))
        {
            msgRet = "Afff";
        }

        if (string.IsNullOrEmpty(msgRet) && paraMim)
        {
            msgRet = "Oi, falou comigo ?";
        }

        var placar = GameCardsManager.Instancia.GetPlacar();
        if ((placar.Item1 >= 3000 || placar.Item2 >= 3000) && (palavras.Any(x => x.Contains("VALEU")) || palavras.Any(x => x.Contains("VLW"))))
        {
            msgRet = "Bom jogo, valeu! Vamos mais um ou fim ?";
        }
        if (string.IsNullOrEmpty(msgRet) && Rand(25) == Rand(25))
            msgRet = "Hmm... " + msg.Replace(',', ' ').Trim().ToLower();
        if (!string.IsNullOrEmpty(msgRet) && responder)
        {
            ChatManager.Instancia.EnviarBot(msgRet);
        }
    }
    private string LimpaChar(string msg)
    {
        msg = msg.Replace("Ã", "A");
        msg = msg.Replace("À", "A");
        msg = msg.Replace("Á", "A");
        msg = msg.Replace("É", "E");
        msg = msg.Replace("Í", "I");
        msg = msg.Replace("Ó", "O");
        msg = msg.Replace("Ú", "U");
        msg = msg.Replace("Õ", "O");
        msg = msg.Replace("Ñ", "N");
        msg = msg.Replace("Ç", "C");

        return msg;
    }
    public void Cutucar(int actorNumber, int sec)
    {
        if (GameCardsManager.Instancia.IsBot(actorNumber))
            return;
        photonView.RPC("CutucarRPC", RpcTarget.All, actorNumber);
    }

    [PunRPC]
    private void CutucarRPC(int actorNumber)
    {
        if (GameCardsManager.Instancia.IsBot() && RandPerc(30, false))
        {
            GameCardsManager.Instancia.SetCutucar(0, actorNumber);
        }
    }
    public void GetDadosBot(int actorNumber)
    {
        int parceiroBot = GameCardsManager.Instancia.GetMeuParceiro(actorNumber);
        photonView.RPC("GetDadosBotRPC", RpcTarget.All, actorNumber, parceiroBot);
    }
    [PunRPC]
    private void GetDadosBotRPC(int actorNumber, int parceiroBot)
    {
        var localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        if (localActor == parceiroBot && GameCardsManager.Instancia.IsBot())
        {
            string msgDados = "Perfil: " + perfil;
            GameCardsManager.Instancia.MsgMsg(msgDados, actorNumber);
        }
    }

    #endregion Bot responde

    [Serializable]
    public class CartasLixadas
    {
        public List<int> listaIdCarta { get; set; }
    }
}
