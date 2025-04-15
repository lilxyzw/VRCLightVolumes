using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightVolume : MonoBehaviour {

    public VolumeType Type;

    public Texture3D Texture1;
    public Texture3D Texture2;
    public Texture3D Texture3;

    // 1x1x1 cube representation
    private readonly Vector3[] _cubeEdges = new Vector3[] {
        new Vector3(0.5f, 0.5f, 0.5f),    new Vector3(-0.5f, 0.5f, 0.5f),
        new Vector3(-0.5f, 0.5f, 0.5f),   new Vector3(-0.5f, -0.5f, 0.5f),
        new Vector3(-0.5f, -0.5f, 0.5f),  new Vector3(0.5f, -0.5f, 0.5f),
        new Vector3(0.5f, -0.5f, 0.5f),   new Vector3(0.5f, 0.5f, 0.5f),
        new Vector3(0.5f, 0.5f, -0.5f),   new Vector3(-0.5f, 0.5f, -0.5f),
        new Vector3(-0.5f, 0.5f, -0.5f),  new Vector3(-0.5f, -0.5f, -0.5f),
        new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f),
        new Vector3(0.5f, -0.5f, -0.5f),  new Vector3(0.5f, 0.5f, -0.5f),
        new Vector3(0.5f, 0.5f, 0.5f),    new Vector3(0.5f, 0.5f, -0.5f),
        new Vector3(-0.5f, 0.5f, 0.5f),   new Vector3(-0.5f, 0.5f, -0.5f),
        new Vector3(-0.5f, -0.5f, 0.5f),  new Vector3(-0.5f, -0.5f, -0.5f),
        new Vector3(0.5f, -0.5f, 0.5f),   new Vector3(0.5f, -0.5f, -0.5f)
    };

    

    private void OnDrawGizmos() {

        if (Type == VolumeType.Static) {
            Gizmos.DrawWireCube(transform.position, transform.lossyScale);
        } else {
            for (int i = 0; i < _cubeEdges.Length; i += 2) {
                Vector3 A = UniformTransformPoint(_cubeEdges[i]);
                Vector3 B = UniformTransformPoint(_cubeEdges[i + 1]);
                Gizmos.DrawLine(A, B);
            }
        }

    }

    // Transforms points from local to world space without skewing
    public Vector3 UniformTransformPoint(Vector3 point) {
        return transform.rotation * Vector3.Scale(point, transform.lossyScale) + transform.position;
    }

    public enum VolumeType {
        Static,
        RotateAroundY,
        Dynamic
    }

}
