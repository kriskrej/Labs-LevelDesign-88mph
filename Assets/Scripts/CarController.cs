﻿using UnityEngine;
using System;
using System.Collections.Generic;
using ProBuilder2.Common;

[Serializable]
public enum DriveType
{
	RearWheelDrive,
	FrontWheelDrive,
	AllWheelDrive
}

public class CarController : MonoBehaviour
{
    [Tooltip("Maximum steering angle of the wheels")]
	public float maxAngle = 30f;
	[Tooltip("Maximum torque applied to the driving wheels")]
	public float maxTorque = 300f;
	[Tooltip("Maximum brake torque applied to the driving wheels")]
	public float brakeTorque = 30000f;
	[Tooltip("If you need the visual wheels to be attached automatically, drag the wheel shape here.")]
	public GameObject wheelShape;

	[Tooltip("The vehicle's speed when the physics engine can use different amount of sub-steps (in m/s).")]
	public float criticalSpeed = 5f;
	[Tooltip("Simulation sub-steps when the speed is above critical.")]
	public int stepsBelow = 5;
	[Tooltip("Simulation sub-steps when the speed is below critical.")]
	public int stepsAbove = 1;

	[Tooltip("The vehicle's drive type: rear-wheels drive, front-wheels drive or all-wheels drive.")]
	public DriveType driveType;

    private WheelCollider[] m_Wheels;
    bool inputDisabled;
    bool disappearing;

    [SerializeField] Rigidbody rigidbody;
    [SerializeField] CameraController cameraController;
    [SerializeField] HUDController hudController;
    [SerializeField] ParticleSystem wheelFlamePrefab;
    List<ParticleSystem> wheelFlames = new List<ParticleSystem>();

    public Vector3 targetVelocity;

    // Find all the WheelColliders down in the hierarchy.
	void Start() {
		m_Wheels = GetComponentsInChildren<WheelCollider>();

		for (int i = 0; i < m_Wheels.Length; ++i) {
			var wheel = m_Wheels [i];

			// Create wheel shapes only when needed.
			if (wheelShape != null)
			{
				var ws = Instantiate (wheelShape);
				ws.transform.parent = wheel.transform;
			}
		}
	}

	// This is a really simple approach to updating wheels.
	// We simulate a rear wheel drive car and assume that the car is perfectly symmetric at local zero.
	// This helps us to figure our which wheels are front ones and which are rear.
	void Update() {
	    if (inputDisabled)
	        return;

		m_Wheels[0].ConfigureVehicleSubsteps(criticalSpeed, stepsBelow, stepsAbove);

		float angle = maxAngle * Input.GetAxis("Horizontal");
		float torque = maxTorque * Input.GetAxis("Vertical");
		float handBrake = Input.GetKey(KeyCode.X) ? brakeTorque : 0;

		UpdateWheels(angle, handBrake, torque);
	}

    void FixedUpdate() {
        if (disappearing) {
            UpdateWheels(0, 0, maxTorque);
            rigidbody.velocity = Vector3.MoveTowards(rigidbody.velocity, targetVelocity, 10);
            return;
        }
        if(Speed_o_Meter.convertSpeedToMph(rigidbody.velocity.magnitude) >= 80)
            SpawnWheelFlames();
        if(Speed_o_Meter.convertSpeedToMph(rigidbody.velocity.magnitude) >= 88)
            Disappear();
    }

    private void UpdateWheels(float angle, float handBrake, float torque) {
        foreach (WheelCollider wheel in m_Wheels) {
            // A simple car where front wheels steer while rear ones drive.
            if (wheel.transform.localPosition.z > 0)
                wheel.steerAngle = angle;

            if (wheel.transform.localPosition.z < 0) {
                wheel.brakeTorque = handBrake;
            }

            if (wheel.transform.localPosition.z < 0 && driveType != DriveType.FrontWheelDrive) {
                wheel.motorTorque = torque;
            }

            if (wheel.transform.localPosition.z >= 0 && driveType != DriveType.RearWheelDrive) {
                wheel.motorTorque = torque;
            }

            // Update visual wheels if any.
            if (wheelShape) {
                Quaternion q;
                Vector3 p;
                wheel.GetWorldPose(out p, out q);

                // Assume that the only child of the wheelcollider is the wheel shape.
                Transform shapeTransform = wheel.transform.GetChild(0);
                shapeTransform.position = p;
                shapeTransform.rotation = q;
            }
        }
    }

    void Disappear() {
        disappearing = true;
        hudController.EnableMovieMode(true);
        cameraController.SetCameraDisappearingAnimationPosition();
        targetVelocity = rigidbody.velocity*100;
        inputDisabled = true;
        //SpawnWheelFlames();
        Invoke("DestroyCar", 2);
    }

    private void SpawnWheelFlames() {
        if (wheelFlames.Count > 0)
            return;
        foreach (var wheel in m_Wheels) {
            wheelFlames.Add(Instantiate(wheelFlamePrefab, wheel.transform));
        }
        foreach (var flame in wheelFlames) {
            flame.Play();
        }
    }

    void DestroyCar() {
        foreach (var flame in wheelFlames) {
            flame.transform.parent = null;
            Destroy(flame.gameObject, 10);
        }
        Destroy(gameObject);
    }

}
