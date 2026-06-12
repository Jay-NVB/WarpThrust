
using System;
using System.Collections.Generic;
using Waterfall;
using UnityEngine;

namespace WarpThrust
{
    public class WarpEngine
    {
        public string Effect = "";

        public ModuleEngines Engine = null;
        public ModuleWaterfallFX Waterfall = null;

        public float MaxThrust = 0;
        public float MinThrottle = 0;
        public float maxFuelFlow = 0;
        public float simThrottle = 1;

        public float EcRate = 0;
        public float ISP = 0;
        public int ISPindex = 0;

        public Vector3 EngineDir = Vector3.zero;

        public List<int> PropId = new List<int>();
        public List<string> PropName = new List<string>();
        public List<float> PropFlow = new List<float>();
    }

    public class WarpThrust : PartModule
    {
        [KSPField(guiActiveEditor = false, isPersistant = false)]
        float EcRate = 0; //kW

        const string TAG = "[WarpThrust]";
        const string groupName = "WarpThrust";
        const string toggleISP = "Toggle constant ISP";
        const string toggleRot = "Toggle Thrust direction";
        const string toggleUse = "Toggle Engine activation";
        const string toggleRotate = "Toggle automatic rotation";

        bool timeWarp = false;
        bool constantISP = false;
        bool useRotation = true;
        bool useActive = true;
        bool rotate = true;
        bool active = false;
        float Throttle = 0f;
        float oldThrottle = 1f;
        int transforms = 0;

        List<ModuleEngines> Engines;
        ModuleEnginesFX EngineFX;
        ModuleWaterfallFX WaterfallFX;

        List<WarpEngine> WarpEngines = new List<WarpEngine>();

