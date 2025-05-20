using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using UnityEditor;
using TMPro;


public class CloseTargetGripper : MonoBehaviour
{
    public ArticulationBody LinkLefty;
    public ArticulationBody LinkRighty;
    public ArticulationBody JawLefty;
    public ArticulationBody JawRighty;
    public ArticulationBody LinkLeftyInner;
    public ArticulationBody LinkRightyInner;

    public Transform body;
    public Transform Joint1;
    public Transform Joint2;
    public Transform tip;

    public float velocity = 100;
    public float stiffness = 0;
    public float damping = 0;


    private float speed;


    public float currentPosition;
    public float gripChangeDebug;
    public float speedDebug;
    public float currentTarget;

    public bool ButtonClicked;

    private ArticulationDrive LinkLeftDrive;
    private ArticulationDrive LinkRightDrive;
    private ArticulationDrive LinkLeftInnerDrive;
    private ArticulationDrive LinkRightInnerDrive;
    private ArticulationDrive JawLeftDrive;
    private ArticulationDrive JawRightDrive;

    private float zBody2J1;
    private float link1;
    private float link2;
    private float alpha;
    private float beta;
    private float theta;
    private void Start()
    {
        speed = 30f * velocity / 100;

        LinkLeftDrive = LinkLefty.xDrive;
        LinkRightDrive = LinkRighty.xDrive;
        LinkLeftInnerDrive = LinkLeftyInner.xDrive;
        LinkRightInnerDrive = LinkRightyInner.xDrive;
        JawLeftDrive = JawLefty.xDrive;
        JawRightDrive = JawRighty.xDrive;

        LinkLeftDrive.stiffness = stiffness;
        LinkRightDrive.stiffness = stiffness;
        LinkLeftInnerDrive.stiffness = stiffness;
        LinkRightInnerDrive.stiffness = stiffness;
        JawLeftDrive.stiffness = stiffness;
        JawRightDrive.stiffness = stiffness;

        LinkLeftDrive.damping = damping;
        LinkRightDrive.damping = damping;
        LinkLeftInnerDrive.damping = damping;
        LinkRightInnerDrive.damping = damping;
        JawLeftDrive.damping = damping;
        JawRightDrive.damping = damping;

        LinkLefty.xDrive = LinkLeftDrive;
        LinkRighty.xDrive = LinkRightDrive;
        LinkLeftyInner.xDrive = LinkLeftInnerDrive;
        LinkRightyInner.xDrive = LinkRightInnerDrive;
        JawLefty.xDrive = JawLeftDrive;
        JawRighty.xDrive = JawRightDrive;

        Vector3 bodyPosition = body.position;
        Vector3 Joint1Position = Joint1.position;
        Vector3 Joint2Position = Joint2.position;
        Vector3 tipPosition = tip.position;

        zBody2J1 = Mathf.Abs(bodyPosition.z - Joint1Position.z);
        link1 = Vector3.Distance(Joint1Position, Joint2Position);
        alpha = Mathf.Atan2(Mathf.Abs(Joint1Position.y - Joint2Position.y), Mathf.Abs(Joint1Position.z - Joint2Position.z));
        link2 = Vector3.Distance(Joint2Position, tipPosition);
        beta = Mathf.Atan2(Mathf.Abs(Joint2Position.y - tipPosition.y), Mathf.Abs(Joint2Position.z - tipPosition.z));
    }

    private void FixedUpdate()
    {
        speed = 50 * velocity / 100;
        Transform grandParent1 = tip.parent.parent.parent;
        Vector3 tipWorldPosition = tip.position;
        Vector3 localPositionToGrandParent1 = grandParent1.InverseTransformPoint(tipWorldPosition);
        float tipz = localPositionToGrandParent1.x;

        float temp = tipz / 100 - zBody2J1 + link2 * Mathf.Cos(beta);
        float tempClamp = Mathf.Clamp(temp / link1, -1f, 1f);
        float theta = Mathf.Acos(tempClamp) - alpha;
        float thetaDeg = theta * 180 / Mathf.PI;

        LinkLeftDrive = LinkLefty.xDrive;
        LinkRightDrive = LinkRighty.xDrive;
        LinkLeftInnerDrive = LinkLeftyInner.xDrive;
        LinkRightInnerDrive = LinkRightyInner.xDrive;
        JawLeftDrive = JawLefty.xDrive;
        JawRightDrive = JawRighty.xDrive;

        float gripChange = -speed * Time.fixedDeltaTime;
        
        if (ButtonClicked)
        {
            float RightTargetClosed = Mathf.Clamp(thetaDeg - gripChange, 0.01f, 49.99f);
            LinkLeftDrive.target = RightTargetClosed;
            LinkRightDrive.target = -RightTargetClosed;
            LinkLeftInnerDrive.target = RightTargetClosed;
            LinkRightInnerDrive.target = -RightTargetClosed;
            JawLeftDrive.target = -RightTargetClosed;
            JawRightDrive.target = RightTargetClosed;
            
        }
        else
        {
            float RightTargetOpen = Mathf.Clamp(thetaDeg + gripChange, 0.01f, 49.99f);
            LinkLeftDrive.target = RightTargetOpen;
            LinkRightDrive.target = -RightTargetOpen;
            LinkLeftInnerDrive.target = RightTargetOpen;
            LinkRightInnerDrive.target = -RightTargetOpen;
            JawLeftDrive.target = -RightTargetOpen;
            JawRightDrive.target = RightTargetOpen;
        }


        LinkLefty.xDrive = LinkLeftDrive;
        LinkRighty.xDrive = LinkRightDrive;
        LinkLeftyInner.xDrive = LinkLeftInnerDrive;
        LinkRightyInner.xDrive = LinkRightInnerDrive;
        JawLefty.xDrive = JawLeftDrive;
        JawRighty.xDrive = JawRightDrive;

        speedDebug = speed;
        gripChangeDebug = gripChange;
        currentPosition = JawLefty.jointPosition[0] * 180 / Mathf.PI;
        currentTarget = JawLefty.jointPosition[0] * 180 / Mathf.PI + gripChange;
    }
}

