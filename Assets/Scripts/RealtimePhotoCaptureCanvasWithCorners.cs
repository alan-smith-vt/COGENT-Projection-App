using UnityEngine;
using static UnityEngine.Mathf;

public class RealtimePhotoCaptureCanvasWithCorners : MonoBehaviour
{
    public GameObject cornerPrefab;
    private Camera mainCamera;

    private LineRenderer lineRenderer;

    float followDist = 2;//meters
    float maxFollowDist = 2;//meters

    void Start()
    {
        mainCamera = Camera.main;
        lineRenderer = this.transform.gameObject.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        Color c = Color.red;
        c.a = 0.5f;
        lineRenderer.material.SetColor("_Color", c);
        lineRenderer.positionCount = 5;
    }

    void Update()
    {
        cameraParameters camParams = calculateCameraParameters(mainCamera.cameraToWorldMatrix);

        //Find how far forward to project the framing plane
        //TODO: THERE IS A GLITCH WHERE IF YOUR FORWARD POINT DOESN'T INTERSECT THE MESH
        //  THE OTHER POINTS WHO ALSO DO NOT HIT THE MESH WILL GLITCH OUT INSTEAD OF DEFAULTING TO THE FORWARD DISTANCE AS INTENDED
        Vector3 forwardPoint = RaycastPoint(camParams.focalPoint - camParams.vz, camParams);

        float forwardDistance = Vector3.Distance(camParams.focalPoint, forwardPoint);
        if (forwardDistance < maxFollowDist)
        { 
            followDist = forwardDistance;
        }
        else
        {
            followDist = maxFollowDist;
            forwardPoint = camParams.focalPoint - maxFollowDist * (camParams.vz / camParams.vz.magnitude);
        }

        float hfov = 64.69f / 2;
        float vfov = 39.21f / 2;

        Vector3 vLeft = rotateAboutK(hfov / 2, -camParams.vy, -camParams.vz);
        Vector3 vRight = rotateAboutK(-hfov / 2, -camParams.vy, -camParams.vz);
        Vector3 vTop = rotateAboutK(vfov / 2, -camParams.vx, -camParams.vz);
        Vector3 vBot = rotateAboutK(-vfov / 2, -camParams.vx, -camParams.vz);

        Vector3 leftEdge = intersection(camParams.focalPoint, vLeft, camParams.focalPoint - camParams.vz * followDist, -camParams.vz);
        Vector3 rightEdge = intersection(camParams.focalPoint, vRight, camParams.focalPoint - camParams.vz * followDist, -camParams.vz);
        Vector3 topEdge = intersection(camParams.focalPoint, vTop, camParams.focalPoint - camParams.vz * followDist, -camParams.vz);
        Vector3 botEdge = intersection(camParams.focalPoint, vBot, camParams.focalPoint - camParams.vz * followDist, -camParams.vz);

        Vector3 topLeftCorner = intersection(topEdge, forwardPoint - leftEdge, leftEdge, forwardPoint - leftEdge);
        Vector3 topRightCorner = intersection(topEdge, forwardPoint - rightEdge, rightEdge, forwardPoint - rightEdge);
        Vector3 botLeftCorner = intersection(botEdge, forwardPoint - leftEdge, leftEdge, forwardPoint - leftEdge);
        Vector3 botRightCorner = intersection(botEdge, forwardPoint - rightEdge, rightEdge, forwardPoint - rightEdge);

        Vector3[] corners = new Vector3[]
        {
            botLeftCorner,
            topLeftCorner,
            topRightCorner,
            botRightCorner,
            botLeftCorner,
        };

        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 projCorner = RaycastPoint(corners[i], camParams);
            float distProj = Vector3.Distance(projCorner, camParams.focalPoint);
            float distOG = Vector3.Distance(corners[i], camParams.focalPoint);
            if (distProj < distOG)
            {
                corners[i] = projCorner;
            }
        }

        //SPAWNING SPHERES FOR DEBUGGING
        /*
        cornerSpheres[3].transform.position = corners[3];
        cornerSpheres[3].GetComponent<MeshRenderer>().material.SetColor("_Color", Color.magenta);
        cornerSpheres[2].transform.position = corners[2];
        cornerSpheres[2].GetComponent<MeshRenderer>().material.SetColor("_Color", Color.red);
        cornerSpheres[1].transform.position = corners[1];
        cornerSpheres[1].GetComponent<MeshRenderer>().material.SetColor("_Color", Color.green);
        cornerSpheres[0].transform.position = corners[0];
        cornerSpheres[0].GetComponent<MeshRenderer>().material.SetColor("_Color", Color.blue);
        
        cornerSpheres[4].transform.position = leftEdge;
        cornerSpheres[5].transform.position = rightEdge;
        cornerSpheres[6].transform.position = topEdge;
        cornerSpheres[7].transform.position = botEdge;
        */



        lineRenderer.SetPositions(corners);

        lineRenderer.startWidth = 0.01f;
        lineRenderer.endWidth = 0.01f;

    }

    private cameraParameters calculateCameraParameters(Matrix4x4 c2w)
    {
        cameraParameters outParams = new cameraParameters();
        outParams.focalPoint = c2w.GetColumn(3);
        outParams.vx = c2w.GetColumn(0);
        outParams.vy = c2w.GetColumn(1);
        outParams.vz = c2w.GetColumn(2);

        outParams.vz = outParams.vz / outParams.vz.magnitude;

        outParams.imageW = 3904;
        outParams.imageH = 2196;
        //outParams.imageW = 640; //Webcam
        //outParams.imageH = 480;
        return outParams;
    }

    private class cameraParameters
    {
        public Vector3 vx;
        public Vector3 vy;
        public Vector3 vz;
        public Vector3 focalPoint;
        public float imageW;
        public float imageH;
    }
    private Vector3 RaycastPoint(Vector3 p2, cameraParameters camParams)
    {
        Vector3 v = p2 - camParams.focalPoint;
        float dist = 10f;

        RaycastHit[] hits = Physics.RaycastAll(camParams.focalPoint, v, dist);
        RaycastHit hit = new RaycastHit();
        bool foundHit = false;

        //Take the closer raycast hit if there are multiple
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider.gameObject.layer == 31)
            {
                if (foundHit)
                {
                    float distHit_i = Vector3.Distance(hits[i].point, camParams.focalPoint);
                    float distHit = Vector3.Distance(hit.point, camParams.focalPoint);
                    if (distHit_i < distHit)
                    {
                        hit = hits[i];
                    }
                }
                else
                {
                    hit = hits[i];
                    foundHit = true;
                }
            }
        }

        if (!foundHit)
        {
            hit.point = camParams.focalPoint - camParams.vz * 2f;
            hit.normal = camParams.vz;
        }
        else
        {
            hit.point = hit.point + hit.normal * 0.001f;
        }

        return hit.point;
    }

    private Vector3 intersection(Vector3 l0, Vector3 lv, Vector3 p0, Vector3 n)
    {
        Vector3 intersectionPoint = l0 + (Vector3.Dot((p0 - l0), n) / Vector3.Dot(lv, n)) * lv;
        return intersectionPoint;
    }

    private Vector3 rotateAboutK(float inputAngle, Vector3 rotationAxisK, Vector3 inputV)
    {
        inputAngle = inputAngle * Mathf.PI / 180;
        //Rodrigues' rotation formula
        Vector3 vrot = inputV * Cos(inputAngle) + Vector3.Cross(rotationAxisK, inputV) * Sin(inputAngle) + rotationAxisK * Vector3.Dot(rotationAxisK, inputV) * (1 - Cos(inputAngle));
        return vrot;
    }
}
