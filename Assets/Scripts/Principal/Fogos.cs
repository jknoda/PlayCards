using System;
using UnityEngine;
using UnityEngine.UI;

public class Fogos : MonoBehaviour
{

    [SerializeField]
    private GameObject mesa;

    private DateTime tempoInicial;
    private int sec;

    public bool fogosOn;
    public static Fogos Instancia { get; private set; }
    private void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            fogosOn = false;
            Instancia = this;
        }
    }

    private void Update()
    {
        if (fogosOn)
        {
            TimeSpan intervalo = DateTime.Now - tempoInicial;
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Mouse0) || (intervalo.Seconds >= 5))
            {
                Stop();
            }
        }
    }

    public void Play()
    {
        mesa.GetComponent<RawImage>().color = new Color(1f, 1f, 1f, 0.5f);
        ParticleSystem ps = this.GetComponent<ParticleSystem>();
        ps.Play();
        tempoInicial = DateTime.Now;
        sec = UnityEngine.Random.Range(10, 20);
        fogosOn = true;
    }

    public void Stop()
    {
        ParticleSystem ps = this.GetComponent<ParticleSystem>();
        ps.Stop();
        mesa.GetComponent<RawImage>().color = new Color(1f, 1f, 1f, 1f);
        fogosOn = false;
    }
}
