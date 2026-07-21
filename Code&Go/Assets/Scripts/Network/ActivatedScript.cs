using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System;
using Newtonsoft.Json.Linq;

// Comentado porque daba error de compilacion
//using UnityEditor.PackageManager.Requests;

/**
TODO Esta es la clase que se comunica con el servidor, la he generado en un gameObject a parte porque me ha dado muchisimos problemas para ejecutar si estaba inactivo, 
por lo que siempre la activo y luego la llamo, no se si es muy mala practica, pero es lo �nico que me ha funcionado.
*/
public class ActivatedScript : MonoBehaviour {

    //TODO Esto no se muy bien si estaria mejor en campos que configurara el usuario en el momento de la exportaci�n/Importaci�n

    //Standard local values
    private bool serverEnable;
    private string server; // = "http://localhost";
    private string port;   // = "4200";
    [SerializeField] private RectTransform communityPageButton;

    [SerializeField] private GameObject InfoImportPanel;

    [SerializeField] private Text _title;

    [SerializeField] private ReplyMessage replyMessage;

    private void Start() {
        //Tries to find the file "communityServer". If found parses its information to connect to the url.
        var FileName = "communityServer.conf";

        var filePath = System.IO.Path.Combine(Application.streamingAssetsPath + "/", FileName);

        string contents = "";

        //If the path is found, it add everything to "contents" and then parses it and uses this values to define the url.
        if (System.IO.File.Exists(filePath)) {
            contents = System.IO.File.ReadAllText(filePath);
        }
        if (!string.IsNullOrEmpty(contents)) {
            var serverConf = JObject.Parse(contents);
            serverEnable = bool.Parse(serverConf["serverEnable"].ToString());
            server = serverConf["serverIP"].ToString();
            port = serverConf["serverPort"].ToString();
        }
        if (!serverEnable) {
            communityPageButton.gameObject.SetActive(false);
        }
        //Si no existe el path utiliza el serve y port por defecto
        Debug.Log("Server IP = " + server + " - Port = " + port);
    }

    IEnumerator PostCourutine(string path, string json, Func<UnityWebRequest, int> onOK, Func<UnityWebRequest, int> onKO) {
        string url = server + ":" + port + "/" + path;

        var req = new UnityWebRequest(url, "POST");
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
        req.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
        req.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        if (GameManager.Instance.GetLogged())
            req.SetRequestHeader("Authorization", GameManager.Instance.GetToken());
          
        yield return req.SendWebRequest();
          
        if (req.result == UnityWebRequest.Result.ConnectionError) {
            replyMessage.Configure(MessageReplyID.ServerNotFound);
            showError("Error en post: " + req.error);
        }
        else {
            //Todo guay
            if(req.responseCode == 200) {
                onOK(req);
            }
            else {
                Debug.LogError("error en post: " + req.responseCode);
                onKO(req);
            }
        }
    }

    IEnumerator PutCourutine(string path, string json, Func<UnityWebRequest, int> onOK, Func<UnityWebRequest, int> onKO) {
        string url = server + ":" + port + "/" + path;

        var req = new UnityWebRequest(url, "PUT");
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
        req.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
        req.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        if (GameManager.Instance.GetLogged())
            req.SetRequestHeader("Authorization", GameManager.Instance.GetToken());

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.ConnectionError) {
            showError("Error en put: " + req.error);
            replyMessage.Configure(MessageReplyID.ServerNotFound);

        }
        else {
            if (req.responseCode == 200) {
                onOK(req);
            }
            else {
                onKO(req);
            }
        }
    }

    IEnumerator GetCourutine(string path, Func<UnityWebRequest, int> onOK, Func<UnityWebRequest, int> onKO) {
        string url = server + ":" + port + "/" + path;

        using (UnityWebRequest request = UnityWebRequest.Get(url)) {
            if(GameManager.Instance.GetLogged())
                request.SetRequestHeader("Authorization", GameManager.Instance.GetToken());

            yield return request.Send();

            if (request.isNetworkError) {
                showError("Error en get: " + request.error);
                replyMessage.Configure(MessageReplyID.ServerNotFound);

            }
            else {
                //Todo guay
                if (request.responseCode == 200) {
                    onOK(request);
                }
                else {
                    onKO(request);
                }
            }
        }
    }

    public void Post(string path, string json, Func<UnityWebRequest, int> onOK, Func<UnityWebRequest, int> onKO) {
        StartCoroutine(PostCourutine(path, json, onOK, onKO));
    }

    public void Put(string path, string json, Func<UnityWebRequest, int> onOK, Func<UnityWebRequest, int> onKO) {
        StartCoroutine(PutCourutine(path, json, onOK, onKO));
    }

    public void Get(string path, Func<UnityWebRequest, int> onOK, Func<UnityWebRequest, int> onKO) {
        StartCoroutine(GetCourutine(path, onOK, onKO));
    }

    public void Get(string path, Func<UnityWebRequest, int> onOK, Func<UnityWebRequest, int> onKO, int param) {
        Debug.Log("Get with param");
    }

    public void SetIp(string newip) {
        string[] serverport = newip.Split(':');
        server = serverport[0];
        port = serverport[1];
    }

    public void showError(string msg) {
        if (InfoImportPanel) {
            InfoImportPanel.SetActive(true);

            //To find `child2` which is the second index(1)
            GameObject iconError = InfoImportPanel.transform.GetChild(1).gameObject;
            iconError.SetActive(true);
            //To find `child3` which is the third index(2)
            GameObject iconSuccess = InfoImportPanel.transform.GetChild(2).gameObject;
            iconSuccess.SetActive(false);
            Debug.Log(msg);

            if(_title) _title.text = msg;
            //TODO AQUI LA IDEA ES QUE SE MUESTRE EN UNA VENTANA EMERGENTE GUAY
        }
    }

    public void showSuccess(string msg) {
        if (InfoImportPanel) {
            InfoImportPanel.SetActive(true);

            //To find `child2` which is the second index(1)
            GameObject iconError = InfoImportPanel.transform.GetChild(1).gameObject;
            iconError.SetActive(false);
            //To find `child3` which is the third index(2)
            GameObject iconSuccess = InfoImportPanel.transform.GetChild(2).gameObject;
            iconSuccess.SetActive(true);
            Debug.Log(msg);

            if (_title) _title.text = msg;
            //TODO AQUI LA IDEA ES QUE SE MUESTRE EN UNA VENTANA EMERGENTE GUAY
        }
    }

    public void closeInfoPanel() {
        if (InfoImportPanel)
            InfoImportPanel.SetActive(false);
    }
}
