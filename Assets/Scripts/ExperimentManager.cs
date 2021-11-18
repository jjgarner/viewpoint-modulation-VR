using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Text;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;

public class ExperimentManager : MonoBehaviour
{
    // This script manages a general-purpose Visually Evoked Postural Response (VEPR) experiment design.
    // Lab Streaming Layer (LSL) scripts allow syncronization of time series data recorded by Unity (e.g., HMD pos or rot) with data from other sources (e.g., forceplate)
    // Default experiment parameter settings are imported from a csv into this script, but a GUI on screen allows for changes, for easy pilot testing, debugging, etc.


    ////////////////////////
    //   EXP PARAMETERS   //
    ////////////////////////
    public int participantID;           // each participant has a unique integer identifier
    public int sessionID;               // identify which session the participant is now doing
    public float eyeHeight;             // height in meters of participant's eyes above ground, measured beforehand
    public float fixedTimestep = 0.01f; // edit -> project settings -> time -> fixed timestep value determines how frequently FixedUpdate runs. 
                                        // default is 50 but we choose 100 calls per second to get a higher sampling rate and beter temporal control of stimulus
    public int callsPerTrial;           // number of FixedUpdate calls within a trial of some duration, see sinusoidArray
    public int viewpointOscillation;    // the type of viewpoint oscillation translation or rotation ito use this session for all trials:
                                        // 0 = forward-backward; 1 = left-right; 2 = up-down; 3 = yaw; 4 = pitch; 5 = roll
    public int[] addedTranslations;     // 0 = no added translation of viewpoint through the virtual environment;
                                        // 1 = added translation is a constant forward movement of viewpoint through the corridors
    public float[] amplitudes;          // the amplitudes of oscillations of each trial, either in meters or degrees visual angle
    public float[] frequencies;         // in Hz, how many cycles of a viewpoint oscillation are completed in one second per trial
    public float[] sinusoidArray;       // array of sampled sine wave values, applied each fixed update call during trials. Values depend on amp, freq, and timestep above
    public float trialDuration;         // duration of trial in seconds - depends on sceneDuration, fadeDuration, and betweenDuration
    public int randomTrialOrder;        // 0 = no randomization of trial order; 1 = yes, do randomize
    public int yokedTrials;             // 0 = no yoked design; 1 = yes, yoked (every odd n-th and odd n+1-th trial need to be presented one after the other, but whichever is first is random)
    public int motSicScale;             // which motion sickness self-report scale to use: 0 = 4 point scale (Bagshaw & Scott 1985); 1 = 21 point scale (Keshavarz & Hecht 2011)
    public int motSicAsks;              // number of times per trial the experiment will elicit a ms self-report rating via ms canvas
    public int restDuration;            // duration of maximum resting phase in seconds. Participants can press A to start next trial before this duration is exceeded.
    public int[] randomizedIndices;     // this array will contain randomized trial indices, depending on randomTrialOrder & yokedTrials
    public int[,] motSicRatings;        // to be filled with motion sickness self-report ratings each trial: xth trial, yth response
    public int[] detectionResponse;     // to be filled with self-motion detection judgements each trial; 0 = no motion detected; 1 = yes, motion was detected
    public int[] vectionRating;         // to be filled with vection self-report ratings at end of trial

