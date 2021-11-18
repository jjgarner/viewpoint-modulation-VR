using UnityEngine;

public class WaitForSecondsRealtime : CustomYieldInstruction
{
    private float waitTime;

    public override bool keepWaiting
    {
        get { return Time.realtimeSinceStartup < waitTime; }
    }

    public WaitForSecondsRealtime(float time)
    {
        waitTime = Time.realtimeSinceStartup + time;
    }

}
