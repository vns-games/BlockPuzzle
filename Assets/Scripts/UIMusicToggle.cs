using UnityEngine;
using UnityEngine.UI;
public class UIMusicToggle : MonoBehaviour
{
    [SerializeField] private UIToggleSprite sprite;
    
    private void OnEnable()
    {
        bool isMuted = PlayerPrefs.GetInt("mute_music", 0) == 1;
        sprite.OnValueChanged(!isMuted);
    }
    public void ToggleMute(bool b)
    {
        Music.Mute(!b);
    }
}