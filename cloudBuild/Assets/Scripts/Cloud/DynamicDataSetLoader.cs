﻿using UnityEngine;
using System.Collections;
using System.IO;
using Vuforia;
using System.Collections.Generic;
using UnityEngine.UI;

 
public class DynamicDataSetLoader : MonoBehaviour
{
    // specify these in Unity Inspector
    public GameObject augmentationObject = null;  // you can use teapot or other object
    public string dataSetName = "leadingEstates.xml";  //  Assets/StreamingAssets/QCAR/DataSetName
	public string currentTrackable = "none";
	public Text debugText;
	public string phoneContact;
	public string emailContact;
	public string webContact;
	
	string streamPath;
	
    // every time the app starts it'll update the database. later on we'll do this every once in a while
    void Start()
    {
		streamPath = Application.persistentDataPath + "/";
	#if UNITY_IOS
		streamPath += "Library/Application Support/";
	#endif
		updateDatabase();
    }
         	
	void updateDatabase () {
		downloadFiles();
	}
	
	//go through each URL and download. These are hardcoded now but these should be grabbed from the dashboard
	void downloadFiles(){
		string[] urls =  {"https://s3-eu-west-1.amazonaws.com/launchables/metadata/main/Launchable_Mobile_Application.dat",
						  "https://s3-eu-west-1.amazonaws.com/launchables/metadata/main/Launchable_Mobile_Application.xml", 
						  "https://s3-eu-west-1.amazonaws.com/launchables/metadata/main/target_1.txt",
						  "https://s3-eu-west-1.amazonaws.com/launchables/metadata/main/target_2.txt",
						  "https://s3-eu-west-1.amazonaws.com/launchables/metadata/main/target_3.txt",
						  "https://s3-eu-west-1.amazonaws.com/launchables/metadata/main/target_4.txt",
						  "https://s3-eu-west-1.amazonaws.com/launchables/metadata/main/target_5.txt",
						  "https://s3-eu-west-1.amazonaws.com/launchables/metadata/main/target_6.txt",
						  "https://s3-eu-west-1.amazonaws.com/launchables/metadata/main/target_7.txt",
						  "https://s3-eu-west-1.amazonaws.com/launchables/metadata/main/target_8.txt",
						  "https://s3-eu-west-1.amazonaws.com/launchables/metadata/main/target_9.txt",
						  "https://s3-eu-west-1.amazonaws.com/launchables/metadata/main/target_10.txt",
						  "https://s3-eu-west-1.amazonaws.com/launchables/metadata/main/target_11.txt",
						  "https://s3-eu-west-1.amazonaws.com/launchables/metadata/main/target_12.txt",
						  "https://s3-eu-west-1.amazonaws.com/launchables/metadata/main/target_13.txt",
						  "https://s3-eu-west-1.amazonaws.com/launchables/metadata/main/target_14.txt",
						  "https://s3-eu-west-1.amazonaws.com/launchables/metadata/main/target_15.txt",
						  "https://s3-eu-west-1.amazonaws.com/launchables/metadata/main/target_16.txt",
						  "https://s3-eu-west-1.amazonaws.com/launchables/metadata/main/target_17.txt",
						  "https://s3-eu-west-1.amazonaws.com/launchables/metadata/main/target_18.txt",
						  "https://s3-eu-west-1.amazonaws.com/launchables/metadata/main/target_19.txt",
						  "https://s3-eu-west-1.amazonaws.com/launchables/metadata/main/target_20.txt"
						  };
						  
		//debugText.text += "Dir exists?" + Directory.Exists(streamPath) + "\n";
		
		if(!Directory.Exists(streamPath)){
			//debugText.text += "creating directory";
			Directory.CreateDirectory(streamPath);
		}
		
		//download each file from url
		foreach(string url in urls){
			StartCoroutine(downloadFile(url));
		}
	}
	
	IEnumerator downloadFile(string url){
		//get file name
		string[] splitURL = url.Split('/');
		string fileName =  splitURL[splitURL.Length - 1];
		
		//download 
		WWW www = new WWW(url);
		yield return www;
		
		//save file
		string savePath = streamPath + fileName;
		File.WriteAllBytes(savePath, www.bytes);
		//debugText.text += "saving " + fileName + "\n";
		
		//wait until database is updated to load into app
		if(fileName == "Launchable_Mobile_Application.dat"){
			//debugText.text += "loading dataset \n";
			VuforiaARController.Instance.RegisterVuforiaStartedCallback(LoadDataSet);
		}
	}
	
	//uses vuforia library to load a dataset with all the image targets then creates image target objects that are AR trackable
    void LoadDataSet()
    {
		//object tracker is what finds targets on the camera feed. 
        ObjectTracker objectTracker = TrackerManager.Instance.GetTracker<ObjectTracker>();
        DataSet dataSet = objectTracker.CreateDataSet();
		
		string dataSetPath = streamPath + dataSetName + ".xml";
		//debugText.text += dataSetPath + "\n";

		//load dataset
        if (dataSet.Load(dataSetPath, VuforiaUnity.StorageType.STORAGE_ABSOLUTE)) {
             
            objectTracker.Stop();  // stop tracker so that we can add new dataset
 
            if (!objectTracker.ActivateDataSet(dataSet)) {
                // Note: ImageTracker cannot have more than 1000 total targets activated
                Debug.Log("<color=yellow>Failed to Activate DataSet: " + dataSetName + "</color>");
            }
 
            if (!objectTracker.Start()) {
                Debug.Log("<color=yellow>Tracker Failed to Start.</color>");
            }
 
			//create image targets and add vuforia components
            int counter = 0;
            IEnumerable<TrackableBehaviour> tbs = TrackerManager.Instance.GetStateManager().GetTrackableBehaviours();
            foreach (TrackableBehaviour tb in tbs) {
                if (tb.name == "New Game Object") {
 
                    // change generic name to include trackable name
                    tb.gameObject.name = ++counter + ":DynamicImageTarget-" + tb.TrackableName;
 
                    // add additional script components for trackable
                    tb.gameObject.AddComponent<DefaultTrackableEventHandler>();
                    tb.gameObject.AddComponent<TurnOffBehaviour>();
 
					// attaches all the gameobjects for functionality like video and contact card
                    if (augmentationObject != null) {
                        // instantiate augmentation object and parent to trackable
                        GameObject augmentation = (GameObject)GameObject.Instantiate(augmentationObject);
                        augmentation.transform.SetParent(tb.gameObject.transform,true);
                        augmentation.gameObject.SetActive(true);
                    } else {
                        Debug.Log("<color=yellow>Warning: No augmentation object specified for: " + tb.TrackableName + "</color>");
                    }
                }
            }
        } else {
			//debugText.text += "<color=red>Failed to load dataset: '" + dataSetName + "'</color>";
            Debug.LogError("<color=red>Failed to load dataset: '" + dataSetName + "'</color>");
	}
	}
}