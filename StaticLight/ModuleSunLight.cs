using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KerbalKonstructs.Core;

namespace StaticLight
{
	public class ModuleSunLight : StaticModule
	{
		// Public .cfg fields
		public string animationName;
		public bool reverseAnimation = false;
		public bool timeWarpAnimation = false;
		public float delayLowTimeWarp = 2f;
		public float delayHighTimeWarp = .1f;
		public bool mathHorizontalAngle = false;
		public float horizonAngleOffset = 0;

		private bool hasStarted = false;

//		private StaticInstance staticInstance;
		private CelestialBody sun;

		private List<WaitForSeconds> timeWarpDelays;

		internal bool isMaster = false;
		private ModuleSunLight master;
		private List<ModuleSunLight> slaveList;


		private Animation animationComponent;
		private float animLength;
		private float animationSpeed;

		private bool inSunLight = false;
		private bool animIsOn = false;

		private bool mainCoroutineHasStarted = false;
		private bool animIsPlaying = false;
		private bool guiIsUp = false;

		private Vector3 boundsCenter;
		private Vector3 centerToStatic;
		private float horizonAngle;

		void Start ()
		{
			Debug.Log ("[StaticLight] (for : " + gameObject.name + ") on Start ()");

			sun = Planetarium.fetch.Sun;

//			foreach (StaticInstance sInstance in StaticDatabase.GetAllStatics ()) {
//				if (sInstance.gameObject == gameObject) {
//					staticInstance = sInstance;
//					break;
//				}
//			}

			// Fetch parameter from cfg, using Kerbal Konstructs way
			var myFields = this.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
			foreach (var field in myFields) {
				switch (field.Name) {
				case "animationName":
					animationName = (string)field.GetValue (this);
					break;
				case "reverseAnimation":
					reverseAnimation = (bool)field.GetValue (this);
					break;
				case "timeWarpAnimation":
					timeWarpAnimation = (bool)field.GetValue (this);
					break;
				case "delayLowTimeWarp":
					delayLowTimeWarp = (float)field.GetValue (this);
					break;
				case "delayHighTimeWarp":
					delayHighTimeWarp = (float)field.GetValue (this);
					break;
				case "mathHorizontalAngle":
					mathHorizontalAngle = (bool)field.GetValue (this);
					break;

				case "horizonAngleOffset":
					horizonAngleOffset = (float)field.GetValue (this);
					break;
				default:
					break;
				}
			}
//			Debug.Log ("[StaticLight] (for : " + gameObject.name + ") timeWarpAnimation = " + timeWarpAnimation);
			foreach (Animation anim in gameObject.GetComponentsInChildren<Animation> ()) {
				if (anim [animationName] != null) {
					animationComponent = anim;
					break;
				}
			}
			if (animationComponent == null) {
				foreach (Animation anim in gameObject.GetComponents<Animation> ()) {
					if (anim [animationName] != null) {
						animationComponent = anim;
						break;
					}
				}
			}
			if (animationComponent == null) {
				Debug.Log ("[StaticLight] no anim found, destroying now");
				Destroy (this);
			}

			animLength = animationComponent [animationName].length * animationComponent [animationName].normalizedSpeed;
			animationSpeed = animationComponent [animationName].speed;

			// Make sure the light is off
			AnimOff ();

			SetUp ();

			timeWarpDelays = new List<WaitForSeconds> ();
			timeWarpDelays.Add (new WaitForSeconds (delayHighTimeWarp));
			timeWarpDelays.Add (new WaitForSeconds (delayLowTimeWarp));
			timeWarpDelays.Add (new WaitForSeconds (delayLowTimeWarp / 2f));
			timeWarpDelays.Add (new WaitForSeconds (delayLowTimeWarp / 3f));
			timeWarpDelays.Add (new WaitForSeconds (delayLowTimeWarp / 4f));

			hasStarted = true;
		}

//		private bool ParseBool (string str)
//		{
//			if (str == "true" || str == "True") {
//				
//			}
//		}

		internal void SetUp ()
		{
			// run after the editor has closed

			SetGroup ();

			if (isMaster) {
				boundsCenter = GetBoundsCenter ();
				horizonAngle = GetHorizonAngle ();
				centerToStatic = FlightGlobals.getUpAxis (FlightGlobals.currentMainBody, boundsCenter);
			}
		}

