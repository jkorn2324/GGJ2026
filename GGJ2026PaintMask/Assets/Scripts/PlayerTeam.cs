using UnityEngine;

[CreateAssetMenu(fileName = "PlayerTeam", menuName = "Scriptable Objects/PlayerTeam")]
public class PlayerTeam : ScriptableObject
{
    private int teamNum;
    public string teamName;
    public float timeLimit;
}
