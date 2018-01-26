using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KerbalKonstructs.Core;

namespace StaticLight
{
	public class ModuleSunLight : StaticModule
	{
		public string animationName;
		public bool reverseAnimation = false;
		public float delayLowTimeWrap = 2f;
		public float delayHighTimeWrap = .1f;

		private Animation animationComponent;
		private bool animIsPlaying = false;
		private float animLength;
		private float animationSpeed;

		private CelestialBody sun;
		private bool inSunLight = false;
		private bool lightIsOn = false;

		private bool hasStarted = false;

		private List<WaitForSeconds> timeWrapDelays;

		private bool mainCoroutineHasStarted = false;

		void Start ()
		{
			DoStart ();
		}

		void DoStart ()
		{
			Debug.Log ("[StaticLight]");
			Debug.Log ("[StaticLight] on Start ()");
			Debug.Log ("[StaticLight]");

			var myFields = this.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
			foreach (var field in myFields) {
				Debug.Log ("[StaticLight] field : " + field.Name);
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
			Debug.Log ("[StaticLight] animationName : " + animationName);
			Debug.Log ("[StaticLight] reverseAnimaion : " + reverseAnimation);

			timeWrapDelays = new List<WaitForSeconds> ();
			timeWrapDelays.Add (new WaitForSeconds (delayHighTimeWrap));
			timeWrapDelays.Add (new WaitForSeconds (delayLowTimeWrap));
			timeWrapDelays.Add (new WaitForSeconds (delayLowTimeWrap / 2f));
			timeWrapDelays.Add (new WaitForSeconds (delayLowTimeWrap / 3f));
			timeWrapDelays.Add (new WaitForSeconds (delayLowTimeWrap / 4f));


			foreach (Animation anim in gameObject.GetComponentsInChildren<Animation> ()) {
				Debug.Log ("[StaticLight] gameObject animation in children, name : " + anim.name);
				//if (anim.name == animationName) {
					animationComponent = anim;
//				animationName = anim.name;
				//}
			}

			animLength = animationComponent [animationName].length * animationComponent [animationName].normalizedSpeed;
			animationSpeed = animationComponent [animationName].speed;

			sun = Planetarium.fetch.Sun;


			StartCoroutine ("LightsOff");

//			hasStarted = true; 
		}

		void OnDestroy ()
		{
			Debug.Log ("[StaticLight]");
			Debug.Log ("[StaticLight] on OnDestroy ()");
			Debug.Log ("[StaticLight]");
		}

		public override void StaticObjectUpdate ()
		{
			Debug.Log ("[StaticLight] on StaticObjectUpdate ()");
//			if (!hasStarted) {
//				DoStart ();
//			}
			mainCoroutineHasStarted = false;
			animIsPlaying = false;
			if (this.isActiveAndEnabled && hasStarted) {
				StopAllCoroutines ();

				animationComponent.Stop (animationName);
				animationComponent [animationName].time = 0;
				animationComponent [animationName].speed = animationSpeed;
				lightIsOn = false;
				animIsPlaying = false;
				StartCoroutine ("SearchTheSun");
			}


		}

		private IEnumerator SearchTheSun ()
		{
			Debug.Log ("[StaticLight] SearchTheSun started");
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
				Debug.Log ("[StaticLight] CheckSunPos : Anim already playing");
				return;
			}

			inSunLight = IsUnderTheSun ();
			Debug.Log ("[StaticLight] CheckSunPos is : " + inSunLight);

			if (inSunLight && lightIsOn) {
				Debug.Log ("[StaticLight] CheckSunPos : should turn lights off");
				StartCoroutine ("LightsOff");
				return;
			}
			if (!inSunLight && !lightIsOn) {
				Debug.Log ("[StaticLight] CheckSunPos : should turn lights on");
				StartCoroutine ("LightsOn");
				return;
			}
			Debug.Log ("[StaticLight] CheckSunPos : nothing wrong with the light, they are : " + lightIsOn);
		}

		void Update ()
		{
//			Debug.Log ("[StaticLight] in Update ()");
			if (!hasStarted) {
				return;
			}

			if (hasStarted && !mainCoroutineHasStarted) {
				StartCoroutine ("SearchTheSun");
			}

			if (!animIsPlaying) {
//				if (IsUnderTheSun () && lightIsOn) {
//					StartCoroutine ("LightsOff");
//					return;
//				} else if (!IsUnderTheSun () && !lightIsOn) {
//					StartCoroutine ("LightsOn");
//					return;
//				}
				if (Input.GetKeyDown (KeyCode.X)) {
					Debug.Log ("[StaticLight] click lightsOn");
					StartCoroutine ("LightsOn");
					return;
				}
				if (Input.GetKeyDown (KeyCode.Y)) {
					Debug.Log ("[StaticLight] click lightsOff");
					StartCoroutine ("LightsOff");
					return;
				}
			}

		}

		private bool IsUnderTheSun ()
		{
//			Debug.Log ("[StaticLight] RAYCAST !");
			RaycastHit hit;

			if (Physics.Raycast (transform.position, sun.position, out hit, Mathf.Infinity, (1 << 10 | 1 << 15/*  | 1 << 28*/))) {
				Debug.Log ("[StaticLight] hit is named : " + hit.transform.name);
				if (hit.transform.name == sun.bodyName) {
					
					return true;
				}
			}
			return false;
		}

		private IEnumerator LightsOn ()
		{
			Debug.Log ("[StaticLight] turning lights on");
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
			yield return new WaitForSeconds (animLength);
			hasStarted = true;
			animIsPlaying = false;
		}

		private IEnumerator LightsOff ()
		{
			Debug.Log ("[StaticLight] turning lights off");
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
			yield return new WaitForSeconds (animLength);
			hasStarted = true;
			animIsPlaying = false;
		}
	}
}