        Vector3 TotalDir = Vector3.zero;
        Vector3 WantedRot = Vector3.zero;

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Currently: Keeping Power constant", active = true, groupName = groupName, groupDisplayName = groupName)]
        protected void ConstantISP()
        {
            constantISP = true;
            Events["ConstantEC"].active = true;
            Events["ConstantISP"].active = false;
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Currently: Keeping ISP constant", active = true, groupName = groupName, groupDisplayName = groupName)]
        protected void ConstantEC()
        {
            constantISP = false;
            Events["ConstantEC"].active = false;
            Events["ConstantISP"].active = true;
        }

        [KSPAction(toggleISP)]
        public void ToggleISP(KSPActionParam param)
        {
            constantISP = !constantISP;
            Events["ConstantEC"].active = constantISP;
            Events["ConstantISP"].active = !constantISP;
        }


        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Currently: Thrusting SAS", active = true, groupName = groupName, groupDisplayName = groupName)]
        protected void UseRot()
        {
            useRotation = true;
            Events["UseSAS"].active = true;
            Events["UseRot"].active = false;
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Currently: Thrusting forward", active = true, groupName = groupName, groupDisplayName = groupName)]
        protected void UseSAS()
        {
            useRotation = false;
            Events["UseSAS"].active = false;
            Events["UseRot"].active = true;
        }

        [KSPAction(toggleRot)]
        public void ToggleRot(KSPActionParam param)
        {
            useRotation = !useRotation;
            Events["UseSAS"].active = useRotation;
            Events["UseRot"].active = !useRotation;
        }


        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Currently: Using all engines", active = true, groupName = groupName, groupDisplayName = groupName)]
        protected void UseActive()
        {
            useActive = true;
            Events["UseThrottle"].active = true;
            Events["UseActive"].active = false;
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Currently: Only using an active engine", active = true, groupName = groupName, groupDisplayName = groupName)]
        protected void UseThrottle()
        {
            useActive = false;
            Events["UseThrottle"].active = false;
            Events["UseActive"].active = true;
        }

        [KSPAction(toggleUse)]
        public void ToggleUse(KSPActionParam param)
        {
            useActive = !useActive;
            Events["UseThrottle"].active = useActive;
            Events["UseActive"].active = !useActive;
        }


        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Currently: Not Rotating", active = true, groupName = groupName, groupDisplayName = groupName)]
        protected void Rotate()
        {
            rotate = true;
            Events["NotRotate"].active = true;
            Events["Rotate"].active = false;
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Currently: Automatically Rotating", active = true, groupName = groupName, groupDisplayName = groupName)]
        protected void NotRotate()
        {
            rotate = false;
            Events["NotRotate"].active = false;
            Events["Rotate"].active = true;
        }

        [KSPAction(toggleRotate)]
        public void ToggleRotate(KSPActionParam param)
        {
            rotate = !rotate;
            Events["NotRotate"].active = rotate;
            Events["Rotate"].active = !rotate;
        }


        public ManeuverNode FindactiveManeuver(Vessel vessel)
        {
            List<ManeuverNode> nodes = new List<ManeuverNode>();
            nodes = vessel.patchedConicSolver.maneuverNodes;
            ManeuverNode currentNode = null;
            double currentNodeTime = 0;
            //print(nodes.Count);
            foreach (ManeuverNode node in nodes)
            {
                if (currentNode == null || currentNodeTime > node.UT)
                {
                    currentNode = node;
                    currentNodeTime = node.UT;
                }
            }
            return currentNode;
        }

        public Vector3 GetTargetPositionAtUt(ITargetable Target, double UT)
        {
            Vector3 targetPosition = Target.GetOrbit().getPositionAtUT(UT);
            CelestialBody referenceBody = Target.GetOrbit().referenceBody;
            int i = 0;
            while (i < 10 && referenceBody != null)
            {
                print(referenceBody.name);
                targetPosition = targetPosition + referenceBody.getPositionAtUT(UT);
                if (referenceBody.GetOrbit() == null)
                {
                    break;
                }
                referenceBody = referenceBody.GetOrbit().referenceBody;
            }
            return targetPosition; 
        }

        public Vector3 GetVesselPositionAtUt(Vessel vessel, double UT)
        {
            Vector3 vesselPosition = vessel.GetOrbit().getPositionAtUT(UT);
            CelestialBody referenceBody = vessel.GetOrbit().referenceBody;
            int i = 0;
            while (i < 10 && referenceBody != null)
            {
                print(referenceBody.name);
                vesselPosition = vesselPosition + referenceBody.getPositionAtUT(UT);
                if (referenceBody.GetOrbit() == null)
                {
                    break;
                }
                referenceBody = referenceBody.GetOrbit().referenceBody;
            }
            return vesselPosition;
        }

        public Vector3 CalcWantedOrbRot(Vessel vessel, double UT, Vector3 WantedRot)
        {
            if (vessel.Autopilot.Mode == VesselAutopilot.AutopilotMode.Prograde)
            {
                return vessel.orbit.Prograde(UT);
            }
            else if (vessel.Autopilot.Mode == VesselAutopilot.AutopilotMode.Retrograde)
            {
                return -vessel.orbit.Prograde(UT);
            }
            else if (vessel.Autopilot.Mode == VesselAutopilot.AutopilotMode.Normal)
            {
                return -vessel.orbit.Normal(UT);
            }
            else if (vessel.Autopilot.Mode == VesselAutopilot.AutopilotMode.Antinormal)
            {
                return vessel.orbit.Normal(UT);
            }
            else if (vessel.Autopilot.Mode == VesselAutopilot.AutopilotMode.RadialIn)
            {
                return vessel.orbit.Radial(UT);
            }
            else if (vessel.Autopilot.Mode == VesselAutopilot.AutopilotMode.RadialOut)
            {
                return -vessel.orbit.Radial(UT);
            }
            else if (vessel.Autopilot.Mode == VesselAutopilot.AutopilotMode.Target)
            {
                Vector3 vesselPosition = GetVesselPositionAtUt(vessel, UT);
                Vector3 targetPosition = GetTargetPositionAtUt(vessel.targetObject, UT);
                return (targetPosition - vesselPosition).normalized;
            }
            else if (vessel.Autopilot.Mode == VesselAutopilot.AutopilotMode.AntiTarget)
            {
                Vector3 vesselPosition = GetVesselPositionAtUt(vessel, UT);
                Vector3 targetPosition = GetTargetPositionAtUt(vessel.targetObject, UT);
                return -(targetPosition - vesselPosition).normalized;
            }
            else if (vessel.Autopilot.Mode == VesselAutopilot.AutopilotMode.Maneuver)
            {
                ManeuverNode node = FindactiveManeuver(vessel);
                return (node.nodeRotation * node.DeltaV.normalized).normalized;
            }
            else
            {
                return WantedRot;
            }
        }

        public void Perturb(Orbit orbit, Vector3d deltaVV, double UT) //thanks persistent thrust
        {
            if (deltaVV.magnitude == 0)
                return;

            // Transpose deltaVV Y and Z to match serializedOrbit frame
            Vector3d deltaVVector_orbit = deltaVV.xzy;

            // Position vector
            Vector3d position = orbit.getRelativePositionAtUT(UT);

            // Update with current position and new velocity
            orbit.UpdateFromStateVectors(position, orbit.getOrbitalVelocityAtUT(UT) + deltaVVector_orbit, orbit.referenceBody, UT);
            orbit.Init();
            orbit.UpdateFromUT(UT);
        }

        public void FixedUpdate()
        {
            if (!active || ((int)vessel.BestSituation < 16)) //not active
            {
                return;
            }

            if (TimeWarp.CurrentRate == 1 || TimeWarp.WarpMode == TimeWarp.Modes.LOW)   //not in Timewarp
            {
                WantedRot = vessel.Autopilot.SAS.targetOrientation;                     //save current rotation if just using sas hold rotation
                foreach (WarpEngine Warpengine in WarpEngines)
                {
                    Warpengine.simThrottle = 1;
                    if (vessel.ctrlState.mainThrottle != 0)         //getting current throttle with minThrust != 0
                    {
                        if (useActive)
                        {
                            Throttle = Warpengine.Engine.requestedThrottle;
                        }
                        else
                        {
                            Throttle = vessel.ctrlState.mainThrottle;
                        }
                        Warpengine.simThrottle = Warpengine.MinThrottle + (1 - Warpengine.MinThrottle) * Throttle;
                    }

                    if (constantISP == false)                   //chaning ISP if wanted
                    {
                        Warpengine.Engine.maxFuelFlow = Warpengine.maxFuelFlow * Warpengine.simThrottle;

                        //i hate read only variables
                        Keyframe key = Warpengine.Engine.atmosphereCurve.Curve[Warpengine.ISPindex];
                        Warpengine.Engine.atmosphereCurve.Curve.RemoveKey(Warpengine.ISPindex);

                        key.value = Warpengine.ISP / Warpengine.simThrottle; // important part

                        Warpengine.Engine.atmosphereCurve.Curve.AddKey(key);
                        Warpengine.Engine.atmosphereCurve.Curve.MoveKey(Warpengine.ISPindex, key);
                    }
                    else
                    {
                        Warpengine.Engine.maxFuelFlow = Warpengine.maxFuelFlow;

                        //i hate read only variables
                        Keyframe key = Warpengine.Engine.atmosphereCurve.Curve[Warpengine.ISPindex];
                        Warpengine.Engine.atmosphereCurve.Curve.RemoveKey(Warpengine.ISPindex);

                        key.value = Warpengine.ISP; // important part

                        Warpengine.Engine.atmosphereCurve.Curve.AddKey(key);
                        Warpengine.Engine.atmosphereCurve.Curve.MoveKey(Warpengine.ISPindex, key);
                    }

                    if (timeWarp)  //giving Waterfall controll over effects after coming out of timewarp
                    {
                        if (Warpengine.Waterfall != null)
                        {
                            foreach (WaterfallController Controller in Warpengine.Waterfall.Controllers)
                            {
                                if (Controller.name == "throttle")
                                {
                                    Controller.overridden = false;
                                    Controller.overrideValue = Warpengine.MinThrottle + (1 - Warpengine.MinThrottle) * Throttle;
                                }
                            }
                        }
                        oldThrottle = 0f;
                    }
                }
                timeWarp = false;

                Throttle = vessel.ctrlState.mainThrottle;

                return;
            }

            if (timeWarp == false && Throttle != 0f)    //checking if this Engine is producing thrust in timewarp
            {
                oldThrottle = 0f;
                bool noThrottle = true;
                foreach (WarpEngine Warpengine in WarpEngines)
                {
                    noThrottle = noThrottle && Warpengine.simThrottle == 0;
                }
                if (noThrottle)
                {
                    Throttle = 0f;
                }
            }

            if (Throttle != 0f) //handeling the engine
            {
                WantedRot = CalcWantedOrbRot(vessel, Planetarium.GetUniversalTime(), WantedRot);

                if (rotate)
                {
                    part.Rigidbody.angularVelocity = Vector3.zero; //trying to set the angular velocity to 0 but doesnt work reliably
                    vessel.transform.Rotate(Quaternion.FromToRotation(vessel.ReferenceTransform.up.normalized, WantedRot).eulerAngles, Space.World);
                    vessel.SetRotation(vessel.transform.rotation);
                }

                vessel.ctrlState.mainThrottle = Throttle;
                foreach (WarpEngine Warpengine in WarpEngines)
                {
                    if (Warpengine.simThrottle == 0)
                    {
                        continue;
                    }
                    Warpengine.Engine.EngineIgnited = true;
                    part.Effect(Warpengine.Effect, Warpengine.simThrottle, -1);
                    foreach (Transform Thrusttransform in Warpengine.Engine.thrustTransforms)
                    {
                        TotalDir = TotalDir - Thrusttransform.forward;
                        transforms += 1;
                    }
                    Warpengine.EngineDir = TotalDir / transforms;

                    if (useRotation)
                    {
                        Perturb(vessel.orbit, Warpengine.EngineDir * (float)(TimeWarp.fixedDeltaTime * Warpengine.simThrottle * Warpengine.MaxThrust / vessel.totalMass), Planetarium.GetUniversalTime()); //changes orbit
                    }
                    else
                    {
                        Perturb(vessel.orbit, WantedRot * (float)(TimeWarp.fixedDeltaTime * Warpengine.simThrottle * Warpengine.MaxThrust / vessel.totalMass), Planetarium.GetUniversalTime()); //changes orbit
                    }

                    TotalDir = Vector3.zero;
                    transforms = 0;
                    if (Warpengine.EcRate != 0)
                    {
                        double Ec = 0;
                        if (constantISP)
                            Ec = part.RequestResource("ElectricCharge", Warpengine.EcRate * TimeWarp.fixedDeltaTime * Warpengine.simThrottle);
                        else
                            Ec = part.RequestResource("ElectricCharge", Warpengine.EcRate * TimeWarp.fixedDeltaTime);
                        if (Ec == 0)
                        {
                            print(TAG + " To little electric charge remaining. Shutting down the engines");
                            oldThrottle = Throttle;
                            Throttle = 0f;
                            vessel.ctrlState.mainThrottle = Throttle;
                        }
                    }
                    if (timeWarp == false)
                    {
                        if (constantISP == false)
                        {
                            if (Warpengine.simThrottle == 0)
                            {
                                print(TAG + "Infinite ISP");
                            }
                            else
                            {
                                Warpengine.Engine.maxFuelFlow = Warpengine.maxFuelFlow * Warpengine.simThrottle;

                                //i hate read only variables
                                Keyframe key = Warpengine.Engine.atmosphereCurve.Curve[Warpengine.ISPindex];
                                Warpengine.Engine.atmosphereCurve.Curve.RemoveKey(Warpengine.ISPindex);

                                key.value = Warpengine.ISP / Warpengine.simThrottle; // important part

                                Warpengine.Engine.atmosphereCurve.Curve.AddKey(key);
                                Warpengine.Engine.atmosphereCurve.Curve.MoveKey(Warpengine.ISPindex, key);
                            }
                        }
                        else
                        {
                            Warpengine.Engine.maxFuelFlow = Warpengine.maxFuelFlow;

                            //i hate read only variables
                            Keyframe key = Warpengine.Engine.atmosphereCurve.Curve[Warpengine.ISPindex];
                            Warpengine.Engine.atmosphereCurve.Curve.RemoveKey(Warpengine.ISPindex);

                            key.value = Warpengine.ISP; // important part

                            Warpengine.Engine.atmosphereCurve.Curve.AddKey(key);
                            Warpengine.Engine.atmosphereCurve.Curve.MoveKey(Warpengine.ISPindex, key);
                        }
                    }
                    for (int i = 0; i < Warpengine.PropId.Count; i++)
                    {
                        double Fuel = 0;
                        if (constantISP)
                            Fuel = part.RequestResource(Warpengine.PropId[i], Warpengine.PropFlow[i] * TimeWarp.fixedDeltaTime * Warpengine.simThrottle);
                        else
                            Fuel = part.RequestResource(Warpengine.PropId[i], Warpengine.PropFlow[i] * TimeWarp.fixedDeltaTime * Warpengine.simThrottle * Warpengine.simThrottle);
                        if (Fuel == 0)
                        {
                            print(TAG + " To little " + Warpengine.PropName[i] + " remaining. Shutting down the engines");
                            oldThrottle = Throttle;
                            Throttle = 0f;
                            vessel.ctrlState.mainThrottle = Throttle;
                        }
                    }
                    if (Warpengine.Waterfall != null)
                    {
                        foreach (WaterfallController Controller in Warpengine.Waterfall.Controllers)
                        {
                            if (Controller.name == "throttle")
                            {
                                Controller.overridden = true;
                                Controller.overrideValue = Warpengine.simThrottle;
                            }
                        }
                    }
                    timeWarp = true;
                }
            }
            else if (oldThrottle != 0)
            {
                foreach (WarpEngine Warpengine in WarpEngines)
                {
                    if (Warpengine.EcRate != 0)
                    {
                        double Ec = part.RequestResource("ElectricCharge", EcRate * TimeWarp.fixedDeltaTime * oldThrottle, true);
                        if (Ec != 0)
                        {
                            Throttle = oldThrottle;
                            vessel.ctrlState.mainThrottle = Throttle;
                        }
                    }
                }
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            Engines = part.FindModulesImplementing<ModuleEngines>();
            EngineFX = part.FindModuleImplementing<ModuleEnginesFX>();
            WaterfallFX = part.FindModuleImplementing<ModuleWaterfallFX>();

            foreach (ModuleEngines engine in Engines)
            {
                WarpEngines.Add(new WarpEngine());
                WarpEngines[WarpEngines.Count - 1].Waterfall = WaterfallFX;
                WarpEngines[WarpEngines.Count - 1].Effect = EngineFX.powerEffectName;
                WarpEngines[WarpEngines.Count - 1].MaxThrust = engine.maxThrust;
                WarpEngines[WarpEngines.Count - 1].MinThrottle = engine.throttleMin;
                WarpEngines[WarpEngines.Count - 1].Engine = engine;
                WarpEngines[WarpEngines.Count - 1].maxFuelFlow = engine.maxFuelFlow;
                WarpEngines[WarpEngines.Count - 1].ISP = engine.atmosphereCurve.Evaluate(0);

                foreach (Keyframe key in engine.atmosphereCurve.Curve.keys)
                {
                    if (key.time == 0)
                    {
                        break;
                    }
                    WarpEngines[WarpEngines.Count - 1].ISPindex++;
                }

                float PropsFlow = engine.maxFuelFlow / engine.mixtureDensity;
                List<Propellant> Propellants = engine.propellants;
                foreach (Propellant Propellant in Propellants)
                {
                    if (Propellant.displayName == "Electric Charge")
                    {
                        WarpEngines[WarpEngines.Count - 1].EcRate = Propellant.ratio * PropsFlow;
                        continue;
                    }
                    WarpEngines[WarpEngines.Count - 1].PropId.Add(Propellant.id);
                    WarpEngines[WarpEngines.Count - 1].PropName.Add(Propellant.displayName);
                    WarpEngines[WarpEngines.Count - 1].PropFlow.Add(Propellant.ratio * PropsFlow);
                }
                if (WarpEngines[WarpEngines.Count - 1].EcRate == 0)
                {
                    WarpEngines[WarpEngines.Count - 1].EcRate = EcRate;
                }
            }
            Events["ConstantEC"].active = false;
            Events["UseRot"].active = false;
            Events["UseActive"].active = false;
            Events["Rotate"].active = false;

            if (state == StartState.Editor)
            {
                active = false;
            }
            else
            {
                active = true;
            }
        }
    }
}