    // Bools
    public bool debugMode = false;          // make this true to see the debug window in VR (right hand controller), not just on computer screen
    private bool setupPhase = true;         // starts here, before experimenter is ready (e.g. still setting up recording data, etc.)
    public bool experimenterReady = false;  // don't even give instructions yet until experimenter is ready
    public bool instructionsPhase = false;  // once experimenter is ready, show instructions to participant
    public bool experimentPhase = false;    // move through phases of trials when true
    public bool endOfExperiment = false;    // data has been saved for all trials - nothing else happens anymore
    public bool restingPhase = false;       // the phase after self report input and before new trial of VEPR stimuli
    private bool stimulusPhase = false;     // the phase of long duration exposure to VEPR stimuli
    private bool responsePhase = false;     // the phase after viewpoint modulation, while participant gives self reports for ms & stim detection & vection, b4 resting phase
    public bool needMSResponse = false;     // only true while waiting for participant motion sickness rating response
    public bool needDetcResponse = false;   // only true while waiting for participant motion sickness rating response
    public bool needVectResponse = false;   // only true while waiting for participant vection rating response
    public bool needFloatScreen = true;     // used to instantiate float down screen just once during responsePhase
    public bool needDetectPrefab = false;   // used to instantiate detection response canvas just once during responsePhase
    public bool needVectPrefab = false;     // used to instantiate vection response canvas just once during responsePhase
    private bool isTransitioning = false;   // transition between phases using this and a timer (only change variable values while screen is completely black)
    public bool hasSavedIncomplete = false; // if participant needs to quit before finishing, you can at least save the incomplete data set

    // Timers and counters
    public int fuCounter = 0;           // counts how many calls to FixedUpdate have been completed each trial, to index correct values of sinusiodArray during each trial
    public float fadeDuration = 0.5f;   // how long it takes for the screen to fade out to black, or fade in from black
    private float holdAButtonTimer;     // to prevent false starts, make participant hold button down for 1 sec
    private float waitTimer;            // used for delayed reaction events (e.g., waiting for fade-to-black to complete, etc.)
    public float trialTimer;            // tracks time elapsed during stimulus phase each trial
    private float responseTimer;        // tracks time elapsed during responsePhase each trial
    public float restTimer;             // tracks time elapsed during each rest period, between trials
    private float countdownTimer;       // used to update countdownVal. After each second in restingPhase, reset to 0 and decrement countdownVal
    public int countdownVal;            // used as input to countdownText shown on restingCanv. Value is decremented once every second
    public int currentTrial;            // index of the current trial
    public int nCompletedTrials;        // used when saving incomplete data, also shows as feedback during restingPhase
    private int totalNumberOfTrials;    // total number of trials used in experiment equals length of these arrays: addedTranslations, amplitudes, frequencies, randomizedIndices
                                        // which should all be arrays that have the same number of elements in them

    // References
    public GameObject vrRig;            // participant's vr rig (move the whole rig to move the participant's view through the environment)
    public GameObject vrCamera;         // vr camera gameobject that comes with the vr rig - needed for controlling viewpoint rotations
    public GameObject settingsCanv;     // the GUI the experimenter uses to adjust experiment parameter values
    public GameObject waitingCanv;      // Just shows "waiting for experimenter..." to participant while experimenter is doing setup
    public GameObject instrctCanv;      // displays the experiment instructions, including how to begin the first trial
    public GameObject msCanv;           // child of VR camera, semi-transparent canvas showing participant's chosen motion sickness rating, handled by it's MSUIManager script
    public GameObject detectCanv;       // is intantiated at end of trial - a floating canvas showing participant's chosen self-motion detection judgement, handled by it's DetectUIManager script
    public GameObject vectCanv;         // is intantiated at end of trial - a floating canvas showing participant's chosen vection rating, handled by it's VectUIManager script
    public GameObject restingCanv;      // tell participant that a trial just ended, how many trials remain, and tells them to rest until ready (hold A to start next trial)
    public GameObject endExpCanv;       // tell participant that the experiment is over, take off HMD now
    public GameObject debugInVRCanv;    // show a debug console connected to the right hand controller, if debugMode == true
    public GameObject floatScreen;      // a screen that floats down through the ceiling when a trial ends, so detection and vection canvases appear to be rendered on this
    public Text completedTrialsText;    // number of trials completed so far as text to display on the resting canvas
    public Text totalTrialsTextSett;    // number of trials total as text to display on the settings canvas
    public Text totalTrialsTextRest;    // number of trials total as text to display on the resting canvas
    public Text countdownText;          // number of seconds remaining for resting phase, shown on resting canvas
    public GameObject blackOutSquare;   // adjust this object's opacity to fade in or out: using IEnumerator FadeBlackOutSquare(fadeDuration, true or false)
    public int currentMSRating;         // an int value handled by MSUIManager script
    public ControllerListener listener; // access this script of vrRig_ref to detect joystick or button press

