using System.Collections.Generic;
using UnityEngine;
public static class ParticleColorFactory
{
    public static Dictionary<BlockColorType, ParticleColor> Colors = new Dictionary<BlockColorType, ParticleColor>
    {
        {
            BlockColorType.Blue, new ParticleColor
            {
                GradientColor1 = Color.blue,
                GradientColor2 = Color.blue,
                PrimaryColor = Color.blue,
            }
        },
        {
            BlockColorType.Cyan, new ParticleColor
            {
                GradientColor1 = Color.cyan,
                GradientColor2 = Color.cyan,
                PrimaryColor = Color.cyan,
            }
        },
        {
            BlockColorType.Green, new ParticleColor
            {
                GradientColor1 = Color.green,
                GradientColor2 = Color.green,
                PrimaryColor = Color.green,
            }
        },
        {
            BlockColorType.Orange, new ParticleColor
            {
                GradientColor1 = new Color(1, .5f, 0),
                GradientColor2 = new Color(1, .5f, 0),
                PrimaryColor = new Color(1, .5f, 0),
            }
        },
        {
            BlockColorType.Pink, new ParticleColor
            {
                GradientColor1 = new Color(1,0,1),
                GradientColor2 = new Color(1,0,1),
                PrimaryColor = new Color(1,0,1),
            }
        },
        {
            BlockColorType.Purple, new ParticleColor
            {
                GradientColor1 = new Color(.5f,0,1),
                GradientColor2 = new Color(.5f,0,1),
                PrimaryColor = new Color(.5f,0,1),
            }
        },
        {
            BlockColorType.Red, new ParticleColor
            {
                GradientColor1 = Color.red,
                GradientColor2 = Color.red,
                PrimaryColor = Color.red,
            }
        },
        {
            BlockColorType.Yellow, new ParticleColor
            {
                GradientColor1 = Color.yellow,
                GradientColor2 = Color.yellow,
                PrimaryColor = Color.yellow,
            }
        },

    };
}