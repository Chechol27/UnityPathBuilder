using System;
using UnityEditor;
using UnityEngine;

namespace PathBuilder
{
    [CustomEditor(typeof(Components.PathBuilder))]
    public class PathBuilderInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            //Draw control point inspectors
        }
    }
}