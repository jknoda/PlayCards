using Newtonsoft.Json;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class GestorDeRede : MonoBehaviourPunCallbacks
{
    [SerializeField]
    private Text _msg;

    [SerializeField]
    private Text _duplaText;
    [SerializeField]
    private Dropdown _dropdownAvatar;
    [SerializeField]
    private GameObject[] _jogadorEntrada;
    [SerializeField]
    private Text _carregaSN;
    [SerializeField]
    private Dropdown _dropdownSala;

    public const string urlService = "https://playcardservice.herokuapp.com";
    //public const string urlService = "localhost:3003";

    public int GameIdf { get; set; }
    public int GameRodada { get; set; }
    public bool CtrlRecallFront { get; set; }
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
    public int[] AvatarJogadores = new int[6];
    public float VolumeFundo { get; set; }
    public float VolumeEfeitos { get; set; }
    public bool SoundOn { get; set; }
    public bool Recall { get; set; }
    public bool RodadaDeRecall { get; set; }
    public int TempoChat { get; set; }
    public bool FirstBot { get; set; } // inicializa jogada do bot
    public bool BotDebug { get; set; }
    public bool SoVer { get; set; }
    public int SoVerNumber { get; set; } // Vai ver a  jogada do actorNumber
    public string Versao { get; set; }
    public bool LiberaVisita { get; set; }
    public bool LixoBaguncado { get; set; }
    public string MsgHistorico { get; set; }
    public DateTime HoraInicio { get; set; }

    private string StatisticUsuario { get; set; }

    public static GestorDeRede Instancia { get; private set; }

    private void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Versao = "Versão 4.6a";
            BotDebug = false; //// true = mostra as cartas do bot

            HoraInicio = DateTime.Now;
            LiberaVisita = true;

            LixoBaguncado = true;

            Placar01 = 0;
            Placar02 = 0;

            PlacarGeral01 = 0;
            PlacarGeral02 = 0;

            // Gravar registro para estatistica
            GameIdf = 0;
            GameRodada = 0;
            InicialOk = 0;

            Placar01Det = "";
            Placar02Det = "";

            JogadorInicial = 1;
            VolumeFundo = 0.1f;
            VolumeEfeitos = 0.5f;
            SoundOn = true;
            Instancia = this;
            AvatarJogadores = new int[6] { -1, -1, -1, -1, -1, -1 }; //[0] = -1;
            Recall = false;
            CtrlRecallFront = false;
            RodadaDeRecall = false;
            _carregaSN.text = "N";
            TempoChat = 15;
            FirstBot = true;
            SoVer = false;
            SoVerNumber = -1;
            Dupla01 = new Tuple<int, int>(1, 2);
            Dupla02 = new Tuple<int, int>(3, 4);
            MsgHistorico = "";
            StatisticUsuario = "";
            DontDestroyOnLoad(this.gameObject);
        }
    }

    private void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        PhotonNetwork.ConnectUsingSettings();
        List<string> arquivos = new List<string>
        {
            "avatar01",
            "avatar02",
            "avatar03",
            "avatar06",
            "avatar50",
            "avatar51",
            "avatar52",
            "avatar53",
            "avatar54",
            "avatar55",
            "avatar56",
            "avatar90"
        };
        _dropdownAvatar.options.Clear();
        List<Dropdown.OptionData> items = new List<Dropdown.OptionData>();
        int i = 0;
        foreach (string arquivo in arquivos)
        {
            i++;
            Sprite avatar = Resources.Load<Sprite>("Avatar/" + arquivo);
            var option = new Dropdown.OptionData(arquivo, avatar);
            items.Add(option);
        }
        _dropdownAvatar.AddOptions(items);

        _dropdownSala.options.Clear();
        items.Clear();
        for (int iSala = 1; iSala <= 4; iSala++)
        {
            string sala = "SALA " + iSala.ToString().PadLeft(2, '0');
            var option = new Dropdown.OptionData(sala);
            items.Add(option);
        }
        _dropdownSala.AddOptions(items);

        if (SoVerNumber == -1)
            SoVerNumber = UnityEngine.Random.Range(1, 5);
    }

    public string GetCreateSala()
    {
        return "SALA" + (_dropdownSala.value + 1).ToString().PadLeft(2, '0');
    }

    public string GetNomeSala()
    {
        return PhotonNetwork.CurrentRoom.Name;
    }

    public int GetAvatarNumber(int actorNumber)
    {
        int avatar;
        if (actorNumber > 4)
            actorNumber = 4;
        try
        {
            avatar = (int)PhotonNetwork.PlayerList[actorNumber - 1].CustomProperties["Avatar"];
        }
        catch
        {
            avatar = AvatarJogadores[actorNumber - 1];
        }
        return avatar;
    }

    public string GetAvatar(int actorNumber, bool recall)
    {
        int avatar;
        if (actorNumber > 4)
            actorNumber = 4;
        if (!recall)
        {
            try
            {
                avatar = (int)PhotonNetwork.PlayerList[actorNumber - 1].CustomProperties["Avatar"];
            }
            catch
            {
                avatar = AvatarJogadores[actorNumber - 1];
            }
        }
        else
            avatar = AvatarJogadores[actorNumber - 1];
        return GetAvatarFile(avatar);
    }

    public string GetStatisticFile()
    {
        int actorNumber = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        if (actorNumber > 4)
            actorNumber = GestorDeRede.Instancia.SoVerNumber;

        string ret = "";
        int avatar = AvatarJogadores[actorNumber - 1];
        ret = AvatarName(avatar);
        string fileName = Application.persistentDataPath + "/" + ret.ToLower() + ".json";
        return fileName;
    }

    private string AvatarName(int avatar)
    {
        string ret;
        switch (avatar)
        {
            case 50:
                ret = "Mitsue";
                break;
            case 51:
                ret = "Mie";
                break;
            case 52:
                ret = "Paula";
                break;
            case 53:
                ret = "Sung";
                break;
            case 54:
                ret = "Tiemi";
                break;
            case 55:
                ret = "Dico";
                break;
            case 56:
                ret = "Celso";
                break;
            case 90:
                ret = "Bot";
                break;
            default:
                ret = "Av" + avatar.ToString().PadLeft(2, '0');
                break;
        }
        return ret;
    }

    public void SetStatistic(int valor)
    {
        Statistic st = GetStatistic();
        if (valor > 0)
            st.Vitorias++;
        else
            st.Derrotas++;
        st.UltimoJogo = DateTime.Now;
        st.UltimoPlacar01 = GameCardsManager.Instancia.GetNomeDupla(1).PadRight(15, '.') + ": " + Placar01.ToString();
        st.UltimoPlacar02 = GameCardsManager.Instancia.GetNomeDupla(2).PadRight(15, '.') + ": " + Placar02.ToString();
        string saveStatic = JsonConvert.SerializeObject(st);
        string fileName = GetStatisticFile();
        StreamWriter arquivo = new StreamWriter(fileName);
        arquivo.WriteLine(saveStatic);
        arquivo.Close();
    }

    public Statistic GetStatistic()
    {
        string fileName = GetStatisticFile();
        if (!File.Exists(fileName))
            return new Statistic();
        StreamReader arquivo = new StreamReader(fileName);
        string saveStatic = arquivo.ReadToEnd();
        arquivo.Close();
        Statistic st = JsonConvert.DeserializeObject<Statistic>(saveStatic);
        return st;
    }

    public string GetAvatarFile(int avatar)
    {
        return "Avatar/avatar" + avatar.ToString().PadLeft(2, '0');
    }
    public void SetAvatar(int actorNumber = 0, int avatarNum = 0)
    {
        if (actorNumber == 0)
            actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        int avatar;
        if (avatarNum <= 0)
        {
            string aux = _dropdownAvatar.options[_dropdownAvatar.value].text;
            int avNumber;
            Int32.TryParse(aux.Substring(aux.Length - 2, 2), out avNumber);
            avatar = avNumber;
        }
        else
            avatar = avatarNum;
        if (actorNumber <= 6)
        {
            AvatarJogadores[actorNumber - 1] = avatar;
        }
        ExitGames.Client.Photon.Hashtable hash = new ExitGames.Client.Photon.Hashtable
        {
            { "Avatar", avatar }
        };
        PhotonNetwork.SetPlayerCustomProperties(hash);
    }

    public override void OnConnectedToMaster()
    {
        //Debug.Log("Conexão bem sucedida!");
    }

    //public override void OnDisconnected(DisconnectCause cause)
    //{
    //    int id = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
    //    if (id <= 4)
    //    {
    //        // recuperar
    //    }
    //}

    public int QdeJogadores()
    {
        int nQdePlayer = PhotonNetwork.PlayerList.Length;
        if (nQdePlayer > 4) nQdePlayer = 4;
        return nQdePlayer;
    }
    public void CriaSala(string nomeSala)
    {
        RoomOptions options = new RoomOptions
        {
            PlayerTtl = 60000 // 60 sec
        };
        //options.EmptyRoomTtl = 60000; // 60 sec
        PhotonNetwork.CreateRoom(nomeSala, options);
    }
    public void EntraSala(string nomeSala)
    {
        PhotonNetwork.JoinRoom(nomeSala);
    }

    public void SalaCheia()
    {
        SairDoLobby();
        _msg.text = "Sala cheia !!!";
    }

    public void MudaNick(string nickname)
    {
        if (string.IsNullOrEmpty(nickname))
        {
            string aux = _dropdownAvatar.options[_dropdownAvatar.value].text;
            int avNumber;
            Int32.TryParse(aux.Substring(aux.Length - 2, 2), out avNumber);
            nickname = AvatarName(avNumber);
        }
        PhotonNetwork.NickName = nickname;
    }

    public void ListaDeJogadores()
    {
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            int playerActorNumber = player.ActorNumber; // (int)player.CustomProperties["ID"];
            if (playerActorNumber <= 4)
            {
                _jogadorEntrada[playerActorNumber - 1].SetActive(true);
                string avatarNome = GetAvatar(playerActorNumber, false);
                GameObject childText = _jogadorEntrada[playerActorNumber - 1].transform.GetChild(0).gameObject;
                GameObject childImg = _jogadorEntrada[playerActorNumber - 1].transform.GetChild(1).gameObject;
                childText.transform.GetComponent<Text>().text = playerActorNumber.ToString() + " - " + player.NickName;
                childImg.transform.GetComponent<Image>().sprite = Resources.Load<Sprite>(avatarNome);
            }
        }
    }

    public bool DonoDaSala()
    {
        return PhotonNetwork.IsMasterClient;
    }

    public void SairDoLobby()
    {
        PhotonNetwork.LeaveRoom();
    }

    public void ComecaJogo(string nomeCena, bool first, bool soVer = false)
    {
        int localActor = PhotonNetwork.LocalPlayer.ActorNumber;
        if (localActor > 4)
            localActor = PhotonNetwork.PlayerList.Length;
        ExitGames.Client.Photon.Hashtable hash = new ExitGames.Client.Photon.Hashtable
        {
            { "ID", localActor }
        };
        PhotonNetwork.PlayerList[localActor - 1].CustomProperties = hash; // .SetPlayerCustomProperties(hash);

        if (PhotonNetwork.LocalPlayer.ActorNumber == 1)
        {
            //ExitGames.Client.Photon.Hashtable 
            hash = new ExitGames.Client.Photon.Hashtable
            {
                { "GameOn", 1 }
            };
            PhotonNetwork.SetPlayerCustomProperties(hash);
        }

        this.SoVer = soVer;
        if (soVer)
        {
            int parceiroDo1;
            if (_duplaText == null || !Int32.TryParse(_duplaText.text, out parceiroDo1))
                parceiroDo1 = 2;
            //photonView.RPC("AjustaDuplaRPC", RpcTarget.All, parceiroDo1);
            AjustaDuplaCPL(parceiroDo1);
            PhotonNetwork.LoadLevel(nomeCena);
        }
        else
        {
            if (first)
            {
                if (_carregaSN.text.ToUpper() == "S")
                {
                    photonView.RPC("SetRecall", RpcTarget.All, true);
                }
                else
                    photonView.RPC("SetRecall", RpcTarget.All, false);
                int parceiroDo1;
                if (PhotonNetwork.PlayerList.Length < 4)
                    parceiroDo1 = 3;
                else
                {
                    if (_duplaText != null && _duplaText.text.ToUpper() == "X")
                        parceiroDo1 = UnityEngine.Random.Range(2, 5);
                    else
                    {

                        if (_duplaText == null || !Int32.TryParse(_duplaText.text, out parceiroDo1))
                            parceiroDo1 = 2;
                    }
                }
                photonView.RPC("AjustaDuplaRPC", RpcTarget.All, parceiroDo1);
                if (_carregaSN.text.ToUpper() != "S" && DonoDaSala() && GameIdf == 0)
                {
                    CriarGame(localActor);
                }
            }
            else
            {
                RodadaDeRecall = false;
            }
            photonView.RPC("ComecaJogoRPC", RpcTarget.All, nomeCena);
        }
    }

    public void CriarGame(int actorNumber)
    {
        // criar controle de jogo
        int localActor = PhotonNetwork.LocalPlayer.ActorNumber;
        if (actorNumber > 0 && actorNumber != localActor)
            return;
        WWWForm form = new WWWForm();
        form.AddField("criador", actorNumber);
        form.AddField("sala", GetNomeSala());
        form.AddField("ava01", GetAvatarNumber(Dupla01.Item1));
        form.AddField("ava02", GetAvatarNumber(Dupla01.Item2));
        form.AddField("avb01", GetAvatarNumber(Dupla02.Item1));
        form.AddField("avb02", GetAvatarNumber(Dupla02.Item2));
        form.AddField("joga01", GetNome(Dupla01.Item1));
        form.AddField("joga02", GetNome(Dupla01.Item2));
        form.AddField("jogb01", GetNome(Dupla02.Item1));
        form.AddField("jogb02", GetNome(Dupla02.Item2));
        StartCoroutine(PostGestorRede("/oapi/playctrl/create", actorNumber, form, 1));
    }
    public void CriarRodada(int actorNumber = 0)
    {
        int localActor = PhotonNetwork.LocalPlayer.ActorNumber;
        if (actorNumber > 0 && actorNumber != localActor)
            return;
        photonView.RPC("SetVulOkRPC", RpcTarget.All, 1, false);
        photonView.RPC("SetVulOkRPC", RpcTarget.All, 2, false);
        GameRodada++;
        WWWForm form = new WWWForm();
        form.AddField("idf", GameIdf);
        form.AddField("rodada", GameRodada);
        StartCoroutine(PostGestorRede("/oapi/playctrlstat/create", localActor, form, 2));
    }

    public void SetFimRodada(int actorNumber, string obs)
    {
        int localActor = PhotonNetwork.LocalPlayer.ActorNumber;
        if (actorNumber != localActor)
            return;
        var placar = GameCardsManager.Instancia.GetPlacar();
        WWWForm form = new WWWForm();
        form.AddField("idf", GameIdf);
        form.AddField("inicial", InicialOk);
        form.AddField("placara", placar.Item1);
        form.AddField("placarb", placar.Item2);
        form.AddField("obs", obs);
        StartCoroutine(PostGestorRede("/oapi/playctrl/end", localActor, form, 4));

    }
    public void SetDadosRodada(int actorNumber, int dupla, string tipo, int valorSoma)
    {
        int localActor = PhotonNetwork.LocalPlayer.ActorNumber;
        if (localActor != actorNumber)
            return;
        string cDupla = dupla == 1 ? "a" : dupla == 0 ? "" : "b";
        WWWForm form = new WWWForm();
        form.AddField("idf", GameIdf);
        form.AddField("rodada", GameRodada);
        form.AddField("dupla", cDupla);
        form.AddField("tipo", tipo);
        form.AddField("valor", valorSoma);
        StartCoroutine(PostGestorRede("/oapi/playctrlstat/update", localActor, form, 3));
    }

    private string GetNome(int actorNumber)
    {
        string nome = "";
        int nQdePlayer = PhotonNetwork.PlayerList.Length;
        for (int i = 0; i < nQdePlayer; i++)
        {
            if (PhotonNetwork.PlayerList[i].ActorNumber == actorNumber)
                nome = PhotonNetwork.PlayerList[i].NickName;
        }
        return nome.Trim();
    }

    [PunRPC]
    private void ComecaJogoRPC(string nomeCena)
    {
        FirstBot = true;
        PrimeiraJogada = true;
        JogadorInicial++;
        int nQdePlayer = PhotonNetwork.PlayerList.Length;
        if (nQdePlayer > 4) nQdePlayer = 4;
        if (JogadorInicial > nQdePlayer)
            JogadorInicial = 1;
        PhotonNetwork.LoadLevel(nomeCena);
    }

    [PunRPC]
    public void SetJogadorInicial(int jogador)
    {
        JogadorInicial = jogador;
    }

    [PunRPC]
    public void SetRecall(bool valor)
    {
        Recall = valor;
        if (valor)
        {
            CtrlRecallFront = true;
            RodadaDeRecall = valor;
        }
    }
    [PunRPC]
    private void AjustaDuplaRPC(int parceiroDo1)
    {
        //Dupla01 = new Tuple<int, int>(1, 2);
        //Dupla02 = new Tuple<int, int>(3, 4);
        //Dupla01 = new Tuple<int, int>(1, parceiroDo1);
        //if (parceiroDo1 == 2)
        //    Dupla02 = new Tuple<int, int>(3, 4);
        //if (parceiroDo1 == 3)
        //    Dupla02 = new Tuple<int, int>(2, 4);
        //if (parceiroDo1 == 4)
        //    Dupla02 = new Tuple<int, int>(2, 3);
        //SeqJogadores[0] = Dupla01.Item1;
        //SeqJogadores[1] = Dupla02.Item1;
        //SeqJogadores[2] = Dupla01.Item2;
        //SeqJogadores[3] = Dupla02.Item2;
        AjustaDuplaCPL(parceiroDo1);
    }
    private void AjustaDuplaCPL(int parceiroDo1)
    {
        Dupla01 = new Tuple<int, int>(1, 2);
        Dupla02 = new Tuple<int, int>(3, 4);
        Dupla01 = new Tuple<int, int>(1, parceiroDo1);
        if (parceiroDo1 == 2)
            Dupla02 = new Tuple<int, int>(3, 4);
        if (parceiroDo1 == 3)
            Dupla02 = new Tuple<int, int>(2, 4);
        if (parceiroDo1 == 4)
            Dupla02 = new Tuple<int, int>(2, 3);
        SeqJogadores[0] = Dupla01.Item1;
        SeqJogadores[1] = Dupla02.Item1;
        SeqJogadores[2] = Dupla01.Item2;
        SeqJogadores[3] = Dupla02.Item2;
    }

    public void SetPlacar(int pontos01, int pontos02, bool zerar = false)
    {
        photonView.RPC("SetPlacarRPC", RpcTarget.All, pontos01, pontos02, zerar);
    }
    [PunRPC]
    private void SetPlacarRPC(int pontos01, int pontos02, bool zerar)
    {
        Placar01 = pontos01;
        Placar02 = pontos02;
        if (zerar)
        {
            Placar01 = 0;
            Placar02 = 0;
            Placar01Det = "";
            Placar02Det = "";
        }

    }

    public void SetPlacarDet(string pontos01, string pontos02)
    {
        photonView.RPC("SetPlacarDetRPC", RpcTarget.All, pontos01, pontos02);
    }
    [PunRPC]
    private void SetPlacarDetRPC(string pontos01, string pontos02)
    {
        Placar01Det = pontos01;
        Placar02Det = pontos02;
    }

    public void SetPlacarGeral(int dupla)
    {
        photonView.RPC("SetPlacarGeralRPC", RpcTarget.All, dupla);
    }
    [PunRPC]
    private void SetPlacarGeralRPC(int dupla)
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        if (dupla == 1)
            PlacarGeral01++;
        else
            PlacarGeral02++;
        if (dupla == GameCardsManager.Instancia.GetDupla())
        {
            SetStatistic(1);
        }
        else
        {
            SetStatistic(-1);
        }
    }

    public void SetPrimeiraJogada(bool valor)
    {
        photonView.RPC("SetPrimeiraJogadaRPC", RpcTarget.All, valor);
    }
    [PunRPC]
    public void SetPrimeiraJogadaRPC(bool valor)
    {
        PrimeiraJogada = valor;
    }

    public Color GetCor(string cor)
    {
        Color ret = cor switch
        {
            "padrao" => Color.white,
            "jogada" => new Color(0.952f, 0.956f, 0.443f),
            "moverCarta" => new Color(1f, 1f, 1f, 0.5f),
            "selecao" => Color.yellow,
            "placar" => new Color(0.698f, 0.886f, 0.941f),// Color(0.06420228f, 0.05517978f, 1f);
            "status" => new Color(0.698f, 0.886f, 0.941f),// Color(0.06420228f, 0.05517978f, 1f);
            "msg" => new Color(1f, 1f, 1f, 0.3921569f),
            "fixar" => new Color(0.905f, 0.756f, 0.780f),
            // A SUJO
            "AS" => new Color(1f, 0.8f, 0.8f),
            // A LIMPO
            "AL" => new Color(0.839f, 0.960f, 0.839f),
            // Canastra Suja
            "CS" => new Color(1f, 0.6f, 0.6f),
            // Canastra Limpa
            "CL" => new Color(0.6f, 1f, 0.6f),
            // Real Suja
            "RS" => new Color(0.901f, 0.6f, 1f),
            // Real Limpa
            "RL" => new Color(0.6f, 0.6f, 1f),
            "2lixo" => new Color(0.796f, 0.745f, 0.788f),
            "2coringa" => new Color(0.921f, 0.713f, 0.658f),
            _ => Color.white,
        };
        return ret;
    }

    public void DesligarConvidados()
    {
        photonView.RPC("DesligarRPC", RpcTarget.All);
    }
    [PunRPC]
    private void DesligarRPC()
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber > 4)
        {
            //int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
            //GameManager.Instancia.MostraMsgMain("Visitante, você foi desconectado(a). Obrigado pela visita.", true, "msg");
            //PhotonNetwork.Disconnect();
        }
    }

    public void SetLixoBaguncado()
    {
        bool valor = !LixoBaguncado;
        photonView.RPC("SetLixoBaguncadoRPC", RpcTarget.All, valor);
    }
    [PunRPC]
    private void SetLixoBaguncadoRPC(bool valor)
    {
        LixoBaguncado = valor;
    }

    public void SetGameIdf(int valor)
    {
        photonView.RPC("SetGameIdfRPC", RpcTarget.All, valor);
    }
    [PunRPC]
    private void SetGameIdfRPC(int valor)
    {
        GameIdf = valor;
    }

    public void SetGameRodada(int valor)
    {
        photonView.RPC("SetGameRodadaRPC", RpcTarget.All, valor);
    }
    [PunRPC]
    private void SetGameRodadaRPC(int valor)
    {
        GameRodada = valor;
    }
    public void SetVulOk(int dupla, int actorNumber, int pontos, bool valor)
    {
        photonView.RPC("SetVulOkRPC", RpcTarget.All, dupla, valor);
        Instancia.SetDadosRodada(actorNumber, GameCardsManager.Instancia.GetDupla(actorNumber), "vul", actorNumber);
        Instancia.SetDadosRodada(actorNumber, GameCardsManager.Instancia.GetDupla(actorNumber), "vulpto", pontos);
    }
    [PunRPC]
    private void SetVulOkRPC(int dupla, bool valor)
    {
        if (dupla == 1)
            VulOkA = valor;
        else
            VulOkB = valor;
    }
    public void SetInicialOk(int valor)
    {
        photonView.RPC("SetInicialOkRPC", RpcTarget.All, valor);
    }
    [PunRPC]
    private void SetInicialOkRPC(int valor)
    {
        InicialOk = valor;
    }

    #region servico
    private IEnumerator PostGestorRede(string servico, int actorNumber, WWWForm dados, int controle)
    {
        if (controle == 4)
        {
            //GameIdf = 0;
            //GameRodada = 0;
            //InicialOk = 0;
            SetGameIdf(0);
            SetGameRodada(0);
            SetInicialOk(0);
        }
        // Request and wait for the desired page.
        UnityWebRequest webRequest = UnityWebRequest.Post(urlService + servico, dados);
        yield return webRequest.SendWebRequest();
        switch (webRequest.result)
        {
            case UnityWebRequest.Result.ConnectionError:
            case UnityWebRequest.Result.DataProcessingError:
                break;
            case UnityWebRequest.Result.ProtocolError:
                break;
            case UnityWebRequest.Result.Success:
                switch (controle)
                {
                    case 1: // Inicio da rodada
                        GameIdf = Convert.ToInt32(webRequest.downloadHandler.text);
                        SetGameIdf(GameIdf);
                        SetGameRodada(0);
                        CriarRodada();
                        break;
                    case 2: // dados da rodada
                        SetGameRodada(GameRodada);
                        break;
                }
                break;
        }
    }
    #endregion servico



    [Serializable]
    public class Statistic
    {
        public int Vitorias { get; set; }
        public int Derrotas { get; set; }
        public DateTime UltimoJogo { get; set; }
        public string UltimoPlacar01 { get; set; }
        public string UltimoPlacar02 { get; set; }
    }
}

