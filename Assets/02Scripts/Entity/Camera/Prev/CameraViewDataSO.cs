using UnityEngine;

[CreateAssetMenu(fileName = "ViewData", menuName = "ScriptableObj/AlphaCamera/ViewData")]
public class CameraViewDataSO : ScriptableObject
{
    public ECameraViewType ViewType;
    public float PivotOffsetY;
    public float ShoulderOffsetX;
    public float ZoomMinDistance;
    public float ZoomMaxDistance;
    public float Angle;
    public float FOV;
}
