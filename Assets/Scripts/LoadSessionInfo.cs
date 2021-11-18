using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Data;
using UnityEngine.UI;

public class LoadSessionInfo : MonoBehaviour
{
    /// <summary>
    /// This script imports experiment parameter values from "session_settings.csv" for a specific participant's session
    /// Works by parsing each row as a string, where the first element is a "header name" and the following elements are values, separated by commas
    /// WARNING 1: Three rows will have different parameter values controlling each trial - these MUST all have the same length!!!
    /// E.g. if row with header "amps" has 10 vales, then "add" and "freq" must also have 10 values, one for each of the 10 trials
    /// WARNING 2: USE NOTEPAD, NOT EXCEL TO ADD / REMOVE / CHANGE VALUES. Excel changes the format of the file, which breaks this code.
    /// </summary>


    // path to the session info csv file in Unity project folder
    private string sessDataPath;

    // initialize variables to save the imported data into
    // use same names as in csv se3ttings file, so that you don't get them mixed up!
    public int partID;      // each participant has a unique integer identifier
    public int sessID;      // identify which session the participant is now doing
    public float eyeHgt;    // height in meters of participant's eyes above ground, measured beforehand
    public int osc;         // the type of viewpoint oscillation translation or rotation ito use this session for all trials:
                            // 0 = forward-backward; 1 = left-right; 2 = up-down; 3 = yaw; 4 = pitch; 5 = roll
    public int[] adds;      // 0 = no added translation of viewpoint through the virtual environment; 
                            // 1 = added translation is a constant forward movement of viewpoint through the corridors; 
                            // 2 = added translation depends on participant's controller thumbstickinput input
    public float[] amps;    // the values here are either meters or degrees of visual angle, depending on osc value:
                            // osc = 0-2 sets the amplitude as meters for translation; 3-5 sets degrees for rotation
    public float[] freqs;   // Hz, how many cycles of a viewpoint oscillation are completed in one second
    public float triDur;    // duration of each trial in seconds
    public int restDur;     // duration of maximum resting phase in seconds. Participants can press A to start next trial before this duration is exceeded.
    public int randOrd;     // 0 = no randomization of trial order; 1 = yes, do randomize
    public int yoked;       // 0 = no yoked design; 1 = yes, yoked (adjacent pairs of trials (nth and n+1th) need to be presented one after the other, but whichever is first is random)
    public int msScale;     // which motion sickness self-report scale to use: 0 = 4 point scale (Bagshaw & Scott 1985); 1 = 20 point scale (Keshavarz & Hecht 2011)
    public int msResps;     // number of times per trial the experiment will ask for a ms self-report rating via ms canvas
    public int debugVR;     // 0 = no debug console canvas showing in VR on right hand controller; 1 = yes, do show it


    // these two depend on the parameter values above, as imported from the settings file
    public int totalNumberOfTrials;
    public float estimatedMaxExpDur;
    public float estimatedMinExpDur;

    // references
    public GameObject experimentManager;
    public ExperimentManager expMngScrpt;
    // set these references in inspector
    public GameObject participantID_IF;
    public GameObject sessionID_IF;
    public GameObject eyeHeight_IF;
    public GameObject[] viewpointOscillationButtons;
    public GameObject addedTranslation_IF;
    public GameObject amplitudes_IF;
    public GameObject frequencies_IF;
    public GameObject trialDuration_IF;
    public GameObject restDuration_IF;
    public GameObject[] randomOrderButtons;
    public GameObject[] yokedButtons;
    public GameObject[] msScaleButtons;
    public GameObject msNumberOfResponses_IF;
    public GameObject[] debugButtons;
    public GameObject totalTrialsValue;
    public GameObject estimatedMinutesValueMin;
    public GameObject estimatedMinutesValueMax;


    void Awake()
    {
        // initialize references
        experimentManager = GameObject.Find("Experiment Manager");
        expMngScrpt = experimentManager.GetComponent<ExperimentManager>();

        // set path where session data will be imported from (build/...Data/SessionSettingsCSV)
        sessDataPath = Application.dataPath + "/SessionSettingsCSV";

        // load session data to ExperimentManager script, and visualize the data on task settings GUI in game.
        // this function is called on 1st frame, 
        // but also can be called again by ReloadDefaultData, a function in ParametersButtons script.
        LoadSessionData();

        // update and displays the total # of trials and estimated duration of experiment from these settings
        // This is a seperate function so you can manually change paramter values using the GUI 
        // to see how that affects these 2 things (e.g. when pilot testing or bug testing)
        UpdateTotalTrialsAndExpDur();
    }


