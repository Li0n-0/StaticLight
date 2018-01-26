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

		private Animation animationComponent;
		private bool animIsPlaying = false;
		private float animLength;
		private float animationSpeed;

		private CelestialBody sun;
//		private bool inSunLight = false;
		private bool lightIsOn = false;

		private bool hasStarted = false;

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
			}
			Debug.Log ("[StaticLight] animationName : " + animationName);
			Debug.Log ("[StaticLight] reverseAnimaion : " + reverseAnimation);


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
			if (hasStarted) {
				StopAllCoroutines ();
				animationComponent.Stop (animationName);
				animationComponent [animationName].time = 0;
				animationComponent [animationName].speed = animationSpeed;
				lightIsOn = false;
				animIsPlaying = false;
			}


		}

		void Update ()
		{
//			Debug.Log ("[StaticLight] in Update ()");
			if (!hasStarted) {
				return;
			}
			if (!animIsPlaying) {
				if (IsUnderTheSun () && lightIsOn) {
					StartCoroutine ("LightsOff");
					return;
				} else if (!IsUnderTheSun () && !lightIsOn) {
					StartCoroutine ("LightsOn");
					return;
				}
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

			if (Physics.Raycast (transform.position, sun.position, out hit, Mathf.Infinity, (1 << 10/* | 1 << 15*/))) {
				if (hit.transform.name == "Sun") {
//					Debug.Log ("[StaticLight] found the sun");
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

