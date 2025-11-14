using System.Collections;
using System.Collections.Generic;
using System.Resources;
using UnityEngine;

public class LatticeGenerator : MonoBehaviour
{
    [SerializeField] public LatticeStructures structure;
    [SerializeField] public WireTypes wireType;
    [SerializeField] public ConeTypes coneType;
    [SerializeField] private float coneRadiusMultiplier;
    
    private Vector3 startPos;
    private Vector3 unitCellPos;
    private Vector3 center;
    private Vector3 objectStart;
    float z0;
    public int numWireCharges;
    private int electronCount;
    float latticeSpacing;
    private Vector3 atomRadius;
    
    [SerializeField] private GameObject latticeIon;
    [SerializeField] private GameObject electronPrefab;
    [SerializeField] private GameObject latticeShell;
    [SerializeField] private GameObject latticeParent;
    [SerializeField] private GameObject wireChargeParent;
    [SerializeField] private GameObject chargeParent;
    [SerializeField] private GameObject objectCenter;
    [SerializeField] private Material invisibleMaterial;
    [SerializeField] public bool invisibleLattice;

    private List<GameObject> particles;
    private List<Transform> particleParents;
    private List<Vector3> positions;

    public float latticeX;
    public float latticeY;
    public float latticeZ;
    private float latticeMargin;
    private float cornerMargin;

    private float currentWidth;
    private float currentHeight;
    private float currentLength;
    private float currentSpacing;
    private LatticeStructures currentStructure;

    private static float fccSpacing = 0.4f;//0.33f;
    private static float scSpacing = fccSpacing / Mathf.Sqrt(2);
    private static float bccSpacing = fccSpacing * 2 / Mathf.Sqrt(6);
    private static float copperRadius = 0.1707f; // used in FCC lattice
    private static float poloniumRadius = 0.2240f; // used in SC lattice
    private static float chromiumRadius = 0.1734f; // used in BCC lattice
    
    public enum LatticeStructures
    {
        FCCLattice,
        BCCLattice,
        SCLattice
    }

    public enum WireTypes
    {
        Corner,
        Straight,
        Cone
    }

    public enum ConeTypes
    {
        Growing,
        Shrinking
    }

    
    
    void Start()
    {
        GetDimensions();
        startPos = transform.position;
        currentStructure = structure;
        latticeMargin = 0.05f / (transform.localScale.x * transform.localScale.x);
        cornerMargin = 0.07f / transform.localScale.x;
        z0 = objectStart.z;
        
        // Initiate Lists
        particles = new List<GameObject>();
        particleParents = new List<Transform>();
        positions = new List<Vector3>();
    }

    void Update()
    {
        GetDimensions();
        if (currentLength != latticeX || currentWidth != latticeZ || currentSpacing != latticeSpacing || currentHeight != latticeY || currentStructure != structure)
        {
            foreach (Transform child in latticeParent.transform)
            {
                Destroy(child.gameObject);
            }
            foreach (Transform child in wireChargeParent.transform)
            {
                Destroy(child.gameObject);
            }
            
            // Clear lists
            particles.Clear();
            positions.Clear();
            particleParents.Clear();

            switch (structure)
            {
                case LatticeStructures.FCCLattice:
                    FCCLattice();
                    break;
                case LatticeStructures.BCCLattice:
                    BCCLattice();
                    break;
                case LatticeStructures.SCLattice:
                    SCLattice();
                    break;
                    
            }
            currentLength = latticeX;
            currentWidth = latticeZ;
            currentHeight = latticeY;
            currentSpacing = latticeSpacing;
            currentStructure = structure;
        }
    }

    void SCLattice()
    {
        latticeSpacing = scSpacing / transform.localScale.x;
        latticeIon.transform.localScale = new Vector3(poloniumRadius, poloniumRadius, poloniumRadius); //  polonium atomic size
        
        Vector3[] Offset = new Vector3[]
        {
            Vector3.zero
        };

        Vector3[] ElectronOffset = new Vector3[]
        {
            new Vector3(0.5f, 0.5f, 0.5f) * latticeSpacing
        };
        
        GenerateLattice(Offset, ElectronOffset);
    }
 
