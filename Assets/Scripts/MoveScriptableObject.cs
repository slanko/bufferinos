﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewMove", menuName = "Game Specific/Move")]
public class MoveScriptableObject : ScriptableObject
{
    public string moveName;
    public bool grounded, airborne;
    public int specialCancelTime = 0;
    public bool continuous;
    public AnimationClip moveAnim;
    public InputBuffer.inputType[] inputRequired;

    [Header("On Hit Properties")] public bool launcher;
    public float xChangeAmount;
}
