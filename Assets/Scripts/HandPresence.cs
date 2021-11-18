using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class HandPresence : MonoBehaviour
{
    // This script detects different VR devices (Oculus vs. HTC etc.) 
    // and displays their corresponding controllers with fake hands on top
    // from the youtube tutorial by Valem: "Introduction to VR in Unity - PART 2 : INPUT and HAND PRESENCE"

    public bool showHandOnly = false;
    public bool showControllerOnly = false;
    public bool showControllerAndHand = false;
    public bool showNeither = false;

    public InputDeviceCharacteristics controllerCharacteristics;
    public List<GameObject> controllerPrefabs; // in inspector, you need to manually add as many controller models as devices you want to include (both left and right per device)
    public GameObject handModelPrefab;

    private InputDevice targetDevice;
    private GameObject spawnedController;
    private GameObject spawnedHandModel;
    private Animator handAnimator;

    void Start()
    {
        TryInitialize();
    }


    void TryInitialize()
    {
        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(controllerCharacteristics, devices);

        foreach (var item in devices)
        {
            Debug.Log(item.name + item.characteristics);
        }

        if (devices.Count > 0)
        {
            targetDevice = devices[0];
            GameObject prefab = controllerPrefabs.Find(controller => controller.name == targetDevice.name);
            if (prefab)
            {
                spawnedController = Instantiate(prefab, transform);
            }
            else
            {
                Debug.Log("Did not find corresponding controller model");
                // in this case show the first controller prefab on the list even if it is wrong
                spawnedController = Instantiate(controllerPrefabs[0], transform);
            }

            spawnedHandModel = Instantiate(handModelPrefab, transform);
            handAnimator = spawnedHandModel.GetComponent<Animator>();
        }
    }


    void UpdateHandAnimation()
    {
        if (targetDevice.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue))
        {
            handAnimator.SetFloat("Trigger", triggerValue);
        }
        else
        {
            handAnimator.SetFloat("Trigger", 0);
        }

        if (targetDevice.TryGetFeatureValue(CommonUsages.grip, out float gripValue))
        {
            handAnimator.SetFloat("Grip", gripValue);
        }
        else
        {
            handAnimator.SetFloat("Grip", 0);
        }
    }


    void Update()
    {
        if (!targetDevice.isValid)
        {
            TryInitialize();
        }
        else
        {
            if (showHandOnly)
            {
                spawnedController.SetActive(false);
                spawnedHandModel.SetActive(true);
                UpdateHandAnimation();
            }
            else if (showControllerOnly)
            {
                spawnedController.SetActive(true);
                spawnedHandModel.SetActive(false);
            }
            else if (showControllerAndHand)
            {
                spawnedController.SetActive(true);
                spawnedHandModel.SetActive(true);
                UpdateHandAnimation();
            }
            else
            {
                spawnedController.SetActive(false);
                spawnedHandModel.SetActive(false);
                //// debug by sending inputs to console
                //if (targetDevice.TryGetFeatureValue(CommonUsages.primaryButton, out bool primaryButtonValue) && primaryButtonValue)
                //    Debug.Log("Primary Button is pressed");
                //if (targetDevice.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue) && triggerValue > 0.1f)
                //    Debug.Log("Trigger pressed "+ triggerValue);
                //if (targetDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 primary2DAxisValue) && primary2DAxisValue != Vector2.zero)
                //    Debug.Log("Primary Touchpad/Joystick pressed" + primary2DAxisValue);
            }
        }
    }
}
