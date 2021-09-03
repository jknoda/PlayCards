using UnityEngine;
using UnityEngine.UI;

public class MenuEntrada : MonoBehaviour
{
    [SerializeField]
    private Text _nomeDoJogador;
    [SerializeField]
    private Text _versao;

    private void Start()
    {
        _versao.text = GestorDeRede.Instancia.Versao;
    }
    public void CriaSala()
    {
        GestorDeRede.Instancia.MudaNick(_nomeDoJogador.text);
        GestorDeRede.Instancia.CriaSala(GestorDeRede.Instancia.GetCreateSala());
    }
    public void EntraSala()
    {
      
        GestorDeRede.Instancia.MudaNick(_nomeDoJogador.text);
        GestorDeRede.Instancia.EntraSala(GestorDeRede.Instancia.GetCreateSala());
    }
}
