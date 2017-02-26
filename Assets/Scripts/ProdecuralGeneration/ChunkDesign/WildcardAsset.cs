using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using UnityEngine;
using System;

[System.Serializable]
public struct WildcardChance{
	public GameObject Asset;
	public int Chance;
}
[System.Serializable]
public class WildcardPreviewData{
	public GameObject Asset{ get; set; }
	public Mesh Mesh { get; set; }
	public Transform Transform{ get; set; }
}

[DisallowMultipleComponent]
public class WildcardAsset : InstantiatingProperty {
	[HideInInspector]
	public List<WildcardChance> chancesList = new List<WildcardChance>(0);
	[HideInInspector]
	public int selectedIndex = 0;

	public override void Preview(){
		//Nothing to be done in preview
	}

	public override void Generate(){
		GameObject chosenAsset = ChooseRandomAsset ();
		if (chosenAsset != null) {
			Component[] components = chosenAsset.GetComponents<Component> ();

			foreach (Component go in components) {
				//Transform can't be copied, since every GameObject has a transform component
				//Kopy scale and rotation values instead
				if (go is Transform) {
					AssignTransform (go as Transform);
					continue;
				}

				Component newComponent = gameObject.AddComponent (go.GetType ());
				if (newComponent != null) {
					newComponent.GetCopyOf (go);
				}
			}
		}
	}

	//Assign the scale and rotation instead of copying / adding the other transform
	//Keep the position of the wildcard
	private void AssignTransform(Transform otherTransform){
		//Adding up rotation
		transform.eulerAngles = otherTransform.rotation.eulerAngles + transform.rotation.eulerAngles;
		//Adding up scale. Since default scale is 1, the scale should also stay 1 if both objects have the default scale
		transform.localScale = otherTransform.localScale + transform.localScale - Vector3.one;
	}

	//Choses a random GameObject and returns it
	//Chances are considered
	private GameObject ChooseRandomAsset(){
		float[] rangeTable = GenerateRangeTable ();
		float randomFloat = UnityEngine.Random.value;

		for (int i = 0; i < rangeTable.Length; i++) {
			if (randomFloat > rangeTable [i] && randomFloat < rangeTable [i + 1]) {
				return chancesList [i].Asset;
			}
		}
		return null;
	}

	//Creates a table with ranges from 0f to 1f which represent the chances given by each asset
	//The table is later used to randomly choose an asset
	private float[] GenerateRangeTable(){
		float[] rangeTable = new float[chancesList.Count + 1];
		float sum = 0f;

		rangeTable [0] = 0f;

		for (int i = 0; i < chancesList.Count - 1; i++) {
			sum += chancesList [i].Chance / 100f;
			rangeTable [i + 1] = sum;
		}

		rangeTable [rangeTable.Length - 1] = 1f;

		return rangeTable;
	}		

	//Sums up the total amount of chances from all wildcards
	//Must sum up to 100
	//Used by the Editor script for the error notification if sum != 100
	public int SumUpChances(){
		int sum = 0;
		foreach (WildcardChance wc in chancesList) {
			sum += wc.Chance;
		}
		return sum;
	}

	public override void DrawEditorGizmos(){
		if (chancesList.Count > 0 && chancesList[selectedIndex].Asset != null) {
			
			WildcardPreviewData previewData = new WildcardPreviewData ();
			previewData.Asset = chancesList [selectedIndex].Asset;
			MeshFilter meshFilter = previewData.Asset.GetComponent<MeshFilter> ();
			previewData.Mesh = meshFilter.sharedMesh;
			previewData.Transform = previewData.Asset.GetComponent<Transform> ();

			if (meshFilter != null) {

				Gizmos.color = Color.cyan;
				Gizmos.DrawMesh (previewData.Mesh, transform.position, 
					previewData.Transform.rotation,
					previewData.Transform.localScale + transform.localScale - Vector3.one);				
			}
		}
	}

	//Debug function printing out the actual percantages when generating the assets
	//Choose an accuracy of 1000 for good enough results
	private void TestRandomFunctionality(int accuracy){
		Dictionary<string, int> results = new Dictionary<string,int> ();
		string generatedObj;

		for (int i = 0; i < accuracy; i++) {
			generatedObj = ChooseRandomAsset ().name;
			if (results.ContainsKey (generatedObj)) {
				results [generatedObj] += 1;
			} else {
				results.Add (generatedObj, 1);
			}
		}			

		float percent = 100f / accuracy;

		foreach (KeyValuePair<string, int> pair in results) {
			Debug.Log (pair.Key + ": " + pair.Value * percent + "%");
		}
	}

	public MeshFilter IndexedPreviewMesh{
		get{
			return chancesList [selectedIndex].Asset.GetComponent<MeshFilter>();
		}
	}
}