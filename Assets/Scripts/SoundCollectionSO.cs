using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName = "Audio/Sound Collection")]
public class SoundCollectionSO : ScriptableObject
{
    public List<SoundItem> sounds;
}