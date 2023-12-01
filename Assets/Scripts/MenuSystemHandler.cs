using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuSystemHandler : MonoBehaviour
{
    [Header("UI Menu")]
    public GameObject Interaction_Menu;

    [Header("Sound")]
    public AudioSource sound_Start_Finish;
    public AudioSource sound_camera_shutter;
    public AudioSource sound_Photo;
    public GameObject UILoading;

    [HideInInspector] public string interactionType, crackType, Hand = "Right";

    private ProjectionManager projectionManager;
    private CameraManager cameraManager;
    private NetworkManager networkManager;
    //private UIManager UIManager;
    //private CameraControl CameraControl;
    public enum UIStates
    {
        none = 0,
        Interaction_Menu = 1,
    }
    private readonly Dictionary<UIStates, List<GameObject>> stateObjects = new Dictionary<UIStates, List<GameObject>>
    {
        {UIStates.Interaction_Menu, new List<GameObject>{} },
    };

    private void Awake()
    {
        projectionManager = GameObject.Find("MixedRealityPlayspace/Projection Manager").GetComponent<ProjectionManager>();
        cameraManager = GameObject.Find("MixedRealityPlayspace/Camera Manager").GetComponent<CameraManager>();
        networkManager = GameObject.Find("MixedRealityPlayspace/Network Manager").GetComponent<NetworkManager>();
        Scene scene = SceneManager.GetActiveScene();

        //UIStates from Interaction_Menu
        this.transform.Find("Interaction_Menu/Button_Collection/Take_Picture_Button").GetComponent<Interactable>().OnClick.AddListener(delegate () { cameraManager.TakeDelayedPhoto(); StartCoroutine(SoundWaitPhoto()); });
        this.transform.Find("Interaction_Menu/Button_Collection/Water_Bottle_Button").GetComponent<Interactable>().OnClick.AddListener(delegate () { UILoading.transform.Find("Canvas").gameObject.SetActive(true); StartCoroutine(networkManager.FindWaterBottle()); });
        this.transform.Find("Interaction_Menu/Button_Collection/Bolt_Button").GetComponent<Interactable>().OnClick.AddListener(delegate () { UILoading.transform.Find("Canvas").gameObject.SetActive(true); StartCoroutine(networkManager.FindBolt()); });
        this.transform.Find("Interaction_Menu/Button_Collection/Calculator_Button").GetComponent<Interactable>().OnClick.AddListener(delegate () { UILoading.transform.Find("Canvas").gameObject.SetActive(true); StartCoroutine(networkManager.FindCalculator()); });
        this.transform.Find("Interaction_Menu/Button_Collection/Human_CV_Interaction_Button").GetComponent<Interactable>().OnClick.AddListener(delegate () { });
        this.transform.Find("Interaction_Menu/Button_Collection/Mesh_Mover_Button").GetComponent<Interactable>().OnClick.AddListener(delegate () { });
        

        ////UI States from Manual_Test_Menu
        //this.transform.Find("Manual_Test_Menu/Back_Button").GetComponent<Interactable>().OnClick.AddListener(delegate () { ChangeUIState(UIStates.Interaction_Menu); SceneManager.LoadScene(scene.name); });
        //this.transform.Find("Manual_Test_Menu/Button_Collection/Hand_Method/Hand_Left_Button").GetComponent<Interactable>().OnClick.AddListener(delegate () { Hand = "Left"; });
        //this.transform.Find("Manual_Test_Menu/Button_Collection/Hand_Method/Hand_Right_Button").GetComponent<Interactable>().OnClick.AddListener(delegate () { Hand = "Right"; });
        //this.transform.Find("Manual_Test_Menu/Button_Collection/Flag_Button").GetComponent<Interactable>().OnClick.AddListener(delegate () { LAV_Data.flag = !LAV_Data.flag; Debug.Log($"Flag toggled. New state: {LAV_Data.flag}."); });
        //this.transform.Find("Manual_Test_Menu/Button_Collection/Start_Button").GetComponent<Interactable>().OnClick.AddListener(
        //    delegate ()
        //    {
        //        this.transform.Find("Manual_Test_Menu/Button_Collection/Start_Button").gameObject.SetActive(false);
        //        this.transform.Find("Manual_Test_Menu/Button_Collection/Hand_Method/Hand_Left_Button").gameObject.SetActive(false);
        //        this.transform.Find("Manual_Test_Menu/Button_Collection/Hand_Method/Hand_Right_Button").gameObject.SetActive(false);
        //        this.transform.Find("Manual_Test_Menu/Button_Collection/Finish_Button").gameObject.SetActive(true);
        //        LAV_Data.StartTest();
        //        if (interactionType == "Manual")
        //        {
        //            sound_Start_Finish.Play();
        //            this.transform.Find("Manual_Test_Menu/Button_Collection/Flag_Button").gameObject.SetActive(false);
        //            UIManager.Manual_Interface();
        //        }
        //        if (interactionType == "CV+Manual")
        //        {
        //            CameraControl.Initialize();
        //            this.transform.Find("Manual_Test_Menu/Button_Collection/Flag_Button").gameObject.SetActive(true);
        //            UIManager.CV_Then_Human_Interface();
        //            CameraControl.TakeDelayedPhoto(); StartCoroutine(SoundWaitPhoto()); 

        //        }
        //        if (interactionType == "Manual+CV")
        //        {
        //            CameraControl.Initialize();
        //            this.transform.Find("Manual_Test_Menu/Button_Collection/Flag_Button").gameObject.SetActive(true);
        //            UIManager.Human_Then_CV_Interface();
        //            CameraControl.TakeDelayedPhoto(); StartCoroutine(SoundWaitPhoto());
        //        }

        //    });
        
    }

    void Start()
    {
        //Initilize UI element dict
        stateObjects[UIStates.Interaction_Menu].Add(Interaction_Menu);

        ChangeUIState(UIStates.Interaction_Menu);
    }
    void Update()
    {
        UILoading.transform.Find("Canvas/Image").Rotate(0, 0, 50f * Time.deltaTime);
    }
    public void ChangeUIState(UIStates state)
    {
        ClearAllUI();
        List<GameObject> TempList = new List<GameObject>();//Create a List to hold return of dict call
        TempList = stateObjects[state]; //assign list values from the reference passed UI state into temp list
        foreach (GameObject listgameobject in TempList)
        {
            listgameobject.SetActive(true);//set the specific gameobjects active
        }

    }
    private void ClearAllUI()
    {
        foreach (KeyValuePair<UIStates, List<GameObject>> entry in stateObjects)
        {
            foreach (GameObject go in entry.Value)
            {
                go.SetActive(false);
            }
        }
    }

    IEnumerator SoundWaitPhoto()
    {
        UILoading.transform.Find("PictureFrame").gameObject.SetActive(true);
        sound_Photo.Play();
        yield return new WaitForSeconds(3);
        UILoading.transform.Find("PictureFrame").gameObject.SetActive(false);
        sound_Photo.Stop();
        sound_camera_shutter.Play();
        UILoading.transform.Find("Canvas").gameObject.SetActive(true);
    }
}
