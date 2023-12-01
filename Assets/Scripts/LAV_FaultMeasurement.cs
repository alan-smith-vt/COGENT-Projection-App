using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;


public class FaultPoint //raccolta dati numerici posizione punti - la classe è più flessibile di una struct perchè puoi inserire variabili anche al di fuori delle () e può essere chiamata in altri script direttamente
{
    public FaultPoint(Vector3 p, Quaternion rot, Vector3 n)
    {
        position = p;
        rotation = rot;
        norm = n;
        diameter = 0.01f;
    }
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 norm;
    public float diameter;
}
public struct OrientedBoundingBox
{
    public Vector3 Center;
    public Vector3 Extents;
    public Quaternion Rotation;
}
//Script per aggiungere punti, mostrare bounding box e scritte misure
public class LAV_FaultMeasurement : MonoBehaviour
{
    private ProjectionManager projectionManager;

    public FaultPoint currentPoint;
    public bool showBounds;
    [HideInInspector] public GameObject selectedPoint;

    private float length, width;
    private Vector3 boxZSum, boxZMean;
    public List<FaultPoint> faultPoints; //insieme di punti creati
    private Bounds faultBounds;
    private Transform boundsDisplay, faultPoint;
    private TextMeshPro heightText, widthText, depthText, SphereDiameter;
    public Quaternion Correction_angles;

    void Awake()
    {
        length = 0f;
        width = 0f;
        boxZSum = Vector3.zero;
        boxZMean = Vector3.zero;
        faultPoints = new List<FaultPoint>();
        faultPoint = transform.Find("FaultPoint");
        Debug.Log("FM start: " + faultPoint.name);
        boundsDisplay = transform.Find("BoundsDisplay");
        boundsDisplay.gameObject.SetActive(showBounds); //si attiva se è spuntato 

        heightText = transform.Find("TextObjects/HeightText").GetComponent<TextMeshPro>();
        widthText = transform.Find("TextObjects/WidthText").GetComponent<TextMeshPro>();
        depthText = transform.Find("TextObjects/DepthText").GetComponent<TextMeshPro>();
        heightText.gameObject.SetActive(showBounds);
        widthText.gameObject.SetActive(showBounds);
        depthText.gameObject.SetActive(showBounds);

        projectionManager = GameObject.Find("MixedRealityPlayspace/Projection Manager").GetComponent<ProjectionManager>();
    }
    public void updateBoundsVisibility(bool newVisibility)
    {
        showBounds = newVisibility;
        boundsDisplay.gameObject.SetActive(showBounds);
        heightText.gameObject.SetActive(showBounds);
        widthText.gameObject.SetActive(showBounds);
        depthText.gameObject.SetActive(showBounds);
    }
    void Update()
    {

    }
    public void AddFaultPoint(Vector3 pos)
    {
        Quaternion rot = Quaternion.identity;
        Vector3 norm = Vector3.zero;
        Transform newPt = Instantiate(faultPoint, pos, rot, transform);
        newPt.name = "FaultPoint_" + faultPoints.Count.ToString();
        newPt.GetComponent<MeshRenderer>().enabled = true;

        selectedPoint = newPt.gameObject;  //serve per richiamare il punto nello script FaultManager
        SphereDiameter = newPt.transform.Find("SphereDiameter").GetComponent<TextMeshPro>();
        selectedPoint.transform.forward = projectionManager.zAxisOverride;

        SphereDiameter.transform.LookAt(Camera.main.transform.position + Camera.main.transform.forward * 2f);
        SphereDiameter.transform.position = SphereDiameter.transform.position + projectionManager.zAxisOverride * 0.03f;
        currentPoint = new FaultPoint(pos, rot, norm);
        faultPoints.Add(currentPoint); //aggiunge punto alla lista

        boxZSum += norm;
        boxZMean = boxZSum / faultPoints.Count;
        CalculateMinimumBoundingBox();
    }

    public void RemoveFaultPoint(Vector3 pos)
    { faultPoints.RemoveAll(x => x.position == pos); }

