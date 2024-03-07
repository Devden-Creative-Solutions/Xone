using UnityEngine;
using System.Collections;
using System.IO;
using SimpleFileBrowser;
using Firebase.Database;
using Firebase.Extensions;
using System;
using System.Collections.Generic;
using Lean.Transition;

public class FileBrowserTest : MonoBehaviour
{

    FetchModelDataCountFromFireBase fetchModelDataCountFromFireBase;

    private void Start()
    {
        fetchModelDataCountFromFireBase = FetchModelDataCountFromFireBase.Instance;
    }

    private void HandleValueChanged(object sender, ValueChangedEventArgs e)
    {
        throw new NotImplementedException();
    }

    public void OpenFileExplore()
    {
        FileBrowser.SetFilters(false, new FileBrowser.Filter(".fbx", ".fbx"));
        FileBrowser.SetDefaultFilter(".fbx");
        FileBrowser.SetExcludedExtensions(".lnk", ".tmp", ".zip", ".rar", ".exe");
        FileBrowser.AddQuickLink("Users", "C:\\Users", null);
        StartCoroutine(ShowLoadDialogCoroutine());
    }
    IEnumerator ShowLoadDialogCoroutine()
    {
        fetchModelDataCountFromFireBase.loadingScreen.enabled = true;
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, true, null, null, "Select Files", "Load");

        Debug.Log(FileBrowser.Success);

        if (FileBrowser.Success)
        {
            OnFilesSelected(FileBrowser.Result);
        }
        else
        {
            fetchModelDataCountFromFireBase.loadingScreen.enabled = false;
        }
    }
    public byte[] bytes;
    void OnFilesSelected(string[] filePaths)
    {
        for (int i = 0; i < filePaths.Length; i++)
            Debug.Log(filePaths[i]);

        string filePath = filePaths[0];

        bytes = FileBrowserHelpers.ReadBytesFromFile(filePath);
        fetchModelDataCountFromFireBase.customBytes = bytes;
        fetchModelDataCountFromFireBase.filename = FileBrowserHelpers.GetFilename(filePath);
        string destinationPath = Path.Combine(Application.persistentDataPath, FileBrowserHelpers.GetFilename(filePath));
        FileBrowserHelpers.CopyFile(filePath, destinationPath);
        fetchModelDataCountFromFireBase.UploadToFirebase();
        SaveData();
    }
    public string lastUpload, nameOfFile;
    public void SaveData()
    {
        var dummymap = new Map();
        dummymap.name = fetchModelDataCountFromFireBase.filename;
        dummymap.DateAndTime = DateTime.Now.ToString();
        fetchModelDataCountFromFireBase.maps.Add(dummymap);
        fetchModelDataCountFromFireBase.links.Add("https://firebasestorage.googleapis.com/v0/b/sample-3c5ec.appspot.com/o/" + dummymap.name + "?alt=media&token=2a9e8cc4-50cc-4ae2-ac51-824306612bdd");
        nameOfFile = fetchModelDataCountFromFireBase.filename;
        string[] vs = nameOfFile.Split(new string[] { "." }, StringSplitOptions.None);
        nameOfFile = vs[0];
        print(nameOfFile);
        lastUpload = JsonUtility.ToJson(dummymap);

        FirebaseDatabase database = FirebaseDatabase.DefaultInstance;
        var file = database.RootReference;
        file.Child("Maps").Child(nameOfFile).SetRawJsonValueAsync(lastUpload);
    }
    public void LoadData()
    {
        fetchModelDataCountFromFireBase.maps.Clear();
        fetchModelDataCountFromFireBase.links.Clear();
        fetchModelDataCountFromFireBase.ModelCount = 0;
    }
}