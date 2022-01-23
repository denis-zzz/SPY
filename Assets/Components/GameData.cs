using UnityEngine;
using System.Collections.Generic;

public class GameData : MonoBehaviour
{
    // Advice: FYFY component aims to contain only public members (according to Entity-Component-System paradigm).
    public GameObject Level;
    public Dictionary<string, List<string>> levelList; //key = directory name, value = list of level file name
    public Dictionary<string, List<string>> skillList;
    public (string, int) levelToLoad = ("Campagne", 1); //directory name, level index
    public List<int> completed_levels = new List<int>() { 0 };
    public (string, string) skillCurrent = ("Campagne", "m");
    public float bestTime; // best time saved for this level (used to compute final score)
    public int minAction; // min number of actions to complete the level (used to compute final score)
    public int scoredStars = 0;
    public List<(string, string)> dialogMessage; //list of (dialogText, imageName)
    public Dictionary<string, int> actionBlocLimit;
    public string scoreKey = "score";
    public int totalStep;
    public int totalActionBloc;
    public int totalExecute;
    public int totalCoin;
    public GameObject actionsHistory; //all actions made in the level, displayed at the end
    public float timer;
    public bool timer_paused = true;

    public Dictionary<string, List<string>> dependency_dict = new Dictionary<string, List<string>>();
    public Dictionary<string, List<string>> cbkst_dict = new Dictionary<string, List<string>>();
}