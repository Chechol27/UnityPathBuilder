using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

[Overlay(typeof(SceneView), "3D Path Builder")]
public class PathBuilderOverlay : Overlay
{
    void AddControlPointCall()
    {
        GameObject go = Selection.activeGameObject;
        if (go == null) return;
        if (go.TryGetComponent(out PathBuilder.Components.PathBuilder pathBuilder))
        {
            pathBuilder.RecordControlPoint();
            pathBuilder.UpdateCurveGeometry(PathBuilderTool.currentControlPointEditData.minVertexDistance);
        }
    }

    void ClearCall()
    {
        GameObject go = Selection.activeGameObject;
        if (go == null) return;
        if (go.TryGetComponent(out PathBuilder.Components.PathBuilder pathBuilder))
        {
            pathBuilder.Clear();
        }
    }

	void RecalculateControlTangentsCall()
    {
        GameObject go = Selection.activeGameObject;
        if (go == null) return;
        if (go.TryGetComponent(out PathBuilder.Components.PathBuilder pathBuilder))
        {
            pathBuilder.CalculateControlTangents();
            pathBuilder.UpdateCurveGeometry(PathBuilderTool.currentControlPointEditData.minVertexDistance);
        }
	}

    void SetMovementMode(ChangeEvent<System.Enum> evt)
    {
        PathBuilderTool.ControlPointEditData.MovementMode mode =
            (PathBuilderTool.ControlPointEditData.MovementMode)evt.newValue;
        PathBuilderTool.currentControlPointEditData.movementMode = mode;
    }

    // void SetTangentMode(ChangeEvent<System.Enum> evt)
    // {
    //     AnimationUtility.TangentMode mode =
    //         (AnimationUtility.TangentMode)evt.newValue;
    //     PathBuilderTool. = mode;
    // }

    void SetAutoCalcTangents(ChangeEvent<bool> evt)
    {
        PathBuilderTool.currentControlPointEditData.autoUpdateTangents = evt.newValue;
    }
    
    void SetPointResolution(ChangeEvent<float> evt)
    {
        float newValue = Mathf.Max(evt.newValue, 0.05f);
        PathBuilderTool.currentControlPointEditData.minVertexDistance = newValue;
        GameObject go = Selection.activeGameObject;
        if (go == null) return;
        if (go.TryGetComponent(out PathBuilder.Components.PathBuilder pathBuilder))
        {
            pathBuilder.UpdateCurveGeometry(newValue);
        }
    }

    void CheckSelectedTool(VisualElement target)
    {
        if (target == null) return;
        target.SetEnabled(ToolManager.activeToolType == typeof(PathBuilderTool));
        target.style.display = ToolManager.activeToolType == typeof(PathBuilderTool) ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public override VisualElement CreatePanelContent()
    {
        var root = new VisualElement(){name = "MainPanel"};
        
        var mainPanel = new VisualElement(){name = "MainPanel"};
        root.schedule.Execute(() => CheckSelectedTool(mainPanel)).Every(1000);
        
        root.Add(mainPanel);
        
        var addPointButton = new Button() { text = "Add Control Point" };
        addPointButton.clicked += AddControlPointCall;
        
        var clearButton = new Button() { text = "Clear" };
        clearButton.clicked += ClearCall;
        
        var calculateControlTangentsButton = new Button() { text = "Calculate Control Tangents" };
        calculateControlTangentsButton.clicked += RecalculateControlTangentsCall;
        
        var curveOptionsFoldout = new Foldout(){text = "Curve Controls"};
        
        mainPanel.Add(curveOptionsFoldout);
        curveOptionsFoldout.Add(addPointButton);
        curveOptionsFoldout.Add(calculateControlTangentsButton);
        curveOptionsFoldout.Add(clearButton);

        var pointResolutionFloat = new FloatField() { label = "Min Vertex Distance" , value = PathBuilderTool.currentControlPointEditData.minVertexDistance};
        pointResolutionFloat.RegisterValueChangedCallback(SetPointResolution);
        
        curveOptionsFoldout.Add(pointResolutionFloat);
        
        
        var controlPointMovementModeEnum = new EnumField("Control Point Mode:", PathBuilderTool.ControlPointEditData.MovementMode.PlanarView);
        controlPointMovementModeEnum.RegisterValueChangedCallback(SetMovementMode);
        
        var controlPointOptionsFoldout = new Foldout(){text = "Control Point Controls"};
        mainPanel.Add(controlPointOptionsFoldout);
        controlPointOptionsFoldout.Add(controlPointMovementModeEnum);
        
        var tangentOptionsFoldout = new Foldout(){text = "Tangent Controls"};
        controlPointOptionsFoldout.Add(tangentOptionsFoldout);
        
        // var tangentModeEnum = new EnumField("Tangent Mode:", AnimationUtility.TangentMode.Auto);
        // tangentModeEnum.RegisterValueChangedCallback(SetTangentMode);
        // tangentOptionsFoldout.Add(tangentModeEnum);
        
        var calculateTangentToggle = new Toggle(){text = "Auto calculate control tangents", value = PathBuilderTool.currentControlPointEditData.autoUpdateTangents};
        calculateTangentToggle.RegisterValueChangedCallback(SetAutoCalcTangents);
        
        tangentOptionsFoldout.Add(calculateTangentToggle);
        
        return root;
    }
}
