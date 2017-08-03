﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BulletUnity;
using Assets.Scripts.FSM;
using System.IO;

/// <summary>
/// SimUI serves as an interface between the Unity button UI and the various functions within the simulator.
/// It acomplishes this by having a public function for each button that interacts with the Main State to complete various tasks.
/// </summary>
public class SimUI : MonoBehaviour
{
    MainState main;
    DynamicCamera camera;
    DriverPractice dpm;
    Toolkit toolkit;

    GameObject canvas;

    GameObject dpmWindow;
    GameObject configWindow;
    GameObject defineIntakeWindow;
    GameObject defineReleaseWindow;
    GameObject setSpawnWindow;
    GameObject defineGamepieceWindow;

    GameObject freeroamCameraWindow;
    GameObject spawnpointWindow;
    GameObject robotCameraViewWindow;
    RenderTexture robotCameraView;
    RobotCamera robotCamera;

    GameObject releaseVelocityPanel;

    GameObject xOffsetEntry;
    GameObject yOffsetEntry;
    GameObject zOffsetEntry;
    GameObject releaseSpeedEntry;
    GameObject releaseVerticalEntry;
    GameObject releaseHorizontalEntry;

    GameObject lockPanel;

    GameObject robotCameraList;
    GameObject robotCameraIndicator;
    GameObject showCameraButton;
    GameObject configureRobotCameraButton;
    GameObject cameraConfigurationModeButton;
    GameObject changeCameraNodeButton;
    GameObject configureCameraPanel;
    GameObject cancelNodeSelectionButton;

    GameObject changeRobotPanel;
    GameObject changeFieldPanel;
    GameObject addRobotPanel;

    GameObject driverStationPanel;

    GameObject exitPanel;

    GameObject orientWindow;
    bool isOrienting = false;
    GameObject resetDropdown;

    Text enableDPMText;

    Text primaryGamepieceText;
    Text secondaryGamepieceText;

    Text intakeControlText;
    Text releaseControlText;
    Text spawnControlText;

    Text configHeaderText;
    Text configuringText;

    Text intakeMechanismText;
    Text releaseMechanismText;

    Text primaryCountText;
    Text secondaryCountText;

    Text cameraNodeText;

    public bool dpmWindowOn = false; //if the driver practice mode window is active

    public bool configuring = false; //if the configuration window is active
    public int configuringIndex = 0; //0 if user is configuring primary, 1 if user is configuring secondary

    private int holdCount = 0; //counts how long a button has been pressed (for add/subtract buttons to increase increment)
    const int holdThreshold = 150;
    const float offsetIncrement = 0.01f;
    const float speedIncrement = 0.1f;
    const float angleIncrement = 1f;

    private float deltaOffsetX;
    private float deltaOffsetY;
    private float deltaOffsetZ;

    private float deltaReleaseSpeed;
    private float deltaReleaseHorizontal;
    private float deltaReleaseVertical;

    private int settingControl = 0; //0 if false, 1 if intake, 2 if release, 3 if spawn

    bool isEditing = false;

    private bool freeroamWindowClosed = false;
    private bool usingRobotView = false;

    private bool oppositeSide = false;
    private bool indicatorActive = false;

    /// <summary>
    /// Retreives the Main State instance which controls everything in the simulator.
    /// </summary>
    void Start()
    {
    }

