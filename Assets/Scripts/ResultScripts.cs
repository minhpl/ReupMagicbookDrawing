using OpenCVForUnity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Facebook.Unity;

public class ResultScripts : MonoBehaviour
{
    public static string videoPath = null;
    public static string imagePath = null;
    public static string animPath = null;
    public static Texture2D texture;
    public static string title;

    public RawImage rimg;
    public GameObject panel;
    public Button btPlay;
    public Button btStop;
    public Canvas canvas;
    public Button BackButton;
    public UnityEngine.UI.Text tit;
    public RawImage rimgTitle;
    public MoviePlayer moviePlayer;
    public Button btnShareFB;
    public Button btnDelete;
    public GameObject Pnl_Popup;
    public Button OKButton;
    public Button logoutBtn;
    public GameObject overlayer_comfirmDelete;
    public GameObject panel_comfirmDelete;
    public Button btn_okDelete;
    public Button btn_cancelDelete;
    public AudioSource audioSource;
    public Button backBtn;
    public GameObject pnlShareFB;
    public GameObject player;

    private Texture2D texVideo;    
    private AspectRatioFitter rawImageAspect;
    public enum MODE { FISRT_RESULT, REWATCH_RESULT, ANIM };
    public static MODE mode;
    private int FPS = 60;
    private float currentFPS = 0;
    private float ratioImage = 1;

    private IDisposable cancelCorountineBackButtonAndroid;
    LTDescr ltdescr_ScaleComfirmDeletePanel;
    LTDescr ltdescr_AlphaComfirmDeletePanel;
    private void Awake()
    {
        if (GVs.SOUND_SYSTEM == 1)
            audioSource.Play();


        backBtn.onClick.AddListener(() =>
        {
            Debug.Log("fuck all");
        });

        btnDelete.onClick.AddListener(() =>
        {
            overlayer_comfirmDelete.SetActive(true);
            ltdescr_ScaleComfirmDeletePanel = LeanTween.scale(panel_comfirmDelete, new Vector3(1f, 1f, 1f), GVs.DURATION_TWEEN_UNIFY)
            .setEase(LeanTweenType.easeOutElastic)
                .setRepeat(2).setLoopPingPong()
                .setOnComplete(() =>
                {
                    ltdescr_ScaleComfirmDeletePanel.pause();
                    ltdescr_ScaleComfirmDeletePanel.setEase(LeanTweenType.easeInQuart);
                }).setOnCompleteOnRepeat(true);

            ltdescr_AlphaComfirmDeletePanel = LeanTween.alpha(panel_comfirmDelete.GetComponent<RectTransform>(), 1, GVs.DURATION_TWEEN_UNIFY).setFrom(0)
            .setRepeat(2).setLoopPingPong().setEase(LeanTweenType.easeOutElastic)
            .setOnComplete(() =>
            {
                ltdescr_AlphaComfirmDeletePanel.pause();
            }).setOnCompleteOnRepeat(true);
        });

        btn_okDelete.onClick.AddListener(() =>
        {
            var a = LeanTween.sequence();
            a.append(ltdescr_ScaleComfirmDeletePanel.resume());
            a.append(() =>
            {
                overlayer_comfirmDelete.SetActive(false);
                moviePlayer.Unload();
                player.GetComponent<MoviePlayer>().Unload();
                File.Delete(imagePath);
                if (File.Exists(videoPath))
                {
                    File.Delete(videoPath);
                }
                GFs.BackToPreviousScene();
            });
            ltdescr_AlphaComfirmDeletePanel.resume();
        });

        btn_cancelDelete.onClick.AddListener(() =>
        {
            var a = LeanTween.sequence();
            a.append(ltdescr_ScaleComfirmDeletePanel.resume());
            a.append(() =>
            {
                overlayer_comfirmDelete.SetActive(false);
            });
            ltdescr_AlphaComfirmDeletePanel.resume();
        });

        OKButton.onClick.AddListener(() =>
        {
            Pnl_Popup.SetActive(false);
        });

        btPlay.onClick.AddListener(() =>
        {
            if (mode == MODE.FISRT_RESULT || mode == MODE.REWATCH_RESULT)
            {
                btStop.gameObject.SetActive(true);
                btPlay.gameObject.SetActive(false);
            }
        });


        btPlay.onClick.AddListener(() =>
        {
            if (mode == MODE.FISRT_RESULT || mode == MODE.REWATCH_RESULT)
            {
                rimg.texture = null;
                var isPlay = moviePlayer.play;
                if (isPlay == false)
                {
                    moviePlayer.videoFrame = 0;
                    moviePlayer.play = true;
                }
            }
            else if (mode == MODE.ANIM)
            {
                //Utilities.Log("Animation Path is {0}", animPath);
                if (Application.platform == RuntimePlatform.Android)
                {
                    Handheld.PlayFullScreenMovie(animPath);
                }
                else if (Application.platform == RuntimePlatform.IPhonePlayer)
                {
                    Utilities.Log("Is file exist ?? {0}", File.Exists(animPath));
                    Handheld.PlayFullScreenMovie("file://" + animPath);
                }
            }
        });


        btStop.GetComponent<Button>().onClick.AddListener(() =>
        {
            btStop.gameObject.SetActive(false);
            btPlay.gameObject.SetActive(true);
        });

        btStop.GetComponent<Button>().onClick.AddListener(() =>
        {
            moviePlayer.play = false;
            moviePlayer.videoFrame = 0;
            rimg.texture = texture;
        });

        btnShareFB.onClick.AddListener(() =>
        {
            if (!string.IsNullOrEmpty(videoPath))
            {
                ShareFacebook.ShareMODE = ShareFacebook.mode.SHARE_VIDEO;
                ShareFacebook.filePath = videoPath;
            }
            else if (!string.IsNullOrEmpty(animPath))
            {
                ShareFacebook.ShareMODE = ShareFacebook.mode.SHARE_VIDEO;
                ShareFacebook.filePath = animPath;
            }
            else
            {
                ShareFacebook.ShareMODE = ShareFacebook.mode.SHARE_IMAGE;
                ShareFacebook.filePath = imagePath;
            }
            var isVideoExist = File.Exists(videoPath);
            Debug.LogFormat("is video exist ?? {0}", isVideoExist);
            var shareFacebook = GetComponent<ShareFacebook>();
            
            if (!FB.IsInitialized)
            {
                FB.Init(shareFacebook.InitCallback, shareFacebook.OnHideUnity);
            }
            else
            {
                FB.ActivateApp();
                FB.Mobile.ShareDialogMode = ShareDialogMode.WEB;
            }

            if (FB.IsLoggedIn)
            {
                shareFacebook.onLoggedInSuccess();
                pnlShareFB.gameObject.SetActive(true);
            }
            else
            {
                shareFacebook.onlogin();
            }
        });

        logoutBtn.onClick.AddListener(() =>
        {            
            FB.LogOut();            
        });

        if (mode == MODE.REWATCH_RESULT || mode == MODE.ANIM)
        {
            tit.text = title;
            rimgTitle.gameObject.SetActive(false);
            tit.gameObject.SetActive(true);
        }

        if (mode == MODE.FISRT_RESULT)
        {
            btnDelete.gameObject.SetActive(false);
        }
        else
        {
            btnDelete.gameObject.SetActive(true);
        }

        moviePlayer.OnStop += MoviePlayer_OnStop;

        if (Application.platform == RuntimePlatform.Android)
        {
            cancelCorountineBackButtonAndroid = Observable.EveryUpdate().Where(_ => Input.GetKeyDown(KeyCode.Escape) == true)
                .Subscribe(_ =>
                {
                    if (mode == MODE.FISRT_RESULT)
                    {
                        if (GVs.TRACE_SCENE.Count > 3)
                        {
                            GVs.TRACE_SCENE.Pop();
                            GVs.TRACE_SCENE.Pop();
                            int i = GVs.TRACE_SCENE.Pop();
                            Debug.LogFormat("track scene have {0} elements", GVs.TRACE_SCENE.Count);
                            SceneManager.LoadScene(i);
                        }
                    }
                    else
                    {
                        GFs.BackToPreviousScene();
                    }
                });
        }
    }

