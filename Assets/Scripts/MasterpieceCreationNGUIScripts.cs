using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class MasterpieceCreationNGUIScripts : MonoBehaviour
{
    private string dirPathMP;
    public GameObject item;
    public GameObject canvas;
    public GameObject UIRoot;
    public UIGrid uiGrid;
    public GameObject scrollView;
    private IDisposable cancelCoroutineBackButtonAndroid;
    private IDisposable cancelMCoroutineLoadMasterpiece;
    private void Awake()
    {
        GFs.LoadData();
        cancelCoroutineBackButtonAndroid = GFs.BackButtonAndroidGoHome();
    }

    void Start()
    {
        cancelMCoroutineLoadMasterpiece = Observable.FromMicroCoroutine(LoadMasterpieceDrawing).Subscribe();
        var canvasRect = canvas.GetComponent<RectTransform>().rect;
        var canvasRat = (float)canvasRect.width / (float)canvasRect.height;
        UIPanel uiPanel = item.GetComponent<UIPanel>();
        uiPanel.clipping = UIDrawCall.Clipping.SoftClip;
        var width = uiPanel.width;
        var newheight = width / canvasRat;
        uiPanel.SetRect(0, 0, width, newheight);
        BoxCollider boxCollider = item.GetComponent<BoxCollider>();
        boxCollider.size = new Vector3(width, newheight, 0);
        boxCollider.center = Vector3.zero;
        var padding = 50;
        uiGrid.cellHeight = newheight + padding;
    }

    private void OnDisable()
    {
        LeanTween.cancelAll();
        if (cancelCoroutineBackButtonAndroid != null)
            cancelCoroutineBackButtonAndroid.Dispose();
        if (cancelMCoroutineLoadMasterpiece != null)
            cancelMCoroutineLoadMasterpiece.Dispose();
    }

    IEnumerator LoadMasterpieceDrawing()
    {
        yield return null;
        dirPathMP = GFs.getMasterpieceDirPath();
        var imagefiles = Directory.GetFiles(dirPathMP, "*.*", SearchOption.TopDirectoryOnly)
            .Where(s => s.EndsWith(".png") && !s.EndsWith("thumb.png")).ToList();

        for(int i=0;i<imagefiles.Count();i++)
        {         
            yield return null;
            var f = imagefiles[i];            
            GameObject go = Instantiate(item) as GameObject;
            go.transform.SetParent(item.transform.parent.transform);
            go.transform.localScale = item.transform.localScale;                        
            
            GameObject icon = go.transform.Find("icon").gameObject;

            var fileNameWithouExtension = Path.GetFileNameWithoutExtension(f);
            var thumb = dirPathMP + fileNameWithouExtension + "_thumb.png";
            Texture2D textureThumb = null;

            var c = icon.GetComponent<UITexture>().color;
            c.a = 0;
            icon.GetComponent<UITexture>().color = c;
            LeanTween.value(0, 1, 0.8f).setOnUpdate((float v) =>
              {
                  c.a = v;
                  icon.GetComponent<UITexture>().color = c;
              }).setDelay(0.035f * i + 0.5f);


            //icon.GetComponent<TweenAlpha>().delay = 0.035f * i + 0.5f;
            //icon.GetComponent<TweenAlpha>().enabled = true;

            go.SetActive(true);

            bool haveThumb = false;
            if (File.Exists(thumb))
            {
                //Utilities.Log("HAVE THUMB, THUMB PATH IS {0}", thumb);
                textureThumb = GFs.LoadPNGFromPath(thumb);
                haveThumb = true;
            }                
            else
            {
                haveThumb = false;
                textureThumb = GFs.LoadPNGFromPath(f);
            }

            string videoPath = dirPathMP + fileNameWithouExtension + ".avi";
            if (!File.Exists(videoPath))
            {
                videoPath = null;
            }
            var uiTexture = icon.GetComponent<UITexture>();
            
            go.GetComponent<UIButton>().onClick.Add(new EventDelegate(() =>
            {
                Texture2D texture = null;

                if (!haveThumb)
                {
                    texture = textureThumb;
                }
                else
                {
                    //Utilities.Log("IMAGE Path is  {0}", f);
                    texture = GFs.LoadPNGFromPath(f);
                }

                string vidAnim = null;
                if (f.EndsWith("_anim.png"))
                {
                    var length = f.Length - 9;
                    var pathNoExt = f.Substring(0, length);
                    vidAnim = pathNoExt + ".mp4";
                    ResultScripts.mode = ResultScripts.MODE.ANIM;
                    ResultScripts.texture = texture;
                    var datetime = DateTime.ParseExact(fileNameWithouExtension.Substring(0, fileNameWithouExtension.Length - 5), Utilities.customFmts, new CultureInfo(0x042A));
                    var datemonthyear = string.Format("{0}", datetime.Date.ToString("d-M-yyyy"));
                    //Debug.Log(datemonthyear);
                    ResultScripts.title = datemonthyear;
                }
                else
                {
                    ResultScripts.texture = texture;
                    ResultScripts.videoPath = videoPath;
                    ResultScripts.mode = ResultScripts.MODE.REWATCH_RESULT;
                    var datetime = DateTime.ParseExact(fileNameWithouExtension, Utilities.customFmts, new CultureInfo(0x042A));
                    var datemonthyear = string.Format("{0}", datetime.Date.ToString("d-M-yyyy"));
                    //Debug.Log(datemonthyear);
                    ResultScripts.title = datemonthyear;
                }
                ResultScripts.imagePath = f;
                ResultScripts.animPath = vidAnim;
                GVs.SCENE_MANAGER.loadResultScene();
            }));

            uiTexture.mainTexture = textureThumb;
            uiGrid.Reposition();
        }

        scrollView.GetComponent<UIGrid>().Reposition();
        scrollView.GetComponent<UIScrollView>().ResetPosition();
        var p = scrollView.GetComponent<UIPanel>().transform.localPosition;
        var stadR1 = 0.5625f;  //16:9 -> 1 nua
        var stadR2 = 0.75f;  // 3:4  -> 0
        var ScrR = Screen.width / (float)(Screen.height);
        p.y += ((ScrR - stadR1) / (stadR2 - stadR1)) * (scrollView.GetComponent<UIPanel>().clipSoftness.y / 2);
        scrollView.GetComponent<UIPanel>().transform.localPosition = p;
        
        Destroy(item);

        var videoFiles = Directory.GetFiles(dirPathMP, "*.*", SearchOption.TopDirectoryOnly)
            .Where(s => s.EndsWith(".avi"));
        foreach (var videoPath in videoFiles)
        {
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(videoPath);
            var imageCorresponding = dirPathMP + fileNameWithoutExtension + ".png";
            if (!File.Exists(imageCorresponding))
            {
                File.Delete(videoPath);
            }
        }

        var thumbs = Directory.GetFiles(dirPathMP, "*.*", SearchOption.TopDirectoryOnly)
            .Where(s => s.EndsWith("_thumb.png"));
        foreach (var thumb in thumbs)
        {
            
            var imgName = Path.GetFileNameWithoutExtension(thumb);
            imgName = imgName.Substring(0, imgName.Length - 6);
            var imageCorresponding = dirPathMP + imgName + ".png";
            //Debug.Log("Thumb is " + thumb);
            //Debug.Log("IMG CORESPODING: " + imageCorresponding);
            if (!File.Exists(imageCorresponding))
            {
                File.Delete(thumb);
            }
        }

    }
}
