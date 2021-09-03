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
            switch (clip)
            {
                case "msg":
                    som = Resources.Load<AudioClip>(pasta + "msgSound");
                    break;
                case "canastra":
                    som = Resources.Load<AudioClip>(pasta + "Canastra");
                    break;
                case "lose":
                    som = Resources.Load<AudioClip>(pasta + "Lose");
                    break;
                case "win":
                    som = Resources.Load<AudioClip>(pasta + "Win");
                    break;
                case "reorganizar":
                    som = Resources.Load<AudioClip>(pasta + "Reorganizar");
                    break;
                case "finalizar":
                    som = Resources.Load<AudioClip>(pasta + "Finalizar");
                    break;
                case "selecionar":
                    som = Resources.Load<AudioClip>(pasta + "Selecionar");
                    break;
                case "drag":
                    som = Resources.Load<AudioClip>(pasta + "Drag");
                    break;
                case "endDrag":
                    som = Resources.Load<AudioClip>(pasta + "EndDrag");
                    break;
                case "lixo":
                    som = Resources.Load<AudioClip>(pasta + "Lixo");
                    break;
                case "asAs":
                    som = Resources.Load<AudioClip>(pasta + "AsAs");
                    break;
                case "morto":
                    som = Resources.Load<AudioClip>(pasta + "Morto");
                    break;
                case "suaVez":
                    som = Resources.Load<AudioClip>(pasta + "SuaVez");
                    break;
                case "fimRodada":
                    som = Resources.Load<AudioClip>(pasta + "FimRodada");
                    break;
                case "chat":
                    som = Resources.Load<AudioClip>(pasta + "Chat");
                    break;
                case "cutucar":
                    som = Resources.Load<AudioClip>(pasta + "Cutucar");
                    break;
            }
            audioSrc.PlayOneShot(som);
        }
    }
}

