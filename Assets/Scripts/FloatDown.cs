using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatDown : MonoBehaviour
{
    public GameObject vrRig_ref;
    public bool needFloatDown;
    public float floatDownTimer;
    public float previousValue;
    public float goalValue;
    public float startingX;
    public float startingZ;


    void Start()
    {
        vrRig_ref = GameObject.Find("VR Rig");

        // set the starting position of the floating screen
        transform.position = vrRig_ref.transform.position + new Vector3(0, 8, 4.75f);
        startingX = transform.position.x;
        startingZ = transform.position.z;

        needFloatDown = true;
        floatDownTimer = 0;
        previousValue = 8;
        goalValue = 2;
    }

    void Update()
    {
        // when this first becomes active, fade in from transparent to solid over time
        if (needFloatDown)
        {
            floatDownTimer += Time.deltaTime;

            transform.position = new Vector3(startingX, Mathf.Lerp(previousValue, goalValue, 0.1f), startingZ);
            previousValue = Mathf.Lerp(previousValue, goalValue, 0.1f);

            if (floatDownTimer > 2)
            {
                this.transform.position = new Vector3(startingX, 2, startingZ);
                needFloatDown = false;
            }
        }
    }
}
