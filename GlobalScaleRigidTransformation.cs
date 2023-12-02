using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Random = UnityEngine.Random;

public class GlobalScaleRigidTransformation
{
    public static List<Vector3> sourcePoints = new List<Vector3>();
    public static List<Vector3> targetPoints = new List<Vector3>();

    public static void SetSourcePoints(List<Vector3> points)
    {
        sourcePoints = points;
    }

    public static void SetTargetPoints(List<Vector3> points)
    {
        targetPoints = points;
    }

    List<Vector3> transformedPoints;

    public void Main(Transform transform)
    {
        GlobalScaleRANSACResult result = GlobalScaleRANSAC(sourcePoints, targetPoints, 1000, 0.1f);
        if (result != null)
        {
            Console.WriteLine("\nHesaplanan Transformasyon:");
            PrintTransformation(result.ScaleFactor, result.TranslationX, result.TranslationY, result.TranslationZ, transform);
        }
        else
        {
            Console.WriteLine("Hata: Rigid Transformation hesaplanamadi.");
        }
    }

    // Global olcekli RANSAC ile rigid transformation
    public GlobalScaleRANSACResult GlobalScaleRANSAC(List<Vector3> sourcePoints, List<Vector3> targetPoints, int iterations, float threshold)
    {
        GlobalScaleRANSACResult bestResult = null;
        int n = sourcePoints.Count;

        for (int i = 0; i < iterations; i++)
        {
            // Rastgele nokta eslestirmeleri sec
            List<Vector3> sampledSource = new List<Vector3>();
            List<Vector3> sampledTarget = new List<Vector3>();

            for (int j = 0; j < n; j++)
            {
                int randomIndex = Random.Range(0, n);
                sampledSource.Add(sourcePoints[randomIndex]);
                sampledTarget.Add(targetPoints[randomIndex]);
            }

            // Global olcekli rigid transformation
            GlobalScaleRANSACResult result = CalculateGlobalScaleRANSAC(sampledSource, sampledTarget);

            // Hesaplanan transformasyonu kullanarak tum noktalari donustur
            transformedPoints = TransformPoints(sourcePoints, result.ScaleFactor, result.TranslationX, result.TranslationY, result.TranslationZ);

            // Hata hesapla
            float error = CalculateError(transformedPoints, targetPoints);

            // Eger bu iterasyonda bulunan hata en kucukkse, bu transformasyonu kaydet
            if (bestResult == null || error < bestResult.Error)
            {
                bestResult = new GlobalScaleRANSACResult
                {
                    ScaleFactor = result.ScaleFactor,
                    TranslationX = result.TranslationX,
                    TranslationY = result.TranslationY,
                    TranslationZ = result.TranslationZ,
                    Error = error
                };
            }

            // Eger hata, belirlenen threshold degerinden kucukse, donguyu sonlandir
            if (error < threshold)
                break;
        }

        return bestResult;
    }

    // Global olcekli RANSAC ile rigid transformation
    public GlobalScaleRANSACResult CalculateGlobalScaleRANSAC(List<Vector3> sourcePoints, List<Vector3> targetPoints)
    {
        int n = sourcePoints.Count;

        if (n == 0)
        {
            Debug.Log("Hata: Nokta listeleri bos veya esit degil.");
            return null; // veya uygun bir deger dondurebilirsiniz.
        }

        float meanSourceX = sourcePoints.Average(p => p.x);
        float meanSourceY = sourcePoints.Average(p => p.y);
        float meanSourceZ = sourcePoints.Average(p => p.z);

        float meanTargetX = targetPoints.Average(p => p.x);
        float meanTargetY = targetPoints.Average(p => p.y);
        float meanTargetZ = targetPoints.Average(p => p.z);

        float sxx = 0, sxy = 0, sxz = 0, syx = 0, syy = 0, syz = 0, szx = 0, szy = 0, szz = 0;

        for (int i = 0; i < n; i++)
        {
            float xDiffSource = sourcePoints[i].x - meanSourceX;
            float yDiffSource = sourcePoints[i].y - meanSourceY;
            float zDiffSource = sourcePoints[i].z - meanSourceZ;

            float xDiffTarget = targetPoints[i].x - meanTargetX;
            float yDiffTarget = targetPoints[i].y - meanTargetY;
            float zDiffTarget = targetPoints[i].z - meanTargetZ;

            sxx += xDiffSource * xDiffTarget;
            sxy += xDiffSource * yDiffTarget;
            sxz += xDiffSource * zDiffTarget;

            syx += yDiffSource * xDiffTarget;
            syy += yDiffSource * yDiffTarget;
            syz += yDiffSource * zDiffTarget;

            szx += zDiffSource * xDiffTarget;
            szy += zDiffSource * yDiffTarget;
            szz += zDiffSource * zDiffTarget;
        }

        float detX = syy * szz - szy * syz;
        float detY = syz * szx - szz * syx;
        float detZ = syx * szy - syz * szx;

        float scale = Mathf.Pow(detX * detX + detY * detY + detZ * detZ, 1.0f / 3.0f);

        GlobalScaleRANSACResult result = new GlobalScaleRANSACResult
        {
            ScaleFactor = scale,
            TranslationX = meanTargetX - scale * (sxx + sxy + sxz) / n,
            TranslationY = meanTargetY - scale * (syx + syy + syz) / n,
            TranslationZ = meanTargetZ - scale * (szx + szy + szz) / n
        };

        return result;
    }

    // Noktalari belirtilen transformasyona gore donusturelim
    public List<Vector3> TransformPoints(List<Vector3> points, float scale, float tx, float ty, float tz)
    {
        return points.Select(p => new Vector3(p.x * scale + tx, p.y * scale + ty, p.z * scale + tz)).ToList();
    }

    // Noktalar arasindaki hata degerini hesaplayan fonksiyon
    public float CalculateError(List<Vector3> points1, List<Vector3> points2)
    {
        float error = 0;
        for (int i = 0; i < points1.Count; i++)
        {
            float dx = points1[i].x - points2[i].x;
            float dy = points1[i].y - points2[i].y;
            float dz = points1[i].z - points2[i].z;

            error += Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        return error / points1.Count;
    }

    // Transformasyon sonuclari
    public void PrintTransformation(float scale, float tx, float ty, float tz, Transform transform)
    {
        Debug.Log($"Scale Factor: {scale}");
        Debug.Log($"Hesaplanan GlobalScale Rigid Transformasyon:\nTranslationX: {tx}, TranslationY: {ty}, TranslationZ: {tz}");
        GameObject mainCamera = GameObject.FindGameObjectWithTag("MainCamera"); 
        foreach (Vector3 point in transformedPoints){
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = point;
            sphere.transform.parent = transform;
            //sphere.transform.localScale = new Vector3(2.0f, 2.0f, 2.0f);
            Color greenColor = new Color(255, 255, 0);
            sphere.GetComponent<Renderer>().material.color = greenColor;
            mainCamera.transform.position = point;
        }
        mainCamera.transform.position = mainCamera.transform.position - mainCamera.transform.forward * 25f;
    }

    // Global olcekli RANSAC sonuclari 
    public class GlobalScaleRANSACResult
    {
        public float ScaleFactor { get; set; }
        public float TranslationX { get; set; }
        public float TranslationY { get; set; }
        public float TranslationZ { get; set; }
        public float Error { get; set; }
    }
}

