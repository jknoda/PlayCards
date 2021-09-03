using UnityEngine;
using UnityEngine.UI;

public class Carta : MonoBehaviour
{
    //public int seq { get; set; }
    public int Id { get; set; }
    public short Deck { get; set; }
    public string Nome { get; set; }
    public string Verso { get; set; }
    public Color Cor { get; set; }
    public Color CorJogada { get; set; }
    //public Color CorAntesJogada { get; set; }
    public Vector3 PosicaoInicial { get; set; }

    private void Awake()
    {
        this.GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/Cards/BackColor_Red");
    }

    public void MostraCarta(bool frente) //string name)
    {

        string name = Nome.Replace("14", "1");
        if (!frente)
            name = Verso;
        this.GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/Cards/" + name);
        this.GetComponent<Image>().color = this.CorJogada;
        if (!name.Contains("BackColor"))
            if (GameCardsManager.Instancia.GetCarta(this.Id).Lixo)
                this.GetComponent<Image>().color = GestorDeRede.Instancia.GetCor("2lixo");

    }
}
