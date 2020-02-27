using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TeamManager : MonoBehaviour
{
    public GameConfiguration gameConfig;
    public List<iconToCompare> team1Bans = new List<iconToCompare>();
    public List<iconToCompare> team1Comp = new List<iconToCompare>();
    public List<iconToCompare> team2Bans = new List<iconToCompare>();
    public List<iconToCompare> team2Comp = new List<iconToCompare>();
    public TextMeshProUGUI team1;
    public TextMeshProUGUI team2;

    public MeshRenderer _renderer;

    [SerializeField] uDesktopDuplication.Texture uddTexture;
    [SerializeField] int x = 0;
    [SerializeField] int y = 0;
    [SerializeField] int w = 1920;
    [SerializeField] int h = 1080;

    public Texture2D texture;
    Color32[] colors;

    IconPosition iconToAnalyse = null;
    int iconListCounter = 0;
    [SerializeField] float analysePatternTimer = 5f;

    [SerializeField]
    PatternMatching patternMatcher;

    ScreenCaptureManager screenCapture;
    private static TeamManager _instance;

    public static TeamManager Instance { get { return _instance; } }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }

    }

    void CreateTextureIfNeeded()
    {
        if (!texture || texture.width != w || texture.height != h)
        {
            colors = new Color32[w * h];
            texture = new Texture2D(w, h, TextureFormat.ARGB32, false);
        }
    }


    void Start()
    {
        CreateTextureIfNeeded();
        iconListCounter = 0;
        screenCapture = ScreenCaptureManager.Instance;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            team1Comp.Clear();
            team2Comp.Clear();
            team1.text = gameConfig.defaultTeam1Name;
            team2.text = gameConfig.defaultTeam2Name;
        }

        CreateTextureIfNeeded();

        // must be called (performance will be slightly down).
        uDesktopDuplication.Manager.primary.useGetPixels = true;
        var monitor = uddTexture.monitor;
        if (!monitor.hasBeenUpdated) return;

        if (iconToAnalyse != null) return;

        if (gameConfig.pickBanOrder.Count > 0)
        {
            iconToAnalyse = gameConfig.pickBanOrder[iconListCounter].iconPosition;
            Invoke("AnalyseIcon", 1f);
        }
    }


    public void AnalyseIcon()
    {
        // FLOW

        /* Analyse icon
         * Check if icon is not default icon
         * Check if icon is B&W
         * Check if icon is champion
         */

        var iconToCheck = screenCapture.GetIconIn1080Screen(iconToAnalyse);
        var matchedIcon = patternMatcher.AnalysePatternMultiThreaded(iconToAnalyse, iconToCheck);
        if (gameConfig.pickBanOrder[iconListCounter].canBeHovered)
        {
            if (matchedIcon.name == "Default")
            {
                Debug.Log("Detected default icon: Waiting 1s");
                Invoke("AnalyseIcon", 1f);
            }
            else
            {
                if (isIconBW(iconToCheck))
                {
                    Debug.Log("Hovered champion: " + matchedIcon.name);
                    Invoke("AnalyseIcon", 1f);
                }
                else
                {
                    if (gameConfig.pickBanOrder[iconListCounter].isTeam1)
                    {
                        team1Comp.Add(matchedIcon);
                        Debug.Log("Team 1 Selected Champion: " + matchedIcon.name);
                    }
                    else
                    {
                        team2Comp.Add(matchedIcon);
                        Debug.Log("Team 2 Selected Champion: " + matchedIcon.name);
                    }

                    if (iconListCounter < gameConfig.pickBanOrder.Count)
                    {
                        iconListCounter++;
                        Invoke("AnalyseIcon", 1f);
                    }
                }
            }


        }

    }

    public bool isIconBW(Color32[] icon)
    {
        var isRGB = false;
        foreach (var color in icon)
        {
            if (color.r != color.b || color.b != color.g)
            {
                isRGB = true;
            }
        }

        return isRGB;
    }

    //public void TestUpdate()
    //{

    //    var monitor = uddTexture.monitor;
    //    if (!monitor.hasBeenUpdated) return;

    //    if (gameConfig.team1IconPositions.Count > 0)
    //    {
    //        if ((team1Comp.Count >= gameConfig.team1IconPositions.Count) && (team2Comp.Count >= gameConfig.team2IconPositions.Count)) return;
    //        foreach (var team1Icon in gameConfig.team1IconPositions)
    //        {
    //            if (monitor.GetPixels(colors, team1Icon.x, team1Icon.y, team1Icon.width, team1Icon.height))
    //            {
    //                var championPicked = patternMatcher.AnalysePattern(team1Icon, colors);
    //                team1Comp.Add(championPicked);
    //                team1.text = team1.text + championPicked.name + ", ";
    //            }
    //        }

    //        foreach (var team2Icon in gameConfig.team2IconPositions)
    //        {
    //            if (monitor.GetPixels(colors, team2Icon.x, team2Icon.y, team2Icon.width, team2Icon.height))
    //            {
    //                var championPicked = patternMatcher.AnalysePattern(team2Icon, colors);
    //                team2Comp.Add(championPicked);
    //                team2.text = team2.text + championPicked.name + ", ";
    //            }
    //        }
    //    }
    //}

    public List<iconToCompare> GetIcons()
    {
        return gameConfig.playerIconsForGame;
    }
}

[Serializable]
public struct iconPositionAndSize
{
    public int x;
    public int y;
    public int height;
    public int width;
    public TextMeshProUGUI onscreenText;

    public iconPositionAndSize(int _x, int _y, int _height, int _width, TextMeshProUGUI _onscreenText)
    {
        x = _x;
        y = _y;
        height = _height;
        width = _width;
        onscreenText = _onscreenText;
    }
}

[Serializable]
public struct iconToCompare
{
    public string name;

    public Color32[] pixels;

    public int width;

    public int height;
}