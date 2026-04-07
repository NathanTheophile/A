using PurrNet;
using System.Collections.Generic;
public class Room
{
    public int id;
    public SceneID SceneID;
    public List<PlayerID> players = new();

    public int maxPlayer = 2;

    public bool isPlaying = false;
    public bool isFull => players.Count > maxPlayer;
    public bool isEmpty => players.Count <= 0;

}
