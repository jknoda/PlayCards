using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class Menu : MonoBehaviourPunCallbacks
{
    [SerializeField]
    private MenuEntrada _menuEntrada;
    [SerializeField]
    private MenuLobby _menuLobby;
    private void Start()
    {
        _menuEntrada.gameObject.SetActive(false);
        _menuLobby.gameObject.SetActive(false);
    }

    public override void OnConnectedToMaster()
    {
        _menuEntrada.gameObject.SetActive(true);
    }
    public override void OnJoinedRoom()
    {

        bool GameOn;
        try
        {
            GameOn = (int)PhotonNetwork.PlayerList[0].CustomProperties["GameOn"] == 1;
        }
        catch
        {
            GameOn = false;
        }
        if (
            (PhotonNetwork.PlayerList.Length > 4 && !GameOn) ||
            (PhotonNetwork.PlayerList.Length > 6) ||
            (GameOn && PhotonNetwork.PlayerList.Length < 4)
        )
            GestorDeRede.Instancia.SalaCheia();
        else
        {
            int localActor = PhotonNetwork.LocalPlayer.ActorNumber;
            if (localActor > 6)
            {
                GestorDeRede.Instancia.SalaCheia();
            }
            if (localActor > 4)
                localActor = PhotonNetwork.PlayerList.Length;
            ExitGames.Client.Photon.Hashtable hash = new ExitGames.Client.Photon.Hashtable
            {
                { "ID", localActor }
            };
            PhotonNetwork.PlayerList[localActor - 1].SetCustomProperties(hash); // SetPlayerCustomProperties(hash);

            if (PhotonNetwork.PlayerList.Length > 4)
            {
                if (!GestorDeRede.Instancia.LiberaVisita)
                    GestorDeRede.Instancia.SalaCheia();
                else
                {
                    GestorDeRede.Instancia.SetAvatar();
                    GestorDeRede.Instancia.ComecaJogo("Principal", false, true);
                }
            }
            else
            {
                MudaMenu(_menuLobby.gameObject);
                GestorDeRede.Instancia.SetAvatar();
                _menuLobby.photonView.RPC("AtualizaLista", RpcTarget.All);
            }
        }
    }
    public void MudaMenu(GameObject menu)
    {
        _menuEntrada.gameObject.SetActive(false);
        _menuLobby.gameObject.SetActive(false);

        menu.SetActive(true);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        _menuLobby.AtualizaLista(); // sem RPC pois o jogador sairá do lobby e todos vão executar este evento
    }

    public void SairDoLobby()
    {
        GestorDeRede.Instancia.SairDoLobby();
        MudaMenu(_menuEntrada.gameObject);
    }

    public void ComecaJogoBtn(string nomeCena)
    {
        GestorDeRede.Instancia.ComecaJogo(nomeCena, true);
    }
}