    //// Saving data
    private StringBuilder csvBuilder = new StringBuilder(); // builds a csv of exp data used in analysis
    private string savePath; // where the csv is saved to disk
    private string savePathIncomplete; // where the csv is saved to disk

    // Event Codes / Markers for LSL using TimeSeriesOutlet.cs
    public float markerTrialOnset = 0; // Unity.data channel 11: indicates TRIAL ONSET with a 1 (all other times, 0)
    public float markerRestOnset = 0; // Unity.data channel 12: RESTING PHASE (right after trial ends, before new trial begins) with a 1, all other times 0


    void Awake()
    {
        // initialize references to scripts on game objects
        listener = vrRig.GetComponent<ControllerListener>();

        // participant's HMD should start fully black
        Color objectColor = blackOutSquare.GetComponent<Image>().color;
        objectColor = new Color(objectColor.r, objectColor.g, objectColor.b, 0);

        // start fade in from black
        StartCoroutine(FadeBlackOutSquare(fadeDuration, false));
    }


    // can press esc key !!!!ONLY ONCE!!!! to save incomplete data if participant needs to quit early, a glitch occurs, etc.
    void Update()
    {
        if (hasSavedIncomplete == false)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SaveDataIncomplete();
                hasSavedIncomplete = true;
            }
        }
    }


    ///////////////////////
    // EXP STATE MACHINE //
    ///////////////////////
    void FixedUpdate()
    {
        if (setupPhase)
        {
            // experimenter clicks on the GUI's confirm button to move from setup phase to instructions phase 
            if (experimenterReady)
            {
                // start transitioning and fade out
                isTransitioning = true;
                StartCoroutine(FadeBlackOutSquare(fadeDuration));

                // use isTransitioning bool to update things between phases just once, during fade-out then fade-in transition: the frame when fully blacked out
                if (isTransitioning)
                {
                    waitTimer += Time.deltaTime;

                    // wait until fade to black is complete, then complete setup during one frame
                    if (waitTimer > 2 * fadeDuration)
                    {
                        // use chosen variable parameters (imported or modified) to build actual experiment trials
                        BuildTrials();

                        // change canvases
                        settingsCanv.SetActive(false);
                        waitingCanv.SetActive(false);
                        instrctCanv.SetActive(true);

                        // remove debug canvas from right-hand controller if debugMode == false
                        if (debugMode == false)
                        { debugInVRCanv.SetActive(false); }

                        // change vrRig_ref's y position to match eyeHeight
                        vrRig.transform.position = new Vector3(0, eyeHeight, 0);

                        // update the variables that depend on exp parameters in the motion sickness ratings GUI script
                        msCanv.GetComponent<MSUIManager>().needUpdateExpParameters = true;

                        // start fade in
                        StartCoroutine(FadeBlackOutSquare(fadeDuration, false));

                        // reset fade transition timer for next use
                        waitTimer = 0;

                        // change bool flags to start instructionsPhase
                        setupPhase = false;
                        instructionsPhase = true;
                    }
                }
            }
        }
        else if (instructionsPhase)
        {
            // wait for fade back in from black to complete, and set-up trial 1
            if (isTransitioning)
            {
                waitTimer += Time.deltaTime;
                if (waitTimer > 2 * fadeDuration)
                {
                    // set up trial 1's oscillation values in sinusoidArray
                    // If you want position = sin(x), but can only change values over time, apply the derivative: cos(x)
                    for (int i = 0; i < callsPerTrial; i++)
                    {
                        // amp*cos(freq*time)*freq (don't forget the chain rule!)
                        sinusoidArray[i] = amplitudes[randomizedIndices[currentTrial]] * Mathf.Cos(frequencies[randomizedIndices[currentTrial]] * i * fixedTimestep * 2 * Mathf.PI) / (5.06f * Mathf.PI) * frequencies[randomizedIndices[currentTrial]];
                    }

                    // transition is complete
                    isTransitioning = false;
                    // reset fade transition timer for next use
                    waitTimer = 0;
                }
            }
            else
            {
                // participant must press and hold "A" button (primary button) to start experimentPhase (first trial)
                if (listener.primaryBut)
                {
                    holdAButtonTimer += Time.deltaTime;
                }
                else { holdAButtonTimer = 0; }

                if (holdAButtonTimer > 1 || Input.GetKeyDown(KeyCode.Space))
                {
                    // start fade to black and transition into pre-stimulus (isTransitioning) stage of experiment phase
                    StartCoroutine(FadeBlackOutSquare(fadeDuration));
                    instructionsPhase = false;
                    experimentPhase = true;
                    isTransitioning = true;
                    holdAButtonTimer = 0; // reset for next use
                }
            }
        }
        else if (experimentPhase)
        {
            // if restingPhase == true, participant has finished a trial and is resting for up to restDuration
            if (restingPhase)
            {
                // countdown timer, value, and text runs whether transitioning or not
                countdownTimer += Time.deltaTime;
                if (countdownTimer > 1)
                {
                    countdownTimer = 0;
                    countdownVal--;
                    countdownText.text = countdownVal.ToString();

                    // if restDuration has been exceeded, need to start next trial
                    if (countdownVal <= 0)
                    {
                        // start fade to black and transition into stimulus stage of trial
                        StartCoroutine(FadeBlackOutSquare(fadeDuration));
                        restingPhase = false;
                        isTransitioning = true;
                        restTimer = 0;
                        holdAButtonTimer = 0; // reset for next use
                    }
                }

                // use isTransitioning bool to update things between phases just once during fade-out then fade-in transition: the frame when fully blacked out
                if (isTransitioning)
                {
                    waitTimer += Time.deltaTime;
                    // wait until fade to black is complete
                    if (waitTimer > 2 * fadeDuration)
                    {
                        // mark beginning of resting phase for LSL
                        markerRestOnset = 1;

                        // teleport participant to original location, in front of resting canvas screen
                        vrRig.transform.position = new Vector3(0, eyeHeight, 0);
                        // update the resting canvas current and total trial counts
                        completedTrialsText.text = nCompletedTrials.ToString();
                        totalTrialsTextRest.text = totalNumberOfTrials.ToString();

                        // while finishing transition into rest, reset next trial's oscillation values in sinusoidArray
                        // If you want position = sin(x), but can only change values over time, apply the derivative: cos(x)
                        for (int i = 0; i < callsPerTrial; i++)
                        {
                            // amp*cos(freq * time)*freq (don't forget chain rule!)
                            sinusoidArray[i] = amplitudes[randomizedIndices[currentTrial]] * Mathf.Cos(frequencies[randomizedIndices[currentTrial]] * i * fixedTimestep * 2 * Mathf.PI) / (5.06f * Mathf.PI) * frequencies[randomizedIndices[currentTrial]];
                        }

                        // reset fade transition timer for next use
                        waitTimer = 0;
                        // start fade in
                        StartCoroutine(FadeBlackOutSquare(fadeDuration, false));
                        // transition is complete
                        isTransitioning = false;
                    }
                }
                else
                {
                    // rest phase did not just start
                    markerRestOnset = 0;

                    // increment timer
                    restTimer += Time.deltaTime;

                    // participant must press and hold "A" button (primary button) to start next trial
                    if (listener.primaryBut)
                    {
                        holdAButtonTimer += Time.deltaTime;
                    }
                    else { holdAButtonTimer = 0; }

                    // fade to black and wait 1 sec, then next trial will start
                    if (holdAButtonTimer > 1 || Input.GetKeyDown(KeyCode.Space))
                    {
                        // start fade to black and transition into stimulus stage of trial
                        StartCoroutine(FadeBlackOutSquare(fadeDuration));
                        restingPhase = false;
                        isTransitioning = true;
                        restTimer = 0;
                        holdAButtonTimer = 0; // reset for next use
                    }
                }
            }

            // else, the trial just started (isTransitioning)
            // or, the trial has been in progress (stimulusPhase or responsePhase)
            else
            {
                // use isTransitioning bool to update things between phases just once during fade-out then fade-in transition: the frame when fully blacked out
                if (isTransitioning)
                {
                    waitTimer += Time.deltaTime;

                    // wait until fade to black is complete
                    if (waitTimer > 2 * fadeDuration)
                    {
                        // replace instructions canvas with resting canvas
                        instrctCanv.SetActive(false);
                        restingCanv.SetActive(true);
                        // reset countdown variables for next rest
                        countdownTimer = 0;
                        countdownVal = restDuration;

                        // teleport participant to trial's start location - 16 meters into the corridor (each corridor is 16m long)
                        vrRig.transform.position = new Vector3(0, eyeHeight, 30);
                        // start fade in
                        StartCoroutine(FadeBlackOutSquare(fadeDuration, false));
                        
                        // start stimulus phase
                        isTransitioning = false;
                        stimulusPhase = true;
                        // mark this frame as the start of the trial
                        markerTrialOnset = 1;
                        // tell the MSUIManager script that a trial has begun
                        msCanv.GetComponent<MSUIManager>().trialIsOngoing = true;
                        // reset fade transition timer for next use
                        waitTimer = 0;
                    }
                }

                else if (stimulusPhase)
                {
                    // any frame after the transition is not the beginning of the trial's stimulus phase
                    markerTrialOnset = 0;

                    // depending on experimental conditions, apply the viewpoint modulation stimulus over time, once per FixedUpdate call
                    if (trialTimer <= trialDuration)
                    {
                        // apply oscilalting viewpoint modulation in 1 of 6 translations/rotations, depending on session's viewpointOscillation value
                        if (viewpointOscillation == 0)
                            // forward-backward z axis translational oscillation
                            vrRig.transform.Translate(Vector3.forward * sinusoidArray[fuCounter], Space.World);
                        else if (viewpointOscillation == 1)
                            // left-right x axis translational oscillation
                            vrRig.transform.Translate(Vector3.right * sinusoidArray[fuCounter], Space.World);
                        else if (viewpointOscillation == 2)
                            // up-down y axis translational oscillation
                            vrRig.transform.Translate(Vector3.up * sinusoidArray[fuCounter], Space.World);
                        else if (viewpointOscillation == 3)
                            // yaw (rotation around up-down y axis) oscillation
                            vrRig.transform.RotateAround(new Vector3(vrCamera.transform.position.x, vrCamera.transform.position.y, vrCamera.transform.position.z), 
                                Vector3.up, sinusoidArray[fuCounter]);
                        else if (viewpointOscillation == 4)
                            // pitch (rotation around left-right x axis) oscillation
                            vrRig.transform.RotateAround(new Vector3(vrCamera.transform.position.x, vrCamera.transform.position.y, vrCamera.transform.position.z),
                                Vector3.right, sinusoidArray[fuCounter]);
                        else if (viewpointOscillation == 5)
                            // roll (rotation around forward-backward z axis) oscillation
                            vrRig.transform.RotateAround(new Vector3(vrCamera.transform.position.x, vrCamera.transform.position.y, vrCamera.transform.position.z),
                                Vector3.forward, sinusoidArray[fuCounter]);

                        // also, if this trial has addedTranslations = 1, 
                        // then translate the viewpoint (vrRig) forward at avg. walking speed as the same time as oscillations
                        if (addedTranslations[randomizedIndices[currentTrial]] == 1)
                            vrRig.transform.Translate(Vector3.forward * Time.deltaTime * 1.4f, Space.World);

                        // increment timers after their value is used this call
                        trialTimer += Time.deltaTime;
                        fuCounter++;
                    }

                    // dont fade-to-black immeidately after stimulus is done - go into responsePhase
                    if (trialTimer >= trialDuration)
                    {
                        stimulusPhase = false;
                        responsePhase = true;
                        needMSResponse = true;
                        needDetcResponse = true;
                        needVectResponse = true;
                        trialTimer = 0;
                    }
                }
                // after VEPR stimulus ends, wait to get final ms response, then detection response, then vection response choices
                // only after all 3 are finished, start transition to post-trail rest phase
                if (responsePhase)
                {
                    if (needFloatScreen)
                    {
                        Instantiate(floatScreen, vrRig.transform.position + new Vector3(0, 8, 4.6f), Quaternion.identity);
                        needFloatScreen = false;
                    }

                    if (needMSResponse)
                    {
                        // wait for MSUIManager to make needDetectPrefab = true when done w/ ms rating
                        if (needDetectPrefab)
                        {
                            Instantiate(detectCanv, vrRig.transform.position + new Vector3(0, 2-eyeHeight, 4.6f), Quaternion.identity);
                            needDetectPrefab = false;
                            needMSResponse = false;
                        }
                    }
                    else if (needDetcResponse)
                    {
                        // wait for DetectUIManager needVectPrefab = true when done w/ Detection
                        if (needVectPrefab)
                        {
                            Instantiate(vectCanv, vrRig.transform.position + new Vector3(0, 2-eyeHeight, 4.6f), Quaternion.identity);
                            needVectPrefab = false;
                            needDetcResponse = false;
                        }
                    }
                    else if (needVectResponse)
                    {
                        // just wait for VectUIManager to change this last bool
                    }

                    if (!needMSResponse & !needDetcResponse & !needVectResponse)
                    {
                        // if this trial was the final trial, then the experiment is now complete
                        // proceed to end of experiment screen and save data
                        if (currentTrial + 1 == totalNumberOfTrials)
                        {
                            SaveData();
                            StartCoroutine(FadeBlackOutSquare(fadeDuration));
                            experimentPhase = false;
                            endOfExperiment = true;
                            isTransitioning = true;
                        }
                        else
                        {
                            // delete floating screen, update values for next trial, start fade-to-black transition into restingPhase
                            GameObject go = GameObject.Find("Floating Screen(Clone)");
                            if (go)
                            { Destroy(go.gameObject); }
                            fuCounter = 0;
                            responseTimer = 0;
                            currentTrial += 1;
                            nCompletedTrials += 1;
                            responsePhase = false;
                            restingPhase = true;
                            isTransitioning = true;
                            StartCoroutine(FadeBlackOutSquare(fadeDuration));
                        }
                    }
                }
            }
        }
        else if (endOfExperiment)
        {
            // use isTransitioning bool to update things between phases just once during fade-out then fade-in transition: the frame when fully blacked out
            if (isTransitioning)
            {
                waitTimer += Time.deltaTime;

                // wait until fade to black is complete
                if (waitTimer > 2 * fadeDuration)
                {
                    // get rid of resting canvas, show experiment end canvas
                    restingCanv.SetActive(false);
                    endExpCanv.SetActive(true);
                    // teleport participant to original location, in front of experiment end canvas screen
                    vrRig.transform.position = new Vector3(0, eyeHeight, 0);
                    // start fade in
                    StartCoroutine(FadeBlackOutSquare(fadeDuration, false));
                    // transition is complete
                    isTransitioning = false;
                }
            }

            // then do nothing - the experiment is complete.

        }
    }


    ////////////////////
    // MISC FUNCTIONS //
    ////////////////////
    void BuildTrials()
    {
        // Set the save path for the csv output file in build/..._Data/, and make file name using participant id and session id
        savePath = string.Format("{0}/SessionOutputCSV/ssvepr3_p{1}_s{2}.csv", Application.dataPath, participantID, sessionID);
        savePathIncomplete = string.Format("{0}/SessionOutputCSV/ssvepr3_incomplete_p{1}_s{2}.csv", Application.dataPath, participantID, sessionID);

        // set total number of trials
        totalNumberOfTrials = amplitudes.Length;

        // Set number of FixedUpdate calls per trial to ensure that oscillating movement is fully controlled and consistent on all trials
        float temp = (1 / fixedTimestep * trialDuration) + 1 / fixedTimestep;
        // NOTE: this adds a buffer of 1 second to the length of the array, because sometimes the trial duration timer is erroneously slo
        // which makes the fixedUpdateCounter attempt to index out of sinusoidArray's range, which stops the whole script.  This buffer of extra values prevents this.
        callsPerTrial = (int)temp;
        
        // initialize arrays with correct size
        sinusoidArray = new float[callsPerTrial];

        // set 3 participant response arrays
        motSicRatings = new int[totalNumberOfTrials, motSicAsks];
        detectionResponse = new int[totalNumberOfTrials];
        vectionRating = new int[totalNumberOfTrials];

        // randomizedIndices can be used to randomize trial order by indexing the currentTrial-th elements of all parameter value arrays
        // e.g., amplitudes[randomizedIndices[currentTrial]] and frequencies[randomizedIndices[currentTrial]] would index matching amp and freq values
        // if randomTrialOrder == 0, then these indices are just 0:totalNumberOfTrials
        randomizedIndices = new int[totalNumberOfTrials];
        for (int i = 0; i < totalNumberOfTrials; i++)
        {
            randomizedIndices[i] = i;
        }

        // randomize trial order if desired
        if (randomTrialOrder == 1)
        {
            // if yoked design... randomize the order of paired trials, and also randomize which trial of each pair is presented 1st and which is 2nd
            if (yokedTrials == 1)
            {
                int nPairs = (int) Mathf.Floor(totalNumberOfTrials / 2);

                // randomize the order of paired trials
                int[] pairsArray = new int[nPairs];
                for (int i = 0; i < nPairs; i++)
                {
                    pairsArray[i] = i+1;
                }
                Reshuffle(pairsArray);

                // randomize which trial of each pair is presented 1st and which is 2nd
                int[] orderArray = new int[nPairs];
                for (int i = 0; i < nPairs; i++)
                {
                    // ensure that half of the pairs start with the original odd indexed trial and half with even
                    // by testing if index i divided by 2 has a remainder of 0 or not
                    // yes, this is not perfectly 50/50 balanced when nPairs is odd, but it is as close as possible
                    if (i % 2 == 0)
                    { orderArray[i] = 1; }
                    else
                    { orderArray[i] = 2; }
                }
                Reshuffle(orderArray);

                // using those pairsArray & orderArray, loop to fill randomTrialOrder correctly
                for (int i = 0; i < nPairs; i++)
                {
                    randomizedIndices[i*2] = pairsArray[i]*2 - 1;
                    randomizedIndices[i*2 + 1] = pairsArray[i]*2 - 1;
                }

                for (int i = 0; i < nPairs; i++)
                {
                    if (orderArray[i] == 1)
                    {
                        randomizedIndices[i * 2] -= 1;
                    }
                    else
                    {
                        randomizedIndices[i * 2 + 1] -= 1;
                    }
                }
            }
            // if not yoked... randomize everything
            else
            { Reshuffle(randomizedIndices); }
        }
    }

    void SaveData()
    {
        // change this as needed, if you edit the exp to have more or fewer measurements
        string headers = string.Format("partID,sessID,eyeheight,OscTyp,trial,addedtrans,amp,freq,trialdur,msscale,msratings,detect,vection");

        // add this line of header info to top of csv to be built
        csvBuilder.AppendLine(headers);

        // collapse the variable number of motion sickness ratings elicited during each trial, into one string of characters per trial, to fit that into a single cell
        // e.g. if 4 responses are 1, 1, 2, 3... then you'd get "1123"
        string[] msRatings =  new string[totalNumberOfTrials];
        int[] temp = new int[motSicAsks];

        for (int i = 0; i < totalNumberOfTrials; i++)
        {
            for (int j = 0; j < motSicAsks; j++)
            {
                temp[j] = motSicRatings[i, j];
            }
            msRatings[i] = string.Join("", temp);
        }

        // loop to fill each row with data for that trial
        for (int i = 0; i < totalNumberOfTrials; i++)
        {
            // change this as needed, if you edit the exp to have more or fewer measurements 
            string newLine = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12}",
              participantID, sessionID, eyeHeight, viewpointOscillation, i + 1, addedTranslations[randomizedIndices[i]], 
              amplitudes[randomizedIndices[i]], frequencies[randomizedIndices[i]], trialDuration, motSicScale, msRatings[randomizedIndices[i]],
              detectionResponse[randomizedIndices[i]], vectionRating[randomizedIndices[i]]);
            // add one line at a time
            csvBuilder.AppendLine(newLine);
        }

        // Make array into string
        File.WriteAllText(savePath, csvBuilder.ToString());
    }

    void SaveDataIncomplete()
    {
        // change this as needed, if you edit the exp to have more or fewer measurements
        string headers = string.Format("partID,sessID,eyeheight,OscTyp,trial,addedtrans,amp,freq,trialdur,msscale,msratings,detect,vection");

        // add this line of header info to top of csv to be built
        csvBuilder.AppendLine(headers);

        // collapse the variable number of motion sickness ratings elicited during each trial, into one string of characters per trial, to fit that into a single cell
        // e.g. if 4 responses are 1, 1, 2, 3... then you'd get "1123"
        string[] msRatings = new string[totalNumberOfTrials];
        int[] temp = new int[motSicAsks];

        for (int i = 0; i < totalNumberOfTrials; i++)
        {
            for (int j = 0; j < motSicAsks; j++)
            {
                temp[j] = motSicRatings[i, j];
            }
            msRatings[i] = string.Join("", temp);
        }

        // loop to fill each row with data for that trial
        for (int i = 0; i < totalNumberOfTrials; i++)
        {
            // change this as needed, if you edit the exp to have more or fewer measurements 
            string newLine = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12}",
              participantID, sessionID, eyeHeight, viewpointOscillation, i + 1, addedTranslations[randomizedIndices[i]],
              amplitudes[randomizedIndices[i]], frequencies[randomizedIndices[i]], trialDuration, motSicScale, msRatings[randomizedIndices[i]],
              detectionResponse[randomizedIndices[i]], vectionRating[randomizedIndices[i]]);
            // add one line at a time
            csvBuilder.AppendLine(newLine);
        }

        // Make array into string
        File.WriteAllText(savePathIncomplete, csvBuilder.ToString());
    }

    public IEnumerator FadeBlackOutSquare(float fadeDuration, bool fadeToBlack = true)
    {
        // https://turbofuture.com/graphic-design-video/How-to-Fade-to-Black-in-Unity

        Color objectColor = blackOutSquare.GetComponent<Image>().color;
        float fadeAmount;
        float fadeScale = 1 / fadeDuration;

        if (fadeToBlack)
        {
            while (blackOutSquare.GetComponent<Image>().color.a < 1)
            {
                // because Project Settings -> Time -> Fixed Timestep = 0.01 sec (100fps); 
                // to get a 0.5 seconds-long fade-out effect, make black square image alpha go from 0 to 1 at a rate of 0.02alpha per 0.01seconds
                fadeAmount = objectColor.a + (fadeScale * Time.deltaTime);

                objectColor = new Color(objectColor.r, objectColor.g, objectColor.b, fadeAmount);
                blackOutSquare.GetComponent<Image>().color = objectColor;
                yield return new WaitForEndOfFrame();
            }
        }
        // or fade from black to clear over .5 seconds
        else
        {
            while (blackOutSquare.GetComponent<Image>().color.a > 0)
            {
                fadeAmount = objectColor.a - (fadeScale * Time.deltaTime);

                objectColor = new Color(objectColor.r, objectColor.g, objectColor.b, fadeAmount);
                blackOutSquare.GetComponent<Image>().color = objectColor;
                yield return new WaitForEndOfFrame();
            }
        }
    }

    void Reshuffle(int[] a)
    {
        // Knuth shuffle algorithm
        for (int t = 0; t < a.Length; t++)
        {
            int tmp = a[t];
            int r = Random.Range(t, a.Length);
            a[t] = a[r];
            a[r] = tmp;
        }
    }
}