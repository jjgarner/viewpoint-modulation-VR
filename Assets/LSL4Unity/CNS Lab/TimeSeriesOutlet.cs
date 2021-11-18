using UnityEngine;
using LSL;
using Assets.LSL4Unity.Scripts.Common;

namespace Assets.LSL4Unity.Scripts
{
    // This class provides time series data to LSL
    public class TimeSeriesOutlet : MonoBehaviour
    {
        // Initialize LSL stream variables
        private liblsl.StreamOutlet outlet;
        private liblsl.StreamInfo streamInfo;

        // Create an array to hold the data to be streamed
        private float[] currentSample;

        // Create the variable to hold data sampling rate
        private double dataRate;

        // Creates a drop-down menu to choose sampling style in the Unity editor (update, fixed, last frame)
        public MomentForSampling sampling;

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        // Modify this to suit your needs
        /////////////////////////////////////////////////////////////////////////////////////////////////////

        // The following should be unique for each stream in your scene
        public const string unique_source_id = "CNSLAB1"; // I don't know how this matters, have never changed it
        public string StreamName = "UnityTimeSeries"; // Change this in the editor if you want to be more specific
        private string StreamType = "Unity.Data"; // This doesn't seem to matter (?)

        // How many channels are in this stream
        private int ChannelCount = 17;

        // Here is where you want to access the variables you wish to stream to LSL
        public GameObject markerSource;
        private float time;
        [SerializeField] GameObject HMD_Camera_ref;
        [SerializeField] GameObject VR_rig_ref;

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////

        // Specifies channel format to floats
        private const liblsl.channel_format_t ChannelFormat = liblsl.channel_format_t.cf_float32;

        void Start()
        {
            // initialize the array once
            currentSample = new float[ChannelCount];
            // sets the data sampling rate
            dataRate = LSLUtils.GetSamplingRateFor(sampling);
            // actually creates the LSL stream
            streamInfo = new liblsl.StreamInfo(StreamName, StreamType, ChannelCount, dataRate, ChannelFormat, unique_source_id);
            // create the outlet "port" for this stream
            outlet = new liblsl.StreamOutlet(streamInfo);
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        // Modify this to suit your needs
        /////////////////////////////////////////////////////////////////////////////////////////////////////

        // This method is used to gather the data you wish to send on each update
        private void GatherData()
        {
            // Keep track of time
            time += Time.deltaTime;

            // Record HMD real-world rotation as quaternion
            // to do: does this have to be quaternions??? Euler angles would be more convenient
            float HMDw = HMD_Camera_ref.transform.localRotation.w;
            float HMDx = HMD_Camera_ref.transform.localRotation.x;
            float HMDy = HMD_Camera_ref.transform.localRotation.y;
            float HMDz = HMD_Camera_ref.transform.localRotation.z;

            // Record HMD real-world position in x, y, and z coordinates (centemeters!!!)
            float HMDpx = HMD_Camera_ref.transform.localPosition.x;
            float HMDpy = HMD_Camera_ref.transform.localPosition.y;
            float HMDpz = HMD_Camera_ref.transform.localPosition.z;

            // Record modulated viewpoint rotation (relative to the Unity environment) as quaternion
            float playerw = VR_rig_ref.transform.localRotation.w;
            float playerx = VR_rig_ref.transform.localRotation.x;
            float playery = VR_rig_ref.transform.localRotation.y;
            float playerz = VR_rig_ref.transform.localRotation.z;

            // Record modulated viewpoint position (relative to the Unity environment) (simulated meters)
            float playerpx = VR_rig_ref.transform.position.x;
            float playerpy = VR_rig_ref.transform.position.y;
            float playerpz = VR_rig_ref.transform.position.z;

            // Get event codes / markers from the ExperimentController script
            float markerTrial = markerSource.GetComponent<ExperimentManager>().markerTrialOnset;
            float markerRest = markerSource.GetComponent<ExperimentManager>().markerRestOnset;


            // Put the data into the sample array
            currentSample[0] = time;

            currentSample[1] = HMDw;
            currentSample[2] = HMDx;
            currentSample[3] = HMDy;
            currentSample[4] = HMDz;
            currentSample[5] = HMDpx;
            currentSample[6] = HMDpy;
            currentSample[7] = HMDpz;

            currentSample[8] = playerw;
            currentSample[9] = playerx;
            currentSample[10] = playery;
            currentSample[11] = playerz;
            currentSample[12] = playerpx;
            currentSample[13] = playerpy;
            currentSample[14] = playerpz;

            currentSample[15] = markerTrial;
            currentSample[16] = markerRest;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////

        // This method is called each "update" (depending on which MomentForSampling is chosen)
        // to stream the data in currentSample to LSL
        private void pushSample()
        {
            if (outlet == null)
                return;

            GatherData();

            outlet.push_sample(currentSample, liblsl.local_clock());
        }

        // Called ever physics frame, guaranteed to be stable
        void FixedUpdate()
        {
            if (sampling == MomentForSampling.FixedUpdate)
                pushSample();
        }

        // Called every video refresh frame, this is considered unstable by LSL
        void Update()
        {
            if (sampling == MomentForSampling.Update)
                pushSample();
        }

        // Called after the last video frame has been drawn, also unstable but more stable than update
        void LateUpdate()
        {
            if (sampling == MomentForSampling.LateUpdate)
                pushSample();
        }
    }
}
