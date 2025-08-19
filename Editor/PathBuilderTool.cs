using System;
using System.Collections.Generic;
using System.Linq;
using PathBuilder;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

[EditorTool(displayName:"Path Builder", componentToolTarget: typeof(PathBuilder.Components.PathBuilder))]
public class PathBuilderTool : EditorTool
{
    int selectedIndex = -1;

    [Serializable]
    public class ControlPointEditData
    {
        public enum MovementMode
        {
            PlanarView,
            Translate,
            Rotate,
            Scale,
        }
        public MovementMode movementMode;
        public bool autoUpdateTangents = true;
        public float minVertexDistance = 0.2f;
    }
    public static ControlPointEditData currentControlPointEditData = new ControlPointEditData();
    private static Material profileMeshMaterial = null;

    private void EditObjectPositioning()
    {
        var tg = Target;
        Vector3 pos = Target.transform.position;
        if (tg.ControlPoints.Count > 0)
        {
            Color handleColor = Handles.color;
            Handles.color = Color.green;
            Handles.DrawDottedLine(tg.ControlPoints[^1].vertex.position, tg.transform.position, 5.0f);
            Handles.color = handleColor;
        }

        EditorGUI.BeginChangeCheck();
        pos = Handles.PositionHandle(pos, Target.transform.rotation);
        if (EditorGUI.EndChangeCheck())
        {
            Target.transform.position = pos;
        }
    }
    
    private void SelectControlPoint()
    {
        var tg = Target;
        var ControlPoints = tg.ControlPoints;
        Dictionary<int,int> controlIds = new Dictionary<int, int>();
        EditorGUI.BeginChangeCheck();
        for (int i = 0; i < ControlPoints.Count; i++)
        {
            ControlPoint point = ControlPoints[i];
            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            controlIds.Add(i, controlId);
            Handles.FreeMoveHandle(controlId,point.vertex.position, HandleUtility.GetHandleSize(point.vertex.position) * 0.05f, Vector3.zero, Handles.DotHandleCap);
        }
        EditorGUI.EndChangeCheck();
        foreach (KeyValuePair<int, int> keyValuePair in controlIds)
        {
            if (GUIUtility.hotControl == keyValuePair.Value)
            {
                selectedIndex = keyValuePair.Key;
            }
        }
    }
    
