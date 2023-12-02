using System;      
using System.IO;  
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class ReadPoint2 : MonoBehaviour
{
    // Vector3 tipindeki noktalari iceren bir liste
    public List<Vector3> points = new List<Vector3>();
    private String filePath = "Assets/Scripts/PointSet2.txt";

    void Start()
    {
        LoadPointClouds();
        VisualizePointCloud();
    }

    public void LoadPointClouds()
    {
        string[] lines = File.ReadAllLines(filePath);
        //ilk satir baslik oldugu icin okunmuyor
        for (int i = 1; i < lines.Length; i++)
        {
            string[] coordinates = lines[i].Split(' ');
            float x = float.Parse(coordinates[0]);
            float y = float.Parse(coordinates[1]);
            float z = float.Parse(coordinates[2]);
            points.Add(new Vector3(x, y, z));
        }
        Debug.Log("Toplam " + points.Count + " nokta olusturuldu");
        RigidTransformation.SetTargetPoints(points);
        GlobalScaleRigidTransformation.SetTargetPoints(points);
    }

    void VisualizePointCloud()
    {
        Transform pointCloudTransform = this.transform;
        foreach (Vector3 point in points)
        {
            //Her noktayi kucuk bir kure olarak gosterelim
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            
            // Kürenin konumunu belirtilen noktaya ayarla
            sphere.transform.position = point;
            sphere.transform.parent = pointCloudTransform;
            Color blueColor = new Color(1, 0, 0);
            sphere.GetComponent<Renderer>().material.color = blueColor;

            GameObject mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            mainCamera.transform.position = sphere.transform.position - mainCamera.transform.forward * 15f;
        }
    }

    public void CallRigitTransformation(){
        RigidTransformation rigidTransformation = new RigidTransformation();
        Transform transform = this.transform;
        rigidTransformation.Main(transform);
    }

    public void CallGlobalScaleRigidTransformation(){
        GlobalScaleRigidTransformation globalScaleRigidTransformation = new GlobalScaleRigidTransformation();
        Transform transform = this.transform;
        globalScaleRigidTransformation.Main(transform);
    }

}
