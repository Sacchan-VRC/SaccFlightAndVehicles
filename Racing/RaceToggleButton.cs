
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class RaceToggleButton : UdonSharpBehaviour
{
    public RacingTrigger[] RacingTriggers;
    public RaceCourseAndScoreboard[] Races;
    public int CurrentCourseSelection = -1;
    private void Start()
    {
        if (CurrentCourseSelection == -1) //-1 = all races disabled
        {
            foreach (RaceCourseAndScoreboard race in Races)
            {
                race.RaceObjects.SetActive(false);
            }
        }
        else
        {
            if (CurrentCourseSelection != -1)
            {
                foreach (RacingTrigger RaceTrig in RacingTriggers)
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
            RaceCourseAndScoreboard race = Races[CurrentCourseSelection].GetComponent<RaceCourseAndScoreboard>();
            race.RaceObjects.SetActive(true);
            race.UpdateTimes();

            if (CurrentCourseSelection == 0)
            {
                foreach (RacingTrigger RaceTrig in RacingTriggers)
                {
                    RaceTrig.gameObject.SetActive(true);
                }
            }
        }
        else
        {
            foreach (RacingTrigger RaceTrig in RacingTriggers)
            {
                RaceTrig.gameObject.SetActive(false);
            }
        }
        foreach (RacingTrigger RaceTrig in RacingTriggers)
        {
            RaceTrig.SetUpNewRace();
        }
    }
}