    private void Update()
    {
        if (main == null)
        {
            main = transform.GetComponent<StateMachine>().CurrentState as MainState;
            camera = GameObject.Find("Main Camera").GetComponent<DynamicCamera>();
            //Get the render texture from Resources/Images
            robotCameraView = Resources.Load("Images/RobotCameraView") as RenderTexture;
            toolkit = GetComponent<Toolkit>();
        }
        else if (dpm == null)
        {
            camera = GameObject.Find("Main Camera").GetComponent<DynamicCamera>();
            //dpm = main.GetDriverPractice();
            FindElements();
        }
        else
        {
            UpdateDPMValues();
            UpdateVectorConfiguration();
            UpdateWindows();
            if (settingControl != 0) ListenControl();

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (StateMachine.Instance.CurrentState.GetType().Equals(typeof(MainState)))
                {
                    if (!exitPanel.activeSelf) MainMenuExit("open");
                    else MainMenuExit("cancel");
                }
            }

        }

    }

    private void OnGUI()
    {
    }

    /// <summary>
    /// Finds all the necessary UI elements that need to be updated/modified
    /// </summary>
    private void FindElements()
    {
        canvas = GameObject.Find("Canvas");

        dpmWindow = AuxFunctions.FindObject(canvas, "DPMPanel");
        configWindow = AuxFunctions.FindObject(canvas, "ConfigurationPanel");

        enableDPMText = AuxFunctions.FindObject(canvas, "EnableDPMText").GetComponent<Text>();

        primaryGamepieceText = AuxFunctions.FindObject(canvas, "PrimaryGamepieceText").GetComponent<Text>();
        secondaryGamepieceText = AuxFunctions.FindObject(canvas, "SecondaryGamepieceText").GetComponent<Text>();

        configuringText = AuxFunctions.FindObject(canvas, "ConfiguringText").GetComponent<Text>();
        configHeaderText = AuxFunctions.FindObject(canvas, "ConfigHeaderText").GetComponent<Text>();

        releaseVelocityPanel = AuxFunctions.FindObject(canvas, "ReleaseVelocityPanel");

        xOffsetEntry = AuxFunctions.FindObject(canvas, "XOffsetEntry");
        yOffsetEntry = AuxFunctions.FindObject(canvas, "YOffsetEntry");
        zOffsetEntry = AuxFunctions.FindObject(canvas, "ZOffsetEntry");
        releaseSpeedEntry = AuxFunctions.FindObject(canvas, "ReleaseSpeedEntry");
        releaseVerticalEntry = AuxFunctions.FindObject(canvas, "ReleaseVerticalEntry");
        releaseHorizontalEntry = AuxFunctions.FindObject(canvas, "ReleaseHorizontalEntry");

        releaseMechanismText = AuxFunctions.FindObject(canvas, "ReleaseMechanismText").GetComponent<Text>();
        intakeMechanismText = AuxFunctions.FindObject(canvas, "IntakeMechanismText").GetComponent<Text>();

        defineIntakeWindow = AuxFunctions.FindObject(canvas, "DefineIntakePanel");
        defineReleaseWindow = AuxFunctions.FindObject(canvas, "DefineReleasePanel");
        defineGamepieceWindow = AuxFunctions.FindObject(canvas, "DefineGamepiecePanel");
        setSpawnWindow = AuxFunctions.FindObject(canvas, "SetGamepieceSpawnPanel");

        intakeControlText = AuxFunctions.FindObject(canvas, "IntakeInputButton").GetComponentInChildren<Text>();
        releaseControlText = AuxFunctions.FindObject(canvas, "ReleaseInputButton").GetComponentInChildren<Text>();
        spawnControlText = AuxFunctions.FindObject(canvas, "SpawnInputButton").GetComponentInChildren<Text>();

        lockPanel = AuxFunctions.FindObject(canvas, "DPMLockPanel");

        freeroamCameraWindow = AuxFunctions.FindObject(canvas, "FreeroamPanel");
        spawnpointWindow = AuxFunctions.FindObject(canvas, "SpawnpointPanel");

        robotCameraViewWindow = AuxFunctions.FindObject(canvas, "RobotCameraPanel");

        primaryCountText = AuxFunctions.FindObject(canvas, "PrimaryCountText").GetComponent<Text>();
        secondaryCountText = AuxFunctions.FindObject(canvas, "SecondaryCountText").GetComponent<Text>();

        robotCameraList = GameObject.Find("RobotCameraList");
        robotCameraViewWindow = AuxFunctions.FindObject(canvas, "RobotCameraPanelBorder");

        changeRobotPanel = AuxFunctions.FindObject(canvas, "ChangeRobotPanel");
        changeFieldPanel = AuxFunctions.FindObject(canvas, "ChangeFieldPanel");
        robotCameraIndicator = AuxFunctions.FindObject(robotCameraList, "CameraIndicator");
        showCameraButton = AuxFunctions.FindObject(canvas, "ShowCameraButton");
        configureRobotCameraButton = AuxFunctions.FindObject(canvas, "CameraConfigurationButton");
        changeCameraNodeButton = AuxFunctions.FindObject(canvas, "ChangeNodeButton");
        configureCameraPanel = AuxFunctions.FindObject(canvas, "CameraConfigurationPanel");
        driverStationPanel = AuxFunctions.FindObject(canvas, "DriverStationPanel");


        orientWindow = AuxFunctions.FindObject(canvas, "OrientWindow");
        resetDropdown = GameObject.Find("Reset Robot Dropdown");

        cameraConfigurationModeButton = AuxFunctions.FindObject(canvas, "ConfigurationMode");
        cameraNodeText = AuxFunctions.FindObject(canvas, "NodeText").GetComponent<Text>();
        cancelNodeSelectionButton = AuxFunctions.FindObject(canvas, "CancelNodeSelectionButton");

        exitPanel = AuxFunctions.FindObject(canvas, "ExitPanel");

        addRobotPanel = AuxFunctions.FindObject(canvas, "AddRobotPanel");

    }

    /// <summary>
    /// Updates the UI elements in the driver practice mode toolbars to reflect changes in configurable values
    /// </summary>
    private void UpdateDPMValues()
    {
        if (dpm.gamepieceNames[0] == null) primaryGamepieceText.text = "Primary Gamepiece:  NOT CONFIGURED";
        else primaryGamepieceText.text = "Primary Gamepiece:  " + dpm.gamepieceNames[0];

        if (dpm.gamepieceNames[1] == null) secondaryGamepieceText.text = "Secondary Gamepiece:  NOT CONFIGURED";
        else secondaryGamepieceText.text = "Secondary Gamepiece:  " + dpm.gamepieceNames[1];

        primaryCountText.text = "Spawned: " + dpm.spawnedGamepieces[0].Count + "\nHeld: " + dpm.objectsHeld[0].Count;
        secondaryCountText.text = "Spawned: " + dpm.spawnedGamepieces[1].Count + "\nHeld: " + dpm.objectsHeld[1].Count;

        if (configuring)
        {
            if (dpm.gamepieceNames[configuringIndex] == null) configuringText.text = "Gamepiece not defined yet!";
            else configuringText.text = "Configuring:  " + dpm.gamepieceNames[configuringIndex];

            releaseMechanismText.text = "Current Part:  " + dpm.releaseNode[configuringIndex].name;
            intakeMechanismText.text = "Current Part:  " + dpm.intakeNode[configuringIndex].name;

            if (configuringIndex == 0)
            {
                intakeControlText.text = InputControl.GetButton(Controls.buttons[0].pickupPrimary).ToString();
                releaseControlText.text = InputControl.GetButton(Controls.buttons[0].releasePrimary).ToString();
                spawnControlText.text = InputControl.GetButton(Controls.buttons[0].spawnPrimary).ToString();
            }
            else
            {
                intakeControlText.text = InputControl.GetButton(Controls.buttons[0].pickupSecondary).ToString();
                releaseControlText.text = InputControl.GetButton(Controls.buttons[0].releaseSecondary).ToString();
                spawnControlText.text = InputControl.GetButton(Controls.buttons[0].spawnSecondary).ToString();
            }
        }
    }

    /// <summary>
    /// Updates DPM values regarding release position vector and release velocity values
    /// </summary>
    private void UpdateVectorConfiguration()
    {
        if (holdCount > 0 && !isEditing) //This indicates that any of the configuration increment buttons are being pressed
        {
            if (deltaOffsetX != 0)
            {
                if (holdCount < holdThreshold) dpm.ChangeOffsetX(deltaOffsetX, configuringIndex);
                else dpm.ChangeOffsetX(deltaOffsetX * 5, configuringIndex);
            }
            else if (deltaOffsetY != 0)
            {
                if (holdCount < holdThreshold) dpm.ChangeOffsetY(deltaOffsetY, configuringIndex);
                else dpm.ChangeOffsetY(deltaOffsetY * 5, configuringIndex);
            }
            else if (deltaOffsetZ != 0)
            {
                if (holdCount < holdThreshold) dpm.ChangeOffsetZ(deltaOffsetZ, configuringIndex);
                else dpm.ChangeOffsetZ(deltaOffsetZ * 5, configuringIndex);
            }
            else if (deltaReleaseSpeed != 0)
            {
                if (holdCount < holdThreshold) dpm.ChangeReleaseSpeed(deltaReleaseSpeed, configuringIndex);
                else dpm.ChangeReleaseSpeed(deltaReleaseSpeed * 5, configuringIndex);
            }
            else if (deltaReleaseHorizontal != 0)
            {
                if (holdCount < holdThreshold) dpm.ChangeReleaseHorizontalAngle(deltaReleaseHorizontal, configuringIndex);
                else dpm.ChangeReleaseHorizontalAngle(deltaReleaseHorizontal * 5, configuringIndex);
            }
            else if (deltaReleaseVertical != 0)
            {
                if (holdCount < holdThreshold) dpm.ChangeReleaseVerticalAngle(deltaReleaseVertical, configuringIndex);
                else dpm.ChangeReleaseVerticalAngle(deltaReleaseVertical * 5, configuringIndex);
            }
            holdCount++;
        }

        if (!isEditing)
        {

            xOffsetEntry.GetComponent<InputField>().text = dpm.positionOffset[configuringIndex][0].ToString();
            yOffsetEntry.GetComponent<InputField>().text = dpm.positionOffset[configuringIndex][1].ToString();
            zOffsetEntry.GetComponent<InputField>().text = dpm.positionOffset[configuringIndex][2].ToString();

            releaseSpeedEntry.GetComponent<InputField>().text = dpm.releaseVelocity[configuringIndex][0].ToString();
            releaseHorizontalEntry.GetComponent<InputField>().text = dpm.releaseVelocity[configuringIndex][1].ToString();
            releaseVerticalEntry.GetComponent<InputField>().text = dpm.releaseVelocity[configuringIndex][2].ToString();
        }


    }

    private void UpdateWindows()
    {
        if (configuring)
        {
            if (dpm.addingGamepiece)
            {
                configWindow.SetActive(false);
                dpmWindow.SetActive(false);
                defineGamepieceWindow.SetActive(true);
            }
            else if (dpm.settingSpawn != 0)
            {
                configWindow.SetActive(false);
                dpmWindow.SetActive(false);
                setSpawnWindow.SetActive(true);
            }
            else if (dpm.definingIntake)
            {
                configWindow.SetActive(false);
                dpmWindow.SetActive(false);
                defineIntakeWindow.SetActive(true);
            }
            else if (dpm.definingRelease)
            {
                configWindow.SetActive(false);
                dpmWindow.SetActive(false);
                defineReleaseWindow.SetActive(true);
            }
            else
            {
                defineGamepieceWindow.SetActive(false);
                setSpawnWindow.SetActive(false);
                defineIntakeWindow.SetActive(false);
                defineReleaseWindow.SetActive(false);
                dpmWindow.SetActive(true);
                configWindow.SetActive(true);
            }
        }

        UpdateFreeroamWindow();
        UpdateSpawnpointWindow();
        UpdateCameraWindow();
        UpdateDriverStationPanel();
    }


    #region main button functions
    /// <summary>
    /// Resets the robot
    /// </summary>
    //public void PressReset()
    //{
    //    main.ResetRobot();
    //}
    public void ChangeRobot()
    {
        GameObject panel = GameObject.Find("RobotListPanel");
        string directory = PlayerPrefs.GetString("RobotDirectory") + "\\" + panel.GetComponent<ChangeRobotScrollable>().selectedEntry;
        if (Directory.Exists(directory))
        {
            PlayerPrefs.SetString("simSelectedReplay", string.Empty);
            PlayerPrefs.SetString("simSelectedRobot", directory);
            PlayerPrefs.SetString("simSelectedRobotName", panel.GetComponent<ChangeRobotScrollable>().selectedEntry);
            main.ChangeRobot(directory);
            ToggleChangeRobotPanel();
        }
        else
        {
            UserMessageManager.Dispatch("Robot directory not found!", 5);
        }
    }

    public void ToggleChangeRobotPanel()
    {
        if (changeRobotPanel.activeSelf)
        {
            changeRobotPanel.SetActive(false);
        }
        else
        {
            EndOtherProcesses();
            changeRobotPanel.SetActive(true);
        }
    }

    public void ChangeField()
    {
        GameObject panel = GameObject.Find("FieldListPanel");
        string directory = PlayerPrefs.GetString("FieldDirectory") + "\\" + panel.GetComponent<ChangeFieldScrollable>().selectedEntry;
        if (Directory.Exists(directory))
        {
            PlayerPrefs.SetString("simSelectedReplay", string.Empty);
            PlayerPrefs.SetString("simSelectedField", directory);
            PlayerPrefs.SetString("simSelectedFieldName", panel.GetComponent<ChangeFieldScrollable>().selectedEntry);
            PlayerPrefs.Save();
            Application.LoadLevel("Scene");
        }
        else
        {
            UserMessageManager.Dispatch("Field directory not found!", 5);
        }
    }

    public void ToggleChangeFieldPanel()
    {
        if (changeFieldPanel.activeSelf)
        {
            changeFieldPanel.SetActive(false);
        }
        else
        {
            EndOtherProcesses();
            changeFieldPanel.SetActive(true);
        }

    }

    public void ChooseResetMode(int i)
    {
        switch (i)
        {
            case 1:
                main.BeginRobotReset();
                main.EndRobotReset();
                resetDropdown.GetComponent<Dropdown>().value = 0;
                break;
            case 2:
                EndOtherProcesses();
                main.IsResetting = true;
                main.BeginRobotReset();
                resetDropdown.GetComponent<Dropdown>().value = 0;
                break;
        }
    }

    /// <summary>
    /// Call this function whenever the user enters a new state (ex. selecting a new robot, using ruler function, orenting robot)
    /// </summary>
    public void EndOtherProcesses()
    {
        changeFieldPanel.SetActive(false);
        changeRobotPanel.SetActive(false);
        addRobotPanel.SetActive(false);
        exitPanel.SetActive(false);
        CloseOrientWindow();
        main.IsResetting = false;
        toolkit.ToggleRulerWindow(false);
        if (configuring)
        {
            CancelDefineGamepiece();
            CancelDefineIntake();
            CancelDefineRelease();
            CancelGamepieceSpawn();
        }
    }
    #endregion
    #region camera button functions
    //Camera Functions
    public void SwitchCameraFreeroam()
    {
        camera.SwitchCameraState(0);
    }

    public void SwitchCameraOrbit()
    {
        camera.SwitchCameraState(1);
    }

    public void SwitchCameraDriverStation()
    {
        camera.SwitchCameraState(2);
    }
    #endregion
    #region orient button functions

    public void ToggleOrientWindow()
    {
        if (isOrienting)
        {
            isOrienting = false;
            main.EndRobotReset();
        }
        else
        {
            EndOtherProcesses();
            isOrienting = true;
            main.BeginRobotReset();
        }
        orientWindow.SetActive(isOrienting);
    }

    public void OrientLeft()
    {
        main.RotateRobot(new Vector3(Mathf.PI * 0.25f, 0f, 0f));
    }
    public void OrientRight()
    {
        main.RotateRobot(new Vector3(-Mathf.PI * 0.25f, 0f, 0f));
    }
    public void OrientForward()
    {
        main.RotateRobot(new Vector3(0f, 0f, Mathf.PI * 0.25f));
    }
    public void OrientBackward()
    {
        main.RotateRobot(new Vector3(0f, 0f, -Mathf.PI * 0.25f));
    }

    public void DefaultOrientation()
    {
        main.ResetRobotOrientation();
        orientWindow.SetActive(isOrienting = false);
    }

    public void SaveOrientation()
    {
        main.SaveRobotOrientation();
        orientWindow.SetActive(isOrienting = false);
    }

    public void CloseOrientWindow()
    {
        isOrienting = false;
        orientWindow.SetActive(isOrienting);
        main.EndRobotReset();
    }

    #endregion
    #region driver practice mode button functions
    /// <summary>
    /// Toggles the Driver Practice Mode window
    /// </summary>
    public void DPMToggleWindow()
    {
        if (dpmWindowOn)
        {
            dpmWindowOn = false;

        }
        else
        {
            EndOtherProcesses();
            dpmWindowOn = true;
        }
        dpmWindow.SetActive(dpmWindowOn);
    }

    /// <summary>
    /// Sets the driver practice mode to either be enabled or disabled, depending on what state it was at before.
    /// </summary>
    public void DPMToggle()
    {
        if (!dpm.modeEnabled)
        {
            dpm.modeEnabled = true;
            enableDPMText.text = "Disable Driver Practice Mode";
            lockPanel.SetActive(false);

        }
        else
        {
            if (configuring) UserMessageManager.Dispatch("You must close the configuration window first!", 5);
            else
            {
                enableDPMText.text = "Enable Driver Practice Mode";
                dpm.displayTrajectories[0] = false;
                dpm.displayTrajectories[1] = false;
                dpm.modeEnabled = false;
                lockPanel.SetActive(true);
            }

        }
    }

    /// <summary>
    /// Clears all the gamepieces sharing the same name as the ones that have been configured from the field.
    /// </summary>
    public void ClearGamepieces()
    {
        dpm.ClearGamepieces();
    }

    /// <summary>
    /// Toggles the display of primary gamepiece release trajectory.
    /// </summary>
    public void DisplayTrajectoryPrimary()
    {
        dpm.displayTrajectories[0] = !dpm.displayTrajectories[0];
    }

    /// <summary>
    /// Toggles the display of primary gamepiece release trajectory.
    /// </summary>
    public void DisplayTrajectorySecondary()
    {
        dpm.displayTrajectories[1] = !dpm.displayTrajectories[1];
    }

    /// <summary>
    /// Opens the configuration window and sets it up for the primary gamepiece
    /// </summary>
    public void DPMConfigurePrimary()
    {
        if (dpm.modeEnabled)
        {
            EndOtherProcesses();
            configuring = true;
            configuringIndex = 0;
            configHeaderText.text = "Configuration Menu - Primary Gamepiece";
            configWindow.SetActive(true);
            dpm.displayTrajectories[0] = true;
            dpm.displayTrajectories[1] = false;
        }
        else UserMessageManager.Dispatch("You must enable Driver Practice Mode first!", 5);
    }

    /// <summary>
    /// Opens the configuration window and sets it up for the secondary gamepiece
    /// </summary>
    public void DPMConfigureSecondary()
    {
        if (dpm.modeEnabled)
        {
            EndOtherProcesses();
            configuring = true;
            configuringIndex = 1;
            configHeaderText.text = "Configuration Menu - Secondary Gamepiece";
            configWindow.SetActive(true);
            dpm.displayTrajectories[0] = false;
            dpm.displayTrajectories[1] = true;
        }
        else UserMessageManager.Dispatch("You must enable Driver Practice Mode first!", 5);
    }

    /// <summary>
    /// Spawns the primary gamepiece at its defined spawn location, or at the field's origin if one hasn't been defined
    /// </summary>
    public void SpawnGamepiecePrimary()
    {
        dpm.SpawnGamepiece(0);
    }

    /// <summary>
    /// Spawns the secondary gamepiece at its defined spawn location, or at the field's origon if one hasn't been defined.
    /// </summary>
    public void SpawnGamepieceSecondary()
    {
        dpm.SpawnGamepiece(1);
    }

    #endregion
    #region dpm configuration button functions
    public void CloseConfigurationWindow()
    {
        configWindow.SetActive(false);
        configuring = false;
        dpm.displayTrajectories[configuringIndex] = false;
        dpm.Save();
    }

    public void DefineGamepiece()
    {
        EndOtherProcesses();
        dpm.DefineGamepiece(configuringIndex);
    }

    public void CancelDefineGamepiece()
    {
        dpm.addingGamepiece = false;
    }

    public void DefineIntake()
    {
        EndOtherProcesses();
        dpm.DefineIntake(configuringIndex);
    }

    public void CancelDefineIntake()
    {
        dpm.definingIntake = false;
    }

    public void HighlightIntake()
    {
        dpm.HighlightNode(dpm.intakeNode[configuringIndex].name);
    }

    public void DefineRelease()
    {
        EndOtherProcesses();
        dpm.DefineRelease(configuringIndex);
    }

    public void CancelDefineRelease()
    {
        dpm.definingRelease = false;
    }

    public void HighlightRelease()
    {
        dpm.HighlightNode(dpm.releaseNode[configuringIndex].name);
    }

    public void SetGamepieceSpawn()
    {
        EndOtherProcesses();
        dpm.StartGamepieceSpawn(configuringIndex);
    }

    public void CancelGamepieceSpawn()
    {
        dpm.FinishGamepieceSpawn();
    }



    public void ChangeOffsetX(int sign)
    {
        deltaOffsetX = offsetIncrement * sign;
        holdCount++;
    }

    public void ChangeOffsetY(int sign)
    {
        deltaOffsetY = offsetIncrement * sign;
        holdCount++;
    }

    public void ChangeOffsetZ(int sign)
    {
        deltaOffsetZ = offsetIncrement * sign;
        holdCount++;
    }

    public void ChangeReleaseSpeed(int sign)
    {
        deltaReleaseSpeed = speedIncrement * sign;
        holdCount++;
    }

    public void ChangeHorizontalAngle(int sign)
    {
        deltaReleaseHorizontal = angleIncrement * sign;
        holdCount++;
    }

    public void ChangeVerticalAngle(int sign)
    {
        deltaReleaseVertical = angleIncrement * sign;
        holdCount++;
    }

    public void ReleaseConfigurationButton()
    {
        deltaOffsetX = 0;
        deltaOffsetY = 0;
        deltaOffsetZ = 0;

        deltaReleaseSpeed = 0;
        deltaReleaseHorizontal = 0;
        deltaReleaseVertical = 0;
        holdCount = 0;
    }

    public void StartEdit()
    {
        isEditing = true;
    }

    public void EndEdit()
    {
        isEditing = false;
    }

    public void SyncInputFieldEntry()
    {
        if (isEditing)
        {
            float temp = 0;
            if (float.TryParse(xOffsetEntry.GetComponent<InputField>().text, out temp))
                dpm.positionOffset[configuringIndex][0] = temp;
            temp = 0;
            if (float.TryParse(yOffsetEntry.GetComponent<InputField>().text, out temp))
                dpm.positionOffset[configuringIndex][1] = temp;
            temp = 0;
            if (float.TryParse(zOffsetEntry.GetComponent<InputField>().text, out temp))
                dpm.positionOffset[configuringIndex][2] = temp;
            temp = 0;
            if (float.TryParse(releaseSpeedEntry.GetComponent<InputField>().text, out temp))
                dpm.releaseVelocity[configuringIndex][0] = temp;
            temp = 0;
            if (float.TryParse(releaseHorizontalEntry.GetComponent<InputField>().text, out temp))
                dpm.releaseVelocity[configuringIndex][1] = temp;
            temp = 0;
            if (float.TryParse(releaseVerticalEntry.GetComponent<InputField>().text, out temp))
                dpm.releaseVelocity[configuringIndex][2] = temp;
        }
    }
    #endregion
    #region control customization functions

    public void SetNewControl(int index)
    {
        settingControl = index;
    }

    private void ListenControl()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            settingControl = 0;
            return;
        }

        KeyMapping[] keys = GetComponentsInChildren<KeyMapping>();

        foreach (KeyMapping key in keys)
        {
            if (InputControl.GetButtonDown(key))
            {
                if (configuringIndex == 0)
                {
                    if (settingControl == 1)
                    {
                        InputControl.GetButton(Controls.buttons[0].pickupPrimary);
                    }
                    else if (settingControl == 2) InputControl.GetButton(Controls.buttons[0].pickupPrimary);
                    else InputControl.GetButton(Controls.buttons[0].spawnPrimary);
                }
                else
                {
                    if (settingControl == 1) InputControl.GetButton(Controls.buttons[0].pickupSecondary);
                    else if (settingControl == 2) InputControl.GetButton(Controls.buttons[0].releaseSecondary);
                    else InputControl.GetButton(Controls.buttons[0].spawnPrimary);
                }
                Controls.Save();
                settingControl = 0;
            }
        }
    }


        //OLD; remove once the new one is tested 7/27/2017
        //foreach (KeyCode vKey in System.Enum.GetValues(typeof(KeyCode)))
        //{
        //    if (Input.GetKeyDown(vKey))
        //    {
        //        if (configuringIndex == 0)
        //        {
        //            if (settingControl == 1)
        //            {
        //                //Controls.SetControl((int)Controls.Control.PickupPrimary, vKey);
        //                InputControl.GetButton(Controls.buttons[0].pickupPrimary);
        //                Controls.Load();
        //            }
        //            else if (settingControl == 2) Controls.SetControl((int)Controls.Control.ReleasePrimary, vKey);
        //            else Controls.SetControl((int)Controls.Control.SpawnPrimary, vKey);
        //        }
        //        else
        //        {
        //            if (settingControl == 1) Controls.SetControl((int)Controls.Control.PickupSecondary, vKey);
        //            else if (settingControl == 2) Controls.SetControl((int)Controls.Control.ReleaseSecondary, vKey);
        //            else Controls.SetControl((int)Controls.Control.SpawnPrimary, vKey);
        //        }
        //        Controls.SaveControls();
        //        settingControl = 0;
        //    }
        //}
 
    #endregion
    #region robot camera functions
    /// <summary>
    /// Updates the robot camera view window
    /// </summary>
    private void UpdateCameraWindow()
    {
        //Make sure robot camera exists first
        if (robotCamera == null && robotCameraList.GetComponent<RobotCamera>() != null)
        {
            robotCamera = robotCameraList.GetComponent<RobotCamera>();
        }

        if (robotCamera != null)
        {
            //Can use robot view when dynamicCamera is active
            if (usingRobotView && main.dynamicCameraObject.activeSelf)
            {
                robotCamera = robotCameraList.GetComponent<RobotCamera>();
                Debug.Log(robotCamera.CurrentCamera);

                //Make sure there is camera on robot
                if (robotCamera.CurrentCamera != null)
                {
                    robotCamera.CurrentCamera.SetActive(true);
                    robotCamera.CurrentCamera.GetComponent<Camera>().targetTexture = robotCameraView;
                    //Toggle the robot camera using Q (should be changed later)
                    if (Input.GetKeyDown(KeyCode.Q))
                    {
                        robotCamera.CurrentCamera.GetComponent<Camera>().targetTexture = null;
                        robotCamera.ToggleCamera();
                        robotCamera.CurrentCamera.GetComponent<Camera>().targetTexture = robotCameraView;

                    }
                    //Debug.Log("Robot camera view is " + robotCameraView.name);
                    //Debug.Log(robotCamera.CurrentCamera);
                }
            }
            //Don't allow using robot view window when users are currently using one of the robot view
            else if (usingRobotView && !main.dynamicCameraObject.activeSelf)
            {
                UserMessageManager.Dispatch("You can only use robot view window when you are not in robot view mode!", 2f);
                usingRobotView = false;
                robotCameraViewWindow.SetActive(false);
            }
            //Free the target texture of the current camera when the window is closed (for normal toggle camera function)
            else
            {
                if (robotCamera.CurrentCamera != null)
                    robotCamera.CurrentCamera.GetComponent<Camera>().targetTexture = null;
            }
        }
    }

    /// <summary>
    /// Toggles the state of usingRobotView when the button "Toggle Robot Camera" is clicked
    /// </summary>
    public void ToggleCameraWindow()
    {
        usingRobotView = !usingRobotView;
        robotCameraViewWindow.SetActive(usingRobotView);
        if (usingRobotView)
        {
            robotCamera.CurrentCamera.GetComponent<Camera>().targetTexture = robotCameraView;
        }
        else
        {
            //Free the target texture and disable the camera since robot camera has more depth than main camera
            robotCamera.CurrentCamera.GetComponent<Camera>().targetTexture = null;
            robotCamera.CurrentCamera.SetActive(false);
        }
    }

    /// <summary>
    /// Toggles the state of showing or hiding the robot indicator
    /// </summary>
    public void ToggleCameraIndicator()
    {
        indicatorActive = !indicatorActive;
        if (indicatorActive)
        {
            //Only allow the camera configuration when the indicator is active
            showCameraButton.GetComponentInChildren<Text>().text = "Hide Camera";
            configureRobotCameraButton.SetActive(true);
        }
        else
        {
            showCameraButton.GetComponentInChildren<Text>().text = "Show Camera";
            configureRobotCameraButton.SetActive(false);
            //Close the panel when indicator is not active and stop all configuration
            configureCameraPanel.SetActive(false);
            robotCamera.IsChangingHeight = robotCamera.SelectingNode = robotCamera.ChangingCameraPosition = false;
            configureRobotCameraButton.GetComponentInChildren<Text>().text = "Configure Robot Camera";
            robotCamera.SelectingNode = false;
            robotCamera.SelectedNode = null;
        }
        robotCameraIndicator.SetActive(indicatorActive);
    }

    /// <summary>
    /// Activate the configure camera panel and start position configuration
    /// </summary>
    public void ConfigureCameraPosition()
    {
        robotCamera.ChangingCameraPosition = !robotCamera.ChangingCameraPosition;
        configureCameraPanel.SetActive(robotCamera.ChangingCameraPosition);
        if (robotCamera.ChangingCameraPosition)
        {
            //Update the node where current camera is attached to
            cameraNodeText.text = "Current Node: " + robotCamera.CurrentCamera.transform.parent.gameObject.name;
            configureRobotCameraButton.GetComponentInChildren<Text>().text = "End Configuration";
        }
        else
        {
            configureRobotCameraButton.GetComponentInChildren<Text>().text = "Configure Robot Camera";
        }
    }

    /// <summary>
    /// Toggle between changing position along horizontal plane or changing height
    /// </summary>
    public void ToggleConfigurationMode()
    {
        robotCamera.IsChangingHeight = !robotCamera.IsChangingHeight;
        if (robotCamera.IsChangingHeight)
        {
            cameraConfigurationModeButton.GetComponentInChildren<Text>().text = "Configure Horizontal Plane";
        }
        else
        {
            cameraConfigurationModeButton.GetComponentInChildren<Text>().text = "Configure Height";
        }
    }

    /// <summary>
    /// Going into the state of selecting a new node and confirming it
    /// </summary>
    public void ToggleChangeNode()
    {
        if (!robotCamera.SelectingNode && robotCamera.SelectedNode == null)
        {
            robotCamera.DefineNode(); //Start selecting a new node
            changeCameraNodeButton.GetComponentInChildren<Text>().text = "Confirm";
            cancelNodeSelectionButton.SetActive(true);
        }
        else if (robotCamera.SelectingNode && robotCamera.SelectedNode != null)
        {
            //Change the node where camera is attached to, clear selected node, and update name of current node
            robotCamera.ChangeNodeAttachment();
            cameraNodeText.text = "Current Node: " + robotCamera.CurrentCamera.transform.parent.gameObject.name;
            changeCameraNodeButton.GetComponentInChildren<Text>().text = "Change Attachment Node";
            cancelNodeSelectionButton.SetActive(false);

        }
    }

    public void CancelNodeSelection()
    {
        robotCamera.SelectedNode = null;
        robotCamera.SelectingNode = false;
        cameraNodeText.text = "Current Node: " + robotCamera.CurrentCamera.transform.parent.gameObject.name;
        changeCameraNodeButton.GetComponentInChildren<Text>().text = "Change Attachment Node";
        cancelNodeSelectionButton.SetActive(false);
    }
    #endregion

    /// <summary>
    /// Pop reset instructions when main is in reset spawnpoint mode
    /// </summary>
    private void UpdateSpawnpointWindow()
    {
        if (main.IsResetting)
        {
            spawnpointWindow.SetActive(true);
        }
        else
        {
            spawnpointWindow.SetActive(false);
        }
    }

    /// <summary>
    /// Pop freeroam instructions when using freeroam camera, won't show up again if the user closes it
    /// </summary>
    private void UpdateFreeroamWindow()
    {
        if (camera.cameraState.GetType().Equals(typeof(DynamicCamera.FreeroamState)) && !freeroamWindowClosed)
        {
            if (!freeroamWindowClosed)
            {
                freeroamCameraWindow.SetActive(true);
            }

        }
        else if (!camera.cameraState.GetType().Equals(typeof(DynamicCamera.FreeroamState)))
        {
            freeroamCameraWindow.SetActive(false);
        }
    }


    public void CloseFreeroamWindow()
    {
        freeroamCameraWindow.SetActive(false);
        freeroamWindowClosed = true;
    }

    /// <summary>
    /// Activate driver station panel if the main camera is in driver station state
    /// </summary>
    private void UpdateDriverStationPanel()
    {
        driverStationPanel.SetActive(camera.cameraState.GetType().Equals(typeof(DynamicCamera.DriverStationState)));
    }

    /// <summary>
    /// Change to driver station view to the opposite side
    /// </summary>
    public void ToggleDriverStation()
    {
        oppositeSide = !oppositeSide;
        camera.SwitchCameraState(new DynamicCamera.DriverStationState(camera, oppositeSide));
    }


    public void ShowControlPanel(bool show)
    {
        if (show)
        {
            EndOtherProcesses();
            AuxFunctions.FindObject(canvas, "FullscreenPanel").SetActive(true);
        }
        else
        {
            AuxFunctions.FindObject(canvas, "FullscreenPanel").SetActive(false);
        }
    }

    public void MainMenuExit(string option)
    {
        EndOtherProcesses();
        switch (option)
        {
            case "open":
                exitPanel.SetActive(true);
                break;
            case "exit":
                Application.LoadLevel("MainMenu");
                break;

            case "cancel":
                exitPanel.SetActive(false);
                break;
        }

    }
}