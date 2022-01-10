using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    public GameObject soundONOFF;
    public AudioSource audioLoop;
    public AudioSource audioSrc;

    private string pasta;
    
    private bool ON { get; set; }

    public static SoundManager Instancia { get; private set; }
    private void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instancia = this;
            PlayONOFF(false);
            audioLoop.volume = GestorDeRede.Instancia.VolumeFundo;
            audioSrc.volume = GestorDeRede.Instancia.VolumeEfeitos;
        }
    }

    void Start()
    {
        pasta = "Sound/" + Random.Range(1, 3).ToString().PadLeft(3, '0') + "/";
    }

    public void PlayONOFF(bool inverter = true)
    {
        if (inverter)
            this.ON = !this.ON;
        else
            this.ON = GestorDeRede.Instancia.SoundOn;
        if (this.ON)
        {
            audioLoop.mute = false;
            soundONOFF.GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/SomON");
        }
        else
        {
            audioLoop.mute = true;
            soundONOFF.GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/SomOFF");
        }
        GestorDeRede.Instancia.SoundOn = this.ON;
    }
    public void PlaySound(string clip)
    {
        if (pasta == null)
            return;
        if (this.ON)
        {
            AudioClip som = Resources.Load<AudioClip>(pasta + "msgSound");
            switch (clip.ToUpper())
            {
                case "MSG":
                    som = Resources.Load<AudioClip>(pasta + "msgSound");
                    break;
                case "CANASTRA":
                    som = Resources.Load<AudioClip>(pasta + "Canastra");
                    break;
                case "LOSE":
                    som = Resources.Load<AudioClip>(pasta + "Lose");
                    break;
                case "WIN":
                    som = Resources.Load<AudioClip>(pasta + "Win");
                    break;
                case "REORGANIZAR":
                    som = Resources.Load<AudioClip>(pasta + "Reorganizar");
                    break;
                case "FINALIZAR":
                    som = Resources.Load<AudioClip>(pasta + "Finalizar");
                    break;
                case "SELECIONAR":
                    som = Resources.Load<AudioClip>(pasta + "Selecionar");
                    break;
                case "DRAG":
                    som = Resources.Load<AudioClip>(pasta + "Drag");
                    break;
                case "ENDDRAG":
                    som = Resources.Load<AudioClip>(pasta + "EndDrag");
                    break;
                case "LIXO":
                    som = Resources.Load<AudioClip>(pasta + "Lixo");
                    break;
                case "ASAS":
                    som = Resources.Load<AudioClip>(pasta + "AsAs");
                    break;
                case "MORTO":
                    som = Resources.Load<AudioClip>(pasta + "Morto");
                    break;
                case "SUAVEZ":
                    som = Resources.Load<AudioClip>(pasta + "SuaVez");
                    break;
                case "FIMRODADA":
                    som = Resources.Load<AudioClip>(pasta + "FimRodada");
                    break;
                case "CHAT":
                    som = Resources.Load<AudioClip>(pasta + "Chat");
                    break;
                case "CUTUCAR":
                    som = Resources.Load<AudioClip>(pasta + "Cutucar");
                    break;
                case "HUMOR":
                    som = Resources.Load<AudioClip>(pasta + "Humor");
                    break;
            }
            audioSrc.PlayOneShot(som);
        }
    }
}

