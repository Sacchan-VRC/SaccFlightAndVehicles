
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
            foreach (RaceCourseAndScoreboard race in Races)
            {
                race.RaceObjects.gameObject.SetActive(false);
            }
            Races[CurrentCourseSelection].gameObject.SetActive(true);
            Races[CurrentCourseSelection].GetComponent<RaceCourseAndScoreboard>().UpdateTimes();
        }
        foreach (RacingTrigger RaceTrig in RacingTriggers)
        {
            RaceTrig.SetUpNewRace();
        }
    }
    void Interact()
    {
        if (CurrentCourseSelection != -1) { Races[CurrentCourseSelection].RaceObjects.SetActive(false); }
        if (CurrentCourseSelection == Races.Length - 1)
        { CurrentCourseSelection = -1; }
        else { CurrentCourseSelection++; }

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