    private void EditControlPointVertex()
    {
        if (selectedIndex == -1) return;
        var tg = (PathBuilder.Components.PathBuilder)target;
        EditorGUI.BeginChangeCheck();
        if (!tg.GetControlPoint(selectedIndex, out var currentControlPoint))
        {
            return;
        }

        Vector3 pos = currentControlPoint.vertex.position;
        Quaternion quaternion = currentControlPoint.vertex.rotation;
        Vector3 scale = currentControlPoint.vertex.scale;
        switch (currentControlPointEditData.movementMode)
        {
            case ControlPointEditData.MovementMode.Translate:
                pos = Handles.PositionHandle(pos, quaternion);
                break;
            case ControlPointEditData.MovementMode.Rotate:
                quaternion = Handles.RotationHandle(quaternion, pos);
                break;
            case ControlPointEditData.MovementMode.Scale:
                scale = Handles.ScaleHandle(scale, pos, quaternion);
                break;
            case ControlPointEditData.MovementMode.PlanarView:
                pos = Handles.FreeMoveHandle(pos, HandleUtility.GetHandleSize(pos) * 0.1f, Vector3.zero, Handles.DotHandleCap);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (EditorGUI.EndChangeCheck())
        {
            currentControlPoint.vertex.position = pos;
            currentControlPoint.vertex.rotation = quaternion;
            currentControlPoint.vertex.scale = scale;
            
            tg.SetControlPoint(selectedIndex, currentControlPoint);
            tg.UpdateCurveGeometry(currentControlPointEditData.minVertexDistance);
            if (currentControlPointEditData.autoUpdateTangents)
            {
                tg.CalculateControlTangents();
            }
        }
    }

    private void EditControlPointTangent()
    {
        if (selectedIndex == -1) return;
        var tg = (PathBuilder.Components.PathBuilder)target;
        if (!tg.GetControlPoint(selectedIndex, out var currentControlPoint))
        {
            return;
        }
        EditorGUI.BeginChangeCheck();
        
        Vector3 tempLeft = currentControlPoint.vertex.TransformPoint(currentControlPoint.leftTangent.position);

        if (selectedIndex > 0)
        {
            tempLeft = Handles.FreeMoveHandle(tempLeft, HandleUtility.GetHandleSize(tempLeft) * 0.05f, Vector3.zero,
                Handles.DotHandleCap);
            Handles.DrawLine(currentControlPoint.vertex.position, tempLeft);
        }

        Vector3 tempRight = currentControlPoint.vertex.TransformPoint(currentControlPoint.rightTangent.position);

        if (selectedIndex < tg.ControlPoints.Count - 1)
        {
            tempRight = Handles.FreeMoveHandle(tempRight, HandleUtility.GetHandleSize(tempRight) * 0.05f, Vector3.zero,
                Handles.DotHandleCap);
            Handles.DrawLine(currentControlPoint.vertex.position, tempRight);
        }

        if (EditorGUI.EndChangeCheck())
        {
            currentControlPoint.leftTangent.position = currentControlPoint.vertex.InverseTransformPoint(tempLeft);
            currentControlPoint.rightTangent.position = currentControlPoint.vertex.InverseTransformPoint(tempRight);
            tg.SetControlPoint(selectedIndex,currentControlPoint);
            tg.UpdateCurveGeometry(currentControlPointEditData.minVertexDistance);
        }
    }

    private void DrawControlLine()
    {
        var tg = Target;
        var controlPoints = tg.ControlPoints;
        if (controlPoints.Count < 2) return;
        Color[] colors = new Color[controlPoints.Count];
        for (int i = 0; i < controlPoints.Count; i++)
        {
            colors[i] = Color.green;
        }
        Handles.DrawAAPolyLine(3.0f, colors, controlPoints.Select(cp => cp.vertex.position).ToArray());
    }

    private void DrawResultingPolyline()
    {
        var tg = Target;
        List<Vector3> vertices = tg.Vertices;
        Color[] colors = new Color[vertices.Count];
        Color[] backgroundColors = new Color[vertices.Count];
        for (int i = 0; i < vertices.Count; i++)
        {
            colors[i] = Color.Lerp(Color.cyan , Color.magenta, (float)i/(float)vertices.Count);
            backgroundColors[i] = Color.black;
        }
        Handles.DrawAAPolyLine(7.0f, backgroundColors, vertices.ToArray());
        Handles.DrawAAPolyLine(7.0f, backgroundColors, vertices.ToArray());
        Handles.DrawAAPolyLine(7.0f, backgroundColors, vertices.ToArray());
        Handles.DrawAAPolyLine(5.0f, colors, vertices.ToArray());
        Handles.DrawAAPolyLine(5.0f, colors, vertices.ToArray());
        Handles.DrawAAPolyLine(5.0f, colors, vertices.ToArray());
    }

    private void DrawControlPointAxes()
    {
        
    }

    private void DrawNormals()
    {
        var tg = Target;
        List<Vector3> vertices = tg.Vertices;
        List<Vector3> normals = tg.Normals;
        Color handlesColor = Handles.color;
        Handles.color = Color.red;
        for (int i = 0; i < vertices.Count; i++)
        {
            Handles.DrawLine(vertices[i], vertices[i] + normals[i] * 0.2f);
        }
        Handles.color = handlesColor;
    }

    private void DrawTangents()
    {
        var tg = Target;
        List<Vector3> vertices = tg.Vertices;
        List<Vector3> tangents = tg.Tangents;
        Color handlesColor = Handles.color;
        Handles.color = Color.blue;
        for (int i = 0; i < vertices.Count; i++)
        {
            Handles.DrawLine(vertices[i], vertices[i] + tangents[i] * 0.2f);
        }
        Handles.color = handlesColor;
    }
    
    private void DrawBinormals()
    {
        var tg = Target;
        List<Vector3> vertices = tg.Vertices;
        List<Vector3> tangents = tg.Tangents;
        List<Vector3> normals = tg.Normals;
        Color handlesColor = Handles.color;
        Handles.color = Color.green;
        for (int i = 0; i < vertices.Count; i++)
        {
            Vector3 binormal = Vector3.Cross(normals[i], tangents[i]);
            Handles.DrawLine(vertices[i], vertices[i] + binormal * 0.2f);
        }
        Handles.color = handlesColor;
    }

    private void DrawProfileMesh()
    {
        var tg = Target;
        if (tg.WorkingMesh == null) return;
        ProfileMeshMaterial.SetFloat("_RingCount", tg.Vertices.Count);
        ProfileMeshMaterial.SetFloat("_RingVertices", tg.ProfileMesh.vertexCount);
        Graphics.DrawMesh(tg.WorkingMesh, Matrix4x4.identity, ProfileMeshMaterial, tg.gameObject.layer);
    }
    
    public override void OnToolGUI(EditorWindow window)
    {
        base.OnToolGUI(window);
        DrawProfileMesh();
        //DrawControlLine();
        //DrawTangents();
        //DrawNormals();
        //DrawBinormals();
        DrawResultingPolyline();
        EditObjectPositioning();
        SelectControlPoint();
        EditControlPointVertex();
        EditControlPointTangent();
    }

    

    public PathBuilder.Components.PathBuilder Target => target as PathBuilder.Components.PathBuilder;

    public ControlPointEditData CurrentControlPointEditData => currentControlPointEditData;
    
    public void SetCurrentControlPointTangentMode(AnimationUtility.TangentMode mode)
    {
        if (Target.GetControlPoint(selectedIndex, out ControlPoint controlPoint))
        {
            controlPoint.tangentMode = mode;
            Target.ControlPoints[selectedIndex] = controlPoint;
        }
    }

    private static Material ProfileMeshMaterial
    {
        get
        {
            if (profileMeshMaterial == null)
            {
                profileMeshMaterial = new Material(Shader.Find("Hidden/ProfileMeshPreview"));
            }
            return profileMeshMaterial;
        }
    }
}
