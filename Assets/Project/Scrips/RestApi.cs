using Proyecto26;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class RestApi : MonoBehaviour
{
    public Item[] listNiveauArray;
    public string res;

    void OnEnable()
    {
        RestClient.Get("https://firebasestorage.googleapis.com/v0/b/sample-3c5ec.appspot.com/o").Then(responce =>
        {
            res = responce.Text;
            listNiveauArray = JsonHelper.FromJsonString<Item>(responce.Text);
            print(listNiveauArray[0].name);
        });
    }

}
[Serializable]
public class Item
{
    public string name;
    public string bucket;
}
