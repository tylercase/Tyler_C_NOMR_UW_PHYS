using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EMP.Core;
using UnityEngine.UIElements;
using System;

public class ParticleFieldLines : MonoBehaviour
{
    // This script is only for drawing the filed lines, controller script will push the vector values here.
    [SerializeField] private float maxMagnitude;
    
    public Vector3 startPos;
    public Vector3 fieldVector;
    private LineRenderer line;

    private void Start()
    {
        startPos = transform.position;
        
        // Set initial line conditions
        line = GetComponent<LineRenderer>();
        line.startWidth = 0.05f;
        line.endWidth = 0.01f;
        line.positionCount = 2;
    }

    public void DrawFieldLines(Vector3 fieldVector)
    {
        Color32 color = GetColor(fieldVector.magnitude);
        line.startColor = color;
        line.endColor = color;

        Vector3 endPos = startPos + fieldVector;
        line.SetPosition(0, startPos);
        line.SetPosition(1, endPos);
    }

    private Color32 GetColor(float magnitude)
    {
        // Smooth gradient from green do red depending on th strength of the field
        float limit = Mathf.Pow(maxMagnitude, 3);
        float r = magnitude > limit ? 255 : magnitude / limit * 255;
        float g = 255 - r;
        return new Color32((byte)r,(byte)g,0,255);
    }
}
