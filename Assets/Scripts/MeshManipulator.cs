using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshManipulator : MonoBehaviour
{
    [SerializeField] private MeshFilter m_filter;
    [SerializeField] private float m_sphereRadius = 1.0f;
    [SerializeField, Range(0, 1)] private float m_lerpT = 1.0f;

    [NonSerialized] private Mesh m_mesh;
    [NonSerialized] private Mesh m_savedMesh = null;
    [NonSerialized] Vector3[] m_previousVertices;
    [NonSerialized] Vector3[] m_savedVertices;

    [NonSerialized] private float m_previousLerpT = 1.0f;

    public void MakeCubeIntoShere(float t)
    {
        Initialize();
        for (int i = 0; i < m_previousVertices.Length; i++)
        {
            m_previousVertices[i] = Vector3.Lerp(m_savedVertices[i], m_previousVertices[i].normalized * m_sphereRadius, t);
        }
        m_mesh.vertices = m_previousVertices;
        m_mesh.UploadMeshData(false);
    }

    public void ResetMesh()
    {
        ResetMeshFilter();
    }

    private void OnDestroy()
    {
        Destroy(m_mesh);
        ResetMeshFilter();
    }

    private void Update()
    {

        MakeCubeIntoShere(Mathf.Abs(Time.realtimeSinceStartup % 2 - 1));
        if (m_previousLerpT != m_lerpT)
        {
            m_previousLerpT = m_lerpT;
        }
    }

    private void Initialize()
    {
        if (m_savedMesh == null)
        {
            m_savedMesh = m_filter.mesh;
            m_mesh = DuplicateMesh(m_savedMesh);
            m_savedVertices = m_savedMesh.vertices;
            m_previousVertices = m_mesh.vertices;
        }
        m_filter.mesh = m_mesh;
    }

    private void ResetMeshFilter()
    {
        m_filter.mesh = m_savedMesh;
    }



    private Mesh DuplicateMesh(Mesh meshToCopy)
    {
        Mesh copy = new Mesh();
        copy.vertices = meshToCopy.vertices;
        copy.triangles = meshToCopy.triangles;
        copy.uv = meshToCopy.uv;
        copy.uv2 = meshToCopy.uv2;
        copy.uv3 = meshToCopy.uv3;
        copy.uv4 = meshToCopy.uv4;
        copy.normals = meshToCopy.normals;
        copy.colors = meshToCopy.colors;

        return copy;
    }
}
