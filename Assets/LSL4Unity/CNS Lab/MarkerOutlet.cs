using UnityEngine;
using System.Collections;
using LSL;

namespace Assets.LSL4Unity.Scripts
{
    // This class provides marker data to LSL
    public class MarkerOutlet : MonoBehaviour
    {
        // Initialize LSL stream variables
        private liblsl.StreamOutlet outlet;
        private liblsl.StreamInfo StreamInfo;

        // Create a string array to hold the marker data to be streamed
        private string[] sample;

        //Assuming that markers are never sent at regular intervals
        private double dataRate = liblsl.IRREGULAR_RATE;

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////

        // The following should be unique for each stream in your scene
        private const string unique_source_id = "CNSLAB2";
        public string StreamName = "UnityMarker";
        private string StreamType = "LSL_Marker_Strings";

        // How many channels are in this stream
        private int ChannelCount = 1;

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////

        // Specifies channel format to strings
        private const liblsl.channel_format_t ChannelFormat = liblsl.channel_format_t.cf_string;

        void Awake()
        {
            // initialize the array once
            sample = new string[ChannelCount];
            // actually creates the LSL stream
            StreamInfo = new liblsl.StreamInfo(StreamName,StreamType,ChannelCount, dataRate, ChannelFormat,unique_source_id);
            // create the outlet "port" for this stream
            outlet = new liblsl.StreamOutlet(StreamInfo);
        }

        // Writes the marker immediately
        public void Write(string marker)
        {
            sample[0] = marker;
            outlet.push_sample(sample);
        }


        // Writes the marker after the current frame has rendered
        private string pendingMarker;

        public void WriteBeforeFrameIsDisplayed(string marker)
        {
            pendingMarker = marker;
            StartCoroutine(WriteMarkerAfterImageIsRendered());
        }

        IEnumerator WriteMarkerAfterImageIsRendered()
        {
            yield return new WaitForEndOfFrame();

            Write(pendingMarker);

            yield return null;
        }

    }
}
