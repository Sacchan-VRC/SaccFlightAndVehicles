
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class SaccRaceToggleButton : UdonSharpBehaviour
{
    public SaccRacingTrigger[] RacingTriggers;
    public SaccRaceCourseAndScoreboard[] Races;
    [Tooltip("Can be used to set a default course -1 = none")]
    public int CurrentCourseSelection = -1;
    private void Start()
    {
        if (CurrentCourseSelection == -1) //-1 = all races disabled
        {
            foreach (SaccRaceCourseAndScoreboard race in Races)
            {
                race.RaceObjects.SetActive(false);
            }
        }
        else
        {
            if (CurrentCourseSelection != -1)
            {
                foreach (SaccRacingTrigger RaceTrig in RacingTriggers)
                { RaceTrig.gameObject.SetActive(true); }
            }
        }
        SetRace();
    }
    void Interact()
    {
        if (CurrentCourseSelection != -1) { Races[CurrentCourseSelection].RaceObjects.SetActive(false); }
        if (CurrentCourseSelection == Races.Length - 1)
        { CurrentCourseSelection = -1; }
        else { CurrentCourseSelection++; }

        SetRace();
    }
    void SetRace()
    {

        if (CurrentCourseSelection != -1)//-1 = all races disabled
        {
            SaccRaceCourseAndScoreboard race = Races[CurrentCourseSelection].GetComponent<SaccRaceCourseAndScoreboard>();
            race.RaceObjects.SetActive(true);
            race.UpdateTimes();

            if (CurrentCourseSelection == 0)
            {
                foreach (SaccRacingTrigger RaceTrig in RacingTriggers)
                {
                    RaceTrig.gameObject.SetActive(true);
                }
            }
        }
        else
        {
            foreach (SaccRacingTrigger RaceTrig in RacingTriggers)
            {
                RaceTrig.gameObject.SetActive(false);
            }
        }
        foreach (SaccRacingTrigger RaceTrig in RacingTriggers)
        {
            RaceTrig.SetUpNewRace();
        }
    }
}