    void Start()
    {
        if (!string.IsNullOrEmpty(videoPath))
        {
            var fileInfo = new FileInfo(videoPath);
            var bytes = fileInfo.Length;
            var kb = bytes >> 10;
            var mb = kb >> 10;
            Debug.LogFormat("File size is {0} bytes, {1} kb, {2} mb", bytes, kb, mb);
        }

        if (mode == MODE.FISRT_RESULT)
        {
            rimgTitle.gameObject.SetActive(true);
            tit.gameObject.SetActive(false);
            BackButton.onClick.RemoveAllListeners();
            BackButton.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
            BackButton.onClick.AddListener(() =>
            {
                if (GVs.TRACE_SCENE.Count > 3)
                {
                    GVs.TRACE_SCENE.Pop();
                    GVs.TRACE_SCENE.Pop();
                    int i = GVs.TRACE_SCENE.Pop();
                    Debug.LogFormat("track scene have {0} elements", GVs.TRACE_SCENE.Count);
                    SceneManager.LoadScene(i);
                }
            });
        }

        rawImageAspect = rimg.GetComponent<AspectRatioFitter>();
        var canvasRect = canvas.GetComponent<RectTransform>().rect;
        var canvasWidth = canvasRect.width;
        var ratioCanvas = (float)canvasRect.width / canvasRect.height;
        ratioImage = ratioCanvas;
        var panelAspect = panel.GetComponent<AspectRatioFitter>();
        panelAspect.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
        panelAspect.aspectRatio = ratioCanvas;

        if (texture != null)
        {
            rimg.texture = texture;
        }

        if (mode == MODE.FISRT_RESULT || mode == MODE.REWATCH_RESULT)
        {
            if (!string.IsNullOrEmpty(videoPath))
            {                
                btPlay.gameObject.SetActive(true);
                moviePlayer.Load(videoPath);
                moviePlayer.play = false;
                moviePlayer.loop = false;
            }
            else btPlay.gameObject.SetActive(false);
        }
        else if (mode == MODE.ANIM)
        {
            btPlay.gameObject.SetActive(true);
        }
    }


    private void OnDisable()
    {
        if (cancelCorountineBackButtonAndroid != null)
            cancelCorountineBackButtonAndroid.Dispose();
        videoPath = null;
        if (texture != null)
            Destroy(texture);
        if (texVideo != null)
            Destroy(texVideo);
        try
        {
            moviePlayer.play = false;
            moviePlayer.loop = false;
            moviePlayer.Unload();
        }
        catch (Exception e)
        {
            Debug.Log("Error" + e.ToString());
        }
        ShareFacebook.filePath = null;
    }

    private void MoviePlayer_OnStop(MoviePlayerBase caller)
    {
        rimg.texture = texture;        
        btStop.gameObject.SetActive(false);
        btPlay.gameObject.SetActive(true);
    }
}
