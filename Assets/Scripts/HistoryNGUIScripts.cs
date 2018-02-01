using Newtonsoft.Json;
using Newtonsoft.Json.Utilities;
using OpenCVForUnity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class HistoryModel
{
    public enum IMAGETYPE { SNAP, MODEL,SPECIAL };
    public enum SPECIAL { SATAN, CLOWN};
    public string filePath;
    public string thumbPath;
    public IMAGETYPE imgType;
    public SPECIAL specMode;


    public HistoryModel(string filePath, string thumbPath, IMAGETYPE imgType = IMAGETYPE.SNAP)
    {
        this.filePath = filePath;
        this.imgType = imgType;
        this.thumbPath = thumbPath;
    }
}

public class HistoryNGUIScripts : MonoBehaviour
{
    public static LinkedList<HistoryModel> history = null;
    private const string KEY = "history_";
    public GameObject item;
    public UIGrid uiGrid;
    public GameObject scrollView;
    private IDisposable cancelCoroutineBackBtnAndroid;
    private IDisposable cancelLoad;
    public Button btnBack;

    public GameObject satan;
    public GameObject clown;
    public GameObject scroll;
    private void Awake()
    {
        GFs.LoadData();
        if (Application.platform == RuntimePlatform.Android)
        {
            cancelCoroutineBackBtnAndroid = Observable.EveryUpdate().Where(_ => Input.GetKeyDown(KeyCode.Escape) == true).Subscribe(_ =>
            {
                GVs.SCENE_MANAGER.loadLibraryNGUIScene();
            });
        }       

        btnBack.onClick = new Button.ButtonClickedEvent();
        btnBack.onClick.AddListener(() =>
        {
            GVs.SCENE_MANAGER.loadLibraryNGUIScene();
        });
    }

    void Start()
    {
        //if (history == null)
        {
            var user_id = GVs.CURRENT_USER_MODEL.id;

            var json = PlayerPrefs.GetString(getUserHistoryKey());
            history = JsonConvert.DeserializeObject<LinkedList<HistoryModel>>(json);
        }
        cancelLoad = Observable.FromMicroCoroutine(load).Subscribe();
    }

    static private string getUserHistoryKey()
    {
        var user_id = GVs.CURRENT_USER_MODEL.id;
        return KEY + user_id.ToString();
    }


