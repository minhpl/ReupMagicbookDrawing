﻿using OpenCVForUnityExample;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using OpenCVForUnity;
using UnityEngine.UI;
using System;
using System.IO;
public class SnapImageSceneScripts : MonoBehaviour
{
    public GameObject goCam;
    private RawImage rawImgCam;
    private bool camAvailable;
    private WebCamTexture webcamTex;
    private WebCamDevice webCamDevice;
    private bool isFronFacing = false;
    private int requestWidth = 1920;
    private int requestHeight = 1920;
    private WebCamTextureToMatHelper wcHelper;
    Mat snapMat;
    public Button btnSnap;
    public Button btnCancel;
    public Button btnContinue;
    public IDisposable cancelCoroutineBackAndroid;

    private Texture2D snapImage;
    IDisposable cancelMCoroutineSnap;
    Texture2D texRgbaMat;

    private void Awake()
    {
        GFs.LoadData();
        if (MakePersistentObject.Instance)
            MakePersistentObject.Instance.gameObject.SetActive(false);

        cancelCoroutineBackAndroid = GFs.BackButtonAndroidGoPreScene();
    }

    void Start()
    {
        btnSnap.onClick.AddListener(() =>
        {
            btnCancel.gameObject.SetActive(true);
            btnContinue.gameObject.SetActive(true);
        });

        btnCancel.onClick.AddListener(() =>
        {
            btnContinue.gameObject.SetActive(false);
            btnCancel.gameObject.SetActive(false);
        });

        rawImgCam = goCam.GetComponent<RawImage>();
        ShowCam();            
    }

        
    void ResizeAndFlipWebcamTexture()
    {
        webcamTex = wcHelper.GetWebCamTexture();
        webcamTex.Play();

        var widthCam = webcamTex.width;
        var heightCam = webcamTex.height;
        var ratioWH = widthCam / (float)heightCam;
        var ratioHW = heightCam / (float)widthCam;
        var widthDisplay = rawImgCam.rectTransform.rect.width;
        var heightDisplay = rawImgCam.rectTransform.rect.height;
        var ratioDisplay = widthDisplay / (float)heightDisplay;

        int orient = webcamTex.videoRotationAngle;

        rawImgCam.rectTransform.localEulerAngles = new Vector3(0, 0, -orient);
        if (orient == 90 || orient == 270)
        {
            if (ratioHW < ratioDisplay)
            {
                var newHeight = widthDisplay;
                var newWidth = newHeight * ratioWH;

                var heightDelta = newHeight - heightDisplay;
                var widthDelta = newWidth - widthDisplay;
                rawImgCam.rectTransform.sizeDelta = new Vector2(widthDelta, heightDelta);
            }
            else
            {
                var newWidth = heightDisplay;
                var newHeight = newWidth * ratioHW;

                var heightDelta = newHeight - heightDisplay;
                var widthDelta = newWidth - widthDisplay;
                rawImgCam.rectTransform.sizeDelta = new Vector2(widthDelta, heightDelta);
            }
        }
        else
        {
            if (ratioWH < ratioDisplay)
            {
                var newWidth = widthDisplay;
                var newHeight = newWidth * ratioHW;

                rawImgCam.rectTransform.sizeDelta = new Vector2(newWidth - widthDisplay, newHeight - heightDisplay);
            }
            else
            {
                var newHeight = heightDisplay;
                var newWidth = newHeight * ratioWH;

                rawImgCam.rectTransform.sizeDelta = new Vector2(newWidth - widthDisplay, newHeight - heightDisplay);
            }
        }

        if (isFronFacing && (orient == 90 || orient == 270))
        {
            rawImgCam.rectTransform.localScale = new Vector3(1, -1, 1);
        }

        if (WebCamTexture.devices.Length == 1 && WebCamTexture.devices[0].isFrontFacing == true && orient == 0)
        {
            rawImgCam.rectTransform.localScale = new Vector3(-1, 1, 1);
        }

        if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
            var localScale = rawImgCam.rectTransform.localScale;
            localScale.y = -localScale.y;
            rawImgCam.rectTransform.localScale = localScale;
        }

    }

    void ShowCam()
    {
        if (wcHelper == null)
            wcHelper = goCam.GetComponent<WebCamTextureToMatHelper>();
        wcHelper.onInitialized.RemoveAllListeners();
        wcHelper.onInitialized.AddListener(() =>
        {
            ResizeAndFlipWebcamTexture();
            rawImgCam.texture = webcamTex;
            camAvailable = true;
        });

        wcHelper.Initialize(null, requestWidth, requestHeight, isFronFacing);
    }

    private void OnDisable()
    {
        if (webcamTex)
            webcamTex.Stop();
        webcamTex = null;
        if (cancelMCoroutineSnap != null)
            cancelMCoroutineSnap.Dispose();
        if (cancelCoroutineBackAndroid != null)
            cancelCoroutineBackAndroid.Dispose();
    }

    public void OnChangeCameraButton()
    {
        wcHelper.Stop();
        wcHelper = null;
        rawImgCam.rectTransform.sizeDelta = new Vector2(0, 0);
        rawImgCam.transform.localScale = new Vector3(1, 1, 1);
        rawImgCam.rectTransform.localEulerAngles = new Vector3(0, 0, 0);
        isFronFacing = !isFronFacing;
        ShowCam();
        btnContinue.gameObject.SetActive(false);
        btnCancel.gameObject.SetActive(false);
    }

    public void OnSnapBtnClicked()
    {        
        cancelMCoroutineSnap = Observable.FromMicroCoroutine(Snap).Subscribe();
    }

    IEnumerator Snap()
    {
            webcamTex.requestedWidth = 1920;
            webcamTex.requestedHeight = 1920;
            webcamTex.Play();
            rawImgCam.texture = webcamTex;
            int countNonZero = 0;
            int numBlackFrame = 0;
            int numberFrameSkip = 0;

            snapMat = new Mat(webcamTex.height, webcamTex.width, CvType.CV_8UC4);
            Mat singleChannel = new Mat();
            Color32[] buffers = new Color32[webcamTex.width * webcamTex.height];
            rawImgCam.texture = null;

            while (countNonZero == 0 || numberFrameSkip < 1)
            {
                Utils.webCamTextureToMat(webcamTex, snapMat, buffers);
                Core.extractChannel(snapMat, singleChannel, 1);
                countNonZero = Core.countNonZero(singleChannel);
                if (countNonZero > 0)
                {
                    numberFrameSkip++;
                }
                else
                {
                    numBlackFrame++;
                }

                //Utilities.Log("Count non zero of mat is {0}", countNonZero);
                yield return null;
            }

            singleChannel.Dispose();
            snapMat.Dispose();

            Mat rgbaMat = wcHelper.GetMat();

            rawImgCam.rectTransform.localScale = new Vector3(1, 1, 1);
            rawImgCam.rectTransform.localEulerAngles = new Vector3(0, 0, 0);
            rawImgCam.rectTransform.sizeDelta = new Vector2(0, 0);

            var widthCam = rgbaMat.width();
            var heightCam = rgbaMat.height();
            var ratCamWH = widthCam / (float)heightCam;            
            var wDis = rawImgCam.rectTransform.rect.width;
            var hDis = rawImgCam.rectTransform.rect.height;
            var ratDis = wDis / (float)hDis;

            var newWidth = 0f;
            var newHeight = 0f;

            if (ratCamWH < ratDis)
            {
                newWidth = widthCam;
                newHeight = newWidth / ratDis;
            }
            else
            {
                newHeight = heightCam;
                newWidth = newHeight * ratDis;
            }


            var deltaWidthMat = (int)(widthCam - newWidth);
            var deltaHeightMat = (int)(heightCam - newHeight);
            var matDisW = widthCam - deltaWidthMat;
            var matDisH = heightCam - deltaHeightMat;

            var rect = new OpenCVForUnity.Rect(deltaWidthMat / 2, deltaHeightMat / 2, matDisW, matDisH);
            snapMat = rgbaMat.submat(rect);

            Destroy(texRgbaMat);
            texRgbaMat = new Texture2D(snapMat.width(), snapMat.height(), TextureFormat.RGBA32, false);
            Utils.matToTexture2D(snapMat, texRgbaMat);
            rawImgCam.texture = texRgbaMat;  
    }

    public void OnContinueBtnClicked()
    {
        string dirPathSnapImage = GFs.getSnapImageDirPath();
        var dateTimeNow = DateTime.Now.ToString(Utilities.customFmts);
        var MPModelPath = dirPathSnapImage + dateTimeNow + ".png";
        if (snapMat != null)
        {
            DrawingScripts.image = snapMat;
            Texture2D texture = new Texture2D(snapMat.width(), snapMat.height(), TextureFormat.RGBA32, false);
            Utils.matToTexture2D(snapMat, texture);
            DrawingScripts.texModel = texture;
            DrawingScripts.drawMode = DrawingScripts.DRAWMODE.DRAW_IMAGE;                        
            File.WriteAllBytes(MPModelPath, texture.EncodeToPNG());
            try
            {
                NativeGallery.SaveToGallery(texture, "MagicBook Vẽ Hình", dateTimeNow + ".png");
            }
            catch (Exception e)
            {
                Utilities.Log("Error Save Gallery: {0}", e.ToString());
            }
            int nh = (int)(GVs.lwidthThumb * ((float)snapMat.height() / (float)snapMat.width()));
            Mat t = new Mat();
            Imgproc.resize(snapMat, t, new Size(GVs.lwidthThumb, nh));
            Texture2D textThumb = new Texture2D(GVs.lwidthThumb, nh, TextureFormat.RGBA32, false);
            Utils.matToTexture2D(t, textThumb);

            var hisP = GFs.getHistoryPath();
            var thumbP = hisP+ dateTimeNow + ".png";
            File.WriteAllBytes(thumbP, textThumb.EncodeToPNG());
            Destroy(textThumb);
            t.Dispose();

            HistoryNGUIScripts.AddHistoryItem(new HistoryModel(MPModelPath, thumbP, HistoryModel.IMAGETYPE.SNAP));
            GVs.SCENE_MANAGER.loadDrawingScene();
        }

    }

    public void OnCancelBtnClicked()
    {
        rawImgCam.rectTransform.sizeDelta = new Vector2(0, 0);
        rawImgCam.transform.localScale = new Vector3(1, 1, 1);
        rawImgCam.rectTransform.localEulerAngles = new Vector3(0, 0, 0);
        ResizeAndFlipWebcamTexture();
        rawImgCam.texture = webcamTex;
    }



    void rotateMat(Mat src, Mat des, float angleDegree)
    {
        Point center = new Point(src.cols() / 2f, src.rows() / 2f);
        Mat rot = Imgproc.getRotationMatrix2D(center, angleDegree, 1);
        OpenCVForUnity.Rect bbox = new RotatedRect(center, src.size(), angleDegree).boundingRect();

        rot.put(0, 2, rot.get(0, 2)[0] + bbox.width / 2f - center.x);
        rot.put(1, 2, rot.get(1, 2)[0] + bbox.height / 2f - center.y);

        Mat dst = new Mat();
        Imgproc.warpAffine(src, des, rot, bbox.size());
    }

}
