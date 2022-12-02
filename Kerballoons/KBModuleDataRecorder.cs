using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace KerBalloons
{
	internal class KBModuleDataRecorder : PartModule, IResourceConsumer
	{
		private class KBDataLog
		{
			public static void Writelog(string thisline)
			{
				File.AppendAllText(saveFile, thisline + Environment.NewLine);
			}
		}

		private bool BaromPresent = false;

		private bool BaramOn = false;

		private bool ThermPresent = false;

		private bool ThermOn = false;

		private bool GravPresent = false;

		private bool ACCPresent = false;

		private bool TimePresent = false;

		private bool SextPresent = false;

		private bool DensPresent = false;

		private bool onePresent = false;

		public static string saveFile = "";

		public static List<ModuleEnviroSensor> EnvSensor = new List<ModuleEnviroSensor>();

		public static List<KBModuleEnviroSensor> KBEnvSensor = new List<KBModuleEnviroSensor>();

		public string dateFormat = "";

		public string hourFormat = "";

		public float checkTime = 0f;

		public bool headerCreated = false;

		private string optionstring = "";

		private bool doreset = false;

		private Animation anim = null;

		private List<PartResourceDefinition> consumedResources;

		[KSPField(guiName = "Recording Frequency", guiActiveEditor = true, guiActive = true, isPersistant = true, guiUnits = "seconds")]
		[UI_FloatRange(minValue = 1f, maxValue = 60f, scene = UI_Scene.All, stepIncrement = 1f)]
		public float recordingSeconds = 1f;

		[KSPField(guiName = "Record", guiActiveEditor = true, guiActive = true, isPersistant = true)]
		[UI_Toggle(controlEnabled = true, disabledText = "Off", enabledText = "On", invertButton = false, scene = UI_Scene.All)]
		public bool recordingActive = false;

		[KSPField(guiName = "Status", guiUnits = "", guiActive = true, guiFormat = "F3")]
		public string readoutInfo = "Ready";

		[KSPField]
		public string animationName = "";

		public List<PartResourceDefinition> GetConsumedResources()
		{
			return consumedResources;
		}

		public override void OnAwake()
		{
			if (consumedResources == null)
			{
				consumedResources = new List<PartResourceDefinition>();
			}
			else
			{
				consumedResources.Clear();
			}
			int i = 0;
			for (int count = base.resHandler.inputResources.Count; i < count; i++)
			{
				consumedResources.Add(PartResourceLibrary.Instance.GetDefinition(base.resHandler.inputResources[i].name));
			}
		}

		public override void OnLoad(ConfigNode node)
		{
			if (base.resHandler.inputResources.Count == 0 && (node.HasValue("resourceName") || node.HasValue("powerConsumption") || base.part.partInfo == null || (UnityEngine.Object)base.part.partInfo.partPrefab == (UnityEngine.Object)null))
			{
				string text = "ElectricCharge";
				float powerConsumption = 0.0075f;
				node.TryGetValue("resourceName", ref text);
				node.TryGetValue("powerConsumption", ref powerConsumption);
				ModuleResource moduleResource = new ModuleResource();
				moduleResource.name = text;
				moduleResource.title = KSPUtil.PrintModuleName(text);
				moduleResource.id = text.GetHashCode();
				moduleResource.rate = (double)powerConsumption;
				base.resHandler.inputResources.Add(moduleResource);
			}
		}

		public override void OnStart(StartState state)
		{
			base.OnStart(state);
			anim = base.part.FindModelAnimator(animationName);
			anim.wrapMode = WrapMode.Loop;
			DoReset();
		}

		public void DoReset()
		{
			string SaveDir = KSPUtil.ApplicationRootPath + "GameData/KerBalloons/LogData/";
			if (!HighLogic.CurrentGame.Parameters.CustomParams<KerBSettings>().saveInModFolder)
			{
				SaveDir = KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/KerBalloonDataLogs/";
			}
				if (!Directory.Exists(SaveDir))
					Directory.CreateDirectory(SaveDir);

			anim.Stop();
			DateTime now = DateTime.Now;
			optionstring = "";
			dateFormat = "MM'-'dd'-'yyyy HH'-'mm'-'ss";
			hourFormat = "HH':'mm':'ss";
			saveFile = base.vessel.mainBody.ToString() + "-" + now.ToString(dateFormat) + ".csv";
			saveFile = SaveDir + saveFile;
			EnvSensor = base.vessel.FindPartModulesImplementing<ModuleEnviroSensor>();
			headerCreated = false;
			foreach (ModuleEnviroSensor item in EnvSensor)
			{
				if (item.sensorType == ModuleEnviroSensor.SensorType.TEMP)
				{
					ThermPresent = true;
					onePresent = true;
					optionstring += ",Temperature";
				}
				if (item.sensorType == ModuleEnviroSensor.SensorType.GRAV)
				{
					GravPresent = true;
					onePresent = true;
					optionstring += ",Gravity";
				}
				if (item.sensorType == ModuleEnviroSensor.SensorType.PRES)
				{
					BaromPresent = true;
					onePresent = true;
					optionstring += ",Atmospheric Pressure";
				}
				if (item.sensorType == ModuleEnviroSensor.SensorType.ACC)
				{
					ACCPresent = true;
					onePresent = true;
					optionstring += ",Acceleration";
				}
			}
			KBEnvSensor = base.vessel.FindPartModulesImplementing<KBModuleEnviroSensor>();
			foreach (KBModuleEnviroSensor item2 in KBEnvSensor)
			{
				if (item2.sensorType == KBModuleEnviroSensor.SensorType.TIME)
				{
					TimePresent = true;
					onePresent = true;
					optionstring = "Earth Time,Universal Time,Mission Time" + optionstring;
				}
				if (ThermPresent && BaromPresent)
				{
					DensPresent = true;
					onePresent = true;
					optionstring += ",Air Density";
				}
				if (item2.sensorType == KBModuleEnviroSensor.SensorType.SEXT)
				{
					SextPresent = true;
					onePresent = true;
					optionstring += ",Latitude,Longitude";
				}
			}
		}

		public void FixedUpdate()
		{
			if (HighLogic.LoadedSceneIsFlight && onePresent)
			{
				if (recordingActive)
				{
					if (base.resHandler.UpdateModuleResourceInputs(ref readoutInfo, 1.0, 0.9, true, true))
					{
						if (!anim.IsPlaying(anim.name))
						{
							anim.Play();
						}
						DateTime now = DateTime.Now;
						if (!headerCreated)
						{
							KBDataLog.Writelog("//Data recording began on: " + base.vessel.mainBody.ToString());
							KBDataLog.Writelog("//" + now.ToString(dateFormat));
							KBDataLog.Writelog("//Format: ");
							KBDataLog.Writelog(optionstring + ",Altitude");
							headerCreated = true;
						}
						checkTime -= Time.deltaTime;
						Debug.Log(checkTime);
						if (checkTime <= 0f)
						{
							string text = "";
							foreach (ModuleEnviroSensor item in EnvSensor)
							{
								if (item.sensorType == ModuleEnviroSensor.SensorType.TEMP)
								{
									if (item.sensorActive)
									{
										ThermOn = true;
										text = text + "," + base.vessel.atmosphericTemperature;
									}
									else
									{
										ThermOn = false;
										text += ",Sensor Inactive";
									}
								}
								if (item.sensorType == ModuleEnviroSensor.SensorType.GRAV)
								{
									if (item.sensorActive)
									{
										double distFromCenter = base.vessel.mainBody.Radius + base.vessel.altitude;
										text = text + "," + base.vessel.mainBody.gravParameter / (double)Mathf.Pow((float)distFromCenter, 2f);
									}
									else
									{
										text += ",Sensor Inactive";
									}
								}
								if (item.sensorType == ModuleEnviroSensor.SensorType.PRES)
								{
									if (item.sensorActive)
									{
										BaramOn = true;
										text = text + "," + base.vessel.staticPressurekPa;
									}
									else
									{
										BaramOn = false;
										text += ",Sensor Inactive";
									}
								}
								if (item.sensorType == ModuleEnviroSensor.SensorType.ACC)
								{
									text = ((!item.sensorActive) ? (text + ",Sensor Inactive") : (text + "," + base.vessel.geeForce));
								}
							}
							foreach (KBModuleEnviroSensor item2 in KBEnvSensor)
							{
								if (item2.sensorType == KBModuleEnviroSensor.SensorType.TIME)
								{
									text = ((!item2.sensorActive) ? ("Sensor Inactive,Sensor Inactive,Sensor Inactive" + text) : (now.ToString(hourFormat) + "," + KSPUtil.PrintTimeCompact(Planetarium.GetUniversalTime(), true) + "," + base.vessel.missionTime + text));
								}
								if (ThermPresent && BaromPresent)
								{
									text = ((!ThermOn || !BaramOn) ? (text + ",Sensor Inactive") : (text + "," + base.vessel.atmDensity));
								}
								if (item2.sensorType == KBModuleEnviroSensor.SensorType.SEXT)
								{
									text = ((!item2.sensorActive) ? (text + ",Sensor Inactive,Sensor Inactive") : (text + "," + base.vessel.latitude + "," + base.vessel.longitude));
								}
							}
							text = text + "," + base.vessel.altitude;
							KBDataLog.Writelog(text);
							checkTime = recordingSeconds + Math.Abs(checkTime);
						}
					}
				}
				else
				{
					DoReset();
				}
			}
		}
	}
}