    IEnumerator load()
    {
        yield return null;
        if (history == null)
        {
            yield break;
        }

        int i = -1;
        for (var a = history.First; a != null; a = a.Next)
        {
            i++;
            var hModel = a.Value;
            GameObject cloneItem = null;

            if (hModel.imgType == HistoryModel.IMAGETYPE.SNAP || hModel.imgType == HistoryModel.IMAGETYPE.MODEL)
            {
                try
                {
                    cloneItem = Instantiate(item);
                    cloneItem.transform.parent = item.transform.parent;
                    cloneItem.transform.localScale = item.transform.localScale;
                    var thumbPath = hModel.thumbPath;
                    var filePath = hModel.filePath;
                    string loadPath = hModel.filePath;
                    if (hModel.imgType == HistoryModel.IMAGETYPE.SNAP)
                    {
                        loadPath = filePath;
                    }
                    else
                    {
                        loadPath = filePath;
                    }
                    Texture2D texture = GFs.LoadPNGFromPath(loadPath);
                    Mat image = new Mat(texture.height, texture.width, CvType.CV_8UC4);
                    Utils.texture2DToMat(texture, image);
                    var rimgGameObject = cloneItem.transform.Find("icon");
                    rimgGameObject.GetComponent<UITexture>().mainTexture = texture;                    
                    cloneItem.SetActive(true);
                    cloneItem.GetComponent<UIButton>().onClick.Add(new EventDelegate(() =>
                    {
                        if (hModel.imgType == HistoryModel.IMAGETYPE.MODEL)
                        {
                            DrawingScripts.drawMode = DrawingScripts.DRAWMODE.DRAW_MODEL;
                            DrawingScripts.imgModelPath = filePath;
                            AddHistoryItem(new HistoryModel(filePath, thumbPath, HistoryModel.IMAGETYPE.MODEL));
                        }
                        else
                        {
                            DrawingScripts.drawMode = DrawingScripts.DRAWMODE.DRAW_IMAGE;
                            DrawingScripts.image = image;
                            DrawingScripts.texModel = texture;
                            AddHistoryItem(new HistoryModel(filePath, thumbPath, HistoryModel.IMAGETYPE.MODEL));
                        }

                        GVs.SCENE_MANAGER.loadDrawingScene();
                    }));
                }
                catch (System.Exception e)
                {
                    
                }
            }
            else if (hModel.imgType == HistoryModel.IMAGETYPE.SPECIAL)
            {                
                if (hModel.specMode == HistoryModel.SPECIAL.SATAN)
                {                    
                    cloneItem = Instantiate(satan);
                    cloneItem.GetComponent<UIButton>().onClick.Add(new EventDelegate(() =>
                    {
                        DrawingScripts.drawMode = DrawingScripts.DRAWMODE.DRAW_SPECIAL;
                        DrawingScripts.spineMode = DrawingScripts.SPINE.SATAN;
                        HistoryNGUIScripts.AddHistoryItem(new HistoryModel("", "", HistoryModel.IMAGETYPE.SPECIAL) { specMode = HistoryModel.SPECIAL.SATAN });
                        GVs.SCENE_MANAGER.loadDrawingScene();
                    }));
                }
                else if (hModel.specMode == HistoryModel.SPECIAL.CLOWN)
                { 
                    cloneItem = Instantiate(clown);
                    cloneItem.GetComponent<UIButton>().onClick.Add(new EventDelegate(() =>
                    {       
                        DrawingScripts.drawMode = DrawingScripts.DRAWMODE.DRAW_SPECIAL;
                        DrawingScripts.spineMode = DrawingScripts.SPINE.CLOWN;
                        HistoryNGUIScripts.AddHistoryItem(new HistoryModel("", "", HistoryModel.IMAGETYPE.SPECIAL) { specMode = HistoryModel.SPECIAL.CLOWN });
                        GVs.SCENE_MANAGER.loadDrawingScene();
                    }));
                }
                cloneItem.transform.parent = scroll.transform;
                cloneItem.transform.SetSiblingIndex(i + 1);
                cloneItem.transform.localScale = Vector3.one;
            }


            UITexture uitex = cloneItem.GetComponent<UITexture>();
            var c = uitex.color;
            c.a = 0;
            uitex.color = c;
            LeanTween.value(0, 1, 0.85f).setOnUpdate((float v) =>
            {
                c.a = v;
                uitex.color = c;
            }).setDelay(0.035f * i + 0.5f);
            cloneItem.SetActive(true);
        }

        uiGrid.Reposition();
        scrollView.GetComponent<UIGrid>().Reposition();
        scrollView.GetComponent<UIScrollView>().ResetPosition();
        var p = scrollView.GetComponent<UIPanel>().transform.localPosition;
        var stadR1 = 0.5625f;  //16:9 -> 1 nua
        var stadR2 = 0.75f;  // 3:4  -> 0
        var ScrR = Screen.width / (float)(Screen.height);
        p.y += ((ScrR - stadR1) / (stadR2 - stadR1)) * (scrollView.GetComponent<UIPanel>().clipSoftness.y / 2);
        scrollView.GetComponent<UIPanel>().transform.localPosition = p;


        Destroy(item);
    }

    public void OnDisable()
    {
        LeanTween.cancelAll();
        if (cancelCoroutineBackBtnAndroid != null)
            cancelCoroutineBackBtnAndroid.Dispose();
        if (cancelLoad != null)
            cancelLoad.Dispose();        
    }

    public static void AddHistoryItem(HistoryModel historyModel)
    {
        const int MAXHISTORY = 30;
        if (history == null)
        {
            var jsonLoad = PlayerPrefs.GetString(getUserHistoryKey());
            if (!String.IsNullOrEmpty(jsonLoad))
                history = JsonConvert.DeserializeObject<LinkedList<HistoryModel>>(jsonLoad);
            if (history == null)
                history = new LinkedList<HistoryModel>();
        }
        for (var h = history.First; h != null;) 
        {
            var h_ = h.Value;

            if ((h_.filePath == historyModel.filePath) && (h_.specMode == historyModel.specMode))
            {
                var t = h.Next;
                history.Remove(h);
                h = t;
            }
            else
                h = h.Next;
        }
        history.AddFirst(historyModel);
        while (history.Count > MAXHISTORY)
        {

            var last = history.Last.Value;
            if(last.imgType == HistoryModel.IMAGETYPE.SNAP)            
                File.Delete(last.thumbPath);
            history.RemoveLast();
        }
            
        var jsonSave = JsonConvert.SerializeObject(history);
        PlayerPrefs.SetString(getUserHistoryKey(), jsonSave);
        PlayerPrefs.Save();
    }
}
