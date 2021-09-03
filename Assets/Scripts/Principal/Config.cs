using Photon.Pun;
using Photon.Realtime;
using System;
using UnityEngine;
using UnityEngine.UI;

public class Config : MonoBehaviourPunCallbacks
{
    [SerializeField]
    private Slider _sliderVolume;
    [SerializeField]
    private Slider _sliderVolumeEfeito;
    [SerializeField]
    private Slider _sliderChat;
    [SerializeField]
    private Toggle _tglLixo;
    [SerializeField]
    private Text _versao;

    public bool telaArea;

    public static Config Instancia { get; private set; }
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
        telaArea = false;
        _versao.text = GestorDeRede.Instancia.Versao;
        _tglLixo.isOn = GestorDeRede.Instancia.LixoBaguncado;
        SliderChatChange();
    }
    public void SetSlider()
    {
        _sliderVolume.value = SoundManager.Instancia.audioLoop.volume;
    }
    public void SliderChange()
    {
        SoundManager.Instancia.audioLoop.volume = _sliderVolume.value;
        SoundManager.Instancia.audioSrc.volume = _sliderVolumeEfeito.value;
        GestorDeRede.Instancia.VolumeFundo = SoundManager.Instancia.audioLoop.volume;
        GestorDeRede.Instancia.VolumeEfeitos = SoundManager.Instancia.audioSrc.volume;
    }

    public void SliderVolumeEfeitoChange()
    {
        SoundManager.Instancia.audioLoop.volume = _sliderVolume.value;
        SoundManager.Instancia.audioSrc.volume = _sliderVolumeEfeito.value;
        GestorDeRede.Instancia.VolumeFundo = SoundManager.Instancia.audioLoop.volume;
        GestorDeRede.Instancia.VolumeEfeitos = SoundManager.Instancia.audioSrc.volume;
    }

    public void SliderChatChange()
    {
        GestorDeRede.Instancia.TempoChat = Convert.ToInt32(_sliderChat.value);
        _sliderChat.transform.Find("txtS").GetComponent<Text>().text = GestorDeRede.Instancia.TempoChat.ToString().PadLeft(2, '0');
    }

    public void ToggleLixoChange()
    {
        if (!telaArea)
            GestorDeRede.Instancia.SetLixoBaguncado();
    }

    public void SetToggleLixo()
    {
        _tglLixo.isOn = GestorDeRede.Instancia.LixoBaguncado;
    }
}
