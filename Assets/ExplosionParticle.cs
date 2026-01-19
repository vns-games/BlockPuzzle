using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class CellParticle : MonoBehaviour
{
    private ParticleSystem _mainParticle;
    [SerializeField] private ParticleSystem secondParticle, thirdParticle;
    private ParticleSystem.MainModule _mainModule;
    private ParticleSystem.ColorOverLifetimeModule _colorOverLifetimeModule1, _colorOverLifetimeModule2;


    private void Awake()
    {
        _mainParticle = GetComponent<ParticleSystem>();
        _mainModule = _mainParticle.main;
        _colorOverLifetimeModule1 = secondParticle.colorOverLifetime;
        _colorOverLifetimeModule2 = thirdParticle.colorOverLifetime;
    }

    public void Play(BlockColorType type)
    {
        gameObject.SetActive(true);
        var colors = ParticleColorFactory.Colors[type];
        // 4. Oynat (Restart)
        _mainParticle.Play(true);

        var color = _mainModule.startColor;
        color.color = colors.PrimaryColor;
        _mainModule.startColor = color;


        var gradient1 = new Gradient();
        gradient1.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0), new GradientColorKey(colors.GradientColor1, .52f)
            }, new[]
            {
                new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 1)
            });

        var gradient = _colorOverLifetimeModule1.color;
        gradient.gradient = gradient1;
        _colorOverLifetimeModule1.color = gradient;

        var gradient2 = new Gradient();
        gradient2.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0), new GradientColorKey(colors.GradientColor2, 1)
            }, new[]
            {
                new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .109f), new GradientAlphaKey(1, .603f), new GradientAlphaKey(0, 1),
            });

        var maxGradient = _colorOverLifetimeModule2.color;
        maxGradient.gradient = gradient2;
        _colorOverLifetimeModule2.color = maxGradient;
    }
}