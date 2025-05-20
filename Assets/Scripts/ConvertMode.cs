using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Mode { Moving = 0, Manipulating = 1 };
public class ConvertMode : MonoBehaviour
{
    public Mode mode;

    public ArticulationBody link1;
    public ArticulationBody link2;
    public ArticulationBody link3;
    public ArticulationBody link4;
    public ArticulationBody link5;
    public ArticulationBody link6;
    // Start is called before the first frame update

    private ArticulationDrive drive1;
    private ArticulationDrive drive2;
    private ArticulationDrive drive3;
    private ArticulationDrive drive4;
    private ArticulationDrive drive5;   
    private ArticulationDrive drive6;
    void Start()
    {
        mode = Mode.Moving;

        drive1 = link1.xDrive;
        drive2 = link2.xDrive;
        drive3 = link3.xDrive;  
        drive4 = link4.xDrive;
        drive5 = link5.xDrive;
        drive6 = link6.xDrive;
    }

    // Update is called once per frame
    public void OnButtonClick()
    {
        ConvertControlMode();
    }

    private void ConvertControlMode()
    {
        if (mode == Mode.Manipulating)
        {
            mode = Mode.Moving;

            drive1.driveType = ArticulationDriveType.Force;

            link1.xDrive = drive1;
            link2.xDrive = drive1;
            link3.xDrive = drive1;
            link4.xDrive = drive1;
            link5.xDrive = drive1;
            link6.xDrive = drive1;
        } 
        else if (mode == Mode.Moving)
        {
            mode = Mode.Manipulating;

            drive1.driveType = ArticulationDriveType.Target;
            drive2.driveType = ArticulationDriveType.Target;
            drive3.driveType = ArticulationDriveType.Target;
            drive4.driveType = ArticulationDriveType.Target;
            drive5.driveType = ArticulationDriveType.Target;
            drive6.driveType = ArticulationDriveType.Target;

            drive1.target = link1.jointPosition[0] * 180 / Mathf.PI;
            drive2.target = link2.jointPosition[0] * 180 / Mathf.PI;
            drive3.target = link3.jointPosition[0] * 180 / Mathf.PI;
            drive4.target = link4.jointPosition[0] * 180 / Mathf.PI;
            drive5.target = link5.jointPosition[0] * 180 / Mathf.PI;
            drive6.target = link6.jointPosition[0] * 180 / Mathf.PI;

            link1.xDrive = drive1;
            link2.xDrive = drive2;
            link3.xDrive = drive3;
            link4.xDrive = drive4;
            link5.xDrive = drive5;
            link6.xDrive = drive6;
        }
    }
}