    void BCCLattice()
    {
        latticeSpacing = bccSpacing / transform.localScale.x;
        latticeIon.transform.localScale = new Vector3(chromiumRadius, chromiumRadius, chromiumRadius); // chromium atomic size
        
        // Offsets for where lattice ions will be summoned
        Vector3[] Offset = new Vector3[]
        {
            Vector3.zero,
            new Vector3(0.5f, 0.5f, 0.5f) * latticeSpacing,
        };

        // Offsets for where the wire charges will be summoned
        Vector3[] ElectronOffset = new Vector3[]
        {
            new Vector3(0.5f, 0, 0) * latticeSpacing,
            new Vector3(0, 0.5f, 0) * latticeSpacing,
        };
        
        GenerateLattice(Offset, ElectronOffset);
    }
 
    void FCCLattice()
    {
        latticeSpacing = fccSpacing / transform.localScale.x;
        latticeIon.transform.localScale = new Vector3(copperRadius, copperRadius, copperRadius); // copper atomic size
        
        // Offsets for where lattice ions will be summoned
        Vector3[] Offset = new Vector3[]
        {
            Vector3.zero,
            new Vector3(0.5f, 0.5f, 0) * latticeSpacing,
            new Vector3(0.5f, 0, 0.5f) * latticeSpacing,
            new Vector3(0, 0.5f, 0.5f) * latticeSpacing
        };

        // Offsets for where the wire charges will be summoned
        Vector3[] ElectronOffset = new Vector3[]
        {
            new Vector3(0.5f, 0, 0) * latticeSpacing,
            new Vector3(0, 0.5f, 0) * latticeSpacing,
            new Vector3(0, 0, 0.5f) * latticeSpacing,
            new Vector3(0.5f, 0.5f, 0.5f) * latticeSpacing
        };
        
        GenerateLattice(Offset, ElectronOffset);
    }

    /// <summary>
    /// Generates lists of GameObjects and Vector3 positions of all the particles that fit within the region of the wire.
    /// </summary>
    /// <param name="latticeOffset"></param>
    /// <param name="latticeElectronOffset"></param>
    void GenerateLattice(Vector3[] latticeOffset, Vector3[] latticeElectronOffset)
    {
        // Set center position in local coordinates
        center = objectCenter.transform.InverseTransformPoint(startPos);
        
        // Find number of iterations
        int numX = Mathf.Max(1, Mathf.CeilToInt(latticeX / latticeSpacing));
        int numY = Mathf.Max(1, Mathf.CeilToInt(latticeY / latticeSpacing));
        int numZ = Mathf.Max(1, Mathf.CeilToInt(latticeZ  / latticeSpacing));
        
        // center the grid on the object by shifting indices
        //Vector3 half = new Vector3((numX - 1) * 0.5f, (numY - 1) * 0.5f, (numZ - 1) * 0.5f);
        
        for (int i = 0; i < numX; i++)
        {
            for (int j = 0; j < numY; j++)
            {
                for (int k = 0; k < numZ; k++)
                {
                    Vector3 unitCellPos = objectStart + new Vector3(i, j, k) * latticeSpacing;

                    // Lattice Charges
                    foreach (var offset in latticeOffset)
                    {
                        particles.Add(latticeIon);
                        Vector3 summonPos = unitCellPos + offset;
                        positions.Add(summonPos);
                        particleParents.Add(latticeParent.transform);
                    }
                    
                    // Wire Charges
                    foreach (var offset in latticeElectronOffset)
                    {
                        particles.Add(electronPrefab);
                        Vector3 summonPos = unitCellPos + offset;
                        positions.Add(summonPos);
                        particleParents.Add(wireChargeParent.transform);
                    }
                }
            }
        }

        switch (wireType)
        {
            case WireTypes.Straight:
                PopulateStraightLattice();
                break;
            case WireTypes.Corner:
                PopulateCornerLattice();
                break;
            case WireTypes.Cone:
                PopulateConeLattice();
                break;
        }
    }

    
    /// <summary>
    /// Populates lattice in a straight cylindrical shape.
    /// </summary>
    void PopulateStraightLattice()
{
    for (int i = 0; i < particles.Count; i++)
    {
        //float radius = GetRadius(positions[i].x - center.x, positions[i].y - center.y);
        float radius = latticeX / 2;
        if (Vector2.Distance(new Vector2(positions[i].x, positions[i].y),
                new Vector2(center.x, center.y)) < radius - latticeMargin &&
            positions[i].z - z0 <= latticeZ - latticeMargin)
        {
            Vector3 summonPos = transform.TransformPoint(positions[i]);
            GameObject particle = Instantiate(particles[i], summonPos, Quaternion.identity);
            particle.transform.SetParent(particleParents[i], worldPositionStays: true);
            
            // For invisible lattice
            if (invisibleLattice && particle.CompareTag("LatticeChargedIons"))
            {
                particle.GetComponent<Renderer>().material = invisibleMaterial;
            }
        }

        else
        {
            Debug.Log("Couldn't summon particle at " + positions[i]);
        }
    }
}
    
