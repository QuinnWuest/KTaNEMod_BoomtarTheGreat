using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;
using rnd = UnityEngine.Random;

public class boomtarTheGreat : MonoBehaviour
{
    public new KMAudio audio;
    private KMAudio.KMAudioRef mainRef;
    public KMBombInfo bomb;
    public KMBombModule module;

    public KMSelectable dummySelectable;
    public TextMesh[] texts;
    public GameObject statusLight;

    private int rule1;
    private int rule2;
    private string submitKeyword;

    private static readonly string[] submitWords = new string[10] { "PREDICATE", "OBFUSCATE", "DERIVE", "ANOMALIES", "WHISPERING", "PANDORA", "DECADENCE", "IMPERIAL", "AGGREGATE", "BALLAST" };
    private static readonly string table = "BCADFEEAFCDBFBCAEDDFBECAAEDFBCCDEBAF";
    private static HashSet<string> validWords = new HashSet<string>();
    private bool cantInteract = true;

    private bool TwitchPlaysActive;
    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved;

    private void Awake()
    {
        moduleId = moduleIdCounter++;
        // module.OnActivate += delegate () { cantInteract = false; mainRef = audio.HandlePlaySoundAtTransformWithRef("start", transform, false); };
        Action focus = delegate () { FocusModule(); };
        var mainSelectable = GetComponent<KMSelectable>();
        mainSelectable.OnFocus += focus;
        module.OnActivate += delegate ()
        {
            if (TwitchPlaysActive)
                mainSelectable.OnFocus -= focus;
        };

        var allWords = wordList.allWords.Split(',');
        foreach (string word in allWords)
            validWords.Add(word);
    }

    private void Start()
    {
        var mainCam = Camera.main;
        if (mainCam != null)
            mainCam.cullingMask &= ~LayerMask.GetMask("BoomtarPortalCamera");

        StartCoroutine(DisableStuff());
        rule1 = rnd.Range(0, 6);
        do { rule2 = rnd.Range(0, 6); } while (rule2 == rule1);
        Debug.LogFormat("[Boomtar the Great #{0}] The two rules that apply are rule {1} and rule {2}. In the table this becomes {3}.", moduleId, rule1 + 1, rule2 + 1, table[rule1 * 6 + rule2]);
        submitKeyword = submitWords[bomb.GetSerialNumberNumbers().Last()];
        Debug.LogFormat("[Boomtar the Great #{0}] The last digit of the serial number is {1}, so the submission keyword is {2}.", moduleId, bomb.GetSerialNumberNumbers().Last(), submitKeyword.ToLowerInvariant());
    }

    private void FocusModule(string query = "")
    {
        if (cantInteract || moduleSolved)
            return;
        if (mainRef != null)
        {
            mainRef.StopSound();
            mainRef = null;
        }
        var clipboardText = "";
        if (string.IsNullOrEmpty(query))
            clipboardText = GUIUtility.systemCopyBuffer.Trim().ToUpperInvariant();
        else
            clipboardText = query.Trim().ToUpperInvariant();
        var clipboardArray = clipboardText.Split(' ').ToArray();
        if (clipboardArray.Length != 1 && clipboardArray.Length != 2)
            StartCoroutine(Wait("bad word", 2.5f));
        else if (clipboardArray.Length == 2 && clipboardArray[0] != submitKeyword)
            StartCoroutine(Wait("bad word", 2.5f));
        else if (clipboardArray.Length == 2)
        {
            Debug.LogFormat("[Boomtar the Great #{0}] Submitted text: {1}", moduleId, clipboardArray[1]);
            if (!SubmissionCheck(table[rule1 * 6 + rule2], clipboardArray[1]))
            {
                module.HandleStrike();
                Debug.LogFormat("[Boomtar the Great #{0}] That was invalid. Strike!", moduleId);
                StartCoroutine(Wait("strike", 3.5f));
            }
            else
            {
                module.HandlePass();
                moduleSolved = true;
                foreach (TextMesh text in texts)
                    text.color = Color.green;
                Debug.LogFormat("[Boomtar the Great #{0}] That was valid. Module solved!", moduleId);
                StartCoroutine(Wait("solve", 3.5f));
            }
        }
        else if (!validWords.Contains(clipboardText))
            StartCoroutine(Wait("bad word", 2.5f));
        else
        {
            var rule1Applies = false;
            var rule2Applies = false;
            for (int i = 0; i < 6; i++)
            {
                if (rule1 == i && RuleCheck(i, clipboardText))
                    rule1Applies = true;
                if (rule2 == i && RuleCheck(i, clipboardText))
                    rule2Applies = true;
            }
            if (rule1Applies && rule2Applies)
                StartCoroutine(Wait("both", 2.5f));
            else if (rule1Applies)
                StartCoroutine(Wait("rule1", 3.5f));
            else if (rule2Applies)
                StartCoroutine(Wait("rule2", 3.5f));
            else
                StartCoroutine(Wait("neither", 3.5f));
        }
    }

