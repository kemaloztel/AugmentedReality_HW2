using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RigidTransformation : MonoBehaviour
{
    public static List<Vector3> sourcePoints = new List<Vector3>();
    public static List<Vector3> targetPoints = new List<Vector3>();
    private static List<GameObject> createdSpheres = new List<GameObject>(); //olusturdugum kureleri silmek icin

    public static void SetSourcePoints(List<Vector3> points)
    {
        sourcePoints = points;
    }

    public static void SetTargetPoints(List<Vector3> points)
    {
        targetPoints = points;
    }

    List<Point3D> transformedPoints;

    public void Main(Transform transform)
    {
        List<Point3D> sourcePoint3DList = ConvertToPointsList(sourcePoints);
        List<Point3D> targetPoint3DList = ConvertToPointsList(targetPoints);
        RigidTransformationResult result = RANSACRigidTransformation(sourcePoint3DList, targetPoint3DList, 1000, 0.1);

        List<Point3D> ConvertToPointsList(List<Vector3> vectors)
        {
            List<Point3D> points = new List<Point3D>();
            foreach (Vector3 vector in vectors)
            {
                points.Add(new Point3D(vector.x, vector.y, vector.z));
            }
            return points;
        }

        PrintTransformation(result.TranslationX, result.TranslationY, result.TranslationZ, transform);
    }

    // RANSAC ile Rigid Transformation
    public RigidTransformationResult RANSACRigidTransformation(List<Point3D> sourcePoints, List<Point3D> targetPoints, int iterations, double threshold)
    {
        if (sourcePoints.Count == 0 || targetPoints.Count == 0)
        {
            Debug.Log("Error: sourcePoints or targetPoints is empty.");
            return null;
        }
        RigidTransformationResult bestResult = null;
        int n = sourcePoints.Count;


        for (int i = 0; i < iterations; i++)
        {
            // Rastgele nokta eslestirmeleri sec
            List<Point3D> sampledSource = new List<Point3D>();
            List<Point3D> sampledTarget = new List<Point3D>();

            System.Random rand = new System.Random();
            for (int j = 0; j < n; j++)
            {
                int randomIndex = rand.Next(n);
                sampledSource.Add(sourcePoints[randomIndex]);
                sampledTarget.Add(targetPoints[randomIndex]);
            }

            // En kucuk kareler cozumu ile transformasyonu hesapla
            RigidTransformationResult result = CalculateRigidTransformation(sampledSource, sampledTarget);

            // Hesaplanan transformasyonu kullanarak tum noktalari donustur
            transformedPoints = TransformPoints(sourcePoints, result.TranslationX, result.TranslationY, result.TranslationZ);

            // Hata hesapla
            double error = CalculateError(transformedPoints, targetPoints);

            // Eger bu iterasyonda bulunan hata en kucukse, bu transformasyonu kaydet
            if (bestResult == null || error < bestResult.Error)
            {
                bestResult = new RigidTransformationResult
                {
                    TranslationX = result.TranslationX,
                    TranslationY = result.TranslationY,
                    TranslationZ = result.TranslationZ,
                    Error = error
                };
            }

            // Eger hata, belirlenen threshold degerinden kucukse, donguyu sonlandir
            if (error < threshold){
                Debug.Log("Bir hata meydana geldi");
                break;
            }
        }
        
        return bestResult;
    }

    // En kucuk kareler cozumu ile Rigid Transformation
    public RigidTransformationResult CalculateRigidTransformation(List<Point3D> sourcePoints, List<Point3D> targetPoints)
    {
        int n = sourcePoints.Count;

        double meanSourceX = sourcePoints.Select(p => p.X).Average();
        double meanSourceY = sourcePoints.Select(p => p.Y).Average();
        double meanSourceZ = sourcePoints.Select(p => p.Z).Average();

        double meanTargetX = targetPoints.Select(p => p.X).Average();
        double meanTargetY = targetPoints.Select(p => p.Y).Average();
        double meanTargetZ = targetPoints.Select(p => p.Z).Average();

        double sxx = 0, sxy = 0, sxz = 0, syx = 0, syy = 0, syz = 0, szx = 0, szy = 0, szz = 0;

        for (int i = 0; i < n; i++)
        {
            double xDiffSource = sourcePoints[i].X - meanSourceX;
            double yDiffSource = sourcePoints[i].Y - meanSourceY;
            double zDiffSource = sourcePoints[i].Z - meanSourceZ;

            double xDiffTarget = targetPoints[i].X - meanTargetX;
            double yDiffTarget = targetPoints[i].Y - meanTargetY;
            double zDiffTarget = targetPoints[i].Z - meanTargetZ;

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

        double detX = syy * szz - szy * syz;
        double detY = syz * szx - szz * syx;
        double detZ = syx * szy - syz * szx;

        double scale = Math.Pow(detX * detX + detY * detY + detZ * detZ, 1.0 / 3.0);

        RigidTransformationResult result = new RigidTransformationResult
        {
            TranslationX = meanTargetX - scale * (sxx + sxy + sxz) / n,
            TranslationY = meanTargetY - scale * (syx + syy + syz) / n,
            TranslationZ = meanTargetZ - scale * (szx + szy + szz) / n
        };

        return result;
    }

    // Noktalari belirtilen transformasyona gore donusturen fonksiyon
    public List<Point3D> TransformPoints(List<Point3D> points, double tx, double ty, double tz)
    {
        return points.Select(p => new Point3D(p.X + tx, p.Y + ty, p.Z + tz)).ToList();
    }

    // Noktalar arasindaki hata degeri
    public double CalculateError(List<Point3D> points1, List<Point3D> points2)
    {
        double error = 0;
        for (int i = 0; i < points1.Count; i++)
        {
            double dx = points1[i].X - points2[i].X;
            double dy = points1[i].Y - points2[i].Y;
            double dz = points1[i].Z - points2[i].Z;

            error += Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        return error / points1.Count;
    }

    // Transformasyon sonculari
    public void PrintTransformation(double tx, double ty, double tz, Transform transform)
    {
        Debug.Log($"Hesaplanan Rigid Transformasyon:\nTranslationX: {tx}, TranslationY: {ty}, TranslationZ: {tz}");
        foreach (Point3D point in transformedPoints)
        {
            Vector3 vectorPoint = new Vector3((float)point.X, (float)point.Y, (float)point.Z);
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = vectorPoint;
            sphere.transform.parent = transform;
            Color greenColor = new Color(0, 1, 0);
            sphere.GetComponent<Renderer>().material.color = greenColor;
            createdSpheres.Add(sphere);
            GameObject mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            mainCamera.transform.position = sphere.transform.position - mainCamera.transform.forward * 15f;
        }
    }

    // 3D Nokta sinifi
    public class Point3D
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public Point3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    // Rigid transformation sonuclari sinifi
    public class RigidTransformationResult
    {
        public double TranslationX { get; set; }
        public double TranslationY { get; set; }
        public double TranslationZ { get; set; }
        public double Error { get; set; }
    }

    public void DeleteSpheres()
    {
        foreach (GameObject sphere in createdSpheres)
        {
            Destroy(sphere); // Olusturulan tum kureleri sil
        }

        createdSpheres.Clear(); // Listenin icini temizle
    }
}