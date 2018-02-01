using OpenCVForUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShapeDetection : MonoBehaviour {

    public RawImage rimg;
    Texture2D texture;
	// Use this for initialization
	void Start () {
        Mat img = Imgcodecs.imread("C:/Users/mv duc/Desktop/shape-detection/tagram.png");
        float rat = (float)img.width() / (float)img.height();
        rimg.GetComponent<AspectRatioFitter>().aspectRatio = rat;


        Mat gray = new Mat();
        Imgproc.cvtColor(img, gray, Imgproc.COLOR_BGR2GRAY);
        Mat blurred = new Mat();
        Imgproc.GaussianBlur(gray, blurred, new Size(5, 5), 0);
        Mat thresh = new Mat();
        Imgproc.threshold(blurred, thresh, 60, 255, Imgproc.THRESH_BINARY_INV);

        Imgcodecs.imwrite("C:/Users/mv duc/Desktop/shape-detection/a.png", thresh);

        List<MatOfPoint> ls_mop = new List<MatOfPoint>();
        
        

        Mat hierarchy = new Mat();
        Imgproc.findContours(thresh, ls_mop, hierarchy, Imgproc.RETR_EXTERNAL, Imgproc.CHAIN_APPROX_SIMPLE);
        Imgproc.drawContours(img, ls_mop, -1, new Scalar(200, 0, 200), 2, 1, hierarchy, 1000,new Point(0,0));
        Debug.Log(ls_mop.Count);
        for (int i = 0; i < ls_mop.Count; i++)
        {
            var mop = ls_mop[i];            
            Debug.Log(mop.size());
            Debug.Log(mop.toArray().Length);
            var arr = mop.toArray();
            for(int j=0;j<arr.Length;j++)
            {
                var p = arr[j];
                Debug.Log(p.ToString());
            }
        }

        texture = new Texture2D(img.width(), img.height(), TextureFormat.RGBA32, false);
        Imgproc.cvtColor(img, img, Imgproc.COLOR_RGB2BGR);
        Utils.matToTexture2D(img, texture);
        rimg.texture = texture;
        Imgcodecs.imwrite("C:/Users/mv duc/Desktop/shape-detection/img.png", img);
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