    /// <summary>
    /// Populates lattice in a 90 degree curve shape.
    /// Made specifically for CornerWirePrefab.
    /// </summary>
    void PopulateCornerLattice()
    {
        // Find important boundaries, used for determining what section of wire is being populated
        Vector3 localCurveCenter = objectStart + 
                                   new Vector3(1.5f * latticeY, 0.5f * latticeY, 1.5f * latticeY);
        Vector3 localStart = objectStart +
                           new Vector3(0.5f * latticeY, 0.5f * latticeY, 2.5f * latticeY);
        Vector3 localEnd = objectStart +
                         new Vector3(2.5f * latticeY, 0.5f * latticeY, 0.5f * latticeY);
        float radius = latticeY / 2;
        
        for (int i = 0; i < particles.Count; i++)
        {
            // Phase 1
            // Curve part
            if (positions[i].x < localCurveCenter.x && positions[i].z < localCurveCenter.z)
            {
                Vector2 direction = (new Vector2(positions[i].x, positions[i].z) -
                                     new Vector2(localCurveCenter.x, localCurveCenter.z)).normalized; // Get direction toward wire center
                Vector3 wireCenter = localCurveCenter + 2f * radius * new Vector3(direction.x, 0, direction.y); // Calculate the point at the center of the wire in that direction
                float radialDist = Vector3.Distance(wireCenter, positions[i]); // Find how far from the center point the particle is

                // Make sure particles are within the inner and outer radius of the torus
                // and make sure the particle is withing the radius of the wire
                if (Vector2.Distance(new Vector2(positions[i].x, positions[i].z),
                        new Vector2(localCurveCenter.x, localCurveCenter.z)) > radius + latticeMargin &&
                    Vector2.Distance(new Vector2(positions[i].x, positions[i].z),
                        new Vector2(localCurveCenter.x, localCurveCenter.z)) < 3 * radius - latticeMargin &&
                    radialDist < radius - cornerMargin)
                {
                    // Summon Particles
                    GameObject particle = Instantiate(particles[i], transform.TransformPoint(positions[i]), Quaternion.identity);
                    particle.transform.SetParent(particleParents[i], true);
                    
                    // For invisible lattice
                    if (invisibleLattice && particle.CompareTag("LatticeChargedIons"))
                    {
                        particle.GetComponent<Renderer>().material = invisibleMaterial;
                    }
                }
            }

            // Phase 2
            // +X straight bit
            else if (positions[i].x > localCurveCenter.x && positions[i].z < localCurveCenter.z)
            {
                // Check wire radius
                if (Vector2.Distance(new Vector2(positions[i].y, positions[i].z), new Vector2(localEnd.y, localEnd.z)) <
                    radius - latticeMargin)
                {
                    GameObject particle = Instantiate(particles[i], transform.TransformPoint(positions[i]), Quaternion.identity);
                    particle.transform.SetParent(particleParents[i], true);
                    
                    // For invisible lattice
                    if (invisibleLattice && particle.CompareTag("LatticeChargedIons"))
                    {
                        particle.GetComponent<Renderer>().material = invisibleMaterial;
                    }
                }
            }

            // Phase 3
            // +Z straight bit
            else if (positions[i].x < localCurveCenter.x && positions[i].z > localCurveCenter.z)
            {
                // Check wire radius
                if (Vector2.Distance(new Vector2(positions[i].x, positions[i].y), new Vector2(localStart.x, localStart.y)) <
                    radius - latticeMargin)
                {
                    GameObject particle = Instantiate(particles[i], transform.TransformPoint(positions[i]), Quaternion.identity);
                    particle.transform.SetParent(particleParents[i], true);
                    
                    // For invisible lattice
                    if (invisibleLattice && particle.CompareTag("LatticeChargedIons"))
                    {
                        particle.GetComponent<Renderer>().material = invisibleMaterial;
                    }
                }
            }

            else
            {
                Debug.LogWarning("Particle attempted to be summoned at " + positions[i] + ", but was unsuccessful");
            }
        }
    }
    
