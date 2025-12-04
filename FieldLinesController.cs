using System;
using System.Collections;
using System.Collections.Generic;
using EMP.Core;
using UnityEngine;

public class FieldLinesController : MonoBehaviour
{
    // Calculates the field value and sends info to field lines to plot.
    [SerializeField] private bool averageField;
    [SerializeField] private float sampleRate;
    [SerializeField] private float magnitudeScale;
    [SerializeField] private InteractionField interactionField;
    
    private ParticleFieldLines[] measuringPoints;
    private float elapsedTime;
    //private Vector3 fieldVector;
    private int idx;

    private void Start()
    {
        measuringPoints = GetComponentsInChildren<ParticleFieldLines>();
    }

    void Update()
    {
        switch (averageField)
        {
            case true:
                CalculateAverageField();
                break;
            case false:
                CalculateField();
                break;
        }
    }

    void CalculateAverageField()
    {
        // Placing field lines in the center of the wire may require this script to run.
        // This is because field lines will be heavily influenced by electrons zooming past,
        // by averaging we can try to negate this effect.
        
        elapsedTime += Time.deltaTime;
        idx += 1;
        foreach (var point in measuringPoints)
        {
            // Add together total field vector for each frame while tracking how many frames we included.
            point.fieldVector += EMController.Instance.GetFieldValue(point.startPos, interactionField);
            if (elapsedTime >= sampleRate)
            {
                // Divide total field value by number of frames.
                point.fieldVector /= idx;
                point.DrawFieldLines(point.fieldVector * magnitudeScale);

                // Reset values.
                elapsedTime = 0;
                point.fieldVector = Vector3.zero;
                idx = 0;
            }
        }
        
    }

    void CalculateField()
    {
        // Calculates field strength every frame.
        foreach (var point in measuringPoints)
        {
            point.fieldVector = EMController.Instance.GetFieldValue(point.startPos, interactionField);
            point.DrawFieldLines(point.fieldVector * magnitudeScale);
        }
    }
}
