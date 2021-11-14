using Photon.Pun;
using System;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private string _localizacaoPrefab;
    [SerializeField] private Transform[] _spawns;
    [SerializeField] private Transform _jogador;
    [SerializeField] private Button _embaralhar;
    [SerializeField] private GameObject _zPainelMsg;
    [SerializeField] private GameObject _painelStatus;
    [SerializeField] private GameObject _painelPlacar;
    [SerializeField] private GameObject _placarPainel01;
    [SerializeField] private GameObject _placarPainel02;
    [SerializeField] private GameObject _painelMsg;
    [SerializeField] private GameObject _painelChat;
    [SerializeField] private Text _txtPlacar01;
    [SerializeField] private Text _txtPlacar02;

    [SerializeField]
    private GameObject _areaJogo;
    [SerializeField]
    private GameObject _areaZoom;
    [SerializeField]
    private GameObject _areaZoomBotaoSair;
    [SerializeField]
    private GameObject _botoes;

    private GameObject painelMsg;
    private Text painelTexto;
    private Text clickTexto;
    private Text txtWH;

    private bool painelMsgAtivo = false;
    private DateTime painelInicio;

    public bool semTimer = false;
    public int selClick = 0;

    private int _jogadoresEmJogo = 0;

    private float _qdeCutucar;

    public int JogadorInicial { get; set; }

    public bool ZoomOn { get; set; }
    public bool ZoomSomar { get; set; }

    public bool TempoChat { get; set; }

    public static GameManager Instancia { get; private set; }
    private void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            painelMsg = _painelStatus;
            ZoomOn = false;
            ZoomSomar = false;
            _qdeCutucar = 3;
            TempoChat = false;
            Instancia = this;
        }
    }

    private void Start()
    {
        _qdeCutucar = UnityEngine.Random.Range(4, 7);
        if (!GestorDeRede.Instancia.SoVer)
        {
            int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"]; // PhotonNetwork.LocalPlayer.ActorNumber;
            if (localActor > 4)
                _botoes.SetActive(false);
            photonView.RPC("AdicionaJogador", RpcTarget.AllBuffered);
        }
    }

    private void Update()
    {
        if (painelMsgAtivo)
        {
            TimeSpan intervalo = DateTime.Now - painelInicio;
            int tempo = (painelTexto.text.Length / 3);
            if (Input.GetKeyDown(KeyCode.Escape) || (intervalo.Seconds >= tempo && !semTimer))
                DesativaPainelMsg();
        }
    }

    [PunRPC]
    private void AdicionaJogador()
    {
        if (GestorDeRede.Instancia.SoVer)
            return;
        _jogadoresEmJogo++;
        int nQdePlayer = PhotonNetwork.PlayerList.Length;
        if (nQdePlayer > 4) nQdePlayer = 4;
        if (_jogadoresEmJogo == nQdePlayer)
        {
            CriaJogador(0);
        }
    }
    public void CriaJogador(int actorNumber, bool recall = false)
    {
        var jogadorObj = PhotonNetwork.Instantiate(_localizacaoPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        var jogador = jogadorObj.GetComponent<Jogador>();
        if (actorNumber == 0)
        {
            jogador.Inicializa(PhotonNetwork.LocalPlayer, recall);
            _embaralhar.interactable = PhotonNetwork.IsMasterClient;
        }
        else
        {
            string avatar = "Avatar00";
            if (actorNumber <= 4)
                avatar = GestorDeRede.Instancia.GetAvatar(actorNumber, true);
            jogador.InicializaRPCCPL(PhotonNetwork.PlayerList[actorNumber - 1], avatar);
        }
    }

    public float GetCutucadas()
    {
        return _qdeCutucar;
    }

    public Tuple<Transform, int> Spawn(int meuId, int Id)
    {
        if (meuId == -1)
        {
            meuId = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"]; // PhotonNetwork.LocalPlayer.ActorNumber;
        }

        if (meuId > 4)
            meuId = GestorDeRede.Instancia.SoVerNumber;
        if (Id > 4)
            Id = GestorDeRede.Instancia.SoVerNumber;

        int sp;
        if (meuId == Id)
        {
            sp = 0;
        }
        else
        {
            sp = 1;
            int minhaPosicao = GestorDeRede.Instancia.SeqJogadores[meuId - 1];
            int playerPosicao = minhaPosicao + 1; // meuId + 1;
            int nQdePlayer = PhotonNetwork.PlayerList.Length;
            if (nQdePlayer > 4) nQdePlayer = 4;
            for (int ind = 1; ind <= nQdePlayer - 1; ind++)
            {
                if (playerPosicao > nQdePlayer)
                {
                    playerPosicao = 1;
                }
                if (GestorDeRede.Instancia.SeqJogadores[playerPosicao - 1] == Id) break;
                sp++;
                playerPosicao++;
            }
        }
        return new Tuple<Transform, int>(_spawns[sp], sp);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="msg"></param>
    /// <param name="semTimer"></param>
    /// <param name="playSound"></param>
    /// <param name="actorNumber"></param>
    /// <param name="mostraMsg"></param>
    public void MostraMsgMainAll(string msg, bool semTimer, string playSound, int actorNumber, bool mostraMsg = false)
    {
        if (actorNumber > 4 && actorNumber != 99)
        {
            actorNumber = 0;
        }
        photonView.RPC("MostraMsgMainRPC", RpcTarget.All, msg, semTimer, playSound, actorNumber, mostraMsg);
    }

    [PunRPC]
    private void MostraMsgMainRPC(string msg, bool semTimer, string playSound, int actorNumber, bool mostraMsg)
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];

        if (actorNumber == 99 || localActor == 1)
            MostraMsgMain(msg, semTimer, playSound, false, 0, mostraMsg);
        else
        {
            if (actorNumber == 0 || (actorNumber == localActor))
                MostraMsgMain(msg, semTimer, playSound, false, 0, mostraMsg);
        }
    }

    public void MostraMsgMain(string msg, bool semTimer, string playSound, bool status, int actorNumber, bool mostraMsg = false)
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        if (actorNumber != 0 && actorNumber != localActor)
            return;
        this.semTimer = semTimer;
        if (playSound != "0" && !string.IsNullOrEmpty(playSound))
            SoundManager.Instancia.PlaySound(playSound);
        AtivaPainelMsg(status);
        painelTexto.text = msg;
        painelInicio = DateTime.Now;
        if (mostraMsg)
            GameCardsManager.Instancia.MsgMsg(msg, localActor);
    }

    private void AtivaPainelMsg(bool status)
    {
        DesativaPainelMsg();
        if (status)
            painelMsg = _painelStatus;
        else
            painelMsg = _painelMsg;

        txtWH = painelMsg.transform.Find("txtWH").GetComponent<Text>();
        painelTexto = painelMsg.transform.Find("TextoMsg").GetComponent<Text>();
        clickTexto = painelMsg.transform.Find("txtClickMsg").GetComponent<Text>();

        painelMsg.transform.SetSiblingIndex(0);
        if (status)
        {
            painelMsg.SetActive(true);
            _painelPlacar.SetActive(true);
            _txtPlacar01.text = GameCardsManager.Instancia.GetNomeDupla(1, true) + "\n" +
                GestorDeRede.Instancia.Placar01Det;
            _txtPlacar02.text = GameCardsManager.Instancia.GetNomeDupla(2, true) + "\n" +
                GestorDeRede.Instancia.Placar02Det;
            _painelPlacar.transform.GetComponent<Image>().color = GestorDeRede.Instancia.GetCor("placar"); // new Color(0.06420228f, 0.05517978f, 1f);
            painelMsg.transform.GetComponent<Image>().color = GestorDeRede.Instancia.GetCor("status"); //new Color(0.06420228f, 0.05517978f, 1f); // = new Color(1f, 1f, 1f, 1f);
            for (int i = 1; i <= GestorDeRede.Instancia.PlacarGeral01; i++)
            {
                string star = "star" + i.ToString().Trim();
                _placarPainel01.transform.Find(star).gameObject.SetActive(true);
            }
            for (int i = 1; i <= GestorDeRede.Instancia.PlacarGeral02; i++)
            {
                string star = "star" + i.ToString().Trim();
                _placarPainel02.transform.Find(star).gameObject.SetActive(true);
            }
        }
        else
        {
            painelMsg.SetActive(true);
            _painelPlacar.SetActive(false);
            painelMsg.transform.GetComponent<Image>().color = GestorDeRede.Instancia.GetCor("msg"); // new Color(1f, 1f, 1f, 0.3921569f);
        }

        if (semTimer)
            clickTexto.text = "Clique na mensagem";
        else
            clickTexto.text = "";
        if (!GameManager.Instancia.ZoomOn)
        {
            txtWH.text = GestorDeRede.Instancia.GetNomeSala() + " ... " + GestorDeRede.Instancia.Versao; // msg;
        }
        painelMsgAtivo = true;
    }
    public void DesativaPainelMsg()
    {
        painelMsgAtivo = false;
        _painelPlacar.SetActive(false);
        painelMsg.SetActive(false);
        semTimer = false;
    }

    public void AtivarZoom()
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"]; // 
        if (localActor > 4)
            return;
        GameCardsManager.Instancia.SetReorganizaMao();
        ZoomOn = true;
        DesativaPainelMsg();
        _areaZoom.SetActive(true);
        _areaZoomBotaoSair.SetActive(true);
        _areaJogo.SetActive(false);
        painelMsg = _zPainelMsg;
        Zoom.Instancia.InicializaZoom();
    }

    public void DesativarZoom()
    {
        ZoomOn = false;
        DesativaPainelMsg();
        _areaJogo.SetActive(true);
        _areaZoom.SetActive(false);
        _areaZoomBotaoSair.SetActive(false);
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"]; // PhotonNetwork.LocalPlayer.ActorNumber;
        int outroActorNumber;
        if (GestorDeRede.Instancia.Dupla01.Item1 != localActor)
            outroActorNumber = GestorDeRede.Instancia.Dupla01.Item1;
        else
            outroActorNumber = GestorDeRede.Instancia.Dupla02.Item1;
        painelMsg = _painelStatus;
        GameCardsManager.Instancia.GetListaCartasJogo()
        .ForEach(cartaItem =>
        {
            GameCardsManager.Instancia.SetVisible(cartaItem.Id, true);
        });
        FrontManager.Instancia.RedrawJogador(localActor, true); // e redrawothers
        FrontManager.Instancia.RedrawJogada(localActor, "");
        FrontManager.Instancia.RedrawJogada(outroActorNumber, "");
    }

    public void ChatHistor(bool valor)
    {
        _painelChat.SetActive(valor);
    }

    public bool PodeCutucar(float qde, int actorNumber = 0)
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        bool ret = true;
        if (actorNumber != 0)
        {
            photonView.RPC("PodeCutucarRPC", RpcTarget.All, qde, actorNumber);
        }
        else
        {
            _qdeCutucar += qde;
            if (_qdeCutucar < 0)
            {
                _qdeCutucar = 0;
                ret = false;
            }
            else
            {
                int nlimite = UnityEngine.Random.Range(6, 9);
                if (_qdeCutucar > nlimite)
                    _qdeCutucar = nlimite;
                GameManager.Instancia.MostraMsgMain("Cutucadas restantes: " + Math.Truncate(_qdeCutucar).ToString().PadLeft(2, '0'), false, "cutucar", false, 0, true);
            }
        }
        return ret;
    }

    [PunRPC]
    private void PodeCutucarRPC(float qde, int actorNumber)
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        if (localActor == actorNumber)
        {
            _qdeCutucar += qde;
            if (_qdeCutucar < 0)
                _qdeCutucar = 0;
            else
            {
                int nlimite = UnityEngine.Random.Range(6, 9);
                if (_qdeCutucar > nlimite)
                    _qdeCutucar = nlimite;
            }
        }
    }
}
