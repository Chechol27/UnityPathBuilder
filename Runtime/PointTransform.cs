using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PathBuilder
{
    [Serializable]
    public struct PointTransform
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
		public bool dirty;
        
        [HideInInspector]public List<PointTransform> children;

        public static implicit operator PointTransform(Transform transform)
        {
            PointTransform[] childrenTransforms = new PointTransform[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
            {
                childrenTransforms[i] = transform.GetChild(i);
            }

            return new PointTransform
            {
                position =  transform.position,
                rotation =  transform.rotation,
                scale =  transform.localScale,
                dirty = false,
                children = childrenTransforms.ToList()
            };
        }

        public static PointTransform Zero()
        {
            return new PointTransform()
            {
                position = Vector3.zero,
                rotation = Quaternion.identity,
                scale = Vector3.one
            };
        }

        public Vector3 TransformPoint(Vector3 localSpacePosition)
        {
            Matrix4x4 trs = Matrix4x4.TRS(position, rotation, scale);
            Vector3 transformed = trs.MultiplyPoint(localSpacePosition);

            return transformed;
        }

        public Vector3 InverseTransformPoint(Vector3 worldSpacePosition)
        {
            Matrix4x4 itrs = Matrix4x4.TRS(position, rotation, scale).inverse;
            return itrs.MultiplyPoint(worldSpacePosition);
        }
    }
}