		internal void StopObject ()
		{
			StopAllCoroutines ();
			mainCoroutineHasStarted = false;
			AnimOff ();
		}

		private void AnimOff ()
		{
			animIsPlaying = false;
			animIsOn = false;

			if (reverseAnimation) {
				animationComponent [animationName].normalizedTime = 1f;
			} else {
				animationComponent [animationName].time = 0;
			}
		}

		void OnDestroy ()
		{
			Debug.Log ("[StaticLight] (for : " + gameObject.name + ") on OnDestroy ()");

			if (isMaster) {
				if (slaveList.Count > 0) {
//					slaveList [0].isMaster = true;
					slaveList [0].SetUp ();
				}
			} else {
				master.SetUp ();
			}
		}

		public override void StaticObjectUpdate ()
		{
			Debug.Log ("[StaticLight] (for : " + gameObject.name + ") on StaticObjectUpdate ()");

			if (hasStarted) {
				StopObject ();
			}
		}

		private void StaticObjectEditorOpen ()
		{
			Debug.Log ("[StaticLight] (for : " + gameObject.name + ") on StaticObjectEditorOpen ()");
			StopObject ();
			foreach (ModuleSunLight module in slaveList) {
				module.StopObject ();
			}
			guiIsUp = true;
		}

		private void StaticObjectEditorClose ()
		{
			Debug.Log ("[StaticLight] (for : " + gameObject.name + ") on StaticObjectEditorClose ()");
			SetUp ();
			StartCoroutine ("SearchTheSun");
			guiIsUp = false;
		}

		private void SetGroup ()
		{
			Debug.Log ("[SL] Set group for : " + gameObject.name + ", group : " + staticInstance.Group);
			slaveList = new List<ModuleSunLight> ();

			if (staticInstance.Group == "Ungrouped") {
				master = this;
				isMaster = true;
				return;
			}

			bool findMaster = false;

			foreach (StaticInstance sInstance in StaticDatabase.GetAllStatics ()) {
				if (sInstance != staticInstance) {
					if (sInstance.Group == staticInstance.Group) {
						ModuleSunLight module = sInstance.gameObject.GetComponentInChildren<ModuleSunLight> ();
						if (module != null) {
							if (module.isMaster) {
								master = module;
								findMaster = true;
								isMaster = false;
								break;
							} else {
								slaveList.Add (module);
							}
						}
					}
				}
			}

			if (!findMaster) {
				master = this;
				isMaster = true;
			}
		}

		private Vector3 GetBoundsCenter ()
		{
			Bounds groupBounds = new Bounds ();
			foreach (ModuleSunLight module in slaveList) {
				
				groupBounds.Encapsulate (GetBounds (module.gameObject));
			}
			groupBounds.Encapsulate (GetBounds (gameObject));
			groupBounds.Expand (1f);

			return (groupBounds.center + (FlightGlobals.getUpAxis (FlightGlobals.currentMainBody, groupBounds.center) * (groupBounds.size.y / 2f)));
		}

		private Bounds GetBounds (GameObject staticObject)
		{
			Bounds meshBounds = staticObject.GetComponentInChildren<MeshFilter> ().mesh.bounds;
			Bounds colliderBounds = new Bounds ();

			if (staticObject.GetComponentInChildren<Collider> () != null) {
				colliderBounds = staticObject.GetComponentInChildren<Collider> ().bounds;
			}

			if (meshBounds.size.y > colliderBounds.size.y) {
				return meshBounds;
			} else {
				return colliderBounds;
			}
		}

		private float GetHorizonAngle ()
		{
			float angleHor;

			if (mathHorizontalAngle) {
				float height = Vector3.Distance (boundsCenter, FlightGlobals.currentMainBody.position);
				float sinus = (float)FlightGlobals.currentMainBody.Radius / height;
				float angle = Mathf.Asin (sinus) * Mathf.Rad2Deg;
				angleHor = 180f - angle;
			} else {
				angleHor = 90f;
			}

			return (angleHor + horizonAngleOffset);
		}

