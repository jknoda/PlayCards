using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Acoes : MonoBehaviour
{
    public void Fixar()
    {
        if (Baralho.Instancia.cartasSel.Count == 1)
        {
            var carta = GameCardsManager.Instancia.GetCarta(Baralho.Instancia.cartasSel[0]);
            if (!(carta.Valor == 2 && carta.Neutro2))
            {
                Color cor = GestorDeRede.Instancia.GetCor("fixar");
                Baralho.Instancia.cartas[carta.Id].GetComponent<Carta>().Cor = cor; // new Color(0.905f, 0.756f, 0.780f);
            }
            GameCardsManager.Instancia.SetSeqFixoCarta(carta.Id, carta.Seq);
            Baralho.Instancia.LimparSelecionados();
        }
    }
    public void DesFixar()
    {
        if (Baralho.Instancia.cartasSel.Count == 1)
        {
            var carta = GameCardsManager.Instancia.GetCarta(Baralho.Instancia.cartasSel[0]);
            if (!(carta.Valor == 2 && carta.Neutro2))
            {
                Baralho.Instancia.cartas[carta.Id].GetComponent<Carta>().Cor = GestorDeRede.Instancia.GetCor("padrao"); // Color.white;
            }
            GameCardsManager.Instancia.SetSeqFixoCarta(carta.Id, 9000);
            Baralho.Instancia.LimparSelecionados();
        }
    }

    public void SomarSwap()
    {
        GameManager.Instancia.ZoomSomar = !GameManager.Instancia.ZoomSomar;
        if (GameManager.Instancia.ZoomSomar)
        {
            Zoom.Instancia.Somar(0);
        }
        else
        {
            Zoom.Instancia.DesligarSoma();
        }
    }
}
