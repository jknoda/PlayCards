using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static GameCardsManager;

public class Baralho : MonoBehaviourPunCallbacks
{
    public Transform areaJogo;
    public Transform cartaUI;
    private const int _qdeCartasJogador = 11; //// 11;
    private const int _qdeCartasMorto = 11; //// 11;

    public string primeiraSelecao;

    public Vector3 scale = new Vector3(1.3f, 1f, 1f);

    public float xStep = 0.2f; // step para distancia baralho do monte

    public string cartasAPI;

    public List<int> cartasSel;

    public List<GameObject> cartas;

    public static Baralho Instancia { get; private set; }
    private void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            cartas = new List<GameObject>();
            cartasSel = new List<int>();
            Instancia = this;
        }
    }

    void Start()
    {
        this.cartasSel.Clear();
        if (GestorDeRede.Instancia.DonoDaSala())
        {
            Iniciar();
        }
        if (GestorDeRede.Instancia.SoVer)
        {
            GameCardsManager.Instancia.GetGameVer();
        }
    }

    public void GetGameVerBaralho()
    {
        ListaCartas baralho;
        // criar jogadores
        int ind = 1;
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            int playerActorNumber = (int)player.CustomProperties["ID"];
            if (playerActorNumber <= 4)
                GameManager.Instancia.CriaJogador(playerActorNumber, true);
            else
                GameManager.Instancia.CriaJogador(ind);
            ind++;
        }
        baralho = GerarCartasRecall();
        this.cartasAPI = JsonUtility.ToJson(baralho);
        GerarBaralhoBuracoCPL(cartasAPI, true);
    }

    public void Iniciar()
    {
        this.GerarCartas();
    }
    private void GerarCartas()
    {
        bool recall = GestorDeRede.Instancia.Recall;
        ListaCartas baralho;
        if (recall)
        {
            GameCardsManager.Instancia.GetGameRecall(PhotonNetwork.CurrentRoom.Name);
            //baralho = GerarCartasRecall();
            //GestorDeRede.Instancia.Recall = false;
        }
        else
        {
            baralho = CriarCartas(2);
            this.cartasAPI = JsonUtility.ToJson(baralho);
            GerarBaralho(this.cartasAPI, recall);
        }

        //this.cartasAPI = JsonUtility.ToJson(baralho);
        //GerarBaralho(this.cartasAPI, recall);
    }

    public void GerarCartasCB()
    {
        ListaCartas baralho = GerarCartasRecall();
        //GestorDeRede.Instancia.Recall = false;
        photonView.RPC("ZerarRecall", RpcTarget.All);
        this.cartasAPI = JsonUtility.ToJson(baralho);
        GerarBaralho(this.cartasAPI, true);
    }


    #region GerarCartas
    /// <summary>
    /// Em caso de recomeço de uma partida interrompida
    /// </summary>
    private ListaCartas GerarCartasRecall()
    {
        ListaCartas baralho = new ListaCartas
        {
            cartas = new List<CartasDeck>()
        };
        GameCardsManager.Instancia.GetListaCartasJogo().OrderBy(x => x.Id).ToList().
        ForEach(cartas =>
        {
            CartasDeck carta = new CartasDeck
            {
                sequencia = cartas.Seq,
                idDeck = cartas.Deck,
                id = cartas.Id,
                valor = (short)cartas.Valor
            };
            if (carta.valor == 1)
                carta.ouValor = 14; // A pode ser 1 ou 14
            else
                carta.ouValor = (short)cartas.Valor;
            carta.peso = cartas.Peso;
            carta.naipe = (short)cartas.Naipe;
            baralho.cartas.Add(carta);
        });
        return baralho;
    }

    private ListaCartas CriarCartas(int qdeDeck = 1)
    {
        ListaCartas retorno = new ListaCartas
        {
            cartas = new List<CartasDeck>()
        };

        CartasDeck carta;

        int id = 0;
        for (short deck = 1; deck <= qdeDeck; deck++)
        {
            // Coringa 1
            carta = new CartasDeck();
            id++;
            carta.id = id - 1;
            carta.idDeck = deck;
            carta.naipe = 0;
            carta.valor = 99;
            carta.ouValor = 99;
            carta.peso = 99;
            retorno.cartas.Add(carta);

            for (short naipe = 1; naipe <= 4; naipe++)
            {
                for (short valor = 1; valor <= 13; valor++)
                {
                    id++;
                    carta = new CartasDeck
                    {
                        idDeck = deck,
                        id = id - 1,
                        naipe = naipe,
                        valor = valor
                    };
                    if (valor == 1) carta.ouValor = 14; // A pode ser 1 ou 14
                    else carta.ouValor = valor;
                    carta.peso = valor;
                    retorno.cartas.Add(carta);
                }
            }

            // Coringa 2
            carta = new CartasDeck();
            id++;
            carta.id = id - 1;
            carta.idDeck = deck;
            carta.naipe = 0;
            carta.valor = 99;
            carta.ouValor = 99;
            carta.peso = 99;
            retorno.cartas.Add(carta);
        }

        Embaralhar(retorno);

        return retorno;
    }
    private void Embaralhar(ListaCartas baralho)
    {
        int seq1, seq2, it = UnityEngine.Random.Range(150, 250);

        int ct = baralho.cartas.Count - 1;

        #region Embaralhar Random
        for (int j = 1; j <= 5; j++)
        {
            int k = (int)Math.Pow(j, 2);
            for (int i = 0; i <= ct; i += k)
            {
                seq1 = i;
                if (seq1 > ct)
                    seq1 = ct;
                seq2 = (i - k) + UnityEngine.Random.Range(k, k+10);
                if (seq2 > ct)
                    seq2 = ct;
                if (seq2 < 0)
                    seq2 = 0;
                var aux = baralho.cartas[seq1];
                baralho.cartas[seq1] = baralho.cartas[seq2];
                baralho.cartas[seq2] = aux;
            }
        }
        for (int i = 1; i <= it; i++)
        {

            seq1 = UnityEngine.Random.Range(0, baralho.cartas.Count);
            seq2 = UnityEngine.Random.Range(0, baralho.cartas.Count);
            var aux = baralho.cartas[seq1];
            baralho.cartas[seq1] = baralho.cartas[seq2];
            baralho.cartas[seq2] = aux;
        }
        #endregion Embaralhar Random

        seq1 = 0;
        baralho.cartas.ForEach(x =>
        {
            x.sequencia = seq1;
            x.id = seq1;
            seq1++;
        });
    }
    #endregion GerarCartas

    private void GerarBaralho(string baralho, bool recall)
    {
        photonView.RPC("GerarBaralhoBuraco", RpcTarget.All, baralho, recall);
        GameCardsManager.Instancia.photonView.RPC("MostraStatus", RpcTarget.All);
        GameCardsManager.Instancia.MostraQuemJoga();
        GameCardsManager.Instancia.photonView.RPC("MostraQdeMonte", RpcTarget.All, "");
        GameCardsManager.Instancia.MsgPlacar();
    }

    #region PunRPC
    [PunRPC]
    private void GerarBaralhoBuraco(string baralhoJson, bool recall)
    {
        if (GameObject.FindGameObjectsWithTag("CARTA").Length > 0)
            return;
        GerarBaralhoBuracoCPL(baralhoJson, recall);
    }

    [PunRPC]
    private void ZerarRecall()
    {
        GestorDeRede.Instancia.Recall = false;
    }


    private void GerarBaralhoBuracoCPL(string baralhoJson, bool recall)
    {
        ListaCartas baralho = JsonUtility.FromJson<ListaCartas>(baralhoJson);
        bool jokerColor01 = false;
        bool jokerColor02 = false;
        GameObject baralhoObj = GameObject.FindGameObjectWithTag("BARALHO");
        foreach (CartasDeck cartaItem in baralho.cartas)
        {
            string cNaipe = "";
            switch (cartaItem.naipe)
            {
                case 0:
                    if (cartaItem.idDeck == 1)
                    {
                        if (jokerColor01)
                        {
                            cNaipe = "JokerBlack";
                        }
                        else
                        {
                            cNaipe = "JokerColor";
                            jokerColor01 = true;
                        }
                    }
                    else
                    {
                        if (jokerColor02)
                        {
                            cNaipe = "JokerBlack";
                        }
                        else
                        {
                            cNaipe = "JokerColor";
                            jokerColor02 = true;
                        }
                    }
                    break;
                case 1:
                    cNaipe = "P";
                    break;
                case 2:
                    cNaipe = "C";
                    break;
                case 3:
                    cNaipe = "E";
                    break;
                case 4:
                    cNaipe = "O";
                    break;
                default:
                    cNaipe = "E";
                    break;
            }
            string cartaNome = cartaItem.naipe > 0 ? cartaItem.valor.ToString() + cNaipe : cNaipe; //cNaipe + (cartaItem.naipe > 0 ? cartaItem.valor.ToString().PadLeft(2, '0') : "");
            string cartaVerso = cartaItem.idDeck == 1 ? "BackColor_Red" : "BackColor_Blue";
            Transform obj;
            obj = Instantiate(cartaUI, baralhoObj.transform.position, Quaternion.identity, this.transform);
            obj.tag = "CARTA";
            obj.GetComponent<Image>().alphaHitTestMinimumThreshold = 01f;
            obj.GetComponent<Carta>().Deck = cartaItem.idDeck;
            obj.GetComponent<Carta>().Id = cartaItem.id;
            obj.GetComponent<Carta>().Verso = cartaVerso;
            obj.GetComponent<Carta>().Nome = cartaNome;
            obj.GetComponent<Carta>().Cor = GestorDeRede.Instancia.GetCor("padrao"); // Color.white;
            obj.GetComponent<Carta>().CorJogada = GestorDeRede.Instancia.GetCor("padrao"); // Color.white;
            obj.GetComponent<Carta>().MostraCarta(false); // cartaVerso);
            obj.SetAsLastSibling();
            this.cartas.Add(obj.gameObject);
            CartaJogo cartaJogo = new CartaJogo
            {
                NovoNaMao = false,
                NovoJogada = false,
                Provisorio = false,

                Deck = cartaItem.idDeck,
                Seq = cartaItem.sequencia,
                SeqFixo = 9000,
                Id = cartaItem.id,
                Peso = cartaItem.naipe * 1000 + cartaItem.valor * 10,
                Valor = cartaItem.valor,
                Naipe = cartaItem.naipe,
                Coringa = cartaItem.valor == 99,
                Neutro2 = false,
                Lixo = false
            };
            int pontos = 0;
            if (cartaItem.valor >= 3 && cartaItem.valor <= 7) pontos = 5;
            if (cartaItem.valor == 2 || (cartaItem.valor >= 8 && cartaItem.valor <= 13)) pontos = 10;
            if (cartaItem.valor == 1 || cartaItem.valor == 14) pontos = 15;
            if (cartaItem.valor == 99) pontos = 20;
            cartaJogo.Pontos = pontos;
            cartaJogo.Portador = "MONTE"; // portador;
            obj.GetComponent<Carta>().PosicaoInicial = obj.transform.position;
            GameCardsManager.Instancia.SetAddCartasJogo(cartaJogo);
        }
        DistribuirCartas(recall);
    }

    private void DistribuirCartas(bool recall) // Faz parte do PUNRPC de GerarBaralho=>GerarBaralhohBuraco
    {
        if (recall)
            DistribuirRecall();
        else
            DistribuirNormal();

        FrontManager.Instancia.RedrawLixoCPL(-1, "");

        int actorNumber = GestorDeRede.Instancia.Dupla01.Item1;
        FrontManager.Instancia.RedrawJogadaCPL(actorNumber, "");
        FrontManager.Instancia.RedrawOthersCPL(actorNumber);

        if (GestorDeRede.Instancia.Dupla02.Item1 > 0)
        {
            actorNumber = GestorDeRede.Instancia.Dupla02.Item1;
            FrontManager.Instancia.RedrawJogadaCPL(actorNumber, "");
            FrontManager.Instancia.RedrawOthersCPL(actorNumber);
        }

        GameCardsManager.Instancia.SetReorganizaMao(false);
    }

    private void DistribuirRecall() // Faz parte do PUNRPC de GerarBaralho=>GerarBaralhohBuraco
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"]; // 
        int mesa = 0;
        GameObject baralhoObj = GameObject.FindGameObjectWithTag("BARALHO");
        GameCardsManager.Instancia.GetListaCartasJogo().
        ForEach(cartaItem =>
        {
            Vector3 objPos = new Vector3(baralhoObj.transform.position.x + mesa * xStep, baralhoObj.transform.position.y, 0);
            GameObject obj = cartas[cartaItem.Id];
            obj.transform.localScale = scale * 0.6f;
            obj.transform.position = objPos;
            string portador = cartaItem.Portador;
            if (portador.Contains("MORTO"))
                obj.transform.position = GameObject.FindGameObjectWithTag(portador).transform.position;
            else if (portador.Contains("JOGADOR"))
            {
                int actorNumber = Convert.ToInt32(portador.Substring(7, 2));
                var ret = GameManager.Instancia.Spawn(localActor, actorNumber);
                Transform spawn = ret.Item1;
                int ind = ret.Item2;
                int inversor = ind <= 1 ? 1 : -1;
                float x = spawn.position.x + 35 * (inversor), y = spawn.position.y;
                obj.transform.position = new Vector3(x, y, 0);
            }
            else if (portador.Contains("MONTE"))
            {
                obj.transform.localScale = scale * 0.6f;
                obj.transform.position = objPos;
                mesa++;
            }
        });
    }

    private void DistribuirNormal() // Faz parte do PUNRPC de GerarBaralho=>GerarBaralhohBuraco
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"]; // 

        int mortoCtrl = 0;
        int jogadorCtrl = 0; // cartas do jogador N

        int mesa = 0;

        int nQdePlayer = PhotonNetwork.PlayerList.Length;
        if (nQdePlayer > 4)
            nQdePlayer = 4;

        int nQdeMorto = 02;
        var cartasJogo = GameCardsManager.Instancia.GetListaCartasJogo();
        int nQdeCartas = cartasJogo.Count;
        int indCarta = 0;
        string portador;
        while (indCarta < nQdeCartas)
        {
            jogadorCtrl++;
            if (jogadorCtrl <= _qdeCartasJogador)
            {
                for (int jog = 0; jog < nQdePlayer; jog++)
                {
                    indCarta++;
                    portador = "JOGADOR" + (jog + 1).ToString().Trim().PadLeft(2, '0');
                    CartaPortador(portador, cartasJogo[indCarta - 1], localActor, mesa);
                }
            }
            else
            {
                mortoCtrl++;
                if (mortoCtrl <= _qdeCartasMorto)
                {
                    for (int morto = 0; morto < nQdeMorto; morto++)
                    {
                        indCarta++;
                        portador = "MORTO" + (morto + 1).ToString().Trim().PadLeft(2, '0');
                        CartaPortador(portador, cartasJogo[indCarta - 1], localActor, mesa);
                    }
                }
                else
                {
                    indCarta++;
                    portador = "MONTE";
                    mesa++;
                    CartaPortador(portador, cartasJogo[indCarta - 1], localActor, mesa);
                }
            }
        }

        #region debug cartas fixas
        bool ok = false; // true; //// false;
        if (ok)
        {
            string portAux = "";
            List<int> auxPeso = new List<int>();

            GameCardsManager.Instancia.SetTrocaPortador("JOGADOR02", "MONTE"); ////

            auxPeso.Clear();
            portAux = "JOGADOR02";
            auxPeso.Add(GameCardsManager.Instancia.GetPesoCartaGer(11, 2));
            auxPeso.Add(GameCardsManager.Instancia.GetPesoCartaGer(12, 2));
            auxPeso.Add(GameCardsManager.Instancia.GetPesoCartaGer(2, 4));
            auxPeso.Add(GameCardsManager.Instancia.GetPesoCartaGer(3, 4));
            auxPeso.Add(GameCardsManager.Instancia.GetPesoCartaGer(4, 4));
            auxPeso.Add(GameCardsManager.Instancia.GetPesoCartaGer(8, 4));
            auxPeso.Add(GameCardsManager.Instancia.GetPesoCartaGer(10, 4));
            auxPeso.Add(GameCardsManager.Instancia.GetPesoCartaGer(11, 4));
            auxPeso.Add(GameCardsManager.Instancia.GetPesoCartaGer(13, 4));
            auxPeso.ForEach(item =>
            {
                int indAux01 = cartasJogo.FindIndex(x => x.Peso == item && x.Portador != portAux);
                if (indAux01 != -1)
                    GameCardsManager.Instancia.SetPortador(cartasJogo[indAux01].Id, portAux);
            });

        }
        #endregion debug cartas fixas
    }
    private void CartaPortador(string portador, CartaJogo cartaJogo, int localActor, int mesaPos)
    {
        GameObject baralhoObj = GameObject.FindGameObjectWithTag("BARALHO");
        Vector3 objPos = new Vector3(baralhoObj.transform.position.x + mesaPos * xStep, baralhoObj.transform.position.y, 0);
        GameObject obj = cartas[cartaJogo.Id];
        obj.transform.localScale = scale * 0.8f;
        obj.transform.position = objPos;
        if (portador.Contains("MORTO"))
            obj.transform.position = GameObject.FindGameObjectWithTag(portador).transform.position;
        else if (portador.Contains("JOGADOR"))
        {
            int actorNumber = Convert.ToInt32(portador.Substring(7, 2));
            var ret = GameManager.Instancia.Spawn(localActor, actorNumber);
            Transform spawn = ret.Item1;
            int ind = ret.Item2;
            int inversor = ind <= 1 ? 1 : -1;
            float x = spawn.position.x + 35 * (inversor), y = spawn.position.y;
            obj.transform.position = new Vector3(x, y, 0);
        }
        cartaJogo.Portador = portador;
    }
    #endregion PunRPC


    #region Metodos 
    public Vector3 Posicao()
    {
        return this.transform.position;
    }
    public void Selecionar(int idCarta, bool mudaCor = true)
    {
        bool selecionar;
        SoundManager.Instancia.PlaySound("selecionar");
        GameObject carta = this.cartas.Find(x => x.GetComponent<Carta>().Id == idCarta);
        if (this.cartasSel.Contains(idCarta))
        {
            selecionar = false;
            int ind = this.cartasSel.FindIndex(x => x == idCarta);
            this.cartasSel.RemoveAt(ind);
            carta.GetComponent<Image>().color = carta.GetComponent<Carta>().Cor;
        }
        else
        {
            selecionar = true;
            if (mudaCor)
                carta.GetComponent<Image>().color = GestorDeRede.Instancia.GetCor("selecao"); // Color.yellow;
            this.cartasSel.Add(idCarta);
        }
        if (this.cartasSel.Count > 0)
        {
            if (string.IsNullOrEmpty(primeiraSelecao))
                primeiraSelecao = GameCardsManager.Instancia.GetPortador(idCarta);
        }
        else
            primeiraSelecao = "";
        if (GameManager.Instancia.ZoomOn && GameManager.Instancia.ZoomSomar)
        {
            Zoom.Instancia.CartaSomar(idCarta, selecionar);
        }
    }
    public void LimparSelecionados(bool mudaCor = true)
    {
        bool jogada = false;
        if (this.cartasSel.Count > 0)
            jogada = GameCardsManager.Instancia.GetPortador(this.cartasSel[0]).Contains("AREA");
        foreach (int idCarta in this.cartasSel)
        {
            GameObject carta = this.cartas.Find(x => x.GetComponent<Carta>().Id == idCarta);
            if (jogada && mudaCor) // cor carta jogada
            {
                Color corAux = GestorDeRede.Instancia.GetCor("jogada");
                GameCardsManager.Instancia.SetCor(idCarta, corAux.r, corAux.g, corAux.b, true, true);
                GameCardsManager.Instancia.SetNovaJogada(idCarta, true);
            }
            else
            {
                carta.GetComponent<Image>().color = carta.GetComponent<Carta>().Cor;
            }
        }
        this.cartasSel.Clear();
        primeiraSelecao = "";
    }
    #endregion Metodos 

    #region classes entidades
    [Serializable]
    public class ListaCartas
    {
        public List<CartasDeck> cartas;
    }

    [Serializable]
    public class CartasDeck
    {
        public int sequencia;
        public short idDeck;
        public int id;
        public short valor;
        public short ouValor;
        public int peso;
        public short naipe;
    }
    #endregion classes entidades
}
