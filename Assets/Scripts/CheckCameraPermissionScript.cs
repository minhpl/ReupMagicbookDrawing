using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckCameraPermissionScript : MonoBehaviour
{

    public UIPlayTween[] uiPlayTweens;
    // Use this for initialization
    IEnumerator Start()
    {
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        if (Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
        }
        else
        {
        }
    }

    IEnumerator Check()
    {
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        if (Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            GFs.PlayTweens(uiPlayTweens);
        }
    }

}
