using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "Create Game Configuration", menuName = "Game Config/Configuration File")]
public class GameConfiguration : ScriptableObject
{
    public string gameName;
    public string defaultTeam1Name;
    public string defaultTeam2Name;
    public List<PickBanOrder> pickBanOrder = new List<PickBanOrder>();
    public List<iconToCompare> playerIconsForGame = new List<iconToCompare>();

    public GameConfiguration(string _gameName, List<PickBanOrder> _pickBanOrder, List<iconToCompare> _playerIconsForGame)
    {
        gameName = _gameName;
        pickBanOrder = _pickBanOrder;
        playerIconsForGame = _playerIconsForGame;
    }
}

[System.Serializable]
[CreateAssetMenu(fileName = "Create Icon Position", menuName = "Game Config/Icon Position")]
public class IconPosition : ScriptableObject
{
    public int x;
    public int y;
    public int height;
    public int width;

    public IconPosition(int _x, int _y, int _height, int _width)
    {
        x = _x;
        y = _y;
        height = _height;
        width = _width;
    }
}




[System.Serializable]
public struct PickBanOrder
{
    public bool isTeam1;
    public bool isBan;
    public bool canBeHovered;
    public IconPosition iconPosition;
}
