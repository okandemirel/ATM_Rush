#if (UNITY_EDITOR) 
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using UnityEngine.Networking;
using System.Text;
using UnityEditor;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
using UnityEditor.Experimental.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif
using System.Collections;


namespace AiKodex
{
    public class CanvasController : MonoBehaviour
    {
        public InputField prompt;
        public InputField negativePrompt;
        public Slider inferenceSteps;
        public Slider cfgScale;
        public Button generate;
        public Button clear;
        public Button save;
        public RawImage result;
        public Text seedUI;
        public Text log;
        string seed = "1";
        string sDb64FromServer;
        RenderTexture outputRenderTexture;
        Texture2D outputTexture;
        Texture defaultImage;
        bool action;
        public RawImage spinner;

        void Start()
        {
            Button gen = generate.GetComponent<Button>();
            gen.onClick.AddListener(Generate);

            Button clr = clear.GetComponent<Button>();
            defaultImage = result.texture;
            clr.onClick.AddListener(Clear);

            Button saveImg = save.GetComponent<Button>();
            save.onClick.AddListener(Save);            

            spinner.enabled = false;
        }

        void Generate()
        {
            seed = UnityEngine.Random.Range(100000, 999999).ToString();
            seedUI.text = "Seed: " + seed;
            action = false;
            if (prompt.text != null || prompt.text != "")
            {
                StartCoroutine(Post(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String("aHR0cDovLzM0LjIyNS4xNTkuOTo1MDAwL2RhdGE=")), "{\"engine\":\"" + $"aiFyXL" + "\",\"data\":\"" + $"{prompt.text}" + "\",\"neg\":\"" + $"{negativePrompt}" + "\",\"width\":\"" + "512" + "\",\"height\":\"" + "512" + "\",\"steps\":\"" + $"{inferenceSteps.value}" + "\",\"cfgScale\":\"" + $"{cfgScale.value}" + "\",\"sampler\":\"" + "k_euler_a" + "\",\"seed\":\"" + $"{seed}" + "\",\"initImg\":\"" + "\",\"img2imgStrength\":\"" + "\",\"mask\":\"" + "" + "\"}"));
                StartCoroutine(Timer());
            }
            else
            {
                log.text = "Log: Please enter a prompt";
                log.color = Color.yellow;
            }
        }
        void Clear()
        {
            result.texture = defaultImage;
        }
        void Save()
        {
            if (result.texture != defaultImage)
            {
                File.WriteAllBytes($"Assets/Aify/{seed}.png", outputTexture.EncodeToPNG());
                log.text = $"Log: Saved in Assets/Aify by the name {seed}";
                log.color = Color.green;
                AssetDatabase.Refresh();
            }
            else
            {
                log.text = "Log: Please Generate an Image before Saving";
                log.color = Color.yellow;
            }
        }
        public void CopyPrompt(string s)
        {
            prompt.text = s;
        }
        IEnumerator Post(string url, string bodyJsonString)
        {
            var request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
            request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();
            while (!request.isDone)
            {
                yield return new WaitForEndOfFrame();
            }
            if (request.responseCode.ToString() != "200")
            {
                action = true;
                log.text = "Log: There was an error in generating the image. Please check the documentation for troubleshooting";
                log.color = Color.red;
            }
            else
            {
                action = true;
                log.text = "Log: Status OK";
                log.color = Color.green;
                sDb64FromServer = request.downloadHandler.text;

                sDb64FromServer = sDb64FromServer.Remove(0, 9);
                sDb64FromServer = sDb64FromServer.Remove(sDb64FromServer.Length - 3);
                byte[] imageBytes = System.Convert.FromBase64String(sDb64FromServer);
                outputRenderTexture = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32);
                outputRenderTexture.Create();
                outputTexture = new Texture2D(outputRenderTexture.width, outputRenderTexture.height);
                outputTexture.LoadImage(imageBytes);
                outputTexture.Apply();
                result.texture = outputTexture;

            }
        }
        IEnumerator Timer()
        {
            generate.interactable = false;
            bool temp = action;
            while (temp == action)
            {
                spinner.enabled = true;
                spinner.GetComponent<RectTransform>().localEulerAngles = new Vector3(0, 0, -Time.time * 360);
                yield return null;
            }
            spinner.enabled = false;
            generate.interactable = true;
        }

    }

}
#endif
