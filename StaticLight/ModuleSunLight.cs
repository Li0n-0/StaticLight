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
		public float delayLowTimeWrap = 2f;
		public float delayHighTimeWrap = .1f;

		private bool hasStarted = false;

		private bool mainCoroutineHasStarted = false;
		private bool animIsPlaying = false;
		private bool inSunLight = false;
		private bool lightIsOn = false;
		private Animation animationComponent;
		private float animLength;
		private float animationSpeed;

		private CelestialBody sun;

		private List<WaitForSeconds> timeWrapDelays;

//		private KerbalKonstructs.UI.WindowManager kkWindowManager;

//		private GameObject debugStatic, debugRay;

		private Vector3 rayPos;

		void Start ()
		{
			Debug.Log ("[StaticLight] (for : " + gameObject.name + ") on Start ()");
			// Fetch parameter from cfg, using Kerbal Konstructs way


//			debugStatic = GameObject.CreatePrimitive (PrimitiveType.Sphere);
//			Destroy (debugStatic.GetComponent<SphereCollider> ());
//			debugStatic.GetComponent<MeshRenderer> ().material = new Material (Shader.Find ("Transparent/Diffuse"));
//			debugStatic.GetComponent<MeshRenderer> ().material.color = Color.red;
//			debugStatic.transform.position = transform.position;
//			debugStatic.layer = 17;
//			debugStatic.transform.SetParent (transform);
//			debugStatic.SetActive (true);
//			Debug.Log ("[StaticLight] (for : " + gameObject.name + ") make static sphere");
//
//			debugRay = GameObject.CreatePrimitive (PrimitiveType.Sphere);
//			Destroy (debugRay.GetComponent<SphereCollider> ());
//			debugRay.GetComponent<MeshRenderer> ().material = new Material (Shader.Find ("Transparent/Diffuse"));
//			debugRay.GetComponent<MeshRenderer> ().material.color = Color.blue;
//			debugRay.transform.position = rayPos;
////			debugRay.transform.position = mesh.bounds.max;
//			debugRay.layer = 17;
//			debugRay.transform.SetParent (transform);
//			debugRay.SetActive (true);
//			Debug.Log ("[StaticLight] (for : " + gameObject.name + ") make ray sphere");

			var myFields = this.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
			foreach (var field in myFields) {
				
				if (field.Name == "animationName") {
					animationName = (string)field.GetValue (this);
				}
				if (field.Name == "reverseAnimation") {
					reverseAnimation = (bool)field.GetValue (this);
				}
				if (field.Name == "delayLowTimeWrap") {
					delayLowTimeWrap = (float)field.GetValue (this);
				}
				if (field.Name == "delayHighTimeWrap") {
					delayHighTimeWrap = (float)field.GetValue (this);
				}
			}

			foreach (Animation anim in gameObject.GetComponentsInChildren<Animation> ()) {
				if (anim [animationName] != null) {
					animationComponent = anim;
					break;
				}
			}

			if (animationComponent == null) {
				Debug.Log ("[StaticLight] no anim found, destroying now");
				Destroy (this);
			}

			animLength = animationComponent [animationName].length * animationComponent [animationName].normalizedSpeed;
			animationSpeed = animationComponent [animationName].speed;

			sun = Planetarium.fetch.Sun;

			timeWrapDelays = new List<WaitForSeconds> ();
			timeWrapDelays.Add (new WaitForSeconds (delayHighTimeWrap));
			timeWrapDelays.Add (new WaitForSeconds (delayLowTimeWrap));
			timeWrapDelays.Add (new WaitForSeconds (delayLowTimeWrap / 2f));
			timeWrapDelays.Add (new WaitForSeconds (delayLowTimeWrap / 3f));
			timeWrapDelays.Add (new WaitForSeconds (delayLowTimeWrap / 4f));

			SetRayPos ();

			StartCoroutine ("LightsOff");


//			kkWindowManager = MonoBehaviour.FindObjectOfType<KerbalKonstructs.UI.WindowManager> ();
//			if (kkWindowManager == null) {
//				Debug.Log ("[StaticLight] instance of KK not found");
//			} else {
//				Debug.Log ("[StaticLight] found the instance of KK !");
//			}


		}

		void OnDestroy ()
		{
			Debug.Log ("[StaticLight] (for : " + gameObject.name + ") on OnDestroy ()");
		}

		public override void StaticObjectUpdate ()
		{
//			Debug.Log ("[StaticLight] (for : " + gameObject.name + ") on StaticObjectUpdate ()");
			StopAllCoroutines ();
			mainCoroutineHasStarted = false;
			animIsPlaying = false;

		}

		private void SetRayPos ()
		{
			float size;
			float meshSize = gameObject.GetComponentInChildren<MeshFilter> ().mesh.bounds.size.y;
			float colliderSize = 0;
			if (gameObject.GetComponentInChildren<Collider> () != null) {
				colliderSize = gameObject.GetComponentInChildren<Collider> ().bounds.size.y;
			}
			if (meshSize > colliderSize) {
				size = meshSize;
			} else {
				size = colliderSize;
			}
//			Debug.Log ("[StaticLight]  meshSize : " + meshSize);
//			Debug.Log ("[StaticLight]  colliderSize : " + colliderSize);

			Vector3 upAxis = FlightGlobals.getUpAxis (FlightGlobals.currentMainBody, transform.position);

			upAxis *= size + 1f;
			rayPos = transform.position + upAxis;
		}

		private IEnumerator SearchTheSun ()
		{
			Debug.Log ("[StaticLight] Main coroutine started");
			mainCoroutineHasStarted = true;
			while (true) {
				
				CheckSunPos ();
				if (TimeWarp.CurrentRate < 5f) {
					yield return timeWrapDelays [(int)TimeWarp.CurrentRate];
				} else {
					yield return timeWrapDelays [0];
				}
			}
		}

		private void CheckSunPos ()
		{
			

			if (animIsPlaying) {
//				Debug.Log ("[StaticLight] CheckSunPos : Anim already playing");
				return;
			}

			inSunLight = IsUnderTheSun ();
//			Debug.Log ("[StaticLight] inSunLight is : " + inSunLight);

			if (inSunLight && lightIsOn) {
//				Debug.Log ("[StaticLight] CheckSunPos : should turn lights off");
				StartCoroutine ("LightsOff");
				return;
			}
			if (!inSunLight && !lightIsOn) {
//				Debug.Log ("[StaticLight] CheckSunPos : should turn lights on");
				StartCoroutine ("LightsOn");
				return;
			}
//			Debug.Log ("[StaticLight] CheckSunPos : nothing wrong with the light, they are : " + lightIsOn);
		}

		void Update ()
		{
//			if (KerbalKonstructs.UI.WindowManager.IsOpen ()
			if (!hasStarted) {
				return;
			}

			if (hasStarted && !mainCoroutineHasStarted) {
				SetRayPos ();
//				debugRay.transform.position = rayPos;
				StartCoroutine ("SearchTheSun");
			}

			if (!animIsPlaying) {
				if (Input.GetKeyDown (KeyCode.X)) {
					StartCoroutine ("LightsOn");
					return;
				}
				if (Input.GetKeyDown (KeyCode.Y)) {
					StartCoroutine ("LightsOff");
				}
			}
		}

		private bool IsUnderTheSun ()
		{
			RaycastHit hit;

			if (Physics.Raycast (/*transform.position*/rayPos, sun.position, out hit, Mathf.Infinity, (1 << 10 | 1 << 15  | 1 << 28))) {
				Debug.Log ("[StaticLight] (for : " + gameObject.name + ") hit is named : " + hit.transform.name);
				if (hit.transform.name == sun.bodyName) {
					
					return true;
				}
			}
			return false;
		}

		private IEnumerator LightsOn ()
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

			animationComponent.Play (animationName);
			lightIsOn = true;
			hasStarted = true;
			yield return new WaitForSeconds (animLength);
//			hasStarted = true;
			animIsPlaying = false;
		}

		private IEnumerator LightsOff ()
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

			animationComponent.Play (animationName);
			lightIsOn = false;
			hasStarted = true;
			yield return new WaitForSeconds (animLength);

			animIsPlaying = false;
		}
	}
}

