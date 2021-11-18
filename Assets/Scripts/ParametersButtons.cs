using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ParametersButtons : MonoBehaviour
{
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
    public GameObject estimatedMinutesValue;
    public GameObject confirmButton;

    // these are initialized on awake
    public GameObject expMngObject_ref;
    public ExperimentManager expMngScrpt_ref;
    public GameObject sessLoadObject_ref;
    public LoadSessionInfo sessLoadScript_ref;

    // for updating exp parameters through buttons
    public int newOscValue;
    public int newRandValue;
    public int newYokedValue;
    public int newMSScaleValue;
    public bool newDebugValue;


    void Awake()
    {
        // initialize references
        expMngObject_ref = GameObject.Find("Experiment Manager");
        expMngScrpt_ref = expMngObject_ref.GetComponent<ExperimentManager>();
        sessLoadObject_ref = GameObject.Find("Session Data Loader");
        sessLoadScript_ref = sessLoadObject_ref.GetComponent<LoadSessionInfo>();
    }


    public void ReloadDefaultData()
    {
        sessLoadScript_ref.LoadSessionData();
        sessLoadScript_ref.UpdateTotalTrialsAndExpDur();
    }

    public void SetParticipantID()
    {
        GameObject.Find("Experiment Manager").GetComponent<ExperimentManager>().participantID = int.Parse(participantID_IF.GetComponent<InputField>().text);
    }

    public void SetSesionID()
    {
        GameObject.Find("Experiment Manager").GetComponent<ExperimentManager>().sessionID = int.Parse(sessionID_IF.GetComponent<InputField>().text);
    }

    public void SetEyeHeight()
    {
        GameObject.Find("Experiment Manager").GetComponent<ExperimentManager>().eyeHeight = float.Parse(eyeHeight_IF.GetComponent<InputField>().text);
    }

    public void SetOsc()
    {
        string thisButtonName  = EventSystem.current.currentSelectedGameObject.name;
        if (thisButtonName == "Osc FB Button")
            newOscValue = 0;
        if (thisButtonName == "Osc LR Button")
            newOscValue = 1;
        if (thisButtonName == "Osc UD Button")
            newOscValue = 2;
        if (thisButtonName == "Osc Yaw Button")
            newOscValue = 3;
        if (thisButtonName == "Osc Pitch Button")
            newOscValue = 4;
        if (thisButtonName == "Osc Roll Button")
            newOscValue = 5;
        GameObject.Find("Experiment Manager").GetComponent<ExperimentManager>().viewpointOscillation = newOscValue;
        for (int i = 0; i < viewpointOscillationButtons.Length; i++)
            viewpointOscillationButtons[i].GetComponent<Image>().color = Color.white;
        viewpointOscillationButtons[newOscValue].GetComponent<Image>().color = Color.green;
    }

    public void SetAddValues()
    {
        string[] addSplit = addedTranslation_IF.GetComponent<InputField>().text.Split(new char[] { ',' });
        int[] chosenAddVals = new int[addSplit.Length];
        for (int i = 0; i < addSplit.Length; i++)
            chosenAddVals[i] = int.Parse(addSplit[i]);
        GameObject.Find("Experiment Manager").GetComponent<ExperimentManager>().addedTranslations = chosenAddVals;
        sessLoadScript_ref.UpdateTotalTrialsAndExpDur();
    }

    public void SetAmpValues()
    {
        string[] ampSplit = amplitudes_IF.GetComponent<InputField>().text.Split(new char[] { ',' });
        float[] chosenAmpVals = new float[ampSplit.Length];
        for (int i = 0; i < ampSplit.Length; i++)
            chosenAmpVals[i] = float.Parse(ampSplit[i]);
        GameObject.Find("Experiment Manager").GetComponent<ExperimentManager>().amplitudes = chosenAmpVals;
        sessLoadScript_ref.UpdateTotalTrialsAndExpDur();
    }

    public void SetFreqValues()
    {
        string[] freqSplit = frequencies_IF.GetComponent<InputField>().text.Split(new char[] { ',' });
        float[] chosenFreqVals = new float[freqSplit.Length];
        for (int i = 0; i < freqSplit.Length; i++)
            chosenFreqVals[i] = float.Parse(freqSplit[i]);
        GameObject.Find("Experiment Manager").GetComponent<ExperimentManager>().frequencies = chosenFreqVals;
        sessLoadScript_ref.UpdateTotalTrialsAndExpDur();
    }

    public void SetTrialDuration()
    {
        GameObject.Find("Experiment Manager").GetComponent<ExperimentManager>().trialDuration = float.Parse(trialDuration_IF.GetComponent<InputField>().text);
        sessLoadScript_ref.UpdateTotalTrialsAndExpDur();
    }

    public void SetRestDuration()
    {
        GameObject.Find("Experiment Manager").GetComponent<ExperimentManager>().restDuration = int.Parse(restDuration_IF.GetComponent<InputField>().text);
        sessLoadScript_ref.UpdateTotalTrialsAndExpDur();
    }

    public void SetRand()
    {
        string thisButtonName = EventSystem.current.currentSelectedGameObject.name;
        if (thisButtonName == "Rand No Button")
            newRandValue = 0;
        if (thisButtonName == "Rand Yes Button")
            newRandValue = 1;
        GameObject.Find("Experiment Manager").GetComponent<ExperimentManager>().randomTrialOrder = newRandValue;
        for (int i = 0; i < randomOrderButtons.Length; i++)
            randomOrderButtons[i].GetComponent<Image>().color = Color.white;
        randomOrderButtons[newRandValue].GetComponent<Image>().color = Color.green;
    }

    public void SetYoked()
    {
        string thisButtonName = EventSystem.current.currentSelectedGameObject.name;
        if (thisButtonName == "Yoked No Button")
            newYokedValue = 0;
        if (thisButtonName == "Yoked Yes Button")
            newYokedValue = 1;
        GameObject.Find("Experiment Manager").GetComponent<ExperimentManager>().yokedTrials = newYokedValue;
        for (int i = 0; i < yokedButtons.Length; i++)
            yokedButtons[i].GetComponent<Image>().color = Color.white;
        yokedButtons[newYokedValue].GetComponent<Image>().color = Color.green;
    }

    public void SetMSScale()
    {
        string thisButtonName = EventSystem.current.currentSelectedGameObject.name;
        if (thisButtonName == "Scale 4 Button")
            newMSScaleValue = 0;
        if (thisButtonName == "Scale 21 Button")
            newMSScaleValue = 1;
        GameObject.Find("Experiment Manager").GetComponent<ExperimentManager>().yokedTrials = newMSScaleValue;
        for (int i = 0; i < msScaleButtons.Length; i++)
            msScaleButtons[i].GetComponent<Image>().color = Color.white;
        msScaleButtons[newMSScaleValue].GetComponent<Image>().color = Color.green;
    }

    public void SetMSNResps()
    {
        GameObject.Find("Experiment Manager").GetComponent<ExperimentManager>().motSicAsks = int.Parse(msNumberOfResponses_IF.GetComponent<InputField>().text);
    }

    public void SetDebug()
    {
        string thisButtonName = EventSystem.current.currentSelectedGameObject.name;
        if (thisButtonName == "Debug No Button")
            newDebugValue = false;
        if (thisButtonName == "Debug Yes Button")
            newDebugValue = true;
        GameObject.Find("Experiment Manager").GetComponent<ExperimentManager>().debugMode = newDebugValue;
        for (int i = 0; i < debugButtons.Length; i++)
            debugButtons[i].GetComponent<Image>().color = Color.white;
        if (newDebugValue == false)
        { debugButtons[0].GetComponent<Image>().color = Color.green; }
        else { debugButtons[1].GetComponent<Image>().color = Color.green; }
    }

    public void SetReadyForExp()
    {
        expMngScrpt_ref.experimenterReady = true;
        confirmButton.GetComponent<Image>().color = Color.green;
    }
}
