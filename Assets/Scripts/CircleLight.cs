using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleLight : MonoBehaviour
{
    private void Awake()
    {
        SpriteRenderer = GetComponent<SpriteRenderer>();
    }
    public SpriteRenderer SpriteRenderer { get; private set; }
}