using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Makes ions in a conductors lattice structure behave as if it were attached to a spring.
/// It feels a force pushing it back to its original position.
/// </summary>
public class IonRestoringForce : MonoBehaviour
{
    void Start()
    {
        var controller = GetComponentInParent<IonRestoringForceController>();
        if (controller != null)
        {
            controller.Initialize();
        }
    }
    /*[SerializeField] private float springConstant;
    [SerializeField] private float displacementMultiplier;
    [SerializeField] private float dampingCoefficient;

    public float displacementMagnitude;
    public float modifiedDisplacement;
    public Vector3 force;

    private Vector3 startPos;
    private Vector3 localStart;
    private Vector3 displacement;
    private Transform origin;
    private Rigidbody rb;

    void Start()
    {
        origin = transform.GetComponentInParent<LatticeIonController>().objectCenter;
        rb = GetComponent<Rigidbody>();
        
        startPos = transform.position;
        localStart = transform.localPosition;
    }

    void FixedUpdate()
    {
        ApplySpringForce();
    }

    void ApplySpringForce()
    {
        displacement = (transform.position - startPos);
        displacementMagnitude = displacement.magnitude;
        modifiedDisplacement = displacementMagnitude * displacementMultiplier;

        Vector3 forceDirection = -displacement.normalized;

        Vector3 springForce = -springConstant * displacementMultiplier * displacement;
        Vector3 dampingForce = -dampingCoefficient * rb.velocity;

        force = springForce + dampingForce;
        
        rb.AddForce(force, ForceMode.Acceleration);
    }*/
}
