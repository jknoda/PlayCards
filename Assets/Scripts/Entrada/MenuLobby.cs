using Photon.Pun;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class MenuLobby : MonoBehaviourPunCallbacks
{
    [SerializeField]
    private Button _comecaJogo;
    [SerializeField]
    private GameObject _dupla;
    [SerializeField]
    private GameObject _botoes;
    [SerializeField]
    private GameObject _carrega;
    [SerializeField]
    private Text _txtNickNames;
    [SerializeField]
    private Text _txtSala;
    [SerializeField]
    private Text _txtVersao;

    private void VerDupla()
    {
        Vector3 pos = _dupla.transform.position;
        if (GestorDeRede.Instancia.DonoDaSala() && PhotonNetwork.PlayerList.Length >= 3)
        {
            _dupla.SetActive(true);
            _botoes.transform.position = new Vector3(pos.x, pos.y - 80, pos.z);
        }
        else
        {
            _dupla.SetActive(false);
            _botoes.transform.position = new Vector3(pos.x, pos.y, pos.z);
        }
        if (GestorDeRede.Instancia.DonoDaSala())
        {
            //Debug.Log("Path=" + Application.persistentDataPath);
            string fileName = Application.persistentDataPath + "/SALA_" + PhotonNetwork.CurrentRoom.Name + "_01.json";
            if (File.Exists(fileName))
            {
                _carrega.SetActive(true);
                StreamReader arquivo = new StreamReader(fileName);
                string saveGame = arquivo.ReadToEnd();
                arquivo.Close();

                GameCardsManager.SaveGame SG = JsonUtility.FromJson<GameCardsManager.SaveGame>(saveGame); 
                _txtNickNames.text = "";
                foreach (string item in SG.NickName)
                {
                    _txtNickNames.text += (_txtNickNames.text.Length > 0 ? " / " : "") + item;
                }
            }
            else
            {
                _carrega.SetActive(false);
            }
        }
    }

    [PunRPC]
    public void AtualizaLista()
    {
        _txtSala.text = GestorDeRede.Instancia.GetCreateSala();
        _txtVersao.text = GestorDeRede.Instancia.Versao;
        VerDupla();
        GestorDeRede.Instancia.ListaDeJogadores();
        _comecaJogo.interactable = GestorDeRede.Instancia.DonoDaSala();
    }

}
