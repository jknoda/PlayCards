using Photon.Pun;
using System;
using UnityEngine;
using UnityEngine.UI;

public class AreaConfig : MonoBehaviourPunCallbacks
{
    [SerializeField]
    private GameObject _config;
    [SerializeField]
    private Text _textoChat;
    public void NovasCartas()
    {
        GameRules.Instancia.NovasCartas();
    }
    public void FinalizaJogada()
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"]; // 
        if (localActor > 4)
            return;

        GameRules.Instancia.FinalizaJogada();
        if (GestorDeRede.Instancia.BotDebug)
        {
            BotManager.Instancia.MsgDebug();
        }
    }
    public void ReorganizarMao()
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"]; // 
        if (localActor > 4)
            return;

        GameCardsManager.Instancia.SetReorganizaMao();
    }

    public void Status()
    {
        int localActor = (int)PhotonNetwork.LocalPlayer.CustomProperties["ID"];
        if (localActor > 4)
            localActor = GestorDeRede.Instancia.SoVerNumber;

        int minhaDupla = GameCardsManager.Instancia.GetDupla(localActor);

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

        //var placar = GameCardsManager.Instancia.GetPlacar();
        int vulPto01_1 = GameCardsManager.Instancia.GetVulPontos(GestorDeRede.Instancia.Dupla01.Item1);
        int vulPto01_2 = GameCardsManager.Instancia.GetVulPontos(GestorDeRede.Instancia.Dupla01.Item2);
        int vulPto02_1 = GameCardsManager.Instancia.GetVulPontos(GestorDeRede.Instancia.Dupla02.Item1);
        int vulPto02_2 = GameCardsManager.Instancia.GetVulPontos(GestorDeRede.Instancia.Dupla02.Item2);
        string vul01 = (vulPto01_1 + vulPto01_2 == 0 ? "Não" : "Sim (" + vulPto01_1.ToString() + " | " + vulPto01_2.ToString() + " ptos)");
        string vul02 = (vulPto02_1 + vulPto02_2 == 0 ? "Não" : "Sim (" + vulPto02_1.ToString() + " | " + vulPto02_2.ToString() + " ptos)");
        if (vul01.Contains("Sim"))
        {
            bool jaBaixou = GameCardsManager.Instancia.GetJogadorObjeto(GestorDeRede.Instancia.Dupla01.Item1).GetComponent<Jogador>().JaBaixou;
            jaBaixou = jaBaixou || GameCardsManager.Instancia.GetJogadorObjeto(GestorDeRede.Instancia.Dupla01.Item2).GetComponent<Jogador>().JaBaixou;
            bool jaPegouMorto = GameCardsManager.Instancia.GetPegouMorto(GestorDeRede.Instancia.Dupla01.Item1);
            if (jaBaixou || jaPegouMorto)
            {
                vul01 += " já baixou.";
            }
            else
            {
                vul01 += " não baixou.";
            }

        }
        if (vul02.Contains("Sim"))
        {
            bool jaBaixou = GameCardsManager.Instancia.GetJogadorObjeto(GestorDeRede.Instancia.Dupla02.Item1).GetComponent<Jogador>().JaBaixou;
            jaBaixou = jaBaixou || GameCardsManager.Instancia.GetJogadorObjeto(GestorDeRede.Instancia.Dupla02.Item2).GetComponent<Jogador>().JaBaixou;
            bool jaPegouMorto = GameCardsManager.Instancia.GetPegouMorto(GestorDeRede.Instancia.Dupla02.Item1);
            if (jaBaixou || jaPegouMorto)
            {
                vul02 += " já baixou.";
            }
            else
            {
                vul02 += " não baixou.";
            }
        }

        string vul;
        if (vul01.Contains("Não") && vul02.Contains("Não"))
            vul = "Ninguém";
        else
        {
            if (minhaDupla == 01)
            {
                vul01 = "NÓS.: " + vul01;
                vul02 = "ELES: " + vul02;
                vul = vul01 + "\n" + vul02;
            }
            else
            {
                vul01 = "ELES: " + vul01;
                vul02 = "NÓS.: " + vul02;
                vul = vul02 + "\n" + vul01;
            }
        }

        string nome = GameCardsManager.Instancia.GetNome(GameCardsManager.Instancia.GetJogadorAtual());
        string msg = "MORTO: " + morto;
        msg += "\n\n" + "VUL: " + vul;

        var st = GestorDeRede.Instancia.GetStatistic();
        string statistic = "Vitórias: " + st.Vitorias.ToString();
        statistic += "\n" + "Derrotas: " + st.Derrotas.ToString();
        statistic += "\nÚltimo jogo em " + st.UltimoJogo.ToString();
        statistic += "\nPLACAR" + "\n" + st.UltimoPlacar01 + "\n" + st.UltimoPlacar02;
        msg += "\n\n" + "*** ESTATÍSTICAS ***\n" + statistic;

        // Visitantes
        if (PhotonNetwork.PlayerList.Length > 4)
        {
            string visita = "";
            int qde = 0;
            for (int i = 4; i < PhotonNetwork.PlayerList.Length; i++)
            {
                qde++;
                visita += (visita.Length > 0 ? " / " : "") + PhotonNetwork.PlayerList[i].NickName;
            }
            if (qde > 1)
                visita = "Visitantes: " + visita;
            else
                visita = "Visitante: " + visita;
            msg += "\n\n" + visita;
        }

        Tuple<int, int> dupla;
        if (minhaDupla == 1)
            dupla = GestorDeRede.Instancia.Dupla01;
        else
            dupla = GestorDeRede.Instancia.Dupla02;

        BotManager.Instancia.GetDadosBot(localActor);

        GameManager.Instancia.MostraMsgMain(msg, true, "msg", true, 0, false);
    }

    public void Configurar()
    {
        _config.SetActive(true);
        Config.Instancia.telaArea = true;
        Config.Instancia.SetToggleLixo();
        Config.Instancia.telaArea = false;
        Config.Instancia.SetSlider();
    }
    public void FecharConfigurar()
    {
        _config.SetActive(false);
    }

    public void ChatHistorMostrar()
    {
        GameManager.Instancia.ChatHistor(true);
        float w = ChatManager.Instancia.qdeMsg * 52;
        if (w < 400)
            w = 400;
        _textoChat.GetComponent<RectTransform>().sizeDelta = new Vector2(375, w);
        _textoChat.text = GestorDeRede.Instancia.MsgHistorico;
    }
    public void ChatHistorFechar()
    {
        GameManager.Instancia.ChatHistor(false);
    }

}
