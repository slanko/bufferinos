﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
public class UniversalPlayerScript : MonoBehaviour
{
    //we need to set up a fucking state machine pronto. this is not looking great in here buddy.

    public PlayerInput myInput;
    public float health;
    public bool P2;
    [SerializeField] float movementSpeed, runMovementSpeed, inputDeadZone;
    InputBuffer buffer;
    int bufferLength;
    public bool xDown, yDown, aDown, bDown;
    //InputBuffer.inputType lastDir; //took this out because of the function holding up the input buffer - maybe reenable it for certain character?
    [SerializeField] Animator anim;
    [SerializeField] Animation myAnimation;
    [SerializeField] AnimatableValues animValues;
    MoveScriptableObject currentMove;
    int currentMoveFrame;
    [SerializeField] int blockExitTime;
    float xPushAmount;

    [SerializeField, Header("Jumping")] AnimationCurve jumpArc;
    [SerializeField] float jumpSpeed, jumpDist;
    bool airborne = false, grounded, crouching, blocking, dead, running;
    public bool hitstun;

    [SerializeField, Header("Move List"), Tooltip("PUT LONGER INPUTS FIRST!! OTHERWISE THEY DON'T GET FIRED")]
    MoveScriptableObject[] specialMoves;
    [SerializeField] MoveScriptableObject[] standingNormals, crouchingNormals, airNormals, movementInputs;

    [Header("Targeting")] public bool leftOfTarget;
    public Transform target;
    [SerializeField] Transform myVisual;

    [System.NonSerialized] public int currentComboCount;

