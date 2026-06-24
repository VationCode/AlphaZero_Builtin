using UnityEngine;

[CreateAssetMenu(fileName = "ViewDataSO", menuName = "ScriptableObj/ViewData")]
public class ViewDataSO : ScriptableObject
{
    public EViewType ViewType;
    public float PivotOffsetY;
    public float ShoulderOffsetX;
    public float ZoomMinDistance;
    public float ZoomMaxDistance;
    public float Angle;
    public float FOV;
}
