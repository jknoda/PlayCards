using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ChatManager : MonoBehaviourPunCallbacks
{
    [SerializeField] GameObject btnChat;
    [SerializeField] GameObject pnMensagem;
    [SerializeField] InputField inputChat;
    [SerializeField] GameObject chatConvidado;
    [SerializeField] Image avatarConvidado;
    [SerializeField] GameObject pnMensagemConvidado;
    [SerializeField] InputField inputChatConvidado;
    [SerializeField] GameObject pnChatViewConvidado;

    //public string msgHistorico;
    public int qdeMsg;

    public string Avatar { get; set; }

    public static ChatManager Instancia { get; private set; }
    private void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Avatar = "Avatar90";
            Instancia = this;
        }
    }

    public void Chat(bool On)
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        if (localActor <= 4)
            btnChat.SetActive(On);
        else
        {
            chatConvidado.SetActive(On);
            int avatarInd;
            try
            {
                avatarInd = (int)PhotonNetwork.PlayerList[localActor - 1].CustomProperties["Avatar"];
            }
            catch
            {
                avatarInd = 90;
            }
            Avatar = GestorDeRede.Instancia.GetAvatarFile(avatarInd); // "Avatar/avatar" + avatarInd.ToString().PadLeft(2, '0');
            avatarConvidado.sprite = Resources.Load<Sprite>(Avatar); ;
        }
    }
    public void StartChat()
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"]; // 
        if (localActor > 4)
        {
            pnMensagemConvidado.SetActive(true);
            inputChatConvidado.text = "";
        }
        else
        {
            pnMensagem.SetActive(true);
            inputChat.text = "";
        }
    }

    public void EnviarBot(string msg)
    {
        EnviarMsg(msg);
    }

    public void Enviar()
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"]; // 
        if (localActor > 4)
            EnviarMsg(inputChatConvidado.text);
        else
            EnviarMsg(inputChat.text);
    }

    private void EnviarMsg(string msg)
    {
        msg = msg.Trim();
        if (string.IsNullOrEmpty(msg))
            return;
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        if (localActor > 4)
            pnMensagemConvidado.SetActive(false);
        else
            pnMensagem.SetActive(false);
        if (localActor > 4)
        {
            MsgConvidado(msg, localActor);
        }
        else
        {
            var jogador = GameCardsManager.Instancia.GetJogadorObjeto(localActor);
            jogador.GetComponent<Jogador>().Msg(msg, localActor);
        }
        photonView.RPC("SetHistor", RpcTarget.All, msg, localActor);
        BotManager.Instancia.LerMsg(msg, localActor);
        this.Interpretar(msg);
    }

    private void Interpretar(string msg)
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"]; // 
        if (localActor > 4)
            return;

        string msgAux = msg.Replace(" ", ",").ToUpper() + "   ";
        List<string> palavras = msgAux.Split(',').ToList();
        if (palavras.Any(x => x.Contains("DESFAZER")))
        {
            // desfazer jogada
            GameObject jogador = GameCardsManager.Instancia.GetJogadorObjeto();
            if (jogador.GetComponent<Jogador>().PodeDesfazer)
            {
                GameRules.Instancia.DesfazerUltimaJogada(jogador);
                jogador.GetComponent<Jogador>().PodeDesfazer = false;
            }
            else
            {
                GameCardsManager.Instancia.MsgMsg("Não pode mais desfazer a jogada", 0);
            }
        }
    }

    [PunRPC]
    private void SetHistor(string msg, int actorNumber)
    {
        string actor = GameCardsManager.Instancia.GetNome(actorNumber);
        GestorDeRede.Instancia.MsgHistorico = actor + " (" + DateTime.Now.ToString("HH:mm:ss") + "): " + msg + "\n" + GestorDeRede.Instancia.MsgHistorico;
        if (GestorDeRede.Instancia.MsgHistorico.Length > 1500)
            GestorDeRede.Instancia.MsgHistorico = GestorDeRede.Instancia.MsgHistorico.Substring(0, 1000);
        qdeMsg++;
    }

    public void Cancelar()
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"]; // 
        if (localActor > 4)
        {
            inputChatConvidado.text = "";
            pnMensagemConvidado.SetActive(false);
        }
        else
        {
            inputChat.text = "";
            pnMensagem.SetActive(false);
        }
    }

    public void MsgConvidado(string txtMsg, int actorNumber)
    {
        if (!string.IsNullOrEmpty(txtMsg))
        {
            int avatarNumber = (int)PhotonNetwork.LocalPlayer.CustomProperties["Avatar"];
            string avatar = GestorDeRede.Instancia.GetAvatarFile(avatarNumber);
            photonView.RPC("MsgConvidadoRPC", RpcTarget.All, txtMsg, actorNumber, avatar, PhotonNetwork.LocalPlayer.NickName);
        }
    }

    [PunRPC]
    private void MsgConvidadoRPC(string txtMsg, int actorNumber, string avatar, string nome)
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"]; // 
        if (localActor == actorNumber)
            return;
        SoundManager.Instancia.PlaySound("chat");
        int linhas = txtMsg.Length / 15 + 1;
        int x = txtMsg.Length + 10;
        if (linhas > 1)
            x = 250;
        pnChatViewConvidado.SetActive(true);
        pnChatViewConvidado.transform.Find("imgAvatar").GetComponent<Image>().sprite = Resources.Load<Sprite>(avatar);
        pnChatViewConvidado.transform.Find("imgAvatar").Find("Nome").GetComponent<Text>().text = nome;
        pnChatViewConvidado.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(x, linhas * 30);
        pnChatViewConvidado.transform.GetComponent<BoxCollider2D>().size = pnChatViewConvidado.transform.GetComponent<RectTransform>().sizeDelta;
        pnChatViewConvidado.transform.Find("txtMensagem").GetComponent<Text>().text = txtMsg;
        GameManager.Instancia.TempoChat = true;
    }

    [PunRPC]
    public void MsgJogador(string txtMsg, int actorNumber)
    {
        var jogador = GameCardsManager.Instancia.GetJogadorObjeto(actorNumber);
        jogador.GetComponent<Jogador>().MsgRPC(txtMsg, actorNumber);
        GameManager.Instancia.TempoChat = true;
    }
}