    #region inputHandling
    Vector2 moveVals = Vector2.zero;
    public void XBUTTON(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            xDown = true;
            handleButtonInputs();
        }
        if (context.canceled) xDown = false;
    }
    //hey you can add negative edge with context.cancelled - remember this
    public void YBUTTON(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            yDown = true;
            handleButtonInputs();
        }
        if (context.canceled) yDown = false;
    }
    public void ABUTTON(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            aDown = true;
            handleButtonInputs();
        }
        if (context.canceled) aDown = false;
    }
    public void BBUTTON(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            bDown = true;
            handleButtonInputs();
        }
        if (context.canceled) bDown = false;
    }

    public void XY(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            xDown = true;
            yDown = true;
            handleButtonInputs();
        }
        if (context.canceled)
        {
            xDown = false;
            yDown = false;
        }
    }

    public void AB(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            aDown = true;
            bDown = true;
            handleButtonInputs();
        }
        if (context.canceled)
        {
            aDown = false;
            bDown = false;
        }
    }

    public void handleButtonInputs()
    {
        var currentBufferOutput = buffer.bufferOutput();
        //X, Y, A, B, XY, AB, XA, YB, XB, YA, XYB, YBA, BAX, AXY, XYBA, FUCK YOU!!
        if (xDown && !yDown && !aDown && !bDown) sendInputToInputBuffer(InputBuffer.inputType.XBUTTON);
        if (!xDown && yDown && !aDown && !bDown) sendInputToInputBuffer(InputBuffer.inputType.YBUTTON);
        if (!xDown && !yDown && aDown && !bDown) sendInputToInputBuffer(InputBuffer.inputType.ABUTTON);
        if (!xDown && !yDown && !aDown && bDown) sendInputToInputBuffer(InputBuffer.inputType.BBUTTON);
        //DOUBLES
        if (xDown && yDown && !aDown && !bDown) sendInputToInputBuffer(InputBuffer.inputType.XY);
        if (!xDown && !yDown && aDown && bDown) sendInputToInputBuffer(InputBuffer.inputType.AB);
        if (xDown && !yDown && aDown && !bDown) sendInputToInputBuffer(InputBuffer.inputType.XA);
        if (!xDown && yDown && !aDown && bDown) sendInputToInputBuffer(InputBuffer.inputType.YB);
        if (xDown && !yDown && !aDown && bDown) sendInputToInputBuffer(InputBuffer.inputType.XB);
        if (!xDown && yDown && aDown && !bDown) sendInputToInputBuffer(InputBuffer.inputType.YA);
        //TRIPLES
        if (xDown && yDown && !aDown && bDown) sendInputToInputBuffer(InputBuffer.inputType.XYB);
        if (!xDown && yDown && aDown && bDown) sendInputToInputBuffer(InputBuffer.inputType.YBA);
        if (xDown && !yDown && aDown && bDown) sendInputToInputBuffer(InputBuffer.inputType.BAX);
        if (xDown && yDown && aDown && !bDown) sendInputToInputBuffer(InputBuffer.inputType.AXY);
        //THE FINALE
        if (xDown && yDown && aDown && bDown) sendInputToInputBuffer(InputBuffer.inputType.XYAB);
        //this has dealt me irreperable psych damage
    }

    public void getMovementInputs(InputAction.CallbackContext context)
    {
        moveVals = context.ReadValue<Vector2>();
    }

    public void sendInputToInputBuffer(InputBuffer.inputType dir)
    {
        buffer.addToBuffer(dir);
    }

    public InputBuffer.inputType getDirFromVector2(Vector2 dirVector)
    {
        InputBuffer.inputType input = new InputBuffer.inputType();
        //this part is gonna SUCK
        if (dirVector.magnitude > inputDeadZone || dirVector.magnitude < -inputDeadZone)
        {
            if (dirVector.x >= 0)
            {
                float tempDir = Vector2.Angle(Vector2.up, dirVector);
                if (tempDir > 0 && tempDir <= 22.5f) input = InputBuffer.inputType.UP;
                if (tempDir > 22.5f && tempDir <= 67.5f) input = InputBuffer.inputType.UPRIGHT;
                if (tempDir > 67.5f && tempDir <= 117.5f) input = InputBuffer.inputType.RIGHT;
                if (tempDir > 117.5f && tempDir <= 157.5f) input = InputBuffer.inputType.DOWNRIGHT;
                if (tempDir > 157.5f && tempDir <= 180f) input = InputBuffer.inputType.DOWN;
            }
            if (dirVector.x <= 0)
            {
                float tempDir = Vector2.Angle(Vector2.down, dirVector);
                if (tempDir > 0 && tempDir <= 22.5f) input = InputBuffer.inputType.DOWN;
                if (tempDir > 22.5f && tempDir <= 67.5f) input = InputBuffer.inputType.DOWNLEFT;
                if (tempDir > 67.5f && tempDir <= 117.5f) input = InputBuffer.inputType.LEFT;
                if (tempDir > 117.5f && tempDir <= 157.5f) input = InputBuffer.inputType.UPLEFT;
                if (tempDir > 157.5f && tempDir <= 180f) input = InputBuffer.inputType.UP;
            }
        }
        else input = InputBuffer.inputType.NEUTRAL;
        return input;
    }

    void handleInputs()
    {
        InputBuffer.inputType dir = getDirFromVector2(moveVals);
        //if (dir != lastDir || dir == InputBuffer.inputType.NEUTRAL) // this is questionable - shouldn't we allow all inputs and scrub the list until we get the one we need??
        buffer.addToBuffer(dir); // yes we should. otherwise the input buffer would just STOP AND WAIT until you'd done the whole input if you kept buttons held.
        //lastDir = dir;
    }

    #endregion

    #region bufferOutputHandling

    InputBuffer.inputType[] currentBufferOutput;
    InputBuffer.inputType currentDir;

    bool getCurrentDir(string dir)
    {
        if (dir == currentDir.ToString()) return true;
        else return false;
    }

    void bufferReadUpdate()
    {
        currentBufferOutput = buffer.bufferOutput();
        currentDir = currentBufferOutput[0];
        if (!myAnimation.isPlaying || (myAnimation.isPlaying && currentMoveFrame >= currentMove.specialCancelTime && currentMove.specialCancelTime != 0))
        {
            bool foundSpecial = false, foundNormal = false;
            foundSpecial = readMoveList(specialMoves);
            if (!foundSpecial)
            {
                if (!crouching && !airborne) foundNormal = readMoveList(standingNormals);
                if (crouching && !airborne) foundNormal = readMoveList(crouchingNormals);
                if (airborne) foundNormal = readMoveList(airNormals);
            }
            if (!foundNormal)
            {
                readMoveList(movementInputs);
            }
        }
    }
    //god this is so much prettier. thank you tired me
    bool readMoveList(MoveScriptableObject[] list)
    {
        bool foundMove = false;
        foreach (MoveScriptableObject move in list)
        {
            if (checkMove(move, currentBufferOutput))
            {
                foundMove = true;
                Debug.Log("found special!");
                if (myAnimation.isPlaying) cancelCurrentMove();
                startMove(move);
                break;
            }
        }
        return foundMove;

    }
    //get input buffer
    InputBuffer.inputType getFlippedInput(InputBuffer.inputType currentInput)
    {
        InputBuffer.inputType input = currentInput;

        switch (input)
        {
            case InputBuffer.inputType.LEFT:
                input = InputBuffer.inputType.RIGHT;
                break;
            case InputBuffer.inputType.RIGHT:
                input = InputBuffer.inputType.LEFT;
                break;
            case InputBuffer.inputType.DOWNLEFT:
                input = InputBuffer.inputType.DOWNRIGHT;
                break;
            case InputBuffer.inputType.DOWNRIGHT:
                input = InputBuffer.inputType.DOWNLEFT;
                break;
            case InputBuffer.inputType.UPLEFT:
                input = InputBuffer.inputType.UPRIGHT;
                break;
            case InputBuffer.inputType.UPRIGHT:
                input = InputBuffer.inputType.UPLEFT;
                break;
        }
        return input;
    }

    //WE NEED TO WORK ON THIS. WE NEED LIKE, A BUFFER SYSTEM. WHEN A MOVE IS EXECUTED, SAVE IT, THEN IF ABLE, DO IT. IF NOT ABLE, WAIT UNTIL YOU ARE. 
    //IF ANOTHER SPECIAL IS DONE, REPLACE BUFFERED SPECIAL. THIS GOES FOR EVERY PART OF THE MOVELIST.
    bool checkMove(MoveScriptableObject move, InputBuffer.inputType[] currentBuffer, int bufLength = 0)
    {
        if (bufLength == 0) bufLength = currentBuffer.Length - 1;
        bool moveGood = true;
        if (move != currentMove)
        {
            int startPoint = bufLength;
            foreach (InputBuffer.inputType motion in move.inputRequired)
            {
                for (int i = startPoint; i > 0; i--)
                {
                    if (motion == currentBuffer[i] && leftOfTarget)
                    {
                        moveGood = true;
                        startPoint = i;
                        break;
                    }
                    else if (motion == getFlippedInput(currentBuffer[i]) && !leftOfTarget)
                    {
                        moveGood = true;
                        startPoint = i;
                        break;
                    }
                    else moveGood = false;
                }
                if (moveGood == false) break;
            }
        }
        else moveGood = false;
        return moveGood;
    }

    void cancelCurrentMove()
    {
        myAnimation.Stop();
        StopCoroutine(currentDisablerCoroutine);
        currentMove.moveAnim.legacy = false;
        currentMove = new MoveScriptableObject();
        anim.enabled = true;
    }

    void startMove(MoveScriptableObject move)
    {
        bool continuousMove = move.continuous;
        buffer.clearBuffer();
        animValues.XMoveMultiplier = 0;
        //LEGACY COMPONENTS?? VOMIT EMOJI
        currentMove = move;
        currentMove.moveAnim.legacy = true;
        myAnimation.AddClip(move.moveAnim, "MOVE");
        myAnimation.Play("MOVE");
        currentDisablerCoroutine = StartCoroutine(disableAnimatorForAMove(continuousMove));
        Debug.Log("Move good!");
    }

    Coroutine currentDisablerCoroutine;
    IEnumerator disableAnimatorForAMove(bool noIdle = default(bool))
    {
        currentMoveFrame = 0;
        anim.enabled = false;
        while (myAnimation.isPlaying)
        {
            currentMoveFrame++;
            yield return new WaitForFixedUpdate();
        }
        currentMove.moveAnim.legacy = false; // this is FUCKING ANNOYING!! they have to be legacy to play but have to not be legacy to be edited in unity. CRINGE!!
        currentMove = new MoveScriptableObject();
        anim.enabled = true;
        if(noIdle) anim.Play("Idle");
    }

    IEnumerator clearTriggerWithDelay(string trigger)
    {
        yield return new WaitForSeconds(0.1f);
        anim.ResetTrigger(trigger);
        if (anim.GetBool(trigger)) Debug.Log("SHIT!!");
        else Debug.Log("YUH");
    }

    #endregion

    private void Start()
    {
        myInput = GetComponent<PlayerInput>();
        buffer = GetComponent<InputBuffer>();
        bufferLength = buffer.bufferOutput().Length - 1;
        MeterHandlerScript meterHandler = GameObject.Find("GOD").GetComponent<MeterHandlerScript>();
        if (!P2) meterHandler.playerOne = this;
        else meterHandler.playerTwo = this;
        //lastDir = InputBuffer.inputType.NEUTRAL;
    }

    private void FixedUpdate()
    {
        if (health <= 0) dead = true;
        if (!anim.GetBool("Crouching") && !myAnimation.isPlaying && !dead && !blocking && !hitstun)
        {
            if (!running)
            {
                if (getCurrentDir("RIGHT")) transform.Translate(new Vector3(movementSpeed * Time.deltaTime, 0, 0));
                if (getCurrentDir("LEFT")) transform.Translate(new Vector3(-movementSpeed * Time.deltaTime, 0, 0));
            }
            else
            {
                if (getCurrentDir("RIGHT")) transform.Translate(new Vector3(runMovementSpeed * Time.deltaTime, 0, 0));
                if (getCurrentDir("LEFT")) transform.Translate(new Vector3(-runMovementSpeed * Time.deltaTime, 0, 0));
            }
            if (getCurrentDir("UP") || getCurrentDir("UPRIGHT") || getCurrentDir("UPLEFT"))
            {
                StartCoroutine(jump(currentDir));
                anim.SetTrigger("Jump");
                StartCoroutine(clearTriggerWithDelay("Jump"));
            }
        }
        if (myAnimation.isPlaying && leftOfTarget) transform.Translate(new Vector3(animValues.XMoveMultiplier, 0, 0) * Time.deltaTime);
        else if (myAnimation.isPlaying && !leftOfTarget) transform.Translate(new Vector3(-animValues.XMoveMultiplier, 0, 0) * Time.deltaTime);

        if (!myAnimation.isPlaying) animValues.XMoveMultiplier = 0;
        if (!dead)
        {
            leftOfTarget = transform.position.x < target.transform.position.x;
            if (leftOfTarget) myVisual.transform.localScale = new Vector3(1, 1, 1);
            else myVisual.transform.localScale = new Vector3(1, 1, -1);
        }
        if (blockExitTime > 0) blockExitTime--;
        if (blockExitTime <= 0)
        {
            blocking = false;
            blockExitTime = 0;
        }

        if (xPushAmount > 0) xPushAmount -= 1 * Time.deltaTime * 2;
        if (xPushAmount < 0) xPushAmount += 1 * Time.deltaTime * 2;
        if (xPushAmount > 1 || xPushAmount < -1) transform.Translate(xPushAmount * Time.deltaTime, 0, 0);
        else transform.Translate(xPushAmount * Time.deltaTime, 0, 0);

        if (!hitstun && currentComboCount > 0) currentComboCount = 0;
        handleInputs();
        if (!hitstun) bufferReadUpdate();
        animatorUpdate();
    }

    void animatorUpdate()
    {
        if (leftOfTarget)
        {
            anim.SetBool("WalkingForward", getCurrentDir("RIGHT"));
            anim.SetBool("WalkingBackward", getCurrentDir("LEFT"));
        }
        else
        {
            anim.SetBool("WalkingForward", getCurrentDir("LEFT"));
            anim.SetBool("WalkingBackward", getCurrentDir("RIGHT"));
        }
        anim.SetBool("Airborne", airborne);
        anim.SetBool("Crouching", getCurrentDir("DOWN") || getCurrentDir("DOWNRIGHT") || getCurrentDir("DOWNLEFT"));
        crouching = anim.GetBool("Crouching");
        anim.SetBool("Dead", dead);
        anim.SetBool("Block", blocking);
        if (running && (getCurrentDir("NEUTRAL") || (leftOfTarget && getCurrentDir("LEFT")) || (!leftOfTarget && getCurrentDir("RIGHT")) || (getCurrentDir("UPLEFT") || getCurrentDir("UP") || getCurrentDir("UPRIGHT"))))
        {
            anim.SetTrigger("EndRun");
            running = false;
        }
    }

    IEnumerator jump(InputBuffer.inputType input)
    {
        if(airborne == false && (input == InputBuffer.inputType.UP || input == InputBuffer.inputType.UPRIGHT || input == InputBuffer.inputType.UPLEFT))
        {
            Vector3 pos = transform.position;
            airborne = true;
            for (float i = 0; i < 1; i += (jumpSpeed * Time.deltaTime))
            {
                switch (input)
                {
                    case InputBuffer.inputType.UP:
                        transform.position = new Vector3(transform.position.x, pos.y + jumpArc.Evaluate(i), pos.z);
                        break;

                    case InputBuffer.inputType.UPRIGHT:
                        transform.position = new Vector3(transform.position.x, pos.y + jumpArc.Evaluate(i), pos.z);
                        transform.Translate(new Vector3(jumpDist * Time.deltaTime, 0, 0));
                        break;

                    case InputBuffer.inputType.UPLEFT:
                        transform.position = new Vector3(transform.position.x, pos.y + jumpArc.Evaluate(i), pos.z);
                        transform.Translate(new Vector3(-jumpDist * Time.deltaTime, 0, 0));
                        break;

                }
                yield return new WaitForEndOfFrame();
                anim.SetFloat("JumpPercent", i);
            }
            airborne = false;
            transform.position = new Vector3(transform.position.x, 1.5f, transform.position.z);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if(other.gameObject.tag == "Hitbox")
        {
            HitboxScript hitbox = other.GetComponent<HitboxScript>();
            if (!hitbox.myHit.applied && hitbox.myHit.P2 != P2)
            {
                hitbox.myHit.applied = true;
                GetHit(hitbox.myHit);
            }
        }
    }

    public void GetHit(HitboxHandler.hitboxStruct hit)
    {
        if((leftOfTarget && (currentBufferOutput[0] == InputBuffer.inputType.LEFT || currentBufferOutput[0] == InputBuffer.inputType.DOWNLEFT) || !leftOfTarget && (currentBufferOutput[0] == InputBuffer.inputType.RIGHT || currentBufferOutput[0] == InputBuffer.inputType.DOWNRIGHT) || currentBufferOutput[0] == InputBuffer.inputType.NEUTRAL) && !myAnimation.isPlaying && !hitstun && !dead) //abandon hope all ye who read this statement
        {
            blocking = true;
            blockExitTime = 20;
            health -= hit.chipDamage;
            addPushForce(hit.xChangeAmount * 0.75f);
        }
        else
        {
            if (myAnimation.isPlaying) cancelCurrentMove();
            health -= hit.damage;
            if (hitstun == true) currentComboCount++;
            switch (hit.height)
            {
                case HitboxScript.attackHeight.HIGH:
                    anim.Play("HitHigh", -1, 0);
                    break;
                case HitboxScript.attackHeight.MID:
                    anim.Play("HitMid", -1, 0);
                    break;
                case HitboxScript.attackHeight.LOW:
                    anim.Play("HitLow", -1, 0);
                    break;
                case HitboxScript.attackHeight.SWEEP:
                    anim.Play("Swept");
                    break;
            }
            hitstun = true;
            addPushForce(hit.xChangeAmount);
            StartCoroutine(hitStop(hit.hitStop));
        }
    }

    void addPushForce(float amount)
    {
        amount = amount * 0.75f;
        if (leftOfTarget) xPushAmount -= amount;
        else xPushAmount += amount;
    }

    public IEnumerator hitStop(int stopFrames)
    {
        Time.timeScale = 0;
        Debug.Log("Stopped!");
        for(int i = 0; i < stopFrames; i++)
        {
            yield return new WaitForEndOfFrame();
        }
        Time.timeScale = 1;
        Debug.Log("Started.");
    }

    public void startRunning()
    {
        running = true;
        anim.enabled = true;
        anim.Play("RunForward");
    }

}
