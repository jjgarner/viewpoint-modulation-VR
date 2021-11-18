using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VectUIManager : MonoBehaviour
{
    public GameObject expManager_ref;
    public GameObject fillCircle_ref;
    public GameObject backCircle_ref;
    public GameObject vrRig_ref; // need to access this thing's ControllerListener script to detect joystick and button inputs
    public ControllerListener listener_ref;

    public bool needFadeIn;
    public bool selected;
    public bool watchingFeedback;
    public bool confirmed;

    public int currentSelection;
    public float goalFadeVal;
    public float prevFadeVal;
    public float goalFillVal;
    public float prevFillVal;

    public float[] possibleLocations;
    public float xLocation;
    public float prevX;
    public float yLocation;
    public float zLocation;

    public float fadeInTimer;
    public float joystickTimer;
    public float feebackTimer;
    public float fadeOutTimer;
    public int trialNumb;


    // Start is called before the first frame update
    void Start()
    {
        // initialize references to things that are not children of this prefab gameobject
        expManager_ref = GameObject.Find("Experiment Manager");
        trialNumb = expManager_ref.GetComponent<ExperimentManager>().currentTrial;
        vrRig_ref = GameObject.Find("VR Rig");
        listener_ref = vrRig_ref.GetComponent<ControllerListener>();
        
        currentSelection = 5;
        goalFadeVal = 1;
        goalFillVal = 0;
        prevFadeVal = 0;
        prevFillVal = 0;
        fadeInTimer = 0;
        fadeOutTimer = 0;
        joystickTimer = 0;
        feebackTimer = 0;

        possibleLocations = new float[] { -1.65f, -1.32f, -.99f, -.66f, -.33f, 0, .33f, .66f, .99f, 1.32f, 1.65f };
        xLocation = transform.position.x;
        yLocation = transform.position.y - 1;
        zLocation = transform.position.z;

        needFadeIn = true;
        selected = true; // for this canvas, starts with the "5" option already selected - without moving joystick, participant can just press A to confirm this selection
        watchingFeedback = false;
        confirmed = false;
    }


    // Update is called once per frame
    void Update()
    {
        // when this first becomes active, fade in from transparent to solid over time
        if (needFadeIn)
        {
            fadeInTimer += Time.deltaTime;

            this.GetComponent<CanvasGroup>().alpha = Mathf.Lerp(prevFadeVal, goalFadeVal, 0.1f);
            prevFadeVal = Mathf.Lerp(prevFadeVal, goalFadeVal, 0.1f);

            if (fadeInTimer > 1)
            {
                this.GetComponent<CanvasGroup>().alpha = 1;
                needFadeIn = false;
                prevFadeVal = 1;
                goalFadeVal = 0;
            }
        }
        // after visual feedback of choice is complete, fade out this text over 1 sec, then get rid of this GUI
        else if (confirmed)
        {
            fadeOutTimer += Time.deltaTime;

            this.GetComponent<CanvasGroup>().alpha = Mathf.Lerp(prevFadeVal, goalFadeVal, 0.1f);
            prevFadeVal = Mathf.Lerp(prevFadeVal, goalFadeVal, 0.1f);

            if (fadeOutTimer >= 1)
            {
                this.GetComponent<CanvasGroup>().alpha = 0;
                // save response for this trial in ExperimentManager arrays
                expManager_ref.GetComponent<ExperimentManager>().vectionRating[trialNumb] = currentSelection;
                // change flag in exp manager to progress through exp
                expManager_ref.GetComponent<ExperimentManager>().needVectResponse = false;
                // delete this instantiated prefab canvas from scene (create a new one next trial)
                Destroy(this.gameObject);
            }
        }
        // else, waiting for participant to make choice
        else
        {
            // limits how quickly participant can change selection
            joystickTimer += Time.deltaTime;
            if (joystickTimer >= 0.25f)
            {
                // right increases selection value
                if (listener_ref.inputAxis.x > listener_ref.thumbStickSensitivity || Input.GetKeyDown(KeyCode.RightArrow))
                {
                    currentSelection++;
                    if (currentSelection >= 10) { currentSelection = 10; };
                    selected = true;
                    prevX = xLocation;
                    xLocation = transform.position.x + possibleLocations[currentSelection];
                    joystickTimer = 0;
                }

                // left decreases selection value
                if (listener_ref.inputAxis.x < -1 * listener_ref.thumbStickSensitivity || Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    currentSelection--;
                    if (currentSelection <= 0) { currentSelection = 0; };
                    selected = true;
                    prevX = xLocation;
                    xLocation = transform.position.x + possibleLocations[currentSelection];
                    joystickTimer = 0;
                }
            }

            // smooth movement of selection indicator to currently selected option
            backCircle_ref.transform.position = new Vector3(Mathf.Lerp(prevX, xLocation, 0.2f), yLocation, zLocation);
            fillCircle_ref.transform.position = new Vector3(Mathf.Lerp(prevX, xLocation, 0.2f), yLocation, zLocation);
            prevX = Mathf.Lerp(prevX, xLocation, 0.1f);

            // confirm selection with A button press, then start 1 sec of visual feedback
            if (selected & listener_ref.primaryBut || Input.GetKeyDown(KeyCode.Space))
            {
                // allow the circle to fill as a visual feedback
                goalFillVal = 1;
                watchingFeedback = true;
                selected = false;
            }

            if (watchingFeedback)
            {
                fillCircle_ref.GetComponent<Image>().fillAmount = Mathf.Lerp(prevFillVal, goalFillVal, 0.1f);
                prevFillVal = Mathf.Lerp(prevFillVal, goalFillVal, 0.1f);

                feebackTimer += Time.deltaTime;

                if (feebackTimer > 1)
                {
                    // now that visual feedback is done, move on to last stage 
                    confirmed = true;
                }
            }
        }  
    }
}
