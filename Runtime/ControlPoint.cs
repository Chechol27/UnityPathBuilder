using System;
using UnityEditor;
using UnityEngine;

namespace PathBuilder
{
    [Serializable]
    public struct ControlPoint
    {
        public AnimationUtility.TangentMode tangentMode;
        public PointTransform vertex;
        public PointTransform leftTangent;
        public PointTransform rightTangent;
    }
}