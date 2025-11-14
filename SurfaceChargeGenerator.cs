using System.Collections;
using System.Collections.Generic;
using EMP.Core;
using UnityEngine;

//TODO We should make an abstract class with methods like GetWireDimensions
public class SurfaceChargeGenerator : MonoBehaviour
{
    [SerializeField] private SurfaceChargeGradient surfaceChargeType;
    [SerializeField] private GameObject latticeShell;
    [SerializeField] private GameObject surfaceChargePrefab;
    [SerializeField] private GameObject surfaceChargeParent;
    [SerializeField] private float maxCharge;
    [Range(3, 40)]
    [SerializeField] private int numSurfaceChargeRings;
    [Range(4, 20)]
    [SerializeField] private int numSurfaceChargeRows;

    [SerializeField] private GameObject groundNegative;
    [SerializeField] private GameObject groundPositive;
    [SerializeField] private bool toggleAc;
    [SerializeField] private float acFrequency;

    [SerializeField] private Material negativeMaterial;
    [SerializeField] private Material positiveMaterial;
    [SerializeField] private Material invisibleMaterial;
    [SerializeField] private bool showSurfaceCharges;
    [SerializeField] private float gradientSlope;
    [SerializeField] private int voltage;
    [Range(0.01f, 2f)]
    [SerializeField] private float coneRadiusMultiplier;

    private Vector3 startPos;
    private Vector3 offset;
    private Vector3 ringPos;

    private List<Vector3> spawnPositions;
    private List<float> chargeValues;
    private List<Material> particleColor;
    
    private float ionCharge;
    private float latticeWidth;
    private float latticeLength;
    private float currentWidth;
    private float currentLength;
    private int currentNumSurfaceChargeRows;
    private int currentNumSurfaceChargeRings;
    private float currentGradient;
    private int currentVoltage;
    private bool currentShowingCharges;
    private float currentConeRadius;

    private float elapsedTime;

    public enum SurfaceChargeGradient
    {
        EvenSpacingLinearGradient,
        UnevenSpacingLinearGradient
    }
    
    private void Start()
    {
        startPos = transform.position;
        GetWireDimensions();
    }

    void Update()
    {
        GetWireDimensions();
        
        if (currentLength != latticeLength || 
            currentWidth != latticeWidth || 
            currentNumSurfaceChargeRows != numSurfaceChargeRows ||
            currentGradient != gradientSlope ||
            currentVoltage != voltage ||
            currentShowingCharges != showSurfaceCharges ||
            currentConeRadius != coneRadiusMultiplier)
        {
            foreach (Transform child in surfaceChargeParent.transform)
            {
                Destroy(child.gameObject);
            }

            switch (surfaceChargeType)
            {
                case SurfaceChargeGradient.EvenSpacingLinearGradient:
                    Debug.LogError("This structure is outdated.");
                    //GenerateEvenSpacedSurfaceCharge();
                    break;
                case SurfaceChargeGradient.UnevenSpacingLinearGradient:
                    GenerateQuantizedSurfaceCharges();
                    break;
            }

            currentLength = latticeLength;
            currentWidth = latticeWidth;
            currentNumSurfaceChargeRings = numSurfaceChargeRings;
            currentNumSurfaceChargeRows = numSurfaceChargeRows;
            currentGradient = gradientSlope;
            currentVoltage = voltage;
            currentShowingCharges = showSurfaceCharges;
            currentConeRadius = coneRadiusMultiplier;
        }

        if (toggleAc)
        {
            AlternatingCurrent();
        }
    }