    public void RemoveAllFaultPoints()
    {
        // Iterate over all fault points
        for (int i = 0; i < faultPoints.Count; i++)
        {
            // Find the corresponding GameObject in the scene and destroy it
            GameObject obj = GameObject.Find("FaultPoint_" + i);
            if (obj != null)
            {
                Destroy(obj);
            }
        }

        // Clear the faultPoints list
        faultPoints.Clear();

        // Reset boxZSum and boxZMean
        boxZSum = Vector3.zero;
        boxZMean = Vector3.zero;

        // You might also need to reset the state of other variables affected by the creation of fault points
    }

    public OrientedBoundingBox CalculateOrientedBoundingBox(List<Vector3> points, Vector3 customNormal)
    {
        // Normalize the custom normal
        customNormal.Normalize();

        // Determine the other two axes for the bounding box, orthogonal to the custom normal
        Vector3 axis1 = Vector3.Cross(customNormal, Vector3.up).normalized;
        if (axis1.sqrMagnitude < 0.001f)
        {
            axis1 = Vector3.Cross(customNormal, Vector3.right).normalized;
        }
        Vector3 axis2 = Vector3.Cross(customNormal, axis1).normalized;

        // Calculate the minimum and maximum extents along each axis
        float min1 = float.MaxValue, min2 = float.MaxValue, min3 = float.MaxValue;
        float max1 = float.MinValue, max2 = float.MinValue, max3 = float.MinValue;

        foreach (Vector3 point in points)
        {
            float projection1 = Vector3.Dot(point, axis1);
            float projection2 = Vector3.Dot(point, axis2);
            float projection3 = Vector3.Dot(point, customNormal);

            min1 = Mathf.Min(min1, projection1);
            min2 = Mathf.Min(min2, projection2);
            min3 = Mathf.Min(min3, projection3);

            max1 = Mathf.Max(max1, projection1);
            max2 = Mathf.Max(max2, projection2);
            max3 = Mathf.Max(max3, projection3);
        }

        // Calculate the center and extents of the bounding box
        Vector3 center = (min1 + max1) / 2 * axis1 + (min2 + max2) / 2 * axis2 + (min3 + max3) / 2 * customNormal;
        Vector3 extents = new Vector3((max1 - min1) / 2, (max2 - min2) / 2, (max3 - min3) / 2);

        // Calculate the rotation that aligns the bounding box with the custom normal
        Quaternion rotation = Quaternion.LookRotation(customNormal, axis2);

        // Create the oriented bounding box
        OrientedBoundingBox obb = new OrientedBoundingBox();
        obb.Center = center;
        obb.Extents = extents;
        obb.Rotation = rotation;

        return obb;
    }
    public void CalculateMinimumBoundingBox()
    {
        updateBoundsVisibility(true);
        if (showBounds)
        {
            OrientedBoundingBox boundingbox = CalculateOrientedBoundingBox(faultPoints.Select(x => x.position).ToList(), projectionManager.zAxisOverride);
            //Debug.Log($"Bounding box center: {boundingbox.Center.x},{boundingbox.Center.y},{boundingbox.Center.z}");

            boundsDisplay.localPosition = boundingbox.Center;
            Vector3 scale = boundingbox.Extents * 2f;
            boundsDisplay.localScale = scale;

            boundsDisplay.forward = projectionManager.zAxisOverride;

            heightText.transform.parent.localPosition = boundingbox.Center;
            heightText.transform.parent.forward = projectionManager.zAxisOverride;
            widthText.transform.localPosition = new Vector3(0f, scale.y / 2, 0f);
            heightText.transform.localPosition = new Vector3(scale.x / 2, 0f, 0f);
            widthText.transform.localRotation = Quaternion.Euler(180, 180, 180);
            heightText.transform.localRotation = Quaternion.Euler(180, 180, 180);

            heightText.SetText(((scale.y * 39.3701f)).ToString("F2") + "in");
            widthText.SetText(((scale.x * 39.3701f)).ToString("F2") + "in");

            if (scale.z > 0.01)
            {
                depthText.gameObject.SetActive(true);
                depthText.transform.localPosition = new Vector3(0f, 0f, 0.1f);
                depthText.transform.localRotation = Quaternion.Euler(180, 180, 180);
                depthText.SetText((scale.z / 39.3701f).ToString("F2") + "in");
            }
            else
            {
                depthText.SetText("0.00 in");
                depthText.gameObject.SetActive(false);
            }
        }
    }
}



