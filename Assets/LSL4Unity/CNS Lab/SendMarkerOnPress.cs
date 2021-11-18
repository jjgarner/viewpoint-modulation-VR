using UnityEngine;
using UnityEngine.Assertions;
using Assets.LSL4Unity.Scripts;

public class SendMarkerOnPress : MonoBehaviour {

    // Don't forget to assign this to the correct stream script
    public MarkerOutlet markerStream;

    // Warns you in case you forget anyway...
    void Start()
    {
        Assert.IsNotNull(markerStream, "You forgot to assign the reference to a marker stream implementation!");
    }

    // Update is called once per frame
    void Update ()
    {
        if (Input.GetKeyDown("1"))
            markerStream.Write("1");
        if (Input.GetKeyDown("2"))
            markerStream.Write("2");
    }
}
