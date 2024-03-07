using Firebase;
using Firebase.Storage;
using Lean.Transition;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using System;
using Proyecto26;
using Firebase.Extensions;
using System.Linq;

public class FetchModelDataCountFromFireBase : MonoBehaviour
{
    public static FetchModelDataCountFromFireBase Instance;
    [Header("Player")]
    public FPSController player;
    public TriLibCore.Samples.AssetViewer load;
    [Header("Canvas And Components")]
    public Canvas startScreen;
    public Canvas listScreen, joystickScreen, loadingScreen, spanPointScreen;
    public HandleButtonComponents mapsPrefab, spawnPositionPrefab;
    public Transform scrollViewContent, spawnPointContent;
    public GameObject exploreButton, floorMap;
    [Header("Mesh And Materials")]
    public GameObject LocalGround;
    internal GameObject spawnObjectFromFile, ModelFromFile, ground, floor, ceiling;
    public Material wallMaterial, ceilingMaterial, groundMaterial;
    [HideInInspector]
    public List<Material> wallMaterials, ceilingMaterials, groundMaterials;
    [Header("Events")]
    public UnityEvent onClearDependency = new UnityEvent();
    [Header("Lists")]
    public List<Map> maps;
    public List<string> links;
    [HideInInspector]
    public List<HandleButtonComponents> buttonHandler, spawnPosButtonHandler;
    [HideInInspector]
    public List<Transform> spawnPoints;
    [HideInInspector]
    public List<string> spawnPointsName;
    internal int ModelCount, spawncount;
    internal GameObject Ground;
    internal byte[] customBytes;
    internal string filename;
    internal SkinnedMeshRenderer skinnedMesh;
    internal Vector3 intialpos, intialRot,startPos,startRot;
    internal Data[] datas;
    internal string res;
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(this);
        }
    }
    private async void Start()
    {
        var dependensyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (dependensyStatus == DependencyStatus.Available)
        {
            onClearDependency?.Invoke();
        }
        intialpos = player.transform.position;
        intialRot = player.transform.eulerAngles;
        FetchDataFromFirebase();
    }
    public void SetPerimeter()
    {
        var bounds = Ground.GetComponent<MeshRenderer>().bounds.size;
        LocalGround.transform.localScale = new Vector3(bounds.x, 1, bounds.z);
        LocalGround.transform.position = new Vector3(0, -0.575f, 0);
    }
    public void FetchDataFromFirebase()
    {
        loadingScreen.enabled = true;
        maps.Clear();
        links.Clear();
        ModelCount = 0;
        RestClient.Get("https://firebasestorage.googleapis.com/v0/b/sample-3c5ec.appspot.com/o").Then(responce =>
        {
            res = responce.Text;
            datas = JsonHelper.FromJsonString<Data>(responce.Text);
            for (int i = 0; i < datas.Length; i++)
            {
                var storageRef = FirebaseStorage.DefaultInstance.RootReference;
                StorageReference forestRef = storageRef.Child(datas[i].name);
                forestRef.GetMetadataAsync().ContinueWithOnMainThread(task =>
                {
                    if (!task.IsFaulted && !task.IsCanceled)
                    {
                        StorageMetadata meta = task.Result;
                        Debug.Log(meta.Name);
                        var buildMaps = new Map();
                        buildMaps.name = meta.Name;
                        buildMaps.DateAndTime = meta.UpdatedTimeMillis.ToString();
                        ModelCount++;
                        maps.Add(buildMaps);
                        links.Add("https://firebasestorage.googleapis.com/v0/b/sample-3c5ec.appspot.com/o/" + meta.Name + "?alt=media&token=2a9e8cc4-50cc-4ae2-ac51-824306612bdd");
                        if (datas.Length == ModelCount)
                        {
                            transform.EventTransition(() =>
                            {
                                GenerateSpawnMaps(links, maps);
                                loadingScreen.enabled = false;
                            }, 1.5f);
                        }
                    }
                });
            }
        });
    }

    public void UploadToFirebase()
    {
        FirebaseStorage storage = FirebaseStorage.DefaultInstance;
        StorageReference storageRef = storage.RootReference;
        StorageReference riversRef = storageRef.Child(filename);

        riversRef.PutBytesAsync(customBytes)
            .ContinueWith((Task<StorageMetadata> task) =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.Log(task.Exception.ToString());
                }
                else
                {
                    StorageMetadata metadata = task.Result;
                    string md5Hash = metadata.Md5Hash;
                    Debug.Log("Finished uploading...");
                    Debug.Log("md5 hash = " + md5Hash);
                }
            });
        transform.EventTransition(() =>
        {
            FetchDataFromFirebase();
        }, 2f);
    }

    public void InstantiateNewButtons(int newCount, List<HandleButtonComponents> handler, HandleButtonComponents objectToInstantiate, Transform content, Action callBack = null)
    {
        for (int i = 0; i < newCount; i++)
        {
            handler.Add(Instantiate(objectToInstantiate, content));
            callBack?.Invoke();
        }
    }

    public void GenerateSpawnMaps(List<string> link, List<Map> map)
    {
        if (buttonHandler.Count < ModelCount)
        {
            InstantiateNewButtons(ModelCount - buttonHandler.Count, buttonHandler, mapsPrefab, scrollViewContent);
        }
        for (int i = 0; i < buttonHandler.Count; i++)
        {
            if (i > link.Count - 1)
            {
                buttonHandler[i].gameObject.SetActive(false);
                continue;
            }
            buttonHandler[i].button.onClick.RemoveAllListeners();
            buttonHandler[i].gameObject.SetActive(true);
            buttonHandler[i].buttonPrefabDateAndTime.text = "Date and Time : " + map[i].DateAndTime;
            buttonHandler[i].buttonPrefabName.text = "Name : " + map[i].name;
            buttonHandler[i].displayInfoAsContact.text = map[i].name.Substring(0, UnityEngine.Random.Range(1, 3));
            buttonHandler[i].image.color = buttonHandler[i].color[UnityEngine.Random.Range(0, buttonHandler[i].color.Length)];
            var buttonLink = link[i];
            buttonHandler[i].button.onClick.AddListener(() =>
            {
                load._modelUrl.text = buttonLink;
                load.LoadModelFromURLWithDialogValues();
                listScreen.gameObject.SetActive(false);
                ResetPlayer(intialpos,intialRot);

            });
        }
    }
    public void GenerateSpawnPoints(List<Transform> spawnposi)
    {

        if (spawnPosButtonHandler.Count < spawncount)
        {
            InstantiateNewButtons(spawncount - spawnPosButtonHandler.Count, spawnPosButtonHandler, spawnPositionPrefab, spawnPointContent);
        }
        for (int i = 0; i < spawnPosButtonHandler.Count; i++)
        {
            if (i > spawnPosButtonHandler.Count - 1)
            {
                spawnPosButtonHandler[i].gameObject.SetActive(false);
                continue;
            }
            spawnPosButtonHandler[i].button.onClick.RemoveAllListeners();
            spawnPosButtonHandler[i].gameObject.SetActive(true);

            spawnPosButtonHandler[i].image.color = spawnPosButtonHandler[i].color[UnityEngine.Random.Range(0, spawnPosButtonHandler[i].color.Length - 1)];
            spawnPosButtonHandler[i].buttonPrefabName.text = spawnPointsName[i];
            spawnPosButtonHandler[i].displayInfoAsContact.text = spawnPointsName[i].Substring(0, UnityEngine.Random.Range(1, 3));
            var val = i;
            spawnPosButtonHandler[i].button.onClick.AddListener(() =>
            {
                startPos = spawnPoints[val].position;
                startRot = spawnPoints[val].eulerAngles;
                ResetPlayer(startPos,startRot);
                spanPointScreen.enabled = false;

            });
        }
    }
    public void ApplyMatForMesh(Renderer meshRenderer, GameObject meshGameObject)
    {
        transform.EventTransition(() =>
        {
            if (!string.Equals(meshGameObject.name, "Walls", StringComparison.CurrentCultureIgnoreCase) || !string.Equals(meshGameObject.name, "Floor", StringComparison.CurrentCultureIgnoreCase) || !string.Equals(meshGameObject.name, "Ceiling", StringComparison.CurrentCultureIgnoreCase) || !string.Equals(meshGameObject.name, "Ground", StringComparison.CurrentCultureIgnoreCase))
            {
                meshRenderer.GetMaterials(wallMaterials);
                for (int i = 0; i < wallMaterials.Count; i++)
                {
                    wallMaterials[i] = wallMaterial;
                }
                meshRenderer.SetMaterials(wallMaterials);
            }
            if (spawnObjectFromFile != null)
            {
                spawnPoints.Clear();
                var childCount = spawnObjectFromFile.transform.childCount;
                for (int i = 0; i < childCount; i++)
                {
                    spawnPoints.Add(spawnObjectFromFile.transform.GetChild(i));
                    spawnPoints[i].name = spawnPointsName[i];
                }
                spawncount = spawnPoints.Count;
            }
            exploreButton.SetActive(true);


        }, 1);
    }
    public void ApplyMaterial(GameObject meshObject, List<Material> materialList, Material changingMaterial)
    {
        var mesh = meshObject.GetComponent<Renderer>();
        if (mesh != null)
        {
            mesh.GetMaterials(materialList);
            for (int i = 0; i < materialList.Count; i++)
            {
                materialList[i] = changingMaterial;
            }
            mesh.SetMaterials(materialList);
        }
        var val = meshObject.transform.childCount;
        for (int i = 0; i < val; i++)
        {
            var skin = meshObject.transform.GetChild(i).GetComponent<MeshRenderer>();
            if (skin != null)
            {
                skin.GetMaterials(materialList);
                for (int j = 0; j < materialList.Count; j++)
                {
                    materialList[i] = changingMaterial;
                }
                skin.SetMaterials(materialList);
            }
        }
    }
    public void ApplyMaterials()
    {
        if (ceiling != null)
        {
            ApplyMaterial(ceiling, ceilingMaterials, ceilingMaterial);
        }
        if (floor != null)
        {
            ApplyMaterial(floor, groundMaterials, groundMaterial);
        }
        if (ground != null)
        {
            ApplyMaterial(ground, groundMaterials, groundMaterial);
        }
    }
    public void GenerateSpawnPoints()
    {
        if (spawnObjectFromFile != null)
        {
            GenerateSpawnPoints(spawnPoints);
            floorMap.SetActive(true);
        }
        else
        {
            floorMap.SetActive(false);
        }
    }
    public void ResetPlayer(Vector3 pos,Vector3 rot)
    {
        player.enabled = false;
        player.transform.localPosition = pos;
        player.transform.eulerAngles = rot;
        transform.EventTransition(() =>
        {
            player.enabled = true;
        }, .5f);
    }
}
[Serializable]
public class Map
{
    public string name;
    public string DateAndTime;
}
[Serializable]
public class Data
{
    public string name;
    public string bucket;
}