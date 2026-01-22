using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleLight : MonoBehaviour
{

    private void Awake()
    {
        SpriteRenderer1 = GetComponent<SpriteRenderer>();
        SpriteRenderer2 = GetComponentInChildren<SpriteRenderer>();
    }
    public SpriteRenderer SpriteRenderer1 { get; private set; }
    public SpriteRenderer SpriteRenderer2 { get; private set; }
}