    void AlternatingCurrent()
    {
        float intervalTime = 1f / acFrequency;
        elapsedTime += Time.deltaTime;

        if (elapsedTime >= intervalTime)
        {
            // Makes ground object switch which end of the wire it's at
            groundPositive.SetActive(!groundPositive.activeSelf);
            groundNegative.SetActive(!groundNegative.activeSelf);
            // Making sure we have exactly 1 ground object
            if (groundPositive.activeSelf == groundNegative.activeSelf)
            {
                Debug.LogError("Needs to have only 1 ground active");
            }

            // Loop over every surface charge particle
            foreach (var particle in surfaceChargeParent.GetComponentsInChildren<DockableMonopoleStrength>())
            {
                // change the sign of the charge
                particle.Strength *= -1;
                
                
                if (showSurfaceCharges)
                {
                    particle.gameObject.GetComponent<Renderer>().material = (particle.Strength < 0) ? negativeMaterial : positiveMaterial;
                }

                else
                {
                    particle.gameObject.GetComponent<Renderer>().material = invisibleMaterial;
                }
            }

            elapsedTime = 0;
        }
    }

    private void GetWireDimensions()
    {
        MeshFilter wireMesh = latticeShell.GetComponent<MeshFilter>();
        if (wireMesh != null)
        {
            Vector3 wireSize = wireMesh.sharedMesh.bounds.size;
            Vector3 wireSizeScaled = Vector3.Scale(wireSize, wireMesh.transform.lossyScale);

            // Coordinates are a little weird here, because the prefab was rotated 90 degrees in the y-direction.
            // So in this case, world z-coord is the wires' x-coord, and the world x-coord is the wires' z-coord.
            latticeWidth = wireSizeScaled.x;
            latticeLength = wireSizeScaled.z;
        }
    }

    /// <summary>
    /// Get surface charge particle color based on the x-position.
    /// Only used with evenly spaced surface charges.
    /// </summary>
    /// <param name="x">Variable i in for loop while generating surface charges.</param>
    /// <returns>Color32 value for particle at defined position.</returns>
    Color32 SurfaceChargeColor(int x)
    {
        float r = (109 / (numSurfaceChargeRings - 1)) * x + 146;
        float g = (237 / (numSurfaceChargeRings - 1)) * x;
        float b = (-255 / (numSurfaceChargeRings - 1)) * x + 255;

        return new Color32((byte)r, (byte)g, (byte)b, 255);
    }

    /// <summary>
    /// Calculates the charge strength based on the x-position of the surface charge.
    /// </summary>
    /// <param name="x">Variable i in for loop while generating surface charges.</param>
    /// <returns>Float value for charge strength.</returns>
    float SurfaceCharge(int x)
    {
        float charge = (maxCharge * 2) / (numSurfaceChargeRings - 1) * x - maxCharge;
        return charge;
    }

    float GetRadius(float x)
    {
        if (surfaceChargeType == SurfaceChargeGradient.EvenSpacingLinearGradient)
        {
            float r_0 = latticeWidth / 2;
            float factor = coneRadiusMultiplier - 1;
            float m = r_0 * factor / latticeLength;
            return m * x + r_0;
        }
        else// if (surfaceChargeType == SurfaceChargeGradient.UnevenSpacingLinearGradient)
        {
            float r_0 = latticeWidth / 2;
            float factor = coneRadiusMultiplier - 1;
            float m = factor / latticeLength;
            return r_0 * (1 + factor / 2 + m * x);
        }
    }

    /// <summary>
    /// Generates the positions for all surface charges, this method is for linear charge density with undetermined charge strength.
    /// Charge strength is based on the position along the x-axis in the wire.
    /// </summary>
    void GenerateEvenSpacedSurfaceCharge()
    {
        // Initiate lists
        spawnPositions = new List<Vector3>();
        chargeValues = new List<float>();
        particleColor = new List<Color32>();

        // Define variables
        Vector3 spawnPosition = startPos - new Vector3(latticeLength, 0, 0) * 0.5f;
        float ringSpacing = latticeLength / (numSurfaceChargeRings - 1);
        float rowSpacing = 2f * Mathf.PI / numSurfaceChargeRows;
        

        for (int i = 0; i < numSurfaceChargeRings; i++)
        {
            float radius = GetRadius(i * ringSpacing);//latticeWidth / 2f;
            
            for (int j = 0; j < numSurfaceChargeRows; j++)
            {
                float x = i * ringSpacing;
                float y = Mathf.Cos(j * rowSpacing) * radius;
                float z = Mathf.Sin(j * rowSpacing) * radius;
                Vector3 summonPos = spawnPosition + new Vector3(x, y, z);
                
                spawnPositions.Add(summonPos);
                chargeValues.Add(SurfaceCharge(i));
                particleColor.Add(SurfaceChargeColor(i));
            }
        }
        
        PopulateSurfaceCharges();
    }
    
