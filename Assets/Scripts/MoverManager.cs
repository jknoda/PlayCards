using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class MoverManager : MonoBehaviourPunCallbacks
{

    [SerializeField] GameObject _objMover;
    [SerializeField] Transform cartaUI;

    private Vector3 _startMarker;
    private Vector3 _endMarker;
    private float _speedInicial = 500f;
    private float _speed;
    private float _distPadrao = 280;
    private float _startTime;
    private float _journeyLength;
    private bool _mover = false;
    private GameObject _objeto;
    public static MoverManager Instancia { get; private set; }
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

    private void Start()
    {
        _mover = false;
    }

    public void MoverCartas(int idCarta, int actorNumber, string portador)
    {
        photonView.RPC("MoverCartasRPC", RpcTarget.All, idCarta, actorNumber, portador);
    }

    [PunRPC]
    private void MoverCartasRPC(int idCarta, int actorNumber, string portador)
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"]; // 
        if (localActor == actorNumber)
            return;
        if (GameManager.Instancia.ZoomOn)
            return;
        if (portador == "MONTE")
        {
            portador = "BARALHO";
            SoundManager.Instancia.PlaySound("endDrag");
        }
        else
        {
            SoundManager.Instancia.PlaySound("morto");
        }
        string verso = Baralho.Instancia.cartas[idCarta].GetComponent<Carta>().Verso;
        _objMover.SetActive(true);
        _objMover.GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/Cards/" + verso);
        _startTime = Time.time;
        var item = GameManager.Instancia.Spawn(-1, actorNumber);
        _endMarker = item.Item1.position;
        _startMarker = GameObject.FindGameObjectWithTag(portador).transform.position;
        _journeyLength = Vector3.Distance(_startMarker, _endMarker);
        _speed = _speedInicial * _journeyLength / _distPadrao;
        _mover = true;
        _objeto = _objMover;
    }

    public void MoverLixo()
    {
        photonView.RPC("MoverLixoRPC", RpcTarget.All);
    }

    [PunRPC]
    private void MoverLixoRPC()
    {
        SoundManager.Instancia.PlaySound("lixo");
        GameObject lixoObj = GameObject.FindGameObjectWithTag("LIXO");
        float x = lixoObj.transform.position.x - 20, y = lixoObj.transform.position.y;
        Color corMover = GestorDeRede.Instancia.GetCor("moverCarta");
        FrontManager.Instancia.lixoLista
        .ForEach(item =>
        {
            Transform obj;
            obj = Instantiate(cartaUI, lixoObj.transform.position, Quaternion.identity, lixoObj.transform);
            obj.transform.localScale = Baralho.Instancia.scale * (0.9f);
            obj.transform.SetAsFirstSibling();
            obj.tag = "CARTALIXO";
            obj.transform.GetComponent<Image>().sprite = item;
            obj.transform.GetComponent<Image>().color = corMover;
            x -= 24f;
            obj.transform.position = new Vector3(x, y, 0);
        });
    }

    public void MoverLixoLimpar()
    {
        photonView.RPC("MoverLixoLimparRPC", RpcTarget.All);
    }
    [PunRPC]
    private void MoverLixoLimparRPC()
    {
        SoundManager.Instancia.PlaySound("lixo");
        GameObject[] obj = GameObject.FindGameObjectsWithTag("CARTALIXO");
        for (int i = 0; i < obj.Length; i++)
        {
            Destroy(obj[i]);
        }
        FrontManager.Instancia.lixoLista.Clear();
    }

    public void MoverJogador(int jogadorOrigem, int jogadorDestino)
    {
        photonView.RPC("MoverJogadorRPC", RpcTarget.All, jogadorOrigem, jogadorDestino);
    }
    [PunRPC]
    private void MoverJogadorRPC(int jogadorOrigem, int jogadorDestino)
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"]; // 
        if (localActor == jogadorOrigem)
            return;
        if (GameManager.Instancia.ZoomOn)
            return;
        if (_objeto != null)
        {
            _objeto.SetActive(false);
        }
            
        var itemEnd = GameManager.Instancia.Spawn(-1, jogadorDestino);
        GameObject imagem = itemEnd.Item1.Find("ImageJog").gameObject;
        imagem.SetActive(true);
        imagem.GetComponent<Image>().sprite = Resources.Load<Sprite>(GestorDeRede.Instancia.GetAvatar(jogadorOrigem, false));
        Color cor = imagem.GetComponent<Image>().color;
        imagem.GetComponent<Image>().color = new Color(cor.r, cor.g, cor.b, 0.5f);
        _startTime = Time.time;
        _endMarker = itemEnd.Item1.position;
        var itemStart = GameManager.Instancia.Spawn(-1, jogadorOrigem);
        _startMarker = itemStart.Item1.position;
        _journeyLength = Vector3.Distance(_startMarker, _endMarker);
        _speed = (_speedInicial / 1.5f) * _journeyLength / _distPadrao;
        _mover = true;
        _objeto = imagem;
    }

    private void Update()
    {
        if (_mover)
        {
            float distCovered = (Time.time - _startTime) * _speed;

            // Fraction of journey completed equals current distance divided by total distance.
            float fractionOfJourney = distCovered / _journeyLength;

            // Set our position as a fraction of the distance between the markers.
            //_objMover.transform.position = Vector3.Lerp(_startMarker, _endMarker, fractionOfJourney);
            _objeto.transform.position = Vector3.Lerp(_startMarker, _endMarker, fractionOfJourney);
            if (fractionOfJourney >= 0.9f)
            {
                //_objMover.SetActive(false);
                _objeto.SetActive(false);
                _mover = false;
            }
        }       
    }
}