    /// <summary>
    /// Populates lattice in a cone shape.
    /// 'Growing' means the larger radius will be at +Z, 'Shrinking' means the larger radius will be at -Z.
    /// </summary>
    void PopulateConeLattice()
    {
        float r0 = latticeX / 2;
        float zMax = latticeZ;
        if (coneType == ConeTypes.Growing)
        {
            for (int i = 0; i < particles.Count; i++)
            {
                float z = positions[i].z - z0;
                float radius = Mathf.Abs((r0 * (1 - coneRadiusMultiplier) * z) / zMax + coneRadiusMultiplier * r0); 
                
                // Check that radius and length requirements are met and summon particle if it passes
                if (Vector2.Distance(new Vector2(positions[i].x, positions[i].y),
                        new Vector2(center.x, center.y)) < radius - 0.05f &&
                        positions[i].z - z0 <= latticeZ - latticeMargin)
                {
                    Vector3 summonPos = transform.TransformPoint(positions[i]);
                    GameObject particle = Instantiate(particles[i], summonPos, Quaternion.identity);
                    particle.transform.SetParent(particleParents[i], worldPositionStays: true);
                    
                    // For invisible lattice
                    if (invisibleLattice && particle.CompareTag("LatticeChargedIons"))
                    {
                        particle.GetComponent<Renderer>().material = invisibleMaterial;
                    }
                }
            }
        }

        else
        {
            for (int i = 0; i < particles.Count; i++)
            {
                float z = positions[i].z - z0;
                float radius = Mathf.Abs((r0 * (coneRadiusMultiplier - 1) * z) / zMax + r0); 
                
                // Check that radius and length requirements are met and summon particle if it passes
                if (Vector2.Distance(new Vector2(positions[i].x, positions[i].y),
                        new Vector2(center.x, center.y)) < radius - 0.05f &&
                        positions[i].z - z0 <= latticeZ - latticeMargin)
                {
                    Vector3 summonPos = transform.TransformPoint(positions[i]);
                    GameObject particle = Instantiate(particles[i], summonPos, Quaternion.identity);
                    particle.transform.SetParent(particleParents[i], worldPositionStays: true);
                    
                    // For invisible lattice
                    if (invisibleLattice && particle.CompareTag("LatticeChargedIons"))
                    {
                        particle.GetComponent<Renderer>().material = invisibleMaterial;
                    }
                }
            }
        }
    }


    /// <summary>
    /// Gets the cylindrical dimensions of the GameObject that was initiated with the latticeShell field.
    /// </summary>
    void GetDimensions()
    {
        MeshFilter wireMesh = latticeShell.GetComponent<MeshFilter>();
        if (wireMesh == null) return;

        // Get the local-space bounds of the mesh
        Bounds localBounds = wireMesh.sharedMesh.bounds;
        Vector3 localScale = latticeShell.transform.localScale;

        // Apply the local scale to bounds
        Vector3 scaledMin = Vector3.Scale(localBounds.min, localScale);
        Vector3 scaledMax = Vector3.Scale(localBounds.max, localScale);

        // Dimensions in local space
        latticeX = scaledMax.x - scaledMin.x;
        latticeY = scaledMax.y - scaledMin.y;
        latticeZ = scaledMax.z - scaledMin.z;

        objectStart = latticeShell.transform.localPosition + scaledMin;
    }
}