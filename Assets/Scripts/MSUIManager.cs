using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class MSUIManager : MonoBehaviour
{
    public GameObject expManager_ref;
    public Text msText_ref;
    public GameObject fillCircle_ref;
    public GameObject backCircle_ref;
    public GameObject vrRig_ref; // need to access this thing's ControllerListener script to detect joystick and button inputs
    public ControllerListener listener_ref;

    public bool needUpdateExpParameters; // because this canvas is on before all experiment parameters are imported from csv or manually selected
    public bool trialIsOngoing;
    public bool needFadeIn;
    public bool needResponse;
    public bool hasntChosen;
    public bool confirmed;

    public int trialNumb;
    public int msScaleTotal;
    public int msRatingsAsks;
    public int ratingCounter;
    public int currentSelection;
    public int confirmedSelection;

    public float nextTimer;
    public float goalFadeVal;
    public float prevFadeVal;
    public float goalFillVal;
    public float prevFillVal;

    public float fadeInTimer;
    public float joystickTimer;
    public float fadeOutTimer;


    // Start is called before the first frame update
    void Start()
    {
        // initialize references to things that are not children of this prefab gameobject
        expManager_ref = GameObject.Find("Experiment Manager");
        vrRig_ref = GameObject.Find("VR Rig");
        listener_ref = vrRig_ref.GetComponent<ControllerListener>();

        nextTimer = 0;

        ratingCounter = 0;
        currentSelection = 0;
        confirmedSelection = -999;

        goalFadeVal = 1;
        goalFillVal = 0;
        prevFadeVal = 0;
        prevFillVal = 0;
        fadeInTimer = 0;
        fadeOutTimer = 0;
        joystickTimer = 0;

        needFadeIn = false;
        needResponse = false;
        hasntChosen = true;
        confirmed = false;
        trialIsOngoing = false; // when this is true, once nextTimer passes X seconds, this GUI becomes visible and participant gives next ms rating
    }


    // Update is called once per frame
    void Update()
    {
        // always have correct current trial index
        trialNumb = expManager_ref.GetComponent<ExperimentManager>().currentTrial;

        // allow participant to change rating at any time, but data is only saved when they press A while canvas is fully visible
        if (trialIsOngoing)
        {
            // limits how quickly participant can change selection
            joystickTimer += Time.deltaTime;
            if (joystickTimer >= 0.25f)
            {
                // right increases selection value
                if (listener_ref.inputAxis.x > listener_ref.thumbStickSensitivity || Input.GetKeyDown(KeyCode.RightArrow))
                {
                    currentSelection++;
                    if (currentSelection >= msScaleTotal) { currentSelection = msScaleTotal; };
                    msText_ref.text = currentSelection.ToString();
                    goalFillVal = ((float)currentSelection) / msScaleTotal;
                    joystickTimer = 0;
                }

                // left decreases selection value
                if (listener_ref.inputAxis.x < -1 * listener_ref.thumbStickSensitivity || Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    currentSelection--;
                    if (currentSelection <= 0) { currentSelection = 0; };
                    msText_ref.text = currentSelection.ToString();
                    goalFillVal = ((float)currentSelection) / msScaleTotal;
                    joystickTimer = 0;
                }
            }

            // fill ring to remind participant what the selected number means (how far up or down on motion sickness scale it is)
            fillCircle_ref.GetComponent<Image>().fillAmount = Mathf.Lerp(prevFillVal, goalFillVal, 0.1f);
            prevFillVal = Mathf.Lerp(prevFillVal, goalFillVal, 0.1f);


            // after a certain amount of time in a trial (depends on exp settings), ms GUI becomes visible until A is pressed
            nextTimer += Time.deltaTime;
            if (nextTimer >= expManager_ref.GetComponent<ExperimentManager>().trialDuration / (float)msRatingsAsks)
            {
                needFadeIn = true;
                nextTimer = 0;

                // if participant did not give a response within that time between previous and new ms rating ask,
                // save a -999 to indicate no response, increment raringCounter, do things that allow trial to progress
                if (needResponse)
                {
                    expManager_ref.GetComponent<ExperimentManager>().motSicRatings[trialNumb, ratingCounter] = confirmedSelection;
                    ratingCounter++;
                    needResponse = false;
                }

                // if raitingCounter adds up to the total number of msAsks needed in one trial, then start over at 0 for next trial
                if (ratingCounter == msRatingsAsks)
                {
                    this.GetComponent<CanvasGroup>().alpha = 0;
                    confirmed = false;
                    confirmedSelection = -999;
                    hasntChosen = true;
                    goalFadeVal = 1; // for when msGUI needs to fade in again
                    fadeOutTimer = 0; // for next time needs to fade out again

                    ratingCounter = 0;
                    nextTimer = 0;
                    // change flags to progress through exp
                    expManager_ref.GetComponent<ExperimentManager>().needDetectPrefab = true;
                    trialIsOngoing = false;

                    needFadeIn = false;
                }
            }

            // when the msUI needs to pop up, fade in from transparent to solid over time
            if (needFadeIn)
            {
                fadeInTimer += Time.deltaTime;

                this.GetComponent<CanvasGroup>().alpha = Mathf.Lerp(prevFadeVal, goalFadeVal, 0.2f);
                prevFadeVal = Mathf.Lerp(prevFadeVal, goalFadeVal, 0.2f);

                // if participant tries to confirm a selection before fade-in completes,
                // save the choice and use it in the needResponse section
                if (listener_ref.primaryBut & hasntChosen || Input.GetKeyDown(KeyCode.Space))
                {
                    confirmedSelection = currentSelection;
                    hasntChosen = false;
                }

                if (fadeInTimer > 1)
                {
                    this.GetComponent<CanvasGroup>().alpha = 1;
                    needFadeIn = false;
                    needResponse = true;
                    prevFadeVal = 1;
                    goalFadeVal = 0;
                    fadeInTimer = 0;
                }
            }

            if (needResponse)
            {
                // either input the choice made during fade-in, or wait for participant to confirm a choice
                if (confirmedSelection != -999)
                {
                    confirmed = true;
                    needResponse = false;
                }
                else
                {
                    if (listener_ref.primaryBut & hasntChosen || Input.GetKeyDown(KeyCode.Space))
                    {
                        confirmedSelection = currentSelection;
                        confirmed = true;
                        hasntChosen = false;
                        needResponse = false;
                    }
                }
            }
        }


        // after participant presses A to confirm choice, fade out this Canvas group over 1 sec, then save data
        if (confirmed)
        {
            fadeOutTimer += Time.deltaTime;

            this.GetComponent<CanvasGroup>().alpha = Mathf.Lerp(prevFadeVal, goalFadeVal, 0.2f);
            prevFadeVal = Mathf.Lerp(prevFadeVal, goalFadeVal, 0.2f);

            if (fadeOutTimer >= 1)
            {
                // save response for this trial in ExperimentManager arrays
                expManager_ref.GetComponent<ExperimentManager>().motSicRatings[trialNumb, ratingCounter] = confirmedSelection;

                this.GetComponent<CanvasGroup>().alpha = 0;
                confirmed = false;
                confirmedSelection = -999;
                hasntChosen = true;
                goalFadeVal = 1; // for when msGUI needs to fade in again
                fadeOutTimer = 0; // for next time needs to fade out again
                
                // increment ms response counter for next participant response
                ratingCounter++;

                // if raitingCounter adds up to the total number of msAsks needed in one trial, then start over at 0 for next trial
                if (ratingCounter == msRatingsAsks)
                {
                    ratingCounter = 0;
                    nextTimer = 0;
                    // change flags to progress through exp
                    expManager_ref.GetComponent<ExperimentManager>().needDetectPrefab = true;
                    trialIsOngoing = false;
                }
            }
        }


        // this is where motion sickness rating-related paramters are set for this script
        // it requires that ExperimentManager script changes needUpdateExpParameters to true when changes are confirmed (setup phase)
        if (needUpdateExpParameters)
        {
            trialNumb = expManager_ref.GetComponent<ExperimentManager>().currentTrial;
            msRatingsAsks = expManager_ref.GetComponent<ExperimentManager>().motSicAsks;
            if (expManager_ref.GetComponent<ExperimentManager>().motSicScale == 0)
            { msScaleTotal = 3; }
            else { msScaleTotal = 20; }

            needUpdateExpParameters = false;
        }
    }
}
