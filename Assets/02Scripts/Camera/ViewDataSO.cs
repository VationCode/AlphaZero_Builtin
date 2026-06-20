using UnityEngine;

[CreateAssetMenu(fileName = "ViewDataSO", menuName = "ScriptableObj/ViewData")]
public class ViewDataSO : ScriptableObject
{
    public float RigOffsetY;
    public Vector3 PivotOffset;
    public Vector3 Angle;
    public float FOV;
}
