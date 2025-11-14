using System;
using System.Collections;
using System.Collections.Generic;
using EMP.Core;
using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Jobs;

public class IonRestoringForceController : MonoBehaviour
{
    [SerializeField] private float springConstant;
    [SerializeField] private float displacementMultiplier;
    [SerializeField] private float dampingCoefficient;
    [SerializeField] private bool isAnchored;

    private TransformAccessArray ionTransforms;
    private NativeArray<float3> startPositions;
    private NativeArray<float3> velocities;
    private NativeArray<float3> forces;

    private Rigidbody[] rbs;

    private bool initialized = false;

    void FixedUpdate()
    {
        if (isAnchored)
        {
            MonopoleVelocityStateController[] ions = GetComponentsInChildren<MonopoleVelocityStateController>();
            foreach (var ion in ions)
            {
                ion.isAnchored = isAnchored;
            }
            return;
        }
        
        //Debug.Log("There are " + ionTransforms.length + " ions.");
        if (ionTransforms.isCreated)
        {
            //Debug.Log("There are " + ionTransforms.length + " ions being calculated");
            var job = new RestoringForceJob()
            {
                startPositions = startPositions,
                velocities = velocities,
                springConstant = springConstant,
                dampingCoefficient = dampingCoefficient,
                displacementMultiplier = displacementMultiplier,
                forces = forces
            };

            JobHandle handle = job.Schedule(ionTransforms);
            handle.Complete();
        }

        if (rbs != null)
        {
            for (int i = 0; i < rbs.Length; i++)
            {
                rbs[i].AddForce(forces[i], ForceMode.Acceleration);
                rbs[i].velocity *= dampingCoefficient;
            }
        }
    }

    public void Initialize()
    {
        if (initialized) return;
        
        var ions = FindObjectsOfType<IonRestoringForce>();
        int numIons = ions.Length;

        if (ions.Length != numIons)
        {
            Debug.LogError("Something ain't right.");
        }
            
        ionTransforms = new TransformAccessArray(numIons);
        startPositions = new NativeArray<float3>(numIons, Allocator.Persistent);
        velocities = new NativeArray<float3>(numIons, Allocator.Persistent);
        forces = new NativeArray<float3>(numIons, Allocator.Persistent);
        rbs = new Rigidbody[numIons];

        for (int i = 0; i < numIons; i++)
        {
            ionTransforms.Add(ions[i].transform);
            startPositions[i] = ions[i].transform.position;
            velocities[i] = ions[i].GetComponent<Rigidbody>().velocity;
            rbs[i] = ions[i].GetComponent<Rigidbody>();
        }
        initialized = true;
        Debug.Log("Initialized IonRestoringForce");
    }

    [BurstCompile(CompileSynchronously = false)]
    struct RestoringForceJob : IJobParallelForTransform
    {
        [ReadOnly] public NativeArray<float3> startPositions;
        public NativeArray<float3> velocities;
        public NativeArray<float3> forces;

        public float springConstant;
        public float dampingCoefficient;
        public float displacementMultiplier;
        
        public void Execute(int i, TransformAccess transform)
        {
            float3 pos = transform.position;
            float3 start = startPositions[i];
            float3 displacement = pos - start;

            float3 springForce = -springConstant * displacementMultiplier * displacement;

            forces[i] = springForce;
        }
    }

    void OnDestroy()
    {
        if (ionTransforms.isCreated) ionTransforms.Dispose();
        if (startPositions.IsCreated) startPositions.Dispose();
        if (velocities.IsCreated) velocities.Dispose();
    }
}
