
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class SaccRaceToggleButton : UdonSharpBehaviour
{
    [HideInInspector] public SaccRacingTrigger[] RacingTriggers;
    [HideInInspector] public SaccRaceCourseAndScoreboard[] Races;
    [Tooltip("Can be used to set a default course -1 = none")]
    public int CurrentCourseSelection = -1;
    private bool Reverse = false;
    public GameObject EnableWhenNoneSelected;
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
    public override void Interact()
    {
        NextRace();
    }
    public void NextRace()
    {
        if (CurrentCourseSelection != -1) { Races[CurrentCourseSelection].RaceObjects.SetActive(false); }
        if (CurrentCourseSelection == Races.Length - 1)
        { CurrentCourseSelection = -1; }
        else { CurrentCourseSelection++; }

        SetRace();
    }
    public void PreviousRace()
    {
        if (CurrentCourseSelection != -1) { Races[CurrentCourseSelection].RaceObjects.SetActive(false); }
        if (CurrentCourseSelection == -1)
        { CurrentCourseSelection = Races.Length - 1; }
        else { CurrentCourseSelection--; }

        SetRace();
    }
    void SetRace()
    {

        if (CurrentCourseSelection != -1)//-1 = all races disabled
        {
            if (EnableWhenNoneSelected) { EnableWhenNoneSelected.SetActive(false); }
            SaccRaceCourseAndScoreboard race = Races[CurrentCourseSelection].GetComponent<SaccRaceCourseAndScoreboard>();
            race.RaceObjects.SetActive(true);
            race.UpdateTimes();

            foreach (SaccRacingTrigger RaceTrig in RacingTriggers)
            {
                RaceTrig.gameObject.SetActive(true);
            }
        }
        else
        {
            if (EnableWhenNoneSelected) { EnableWhenNoneSelected.SetActive(true); }
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
    public void ToggleReverse()
    {
        if (!Reverse)
        {
            Reverse = true;
            foreach (SaccRacingTrigger RaceTrig in RacingTriggers)
            {
                RaceTrig.SetProgramVariable("_TrackForward", false);
            }
        }
        else
        {
            Reverse = false;
            foreach (SaccRacingTrigger RaceTrig in RacingTriggers)
            {
                RaceTrig.SetProgramVariable("_TrackForward", true);
            }
        }
        SetRace();
    }
}
