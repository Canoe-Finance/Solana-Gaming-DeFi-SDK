using LitJson;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Canon
{
    public class JupiterAdapter : MonoBehaviour
    {
        public static JupiterAdapter Instance;
        private void Awake()
        {
            Instance = this;
        }
        void Start()
        {
            //#region test code
            //string testDataUrl = "https://quote-api.jup.ag/v1/quote?inputMint=So11111111111111111111111111111111111111112&outputMint=EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v&amount=100000000&slippage=0.5&feeBps=4";
            //GetRouteData(testDataUrl);
            //#endregion
        }
        /// <summary>
        /// try to get all the routes
        /// </summary>
        /// <param name="pathUrl">data url</param>
        public void GetRouteData(string pathUrl)
        {
            StartCoroutine(GetData(pathUrl));
        }

        IEnumerator GetData(string pathUrl)
        {
            UnityWebRequest unityWebRequest = UnityWebRequest.Get(pathUrl);
            yield return unityWebRequest.SendWebRequest();
            if (unityWebRequest.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log(" Failed to communicate with the server");
                yield return null;
            }
            string data = unityWebRequest.downloadHandler.text;
            Debug.Log(data);
            JsonData jData = JsonMapper.ToObject(data);
            //choose the first
            Debug.Log((string)jData["data"][0].ToJson());

        }
    }
}