    public void LoadSessionData()
    {
        // load the matrix of data in the csv as one text object
        // NOTE: file must be located in build/...Data/SessionSettingsCSV and have name: "session_settings.csv"
        string text = File.ReadAllText(sessDataPath + @"\session_settings.csv");

        // split data matrix into rows
        string[] data = text.Split(new char[] { '\n' });

        // split and save data from each row
        for (int i = 0; i < data.Length - 1; i++)
        {
            string[] row = data[i].Split(new char[] { ',' });

            // for all rows, the 1st element is the "header" so dont use 0 index as data; use index 1
            // some rows give parameter values that are single int values - simply get 1st indx
            // but other rows contain arrays of varying length, so loop through all non-header elements
            if (i == 0)
                partID = int.Parse(row[1]);
            else if (i == 1)
                sessID = int.Parse(row[1]);
            else if (i == 2)
                eyeHgt = float.Parse(row[1]);
            else if (i == 3)
                osc = int.Parse(row[1]);
            else if (i == 4)
            {
                // initialize parameter array to be correct size (ignore header! subtract 1)
                adds = new int[row.Length - 1];

                // loop to fill
                for (int j = 1; j < row.Length; j++)
                {
                    // j - 1 to skip header
                    adds[j - 1] = int.Parse(row[j]);
                }
            }
            else if (i == 5)
            {
                amps = new float[row.Length - 1];
                for (int j = 1; j < row.Length; j++)
                { amps[j - 1] = float.Parse(row[j]); }
            }
            else if (i == 6)
            {
                freqs = new float[row.Length - 1];
                for (int j = 1; j < row.Length; j++)
                { freqs[j - 1] = float.Parse(row[j]); }
            }
            else if (i == 7)
                triDur = float.Parse(row[1]);
            else if (i == 8)
                restDur = int.Parse(row[1]);
            else if (i == 9)
                randOrd = int.Parse(row[1]);
            else if (i == 10)
                yoked = int.Parse(row[1]);
            else if (i == 11)
                msScale = int.Parse(row[1]);
            else if (i == 12)
                msResps = int.Parse(row[1]);
            else if (i == 13)
                debugVR = int.Parse(row[1]);
            else if (i == 14)
                debugVR = int.Parse(row[1]);
        }

        // Finally, now that you extracted the data, send it to:
        // 1) experiment manager script so the experiment runs as desired
        expMngScrpt.participantID = partID;
        expMngScrpt.sessionID = sessID;
        expMngScrpt.eyeHeight = eyeHgt;
        expMngScrpt.viewpointOscillation = osc;
        expMngScrpt.addedTranslations = adds;
        expMngScrpt.amplitudes = amps;
        expMngScrpt.frequencies = freqs;
        expMngScrpt.trialDuration = triDur;
        expMngScrpt.restDuration = restDur;
        expMngScrpt.randomTrialOrder = randOrd;
        expMngScrpt.yokedTrials = yoked;
        expMngScrpt.motSicScale = msScale;
        expMngScrpt.motSicAsks = msResps;
        if (debugVR == 1)
        { expMngScrpt.debugMode = true; }
        else { expMngScrpt.debugMode = false; }

        // 2) experiment settings GUI, so experimenter can see & double-check what settings have been loaded
        participantID_IF.GetComponent<InputField>().text = partID.ToString();
        sessionID_IF.GetComponent<InputField>().text = sessID.ToString();
        eyeHeight_IF.GetComponent<InputField>().text = eyeHgt.ToString();
        viewpointOscillationButtons[osc].GetComponent<Image>().color = Color.green;
        addedTranslation_IF.GetComponent<InputField>().text = String.Join(",", adds);
        amplitudes_IF.GetComponent<InputField>().text = String.Join(",", amps);
        frequencies_IF.GetComponent<InputField>().text = String.Join(",", freqs);
        trialDuration_IF.GetComponent<InputField>().text = triDur.ToString();
        restDuration_IF.GetComponent<InputField>().text = restDur.ToString();
        randomOrderButtons[randOrd].GetComponent<Image>().color = Color.green;
        yokedButtons[yoked].GetComponent<Image>().color = Color.green;
        msScaleButtons[msScale].GetComponent<Image>().color = Color.green;
        msNumberOfResponses_IF.GetComponent<InputField>().text = msResps.ToString();
        debugButtons[debugVR].GetComponent<Image>().color = Color.green;
    }


    public void UpdateTotalTrialsAndExpDur()
    {
        // get total number of trials
        totalNumberOfTrials = expMngScrpt.amplitudes.Length;
        expMngScrpt.totalTrialsTextSett.text = totalNumberOfTrials.ToString();
        // get estimated max experiment duration
        estimatedMinExpDur = Mathf.Round(totalNumberOfTrials * expMngScrpt.trialDuration / 60);
        estimatedMaxExpDur = Mathf.Round(totalNumberOfTrials * (expMngScrpt.trialDuration + restDur) / 60);
        //show on GUI
        totalTrialsValue.GetComponent<Text>().text = totalNumberOfTrials.ToString();
        estimatedMinutesValueMin.GetComponent<Text>().text = estimatedMinExpDur.ToString();
        estimatedMinutesValueMax.GetComponent<Text>().text = estimatedMaxExpDur.ToString();
    }
}
