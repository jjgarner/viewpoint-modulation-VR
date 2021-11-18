using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;


public class ContinuousMovement : MonoBehaviour
{
    // In inspector, choose what input (lefthand controller etc) causes what controlled movement output
    public XRNode inputSource;

    // Average preferred walking speed is ~1.4 m/s (https://en.wikipedia.org/wiki/Preferred_walking_speed)
    public float speedMultiplier = 1.4f;
    public float gravity = -9.81f;
    public LayerMask groundLayer;
    public float eyeLevelOffset = 0.11f; // wikipedia has a page on head anatomy

    private float fallingSpeed;
    private XRRig rig;
    private Vector2 inputAxis;
    private CharacterController charController;


    // Start is called before the first frame update
    void Start()
    {
        charController = GetComponent<CharacterController>();
        rig = GetComponent<XRRig>();
    }


    // Update is called once per frame
    void Update()
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(inputSource);
        device.TryGetFeatureValue(CommonUsages.primary2DAxis, out inputAxis);
    }


    // movement is applied every call to fixed update
    // NOTE: Fixed Timestep can be adjuested in project settings (default is 0.02 = 50 Hz)
    // Oculus Rift has 90 fps, so doing 0.01 sec fixed timesteps (100 Hz) is enough to outpace the framerate 
    // which gives a smooth visual experience & good rate of data collection, without too much computation cost
    private void FixedUpdate()
    {
        // before applying movement,
        // adjust height and position of user's capsule collider based on headset position
        CapsuleFollowHeadset();

        Quaternion headYaw = Quaternion.Euler(0, rig.cameraGameObject.transform.eulerAngles.y, 0);
        // added movement is applied relative to head's yaw rotation 
        Vector3 direction = headYaw * new Vector3(inputAxis.x, 0, inputAxis.y);

        // at full joystick throttle, move at 1.4m/s
        charController.Move(direction * Time.fixedDeltaTime * speedMultiplier);

        // apply gravity if not touching ground
        bool isGrounded = CheckIfGrounded();
        if (isGrounded)
        { fallingSpeed = 0; }
        else
        { fallingSpeed += gravity * Time.fixedDeltaTime; }

        charController.Move(Vector3.up * fallingSpeed * Time.fixedDeltaTime);
    }


    void CapsuleFollowHeadset()
    {
        charController.height = rig.cameraInRigSpaceHeight + eyeLevelOffset;
        Vector3 capsuleCenter = transform.InverseTransformPoint(rig.cameraGameObject.transform.position);
        charController.center = new Vector3(capsuleCenter.x, charController.height/2 + charController.skinWidth, capsuleCenter.z);
    }


    bool CheckIfGrounded()
    {
        Vector3 rayStart = transform.TransformPoint(charController.center);
        float rayLength = charController.center.y + 0.01f;
        bool hasHit = Physics.SphereCast(rayStart, charController.radius, Vector3.down, out RaycastHit hitInfo, rayLength, groundLayer);
        return hasHit;
    }
}
