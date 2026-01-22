using System.Collections.Generic;
using UnityEngine;
using VnS.Utility.ObjectSelector;
using VnS.Utility.Singleton;
internal class VocalParticle : Singleton<VocalParticle>
{
    [SerializeField] private ObjectPairs<string, ParticleSystem> particles = new ObjectPairs<string, ParticleSystem>();
    private Dictionary<string, ParticleSystem> _dictionary;

    protected override void Awake()
    {
        base.Awake();
        _dictionary = particles.Collection;
    }

    public void PlayParticle(string key)
    {
        if (_dictionary.ContainsKey(key))
        {
            ParticleSystem ps = _dictionary[key];
            
            if (ps != null)
            {
                // 1. POZİSYONU AYARLA
                // Z eksenini -5 yapıyoruz ki UI'ın ve blokların önünde görünsün
               

                // 2. SIFIRLA VE OYNAT
                // Eğer zaten çalışıyorsa durdurup baştan başlatır
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play(true);
            }
        }
        else
        {
            Debug.LogWarning($"VocalParticle: '{key}' anahtarı bulunamadı!");
        }
    }
}