using System;
using System.Collections.Generic;
using UnityEngine;

namespace PathBuilder.Components
{
    public class PathBuilder : MonoBehaviour
    {
        private const float CONTROL_DISTANCE = .4f;
        
        MeshFilter meshFilter;
        MeshRenderer meshRenderer;
        [SerializeField] private Mesh profileMesh;
        [SerializeField][HideInInspector] private List<ControlPoint> controlPoints = new List<ControlPoint>();
        [SerializeField][HideInInspector] private List<Vector3> vertices = new List<Vector3>();
        [SerializeField][HideInInspector] private List<Vector3> normals = new List<Vector3>();
        [SerializeField][HideInInspector] private List<Vector3> tangents = new List<Vector3>();

        [SerializeField] private AnimationCurve taper;
        
        [SerializeField] private Mesh workingMesh;

        private void InterpolateVertices(float minVertexDistance)
        {
            vertices.Clear();
            if (controlPoints.Count < 2) return;
            for (int i = 1; i < controlPoints.Count; i++)
            {
                ControlPoint current = controlPoints[i];
                ControlPoint previous = controlPoints[i - 1];
                float dist;
                float vertexDistance = Vector3.Distance(current.vertex.position, previous.vertex.position);
                for (dist = 0;
                     dist < vertexDistance;
                     dist += minVertexDistance)
                {
                    float t = dist / vertexDistance;
                    Vector3 a = Vector3.Lerp(previous.vertex.position,
                        previous.vertex.TransformPoint(previous.rightTangent.position), t);
                    Vector3 b = Vector3.Lerp(previous.vertex.TransformPoint(previous.rightTangent.position),
                        current.vertex.TransformPoint(current.leftTangent.position), t);
                    Vector3 c = Vector3.Lerp(current.vertex.TransformPoint(current.leftTangent.position),
                        current.vertex.position, t);
                    Vector3 d = Vector3.Lerp(a, b, t);
                    Vector3 e = Vector3.Lerp(b, c, t);
                    Vector3 p = Vector3.Lerp(d, e, t);
                    vertices.Add(p);
                }
            }
            vertices.Add(controlPoints[^1].vertex.position);
        }
        
        private void CalculateTangents()
        {
            tangents.Clear();
            if(vertices.Count < 2) return;
            tangents.Add((vertices[1] - vertices[0]).normalized);
            for (int i = 1; i < vertices.Count - 1; i++)
            {
                Vector3 tangent = vertices[i+1] - vertices[i-1];
                tangents.Add(tangent.normalized);
            }
            tangents.Add((vertices[^1] - vertices[^2]).normalized);
        }

        private void CalculateNormalsRMF()
        {
            normals.Clear();
            if(vertices.Count < 2) return;
            normals.Add((tangents[1] - tangents[0]).normalized);
            for (int i = 0; i < tangents.Count-1; i++)
            {
                Vector3 v1 = vertices[i+1] - vertices[i];
                float c1 = Vector3.Dot(v1, v1);
                Vector3 rL = normals[i] - (2 / c1) * Vector3.Dot(v1, normals[i]) * v1;
                Vector3 tl = tangents[i] - (2 / c1) * Vector3.Dot(v1, tangents[i]) * v1;
                
                Vector3 v2 = tangents[i+1] - tl;
                float c2 = Vector3.Dot(v2, v2);
                
                Vector3 ri1 = rL - (2 / c2) * Vector3.Dot(v2, rL) * v2;
                
                normals.Add(ri1.normalized);
            }
        }

        private void ExtrudeProfileMesh()
        {
            if (profileMesh == null) return;
            workingMesh = workingMesh == null ? new Mesh() : workingMesh;
            workingMesh.Clear();
            List<Vector3> meshVertices = new List<Vector3>();
            List<Vector3> meshNormals = new List<Vector3>();
            List <Vector2> meshUvs = new List<Vector2>();
            List<int> meshIndices = new List<int>();
            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 position = vertices[i];
                Vector3 tangent = tangents[i];
                Vector3 normal = normals[i];
                Vector3 binormal = Vector3.Cross(tangent, normal);
                Matrix4x4 matrix = new Matrix4x4();
                matrix.SetColumn(0,normal);
                matrix.SetColumn(1, binormal);
                matrix.SetColumn(2, tangent);
                matrix.SetColumn(3, position);
                matrix.SetRow(3, new Vector4(0, 0, 0, 1));
                float taperValue = taper.Evaluate((float)i / (float)vertices.Count);
                matrix *= Matrix4x4.Scale(Vector3.one * taperValue);

                for (int vertexId = 0; vertexId < profileMesh.vertexCount; vertexId++)
                {
                    meshVertices.Add(matrix.MultiplyPoint(profileMesh.vertices[vertexId]));
                    meshUvs.Add(new Vector2((float)i / (float)vertices.Count, (float)vertexId / (float)profileMesh.vertexCount));
                }
                
                meshVertices.Add(matrix.MultiplyPoint(profileMesh.vertices[0]));
                meshUvs.Add(new Vector2((float)i / (float)vertices.Count, 1));
            }

            for (int controlVertexId = 0; controlVertexId < vertices.Count - 1; controlVertexId++)
            {
                for (int meshVertexId = 0; meshVertexId < profileMesh.vertexCount; meshVertexId++)
                {
                    int flatVertexId = meshVertexId + controlVertexId * (profileMesh.vertexCount + 1);
                    int nextProfileVertexId = meshVertexId + (controlVertexId + 1) * (profileMesh.vertexCount + 1); 
                    int nexProfileVertexIdPlusOne = nextProfileVertexId + 1;
                    int flatVertexIdPlusOne = flatVertexId + 1;
                    
                    meshIndices.Add(nexProfileVertexIdPlusOne);
                    meshIndices.Add(nextProfileVertexId);
                    meshIndices.Add(flatVertexId);
                    
                    meshIndices.Add(flatVertexIdPlusOne);
                    meshIndices.Add(nexProfileVertexIdPlusOne);
                    meshIndices.Add(flatVertexId);
                }
            }
            
