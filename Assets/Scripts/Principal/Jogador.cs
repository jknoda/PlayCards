using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Jogador : MonoBehaviourPunCallbacks //, IPointerDownHandler
{
    [SerializeField] GameObject carta;
    [SerializeField] GameObject pnDireita;
    [SerializeField] GameObject pnEsquerda;
    [SerializeField] GameObject pnDireitaBaixo;
    [SerializeField] GameObject pnEsquerdaBaixo;

    private Player _photonPlayer;

    private int _id;
    public bool PuxouDoMonte { get; set; }
    public bool PegouLixo { get; set; }
    public bool Descartou { get; set; }
    public bool FinalizouJogada { get; set; }
    public bool IniciouJogada { get; set; }
    public bool JaBaixou { get; set; } // se não baixou, desconta 100 pontos da mão
    public bool PrimeiraJogada { get; set; }
    public int QdeCartasNaMao { get; set; }
    public int QdeCartasNoLixo { get; set; }
    public int IdCartaLixo { get; set; } // caso tenha 1 carta no lixo

    /// <summary>
    /// Cartas que estavam na mão antes de descartar
    /// </summary>
    public List<int> UltimaJogada { get; set; }
    public bool Bot { get; set; }

    public bool SoVer { get; set; }

    private DateTime TimeInicio { get; set; }

    private int _sec = 20;
    private bool cutucou;

    void Awake()
    {
        Bot = false;
        SoVer = false;
    }

    private void Update()
    {
        if (IniciouJogada && !FinalizouJogada && !cutucou)
        {
            TimeSpan intervalo = DateTime.Now - TimeInicio;
            if (intervalo.Seconds > _sec)
            {
                BotManager.Instancia.Cutucar(GameCardsManager.Instancia.GetJogadorAtual(), intervalo.Seconds);
                photonView.RPC("SetTimer", RpcTarget.All);
                photonView.RPC("SetCutucou", RpcTarget.All, true);
            }
        }
    }

    [PunRPC]
    private void SetCutucou(bool valor)
    {
        cutucou = valor;
    }

    [PunRPC]
    private void SetTimer()
    {
        TimeInicio = DateTime.Now;
    }

    private void Start()
    {
        if (CompareTag("Untagged"))
        {
            Destroy(this.gameObject);
        }
        else
        {
            InicializaJogada();
            ChatManager.Instancia.Chat(true);
        }
    }

    public void InicializaJogada()
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        PuxouDoMonte = false;
        PegouLixo = false;
        Descartou = false;
        FinalizouJogada = false;
        IniciouJogada = true;
        PrimeiraJogada = true;
        QdeCartasNaMao = GameCardsManager.Instancia.GetCartasNaMao();
        QdeCartasNoLixo = GameCardsManager.Instancia.GetCartasNoLixo();
        if (QdeCartasNoLixo == 1)
            IdCartaLixo = GameCardsManager.Instancia.GetListaCartasJogo("LIXO")[0].Id;
        else
            IdCartaLixo = -1;
        if (UltimaJogada == null)
            UltimaJogada = new List<int>();
        else
            UltimaJogada.Clear();
        if (GestorDeRede.Instancia.FirstBot)
        {
            if (GameCardsManager.Instancia.GetJogadorAtual() == localActor)
            {
                if (this.Bot)
                {
                    //Debug.Log("jogador init");
                    BotManager.Instancia.Jogar(localActor, false, true);
                    GestorDeRede.Instancia.FirstBot = false;
                }
            }
        }
        _sec = UnityEngine.Random.Range(20, 40);
        photonView.RPC("SetTimer", RpcTarget.All);
        photonView.RPC("SetCutucou", RpcTarget.All, false);
    }

    public void FinalizaJogada()
    {
        FinalizouJogada = true;
        IniciouJogada = false;
    }

    public void Inicializa(Player player, bool recall)
    {
        string avatar = GestorDeRede.Instancia.GetAvatar((int)player.CustomProperties["ID"], recall); // player.ActorNumber, recall);
        photonView.RPC("InicializaRPC", RpcTarget.All, PhotonNetwork.LocalPlayer, avatar);
    }

    [PunRPC]
    private void InicializaRPC(Player player, string avatar)
    {
        InicializaRPCCPL(player, avatar);
    }

    public void InicializaRPCCPL(Player player, string avatar)
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"]; // 
        int playerActorNumber = (int)player.CustomProperties["ID"];
        _photonPlayer = player;
        _id = playerActorNumber; // player.ActorNumber;
        if (_id > 4) return;
        this.tag = "JOGADOR" + _id.ToString().Trim().PadLeft(2, '0');
        this.transform.Find("Nome").GetComponent<Text>().text = player.NickName;
        var ret = GameManager.Instancia.Spawn(localActor, playerActorNumber); // player.ActorNumber);
        Transform spawn = ret.Item1;
        this.transform.position = spawn.position;
        this.transform.SetParent(spawn);
        this.GetComponent<Image>().sprite = Resources.Load<Sprite>(avatar);
        int avNumber;
        Int32.TryParse(avatar.Substring(avatar.Length - 2, 2), out avNumber);
        if (avNumber >= 90)
            this.Bot = true;
        else
            this.Bot = false;
        this.SoVer = GestorDeRede.Instancia.SoVer;
        if (!photonView.IsMine)
        {
            ChatManager.Instancia.Chat(false);
        }
    }

    public void IncluirCarta(List<int> itens, bool desfaz)
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"]; // PhotonNetwork.LocalPlayer.ActorNumber;
        string portadorAtual = GameCardsManager.Instancia.GetPortador(itens[0]);
        if (portadorAtual == "MONTE" || portadorAtual.Contains("MORTO"))
            MoverManager.Instancia.MoverCartas(itens[itens.Count - 1], this._id, portadorAtual);
        if (portadorAtual == "LIXO")
            MoverManager.Instancia.MoverLixo();

        string jogador = "JOGADOR" + this._id.ToString().Trim().PadLeft(2, '0');
        int seq = GameCardsManager.Instancia.GetQdeCartaPortador(jogador);
        if (desfaz)
            seq = 0;
        foreach (int idCarta in itens)
        {
            if (desfaz)
            {
                var carta = Baralho.Instancia.cartas.Find(x => x.GetComponent<Carta>().Id == idCarta);
                carta.GetComponent<Carta>().CorJogada = carta.GetComponent<Carta>().Cor;
                carta.GetComponent<Image>().color = carta.GetComponent<Carta>().Cor;
                var cartaCtrl = (GameCardsManager.Instancia.GetCarta(idCarta));
                if (cartaCtrl.Valor == 2)
                {
                    // voltar dados originais do 2, pois ter baixado como coringa
                    GameCardsManager.Instancia.SetCoringa(idCarta, false);
                    GameCardsManager.Instancia.SetCoringaProvisorio(idCarta, false);
                    GameCardsManager.Instancia.SetNeutro2(idCarta, false);
                    GameCardsManager.Instancia.SetLixo(idCarta, false);
                }
                if (cartaCtrl.Valor == 14) 
                {
                    // voltar valor de AS
                    GameCardsManager.Instancia.SetValor(idCarta, 1);
                }
                GameCardsManager.Instancia.SetPesoOriginal(idCarta);
            }
            GameCardsManager.Instancia.SetCartasJogador(idCarta, jogador, !desfaz, seq);
            GameCardsManager.Instancia.SetPesoOriginal(idCarta);
            seq++;
        }
        FrontManager.Instancia.RedrawJogador(localActor, true);
    }

    public void RemoverCarta(int id, int actorNumber, string portador)
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"]; // PhotonNetwork.LocalPlayer.ActorNumber;
        if (actorNumber == -1) // forçar redraw para cartas de AREA
        {
            FrontManager.Instancia.RedrawJogador(localActor, true);
            return;
        }
        if (portador == "LIXO")
        {
            GameCardsManager.Instancia.SetSeqLixo();
        }
        GameCardsManager.Instancia.SetPortador(id, portador);
        if (portador == "LIXO")
        {
            GameCardsManager.Instancia.SetSeqCarta(id, 1);
        }
        FrontManager.Instancia.RedrawJogador(localActor, true);
    }

    public void Msg(string txtMsg, int actorNumber)
    {
        if (!string.IsNullOrEmpty(txtMsg))
            ChatManager.Instancia.photonView.RPC("MsgJogador", RpcTarget.All, txtMsg, actorNumber);
    }

    public void MsgRPC(string txtMsg, int actorNumber)
    {
        //if (PhotonNetwork.LocalPlayer.ActorNumber == actorNumber) return;
        int localActor = PhotonNetwork.LocalPlayer.ActorNumber;
        if (localActor > 4)
            localActor = GestorDeRede.Instancia.SoVerNumber;
        SoundManager.Instancia.PlaySound("chat");
        int pos = GameManager.Instancia.Spawn(localActor, actorNumber).Item2;
        bool direita = pos <= 1; // spawns 0 e 1 posX positivo, 2 e 3 posX negativo
        bool baixo = pos == 1 || pos == 2; // spawns 0 e 1 posX positivo, 2 e 3 posX negativo
        GameObject ob;
        int linhas = txtMsg.Length / 15 + 1;
        int x = txtMsg.Length * 15 + 10;
        if (linhas > 1)
            x = 245;

        if (direita)
        {
            if (baixo)
            {
                pnDireitaBaixo.SetActive(true);
                ob = GameObject.FindGameObjectWithTag("CHATDIREITABAIXO");
            }
            else
            {
                pnDireita.SetActive(true);
                ob = GameObject.FindGameObjectWithTag("CHATDIREITA");
            }
        }
        else
        {
            if (baixo)
            {
                pnEsquerdaBaixo.SetActive(true);
                ob = GameObject.FindGameObjectWithTag("CHATESQUERDABAIXO");
            }
            else
            {
                pnEsquerda.SetActive(true);
                ob = GameObject.FindGameObjectWithTag("CHATESQUERDA");
            }
        }
        GameObject objPos = GameObject.FindGameObjectWithTag("MSGPOS");
        ob.transform.SetParent(objPos.transform);
        ob.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(x, linhas * 30);
        ob.transform.GetComponent<BoxCollider2D>().size = ob.transform.GetComponent<RectTransform>().sizeDelta;
        ob.transform.Find("txtMensagem").GetComponent<Text>().text = txtMsg;
    }
}