    void GenerateQuantizedSurfaceCharges()
    {
        // Initiate lists
        spawnPositions = new List<Vector3>();
        chargeValues = new List<float>();
        particleColor = new List<Material>();
        
        // Define variables
        float particleCharge = maxCharge / numSurfaceChargeRows;
        float rowSpacing = 2f * Mathf.PI / numSurfaceChargeRows;
        //float radius = latticeWidth / 2f;
        int numSurfaceChargeRingsHalf = Mathf.CeilToInt(((voltage / latticeLength) + maxCharge) / maxCharge);
        float normFactor = latticeLength / 2f / Mathf.Sqrt(2 * numSurfaceChargeRingsHalf / gradientSlope);
        //Color32 negativeColor = negativeMaterial.color;
        //Color32 positiveColor = positiveMaterial.color;
        
        // Generate charges at battery terminals
        // Negative terminal
        spawnPositions.Add(startPos + new Vector3(-latticeLength / 2 - 0.1f, 0, 0));
        chargeValues.Add(-voltage / 2 * 0.08f);
        particleColor.Add(negativeMaterial);
        // Positive terminal
        spawnPositions.Add(startPos + new Vector3(latticeLength / 2 + 0.1f, 0, 0));
        chargeValues.Add(voltage / 2 * 0.08f);
        particleColor.Add(positiveMaterial);
        
        // -x direction (negative particles)
        for (int i = 1; i <= numSurfaceChargeRingsHalf; i++)
        {
            for (int j = 0; j < numSurfaceChargeRows; j++)
            {
                float x = -Mathf.Sqrt(2 * i / gradientSlope) * normFactor;
                float radius = GetRadius(x);
                float y = Mathf.Cos(j * rowSpacing) * radius;
                float z = Mathf.Sin(j * rowSpacing) * radius;
                Vector3 summonPos = startPos + new Vector3(x, y, z);
                
                spawnPositions.Add(summonPos);
                chargeValues.Add(-particleCharge);
                particleColor.Add(negativeMaterial);
            }
        }
        
        // +x direction (positive particles)
        for (int i = 1; i <= numSurfaceChargeRingsHalf; i++)
        {
            for (int j = 0; j < numSurfaceChargeRows; j++)
            {
                float x = Mathf.Sqrt(2 * i / gradientSlope) * normFactor;
                float radius = GetRadius(x);
                float y = Mathf.Cos(j * rowSpacing) * radius;
                float z = Mathf.Sin(j * rowSpacing) * radius;
                Vector3 summonPos = startPos + new Vector3(x, y, z);
                
                spawnPositions.Add(summonPos);
                chargeValues.Add(particleCharge);
                particleColor.Add(positiveMaterial);
            }
        }
        
        PopulateSurfaceCharges();
    }
    
    /// <summary>
    /// Spawns the surface charges based on the locations generated in the generation methods.
    /// Requires that spawnPositions, chargeValues, and particleColor are not empty and have the same number of instances.
    /// Includes code to use hidden surface charges.
    /// </summary>
    void PopulateSurfaceCharges()
    {
        for (int i = 0; i < spawnPositions.Count; i++)
        {
            GameObject particle = Instantiate(surfaceChargePrefab, spawnPositions[i], Quaternion.identity);
            particle.transform.SetParent(surfaceChargeParent.transform, true);
            particle.GetComponent<DockableMonopoleStrength>().Strength = chargeValues[i];

            if (showSurfaceCharges)
            {
                particle.GetComponent<Renderer>().material = particleColor[i];
            }

            else
            {
                particle.GetComponent<Renderer>().material = invisibleMaterial;
            }
        }
    }
}