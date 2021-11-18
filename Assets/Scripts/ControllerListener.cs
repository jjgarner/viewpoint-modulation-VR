using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;


public class ControllerListener : MonoBehaviour
{
    // This script works when attached to VR Rig gameobject.
    // it detects certain controller inputs
    // allowing that info to be detected in other scripts that process button inputs
    // so that the right things happen (experiment progresses and data is collected)


    // In inspector, choose right hand controller as inputSource
    public XRNode inputSource;

    public GameObject MSCanvas_ref;
    public MSUIManager msMngScript_ref;

    public GameObject detectCanv_ref;
    public DetectUIManager detectMngScript_ref;

    public GameObject vectCanvas_ref;
    public VectUIManager vectMngScript_ref;

    public GameObject expMngObject_ref;
    public ExperimentManager expMngScrpt_ref;

    private XRRig rig;
    public Vector2 inputAxis;
    public bool primaryBut;
    public bool secondaryBut;

    [Range(0.1f, 0.9f)]
    public float thumbStickSensitivity = 0.5f; // lower = more sensitive


    void Start()
    {
        // you need the XR Rig component for this
        rig = GetComponent<XRRig>();
    }


    void Update()
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(inputSource);

        // each frame, get the chosen control's button & joystick inputs from user
        device.TryGetFeatureValue(CommonUsages.primaryButton, out primaryBut);
        device.TryGetFeatureValue(CommonUsages.secondaryButton, out secondaryBut);
        device.TryGetFeatureValue(CommonUsages.primary2DAxis, out inputAxis);
    }
}
