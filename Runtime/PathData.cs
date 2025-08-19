using UnityEngine;

[CreateAssetMenu(fileName = "PathData", menuName = "Scriptable Objects/PathData")]
public class PathData : ScriptableObject
{
    [Header("General Curve Settings")]
    [SerializeField] private bool closedCurve;
    [SerializeField] private bool closedProfile;
    [SerializeField] private float minVertexDistance = 0.2f;
}