            workingMesh.vertices = meshVertices.ToArray();
            workingMesh.triangles = meshIndices.ToArray();
            workingMesh.uv = meshUvs.ToArray();
            workingMesh.RecalculateNormals();
            workingMesh.RecalculateTangents();
        }

        public void UpdateCurveGeometry(float minVertexDistance)
        {
            InterpolateVertices(minVertexDistance);
            CalculateTangents();
            CalculateNormalsRMF();
            ExtrudeProfileMesh();
        }

        public void CalculateControlTangents()
        {
            if (controlPoints.Count < 3) return;

            ControlPoint SetTangent(int pointIndex, bool leftOrRight, Vector3 tangentPosition)
            {
                ControlPoint current = controlPoints[pointIndex];
                PointTransform vertex = current.vertex;
                if (leftOrRight)
                {
                    current.leftTangent.position = tangentPosition;
                }
                else
                {
                    current.rightTangent.position = tangentPosition;
                }
                
                return current;
            }
            
            ControlPoint tempControlPoint = controlPoints[0];
            PointTransform vertex = tempControlPoint.vertex;
            Vector3 tangent = controlPoints[1].vertex.position - vertex.position;
            tangent = vertex.InverseTransformPoint(vertex.position + tangent.normalized * tangent.magnitude * CONTROL_DISTANCE);
            tempControlPoint = SetTangent(0, true, tangent);
            SetControlPoint(0, tempControlPoint);
            
            for (int i = 1; i < controlPoints.Count - 1; i++)
            {
                tempControlPoint = controlPoints[i];
                ControlPoint previous = controlPoints[i - 1];
                ControlPoint next = controlPoints[i + 1];
                Vector3 closingEdge = previous.vertex.position - next.vertex.position;
                float previousEdgeLength = Vector3.Distance(previous.vertex.position, tempControlPoint.vertex.position);
                float nextEdgeLength = Vector3.Distance(next.vertex.position, tempControlPoint.vertex.position);
                Vector3 leftTangentPos = tempControlPoint.vertex.position + closingEdge.normalized * previousEdgeLength * CONTROL_DISTANCE; 
                Vector3 rightTangentPos = tempControlPoint.vertex.position - closingEdge.normalized * nextEdgeLength * CONTROL_DISTANCE;
                tempControlPoint.leftTangent.position = tempControlPoint.vertex.InverseTransformPoint(leftTangentPos);
                tempControlPoint.rightTangent.position = tempControlPoint.vertex.InverseTransformPoint(rightTangentPos);
                SetControlPoint(i, tempControlPoint);
            }
            
            tempControlPoint = controlPoints[0];
            vertex = tempControlPoint.vertex;
            tangent =  controlPoints[1].vertex.TransformPoint(controlPoints[1].leftTangent.position) - vertex.position;
            tangent = vertex.InverseTransformPoint(vertex.position + tangent.normalized * controlPoints[1].leftTangent.position.magnitude);
            tempControlPoint = SetTangent(0, true, tangent);
            SetControlPoint(0, tempControlPoint);
            
            tempControlPoint = controlPoints[^1];
            vertex = tempControlPoint.vertex;
            tangent = controlPoints[^2].vertex.TransformPoint(controlPoints[^2].rightTangent.position) - vertex.position;
            tangent = vertex.InverseTransformPoint(vertex.position + tangent.normalized * controlPoints[^2].rightTangent.position.magnitude);
            tempControlPoint = SetTangent(controlPoints.Count - 1, true, tangent);
            SetControlPoint(controlPoints.Count - 1, tempControlPoint);
            
        }
        
        public void RecordControlPoint()
        {
            ControlPoint controlPoint = new ControlPoint
            {
                vertex = transform,
                rightTangent = PointTransform.Zero(),
                leftTangent = PointTransform.Zero()
            };
            controlPoint.vertex.children = new List<PointTransform>();
            controlPoint.rightTangent.position = Vector3.forward;
            controlPoint.leftTangent.position = -Vector3.forward;
            controlPoint.vertex.children.Add(controlPoint.rightTangent);
            controlPoint.vertex.children.Add(controlPoint.leftTangent);
            
            controlPoints.Add(controlPoint);

            CalculateControlTangents();
        }

        public bool GetControlPoint(int id, out ControlPoint controlPoint)
        {
            try
            {
                controlPoint = controlPoints[id];
                return true;
            }
            catch (ArgumentOutOfRangeException e)
            {
                controlPoint = default;
                return false;
            }
        }

        public void Clear()
        {
            ControlPoints.Clear();
            vertices.Clear();
            normals.Clear();
            tangents.Clear();
            WorkingMesh?.Clear();
        }

        public List<ControlPoint> ControlPoints => controlPoints;
        public List<Vector3> Vertices => vertices;
        public List<Vector3> Tangents => tangents;
        public List<Vector3> Normals => normals;

        public Mesh ProfileMesh => profileMesh;

        public Mesh WorkingMesh => workingMesh;

        public void SetControlPoint(int selectedIndex, ControlPoint currentControlPoint)
        {
            controlPoints[selectedIndex] = currentControlPoint;
        }
    }
}