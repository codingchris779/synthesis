﻿using BulletUnity;
using Synthesis.BUExtensions;
using Synthesis.DriverPractice;
using Synthesis.FEA;
using Synthesis.FSM;
using Synthesis.Input;
using Synthesis.MixAndMatch;
using Synthesis.RN;
using Synthesis.Camera;
using Synthesis.Sensors;
using Synthesis.StatePacket;
using Synthesis.States;
using Synthesis.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using BulletSharp;
using Synthesis.GUI;
using UnityEngine.Networking;
using Synthesis.Field;

namespace Synthesis.Robot
{
    /// <summary>
    /// To be attached to all robot parent objects.
    /// Handles all robot-specific interaction such as driving joints, resetting, and orienting robot.
    /// </summary>
    public class RobotBase : NetworkBehaviour
    {
        /// <summary>
        /// The <see cref="UnityPacket.OutputStatePacket"/> of the robot.
        /// </summary>
        public UnityPacket.OutputStatePacket Packet { get; set; }


        /// <summary>
        /// Informational class for emulation to grab encoder tick count
        /// </summary>
        public sealed class EmuNetworkInfo
        {
            public RobotSensor RobotSensor;
            public RigidNode_Base wheel;
            public double previousEuler = 0;
            public Vector3 previousPosition = new Vector3();
            public double wheel_radius;

            // for emulation data
            public double encoderTickCount;
        }

        /// <summary>
        /// Generates list of attached encoder sensors on robot nodes
        /// </summary>
        public List<EmuNetworkInfo> emuList;

        /// <summary>
        /// Represents the index specifying what control scheme the robot should use.
        /// </summary>
        public int ControlIndex { get; set; } = 0;

        /// <summary>
        /// The directory from which the robot was loaded.
        /// </summary>
        public string RobotDirectory { get; private set; }

        /// <summary>
        /// The name of the folder continaing the robot files.
        /// </summary>
        public string RobotName { get; private set; }

        /// <summary>
        /// The calculated speed of the robot.
        /// </summary>
        public float Speed { get; protected set; }

        /// <summary>
        /// The calculated weight of the robot.
        /// </summary>
        public float Weight { get; protected set; }

        /// <summary>
        /// The calculated angular velocity of the robot.
        /// </summary>
        public float AngularVelocity { get; protected set; }

        /// <summary>
        /// The calculated acceleration of the robot.
        /// </summary>
        public float Acceleration { get; protected set; }

        /// <summary>
        /// The <see cref="RigidNode_Base"/> generated by the robot files loaded.
        /// </summary>
        public RigidNode RootNode { get; private set; }
        
        /// <summary>
        /// The starting position of the robot.
        /// </summary>
        protected Vector3 robotStartPosition = new Vector3(0f, 1f, 0f);

        /// <summary>
        /// The starting orientation of the robot.
        /// </summary>
        public BulletSharp.Math.Matrix robotStartOrientation = BulletSharp.Math.Matrix.Identity;

        /// <summary>
        /// The default state packet sent by the robot.
        /// </summary>
        protected readonly UnityPacket.OutputStatePacket.DIOModule[] emptyDIO = new UnityPacket.OutputStatePacket.DIOModule[2];

        private float oldSpeed;

        /// <summary>
        /// Called once per frame to ensure all rigid bodie components are activated
        /// </summary>
        void Update()
        {
            UpdateTransform();
        }

        /// <summary>
        /// Called once every physics step (framerate independent) to drive motor joints as well as handle the resetting of the robot
        /// </summary>
        void FixedUpdate()
        {
            if (RootNode != null)
                UpdateMotors();

            UpdatePhysics();
        }

