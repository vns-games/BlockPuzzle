using UnityEngine;
using UnityEngine.UI;
public class UIMusicToggle : MonoBehaviour
{
    [SerializeField] private Toggle toggle;
    private void Awake()
    {
        toggle.onValueChanged.AddListener(ToggleMute);
    }
    private void OnEnable()
    {
        bool isMuted = PlayerPrefs.GetInt("music_mute", 0) == 1;
        toggle.isOn = !isMuted;
    }
    private void ToggleMute(bool b)
    {
        Music.ToggleMute();
    }
}