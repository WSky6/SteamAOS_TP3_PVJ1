using UnityEngine;

[CreateAssetMenu(fileName = "NewHint", menuName = "Tutorial/Hint")]
public class TutorialHint : ScriptableObject
{
    [TextArea(3,6)]
    public string hintText;
}