        /// <summary>
        /// Initializes physical robot based off of robot directory.
        /// </summary>
        /// <param name="directory">folder directory of robot</param>
        /// <returns></returns>
        public bool InitializeRobot(string directory)
        {
            RobotDirectory = directory;
            RobotName = new DirectoryInfo(directory).Name;

            //Deletes all nodes if any exist, take the old node transforms out from the robot object
            foreach (Transform child in transform)
                Destroy(child.gameObject);

            robotStartPosition = FieldDataHandler.robotSpawn != new Vector3(99999, 99999, 99999) ? FieldDataHandler.robotSpawn : robotStartPosition;
            transform.position = robotStartPosition; //Sets the position of the object to the set spawn point

            if (!File.Exists(directory + "\\skeleton.bxdj"))
                return false;

            OnInitializeRobot();

            //Loads the node and skeleton data
            RigidNode_Base.NODE_FACTORY = delegate (Guid guid) { return new RigidNode(guid); };

            List<RigidNode_Base> nodes = new List<RigidNode_Base>();
            RootNode = BXDJSkeleton.ReadSkeleton(directory + "\\skeleton.bxdj") as RigidNode;
            RootNode.ListAllNodes(nodes);

            Debug.Log(RootNode.driveTrainType.ToString());

            emuList = new List<EmuNetworkInfo>();

            foreach (RigidNode_Base Base in RootNode.ListAllNodes())
            {
                try
                {
                    if (Base.GetSkeletalJoint().attachedSensors != null)
                    {
                        foreach (RobotSensor sensor in Base.GetSkeletalJoint().attachedSensors)
                        {
                            if(sensor.type == RobotSensorType.ENCODER)
                            {
                                EmuNetworkInfo emuStruct = new EmuNetworkInfo();
                                emuStruct.encoderTickCount = 0;
                                emuStruct.RobotSensor = sensor;
                                emuStruct.wheel = Base;
                                emuStruct.wheel_radius = 0;

                                emuList.Add(emuStruct);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.Log(e.ToString());
                }
            }

            //Initializes the wheel variables
            float collectiveMass = 0f;

            if (!ConstructRobot(nodes, ref collectiveMass))
                return false;

            foreach (BRaycastRobot r in GetComponentsInChildren<BRaycastRobot>())
            {
                r.RaycastRobot.OverrideMass = collectiveMass;
                r.RaycastRobot.RootRigidBody = (RigidBody)((RigidNode)nodes[0]).MainObject.GetComponent<BRigidBody>().GetCollisionObject();
            }

            OnRobotSetup();

            RotateRobot(robotStartOrientation);

            return true;
        }

        /// <summary>
        /// Rotates the robot about its origin by a mathematical 4x4 matrix
        /// </summary>
        public void RotateRobot(BulletSharp.Math.Matrix rotationMatrix)
        {
            BulletSharp.Math.Vector3? origin = null;

            foreach (RigidNode n in RootNode.ListAllNodes())
            {
                BRigidBody br = n.MainObject.GetComponent<BRigidBody>();

                if (br == null)
                    continue;

                RigidBody r = (RigidBody)br.GetCollisionObject();

                if (origin == null)
                    origin = r.CenterOfMassPosition;

                BulletSharp.Math.Matrix rotationTransform = new BulletSharp.Math.Matrix
                {
                    Basis = rotationMatrix,
                    Origin = BulletSharp.Math.Vector3.Zero
                };

                BulletSharp.Math.Matrix currentTransform = r.WorldTransform;
                BulletSharp.Math.Vector3 pos = currentTransform.Origin;
                currentTransform.Origin -= origin.Value;
                currentTransform *= rotationTransform;
                currentTransform.Origin += origin.Value;

                r.WorldTransform = currentTransform;
            }
        }

        /// <summary>
        /// Get the total weight of the robot
        /// </summary>
        /// <returns></returns>
        public float GetWeight()
        {
            float weight = 0;

            foreach (Transform child in gameObject.transform)
                if (child.GetComponent<BRigidBody>() != null)
                    weight += child.GetComponent<BRigidBody>().mass;

            return weight;
        }

        /// <summary>
        /// Returns true if the robot has a mecanum drive.
        /// </summary>
        /// <returns></returns>
        public virtual bool IsMecanum()
        {
            return false;
        }

        /// <summary>
        /// Constructs the robot from the given list of nodes and number of wheels,
        /// and updates the collective mass.
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="numWheels"></param>
        /// <param name="collectiveMass"></param>
        /// <returns></returns>
        protected virtual bool ConstructRobot(List<RigidNode_Base> nodes, ref float collectiveMass)
        {
            //Initializes the nodes
            foreach (RigidNode_Base n in nodes)
            {
                RigidNode node = (RigidNode)n;
                node.CreateTransform(transform);

                if (!node.CreateMesh(RobotDirectory + "\\" + node.ModelFileName))
                    return false;

                if (node.PhysicalProperties != null)
                    collectiveMass += node.PhysicalProperties.mass;
            }

            RootNode.GenerateWheelInfo();

            foreach (RigidNode_Base n in nodes)
                ((RigidNode)n).CreateJoint(this);

            return true;
        }

        /// <summary>
        /// Updates all motors on the robot.
        /// </summary>
        protected virtual void UpdateMotors(float[] pwm = null)
        {
            DriveJoints.UpdateAllMotors(RootNode, pwm ?? DriveJoints.GetPwmValues(Packet == null ? emptyDIO : Packet.dio, ControlIndex, IsMecanum()), emuList);
        }

        /// <summary>
        /// Update the stats for robot depending on whether it's metric or not
        /// </summary>
        protected virtual void UpdatePhysics()
        {
            GameObject mainNode = transform.GetChild(0).gameObject;

            //calculates stats of robot
            if (mainNode != null)
            {
                float currentSpeed = mainNode.GetComponent<BRigidBody>().GetCollisionObject().InterpolationLinearVelocity.Length;

                Speed = (float)Math.Round(Math.Abs(currentSpeed), 3);
                Weight = (float)Math.Round(GetWeight(), 3);
                AngularVelocity = (float)Math.Round(Math.Abs(mainNode.GetComponent<BRigidBody>().angularVelocity.magnitude), 3);
                Acceleration = (float)Math.Round((currentSpeed - oldSpeed) / Time.deltaTime, 3);
                oldSpeed = currentSpeed;
            }
        }

        /// <summary>
        /// Updates positional information of the robot.
        /// </summary>
        protected virtual void UpdateTransform()
        {
            BRigidBody rigidBody = GetComponentInChildren<BRigidBody>();

            if (rigidBody == null)
                AppModel.ErrorToMenu("Could not generate robot physics data.");
            else if (!rigidBody.GetCollisionObject().IsActive)
                rigidBody.GetCollisionObject().Activate();
        }

        /// <summary>
        /// Called when the robot is initialized, before robot generation.
        /// </summary>
        protected virtual void OnInitializeRobot() { }

        /// <summary>
        /// Called when the robot is setup, after robot generation.
        /// </summary>
        protected virtual void OnRobotSetup() { }
    }
}