		void Update ()
		{
			if (isMaster && hasStarted) {
				if (KerbalKonstructs.UI.StaticsEditorGUI.instance.IsOpen () && !guiIsUp) {
					StaticObjectEditorOpen ();
					return;
				}
				if (guiIsUp && !KerbalKonstructs.UI.StaticsEditorGUI.instance.IsOpen ()) {
					StaticObjectEditorClose ();
					return;
				}

				if (!guiIsUp && !mainCoroutineHasStarted) {
					if (this.isActiveAndEnabled) {
						SetUp ();
						StartCoroutine ("SearchTheSun");
					}
				}
			}
		}

		private void CheckSunPos ()
		{
			inSunLight = IsUnderTheSun ();
			//			Debug.Log ("[StaticLight] inSunLight is : " + inSunLight);

			if (inSunLight && animIsOn) {
				//				Debug.Log ("[StaticLight] CheckSunPos : should turn lights off");

				StartCoroutine ("StartAnim", false);

				foreach (ModuleSunLight module in slaveList) {
					module.StartCoroutine ("StartAnim", false);
				}

				return;
			}
			if (!inSunLight && !animIsOn) {
				//				Debug.Log ("[StaticLight] CheckSunPos : should turn lights on");
				StartCoroutine ("StartAnim", true);

				foreach (ModuleSunLight module in slaveList) {
					module.StartCoroutine ("StartAnim", true);
				}

			}
			//			Debug.Log ("[StaticLight] CheckSunPos : nothing wrong with the light, they are : " + lightIsOn);
		}

		private bool IsUnderTheSun ()
		{
			Vector3 staticToSun = sun.position - boundsCenter;
			float sunAngle = Vector3.Angle (centerToStatic, staticToSun);

			if (sunAngle < horizonAngle) {
				return true;
			} else {
				return false;
			}
			// Raycast method :
			//			RaycastHit hit;
			//
			//			if (Physics.Raycast (/*transform.position*/rayPos, sun.position, out hit, Mathf.Infinity, (1 << 10 | 1 << 15 | 1 << 28))) {
			////				Debug.Log ("[StaticLight] (for : " + gameObject.name + ") hit is named : " + hit.transform.name + ", on layer : " + hit.transform.gameObject.layer);
			//				if (hit.transform.name == sun.bodyName) {
			//					
			//					return true;
			//				}
			//			}
			//			return false;
		}

		private IEnumerator SearchTheSun ()
		{
			Debug.Log ("[StaticLight] Main coroutine started");
			mainCoroutineHasStarted = true;
			while (true) {
				
				CheckSunPos ();
				if (TimeWarp.CurrentRate < 5f) {
					yield return timeWarpDelays [(int)TimeWarp.CurrentRate];
				} else {
					yield return timeWarpDelays [0];
				}
			}
		}

		internal IEnumerator StartAnim (bool turnOn)
		{
			while (animIsPlaying) {
				yield return new WaitForSeconds (.1f);
			}

			if (turnOn) {
				StartCoroutine ("SwitchOn");
			} else {
				StartCoroutine ("SwitchOff");
			}
		}

		private IEnumerator SwitchOn ()
		{
//			Debug.Log ("[StaticLight] turning lights on");
			animIsPlaying = true;
			if (reverseAnimation) {
				animationComponent [animationName].speed = -animationSpeed;
				animationComponent [animationName].normalizedTime = 1f;
			} else {
				animationComponent [animationName].speed = animationSpeed;
				animationComponent [animationName].time = 0;
			}

			if (timeWarpAnimation) {
				animationComponent [animationName].speed *= TimeWarp.CurrentRate;
			}

			animationComponent.Play (animationName);
			animIsOn = true;
			yield return new WaitForSeconds (animLength);
			animIsPlaying = false;
		}

		private IEnumerator SwitchOff ()
		{
//			Debug.Log ("[StaticLight] turning lights off");
			animIsPlaying = true;
			if (reverseAnimation) {
				animationComponent [animationName].speed = animationSpeed;
				animationComponent [animationName].time = 0;
			} else {
				animationComponent [animationName].speed = -animationSpeed;
				animationComponent [animationName].normalizedTime = 1f;
			}

			if (timeWarpAnimation) {
				animationComponent [animationName].speed *= TimeWarp.CurrentRate;
			}

			animationComponent.Play (animationName);
			animIsOn = false;
			yield return new WaitForSeconds (animLength);

			animIsPlaying = false;
		}
	}
}