    private bool RuleCheck(int i, string clipboardText)
    {
        switch (i)
        {
            case 0:
                return bomb.GetModuleNames().Select(x => x.ToUpperInvariant()).Any(x => x.First() == clipboardText.First());
            case 1:
                var allPairs = new List<string>();
                var alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                for (int j = 0; j < 26; j++)
                    allPairs.Add(alphabet[j].ToString() + alphabet[j]);
                return allPairs.Any(x => clipboardText.Contains(x));
            case 2:
                var str = "";
                var vowels = "AEIOU";
                foreach (char c in clipboardText)
                {
                    if (!vowels.Contains(c))
                    {
                        str += c;
                        continue;
                    }
                    else if ((c == 'A' && (str.Contains('E') || str.Contains('I') || str.Contains('O') || str.Contains('U'))) || (c == 'E' && (str.Contains('I') || str.Contains('O') || str.Contains('U'))) || (c == 'I' && (str.Contains('O') || str.Contains('U'))) || (c == 'O' && str.Contains('U')))
                        return false;
                    else
                        str += c;
                }
                return true;
            case 3:
                return clipboardText.Length > 7;
            case 4:
                var digraphs = new string[] { "CH", "NG", "SH", "TH", "PH", "WH" };
                return digraphs.Any(x => clipboardText.Contains(x));
            case 5:
                return bomb.GetSerialNumberLetters().Any(x => clipboardText.Contains(x));
            default:
                return false;
        }
    }

    private bool SubmissionCheck(char c, string clipboardText)
    {
        switch (c)
        {
            case 'A':
                return clipboardText == DateTime.Now.DayOfWeek.ToString().ToUpperInvariant();
            case 'B':
                return clipboardText == (DateTime.Now.Hour < 12 ? "MORNING" : "AFTERNOON");
            case 'C':
                var months = new string[] { "JANUARY", "FEBRUARY", "MARCH", "APRIL", "MAY", "JUNE", "JULY", "AUGUST", "SEPTEMBER", "OCTOBER", "NOVEMBER", "DECEMBER" };
                return clipboardText == months[DateTime.Now.Month - 1];
            case 'D':
                if (!bomb.GetSolvableModuleNames().Any(x => x != "Boomtar the Great"))
                    return clipboardText == "ALONE";
                else
                    return bomb.GetSolvableModuleNames().Select(x => x.ToUpperInvariant()).Select(x => new string(x.Where(xx => !char.IsWhiteSpace(xx)).ToArray())).Any(x => x == clipboardText);
            case 'E':
                return clipboardText == (bomb.GetModuleNames().Count % 2 == 0 ? "ORDERLY" : "STRANGE");
            case 'F':
                var count = bomb.GetBatteryCount() + bomb.GetBatteryHolderCount();
                var numberWords = new string[] { "ZERO", "ONE", "TWO", "THREE", "FOUR", "FIVE", "SIX", "SEVEN", "EIGHT", "NINE", "TEN", "ELEVEN", "TWELVE", "THIRTEEN", "FOURTEEN", "FIFTEEN", "SIXTEEN", "SEVENTEEN", "EIGHTEEN", "NINETEEN", "TWENTY" };
                if (count > 20)
                    return clipboardText == "TONS";
                else
                    return clipboardText == numberWords[count];
            default:
                return false;
        }
    }

    private IEnumerator Wait(string sound, float duration)
    {
        cantInteract = true;
        mainRef = audio.HandlePlaySoundAtTransformWithRef(sound, transform, false);
        yield return new WaitForSeconds(duration);
        cantInteract = false;
    }

    private IEnumerator DisableStuff()
    {
        yield return null;
        statusLight.SetActive(false);
        if (!Application.isEditor)
        {
            var backings = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name == "Foam");
            foreach (var backing in backings)
                Debug.LogFormat("<Boomtar the Great #{0}> {1}", moduleId, Vector3.Distance(backing.transform.position, transform.position));
            backings = backings.OrderBy(o => Vector3.Distance(o.transform.position, transform.position));
            foreach (var backing in backings.Take(5))
                Destroy(backing);
            //Destroy(Resources.FindObjectsOfTypeAll<GameObject>().Where(o => o.name == "Foam").OrderBy(o => Vector3.Distance(o.transform.position, transform.position)).First());
        }
    }

    // Twitch Plays
#pragma warning disable 414
    private readonly string TwitchHelpMessage = "!{0} <any text> [Query that text into Boomtar]";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string input)
    {
        yield return null;
        FocusModule(input);
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
        var answer = validWords.First(x => SubmissionCheck(table[rule1 * 6 + rule2], x));
        FocusModule(submitKeyword + " " + answer);
    }
}
