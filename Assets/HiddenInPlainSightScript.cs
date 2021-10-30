using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text.RegularExpressions;

public class HiddenInPlainSightScript : MonoBehaviour {

    public KMAudio audio;
    public KMBombInfo bomb;
    public KMSelectable ModuleSelectable;
    public GameObject[] hiddenObjects;
    public GameObject statusColl;
    public Mesh[] shapeObjs;
    public Material blendMat;

    private RaycastHit[] allHit;
    private Material[] usedMats = new Material[4];
    private List<int> chosenShapes = new List<int>();
    private int[] quadrants = { 0, 1, 2, 3, 4, 10, 11, 12, 13, 14, 20, 21, 22, 23, 24, 30, 31, 32, 33, 34, 40, 41, 42, 43, 44, 5, 6, 7, 8, 9, 15, 16, 17, 18, 19, 25, 26, 27, 28, 29, 35, 36, 37, 38, 39, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 60, 61, 62, 63, 64, 70, 71, 72, 73, 74, 80, 81, 82, 83, 84, 90, 91, 92, 93, 94, 55, 56, 57, 58, 59, 65, 66, 67, 68, 69, 75, 76, 77, 78, 79, 85, 86, 87, 88, 89, 95, 96, 97, 98, 99 };
    // white = 0, black = 1
    private string[] patterns = { "0001111000001111110001100001100110001110011001011001101001100111000110011000011000111111000001111000", "0000111000000111100000111110000011011000000001100000000110000000011000000001100000011111000001111100", "0000110000000111100000110011000011001100000001110000001110000001110000001110000000111111000011111100", "0000110000001111110000110011000000011000000011100000000111000000001100001100110000111111000000111000", "0000001100000001110000001111000001101100001100110001100011000111111110011111111000000011000000001100", "0111111110011111111001100000000111111100011111111000000001100000000110011000011001111111100011111100", "0001111000001111110001110011100110000000011011000001111111000111001110011100111000111111000001111000", "0111111110011111111000000001100000001110000001110000001110000001110000000110000000011000000001100000", "0001111000001111110001100001100110000110001111110000111111000110000110011000011000111111000001111000", "0001111000001111110001100001100110000110011111111000111111100000000110011000011000111111000001111000" };
    private string[] symbols = { "■", "◆", "▲", "▼", "◉", "◍", "◈", "Ж", "Ӝ", "Җ" };
    private string[] positions = { "TL", "TR", "BL", "BR" };
    private string[] objNames = new string[4];
    private bool[] increase = new bool[4];
    private int correctStart = -1;
    private int correctEnd = -1;
    private int timeHovered = -1;
    private string curGrid = "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000";
    private string lastHit = "";
    private bool hovering = false;
    private bool focused = false;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        moduleSolved = false;
        if (Application.isEditor)
        {
            focused = true;
        }
        ModuleSelectable.OnFocus += delegate () { focused = true; };
        ModuleSelectable.OnDefocus += delegate () { focused = false; };
    }

    void Start() {
        statusColl.name = "hipsStatus" + moduleId;
        for (int i = 0; i < shapeObjs.Length; i++)
            shapeObjs[i].name = "hipsShape" + i + moduleId;
        List<GameObject> placed = new List<GameObject>();
        for (int i = 0; i < 4; i++)
        {
            usedMats[i] = new Material(blendMat);
            if (i == 3)
            {
                chosenShapes.Add(Random.Range(10, shapeObjs.Length));
                if (chosenShapes[i] >= 17)
                    hiddenObjects[i].transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            }
            else
                chosenShapes.Add(Random.Range(0, 10));
            objNames[i] = shapeObjs[chosenShapes[i]].name + i;
            hiddenObjects[i].transform.GetChild(0).name = objNames[i];
            hiddenObjects[i].GetComponent<MeshFilter>().mesh = shapeObjs[chosenShapes[i]];
            hiddenObjects[i].GetComponent<MeshRenderer>().material = usedMats[i];
            hiddenObjects[i].GetComponentInChildren<MeshCollider>().sharedMesh = shapeObjs[chosenShapes[i]];
            float xrange = Random.Range(-0.065f, 0.065f);
            float zrange = Random.Range(-0.06f, 0.06f);
            hiddenObjects[i].transform.localPosition = new Vector3(xrange, 0.013f, zrange);
            while (IsIntersecting(placed, hiddenObjects[i]) || (xrange > 0.025 && zrange > 0.02))
            {
                xrange = Random.Range(-0.065f, 0.065f);
                zrange = Random.Range(-0.06f, 0.06f);
                hiddenObjects[i].transform.localPosition = new Vector3(xrange, 0.013f, zrange);
            }
            placed.Add(hiddenObjects[i]);
            StartCoroutine(ChangeMaterial(i));
        }
        List<int> temp = new List<int>();
        temp.Add(chosenShapes[0]);
        temp.Add(chosenShapes[1]);
        temp.Add(chosenShapes[2]);
        Debug.LogFormat("[Hidden In Plain Sight #{0}] The numbers on the module are: {1}", moduleId, temp.Join(", "));
        Debug.LogFormat("[Hidden In Plain Sight #{0}] The shape on the module is: {1}", moduleId, symbols[chosenShapes[3] - 10]);
        temp.Sort();
        int cellIndex = temp[1] * 10 + chosenShapes[3] - 10;
        curGrid = ToggleCells(patterns[temp[0]], patterns[temp[0]][cellIndex] == '1');
        Debug.LogFormat("[Hidden In Plain Sight #{0}] After placing in {1} the grid is now:", moduleId, temp[0]);
        LogGrid();
        curGrid = ToggleCells(patterns[temp[2]], patterns[temp[2]][cellIndex] == '1');
        Debug.LogFormat("[Hidden In Plain Sight #{0}] After placing in {1} the grid is now:", moduleId, temp[2]);
        LogGrid();
        CalculateTimes();
    }

    void Update()
    {
        if (moduleSolved) return;
        if (focused)
        {
            allHit = Physics.RaycastAll(Camera.main.ScreenPointToRay(Input.mousePosition));
            List<string> names = new List<string>();
            foreach (RaycastHit hit in allHit)
            {
                names.Add(hit.collider.name);
                if (objNames.Contains(hit.collider.name) && !hovering)
                {
                    hovering = true;
                    lastHit = hit.collider.name;
                    for (int i = 0; i < 4; i++)
                    {
                        if (lastHit == objNames[i])
                        {
                            increase[i] = true;
                            break;
                        }
                    }
                }
                else if (hit.collider.name == ("hipsStatus" + moduleId) && !hovering)
                {
                    hovering = true;
                    lastHit = hit.collider.name;
                    timeHovered = (int)bomb.GetTime() % 60;
                }
            }
            if (!names.Contains(lastHit))
            {
                hovering = false;
                if (lastHit != ("hipsStatus" + moduleId))
                {
                    for (int i = 0; i < 4; i++)
                    {
                        if (lastHit == objNames[i])
                        {
                            increase[i] = false;
                            break;
                        }
                    }
                }
                else if (timeHovered == correctStart && ((int)bomb.GetTime() % 60) == correctEnd)
                {
                    moduleSolved = true;
                    GetComponent<KMBombModule>().HandlePass();
                    audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
                }
                else
                    timeHovered = -1;
                lastHit = "";
            }
        }
        else
        {
            hovering = false;
            for (int i = 0; i < 4; i++)
            {
                if (lastHit == objNames[i])
                {
                    increase[i] = false;
                    break;
                }
            }
            timeHovered = -1;
            lastHit = "";  
        }
    }

    string ToggleCells(string pattern, bool invert)
    {
        string newPattern = "";
        for (int i = 0; i < 100; i++)
        {
            if (invert && pattern[i] == '0')
                newPattern += '1';
            else if (invert && pattern[i] == '1')
                newPattern += '0';
            else if (!invert && pattern[i] == '0')
                newPattern += '0';
            else
                newPattern += '1';
        }
        string temp = "";
        for (int i = 0; i < 100; i++)
        {
            if (newPattern[i] == '0' && curGrid[i] == '0')
                temp += '0';
            else if (newPattern[i] == '1' && curGrid[i] == '0')
                temp += '1';
            else if (newPattern[i] == '0' && curGrid[i] == '1')
                temp += '1';
            else
                temp += '0';
        }
        return temp;
    }

    void CalculateTimes()
    {
        List<int> blackCts = new List<int>();
        for (int i = 0; i < 4; i++)
        {
            blackCts.Add(0);
            for (int j = i * 25; j < i * 25 + 25; j++)
            {
                if (curGrid[quadrants[j]] == '1')
                    blackCts[i]++;
            }
        }
        correctStart = 25 - blackCts[blackCts.IndexOf(blackCts.Min())];
        correctEnd = 25 - blackCts[blackCts.IndexOf(blackCts.Max())];
        Debug.LogFormat("[Hidden In Plain Sight #{0}] The quadrant with the least black cells is {1}, which has {2} white cells", moduleId, positions[blackCts.IndexOf(blackCts.Min())], correctStart);
        Debug.LogFormat("[Hidden In Plain Sight #{0}] The quadrant with the most black cells is {1}, which has {2} white cells", moduleId, positions[blackCts.IndexOf(blackCts.Max())], correctEnd);
    }

    bool IsIntersecting(List<GameObject> list, GameObject check)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].gameObject.transform.localPosition.x + 0.03f > check.gameObject.transform.localPosition.x && list[i].gameObject.transform.localPosition.x - 0.03f < check.gameObject.transform.localPosition.x && list[i].gameObject.transform.localPosition.z + 0.045f > check.gameObject.transform.localPosition.z && list[i].gameObject.transform.localPosition.z - 0.045f < check.gameObject.transform.localPosition.z)
            {
                return true;
            }
        }
        return false;
    }

    void LogGrid()
    {
        for (int i = 0; i < 100; i+=10)
            Debug.LogFormat("[Hidden In Plain Sight #{0}] {1}{2}{3}{4}{5}{6}{7}{8}{9}{10}", moduleId, curGrid[i], curGrid[i + 1], curGrid[i + 2], curGrid[i + 3], curGrid[i + 4], curGrid[i + 5], curGrid[i + 6], curGrid[i + 7], curGrid[i + 8], curGrid[i + 9]);
    }

    IEnumerator ChangeMaterial(int index)
    {
        while (true)
        {
            if (increase[index] && usedMats[index].color.a < 1)
                usedMats[index].color = new Color(usedMats[index].color.r, usedMats[index].color.g, usedMats[index].color.b, usedMats[index].color.a + .01f);
            else if (usedMats[index].color.a > 0)
                usedMats[index].color = new Color(usedMats[index].color.r, usedMats[index].color.g, usedMats[index].color.b, usedMats[index].color.a - .01f);
            yield return new WaitForSecondsRealtime(.01f);
        }
    }

    //twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} cycle [Quickly hovers over the numbers and shape] | !{0} toggle <##> [Starts or stops hovering over the status light when the last two digits of the bomb's timer are '##']";
    #pragma warning restore 414
    bool ZenModeActive;

    IEnumerator ProcessTwitchCommand(string command)
    {
        if (Regex.IsMatch(command, @"^\s*cycle\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            for (int i = 0; i < 4; i++)
            {
                increase[i] = true;
                float t = 0f;
                while (t < 2f)
                {
                    yield return "trycancel Halted hovering over the numbers and shape due to a request to cancel!";
                    t += Time.deltaTime;
                }
                increase[i] = false;
                t = 0f;
                while (t < .1f)
                {
                    yield return "trycancel Halted hovering over the numbers and shape due to a request to cancel!";
                    t += Time.deltaTime;
                }
            }
        }
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*toggle\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if (parameters.Length > 2)
            {
                yield return "sendtochaterror Too many parameters!";
            }
            else if (parameters.Length == 2)
            {
                int time = -1;
                if (int.TryParse(parameters[1], out time))
                {
                    if (parameters[1].Length == 2 && time >= 0 && time <= 59)
                    {
                        yield return null;
                        if ((int)bomb.GetTime() % 60 == time)
                            yield return "waiting music";
                        else if (ZenModeActive)
                        {
                            if ((time > (int)bomb.GetTime() % 60 && (time - (int)bomb.GetTime() % 60 > 15)) || (time < (int)bomb.GetTime() % 60 && (60 - (int)bomb.GetTime() % 60 + time > 15)))
                                yield return "waiting music";
                        }
                        else
                        {
                            if ((time > (int)bomb.GetTime() % 60 && (60 - time + (int)bomb.GetTime() % 60 > 15)) || (time < (int)bomb.GetTime() % 60 && ((int)bomb.GetTime() % 60 - time > 15)))
                                yield return "waiting music";
                        }
                        while ((int)bomb.GetTime() % 60 == time) yield return "trycancel Halted waiting to start or stop hovering due to a cancel request.";
                        while ((int)bomb.GetTime() % 60 != time) yield return "trycancel Halted waiting to start or stop hovering due to a cancel request.";
                        yield return "end waiting music";
                        if (timeHovered == -1)
                            timeHovered = (int)bomb.GetTime() % 60;
                        else
                        {
                            if (timeHovered == correctStart && ((int)bomb.GetTime() % 60) == correctEnd)
                                yield return "solve";
                            lastHit = "hipsStatus" + moduleId;
                        }
                        yield break;
                    }
                }
                yield return "sendtochaterror!f The specified digits '" + parameters[1] + "' are invalid!";
            }
            else if (parameters.Length == 1)
            {
                yield return "sendtochaterror Please specify the digits to start or stop hovering on!";
            }
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        if (timeHovered != -1 && timeHovered != correctStart)
            lastHit = "hipsStatus" + moduleId;
        if (timeHovered == -1)
        {
            while ((int)bomb.GetTime() % 60 != correctStart) yield return true;
            timeHovered = (int)bomb.GetTime() % 60;
        }
        while ((int)bomb.GetTime() % 60 != correctEnd) yield return true;
        lastHit = "hipsStatus" + moduleId;
    }
}