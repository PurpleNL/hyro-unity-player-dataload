using Newtonsoft.Json;
using PureMVC.Interfaces;
using PureMVC.Patterns.Command;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityPureMVC.Core.Controller.Notes;
using UnityPureMVC.Core.Libraries.UnityLib.Utilities.Logging;
using UnityPureMVC.Core.Model.VO;
using UnityPureMVC.Modules.DataLoader.Controller.Notes;
using UnityPureMVC.Modules.DataLoader.Model.VO;

namespace UnityPureMVC.Modules.DataLoader.Controller.Commands
{
    internal class RequestLoadDataCommand : SimpleCommand
    {
        protected RequestLoadDataVO requestLoadDataVO;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="notification"></param>
        public override void Execute(INotification notification)
        {
            DebugLogger.Log("RequestLoadDataCommand::Execute");

            // Store the request load data VO
            requestLoadDataVO = notification.Body as RequestLoadDataVO;

            // Check if request is for Resources or WWW data
            if (requestLoadDataVO.requestType == RequestLoadDataType.RESOURCES)
            {
                LoadDataFromResources(requestLoadDataVO.path, requestLoadDataVO.dataType);
            }
            else
            {
                LoadDataFromWWW(requestLoadDataVO.path, requestLoadDataVO.dataType);
            }
        }

        /// <summary>
        /// Specify a data type to convert data and return object of that type
        /// </summary>
        /// <param name="path"></param>
        /// <param name="dataType"></param>
        private void LoadDataFromResources(string path, Type dataType)
        {
            if (dataType == null)
            {
                DebugLogger.Log("RequestLoadDataCommand::LoadDataFromResources -> dataType == null");
                LoadDataFromResources(path);
                return;
            }

            // Get the Raw data as a TextAsset
            TextAsset rawData = Resources.Load<TextAsset>(path.Replace(".json", ""));

            if (rawData == null)
            {
                DebugLogger.LogWarning("RequestLoadDataCommand::LoadDataFromResources -> Could not load application data file");
                SendNotification(DataLoaderNote.REQUEST_LOAD_DATA_ERROR, "Could not process application data file");
                return;
            }

            // Deserialize to the specified object
            object data = JsonConvert.DeserializeObject(rawData.text, dataType);

            // Notify system, passing newly created data object as body
            SendNotification(DataLoaderNote.DATA_LOADED, data);

            // Check for an onComplete event in the request VO (This allows us to easily chain data requests
            if (requestLoadDataVO.onComplete != null)
            {
                requestLoadDataVO.onComplete.Invoke(data);
            }
        }

        /// <summary>
        /// No data type specified will result in a Dictionary (string,object)
        /// </summary>
        /// <param name="path"></param>
        private void LoadDataFromResources(string path)
        {
            TextAsset rawData = Resources.Load<TextAsset>(path.Replace(".json", ""));

            if (rawData == null)
            {
                DebugLogger.LogWarning("RequestLoadDataCommand::LoadDataFromResources -> Could not process application data file");
                SendNotification(DataLoaderNote.REQUEST_LOAD_DATA_ERROR, "Could not process application data file");

                return;
            }

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(rawData.text);

            DataLoaderResultDictionaryVO dataSystemResultDictionaryVO = new DataLoaderResultDictionaryVO
            {
                data = data
            };

            SendNotification(DataLoaderNote.DATA_LOADED, dataSystemResultDictionaryVO);

            // Check for an onComplete event in the request VO (This allows us to easily chain data requests
            if (requestLoadDataVO.onComplete != null)
            {
                requestLoadDataVO.onComplete.Invoke(dataSystemResultDictionaryVO);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="dataType"></param>
        private void LoadDataFromWWW(string path, Type dataType)
        {
            if (dataType == null)
            {
                LoadDataFromWWW(path);
                return;
            }
            RequestStartCoroutineVO requestStartCoroutineVO = new RequestStartCoroutineVO();
            requestStartCoroutineVO.coroutine = DoLoadDataFromWWW(path, dataType);
            SendNotification(CoreNote.REQUEST_START_COROUTINE, requestStartCoroutineVO);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        private void LoadDataFromWWW(string path)
        {
            RequestStartCoroutineVO requestStartCoroutineVO = new RequestStartCoroutineVO();
            requestStartCoroutineVO.coroutine = DoLoadDataFromWWW(path);
            SendNotification(CoreNote.REQUEST_START_COROUTINE, requestStartCoroutineVO);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="dataType"></param>
        /// <returns></returns>
        private IEnumerator DoLoadDataFromWWW(string path, Type dataType)
        {
            // Empty object to hold our processed data
            object data = null;

            // Extract fileName from path 
            string fileName = Path.GetFileName(path);

            // Get the cache location
            string cachePath = Path.Combine(UnityEngine.Application.persistentDataPath, fileName);

            // Attempt to load previously cached version
            if (requestLoadDataVO.cache && !requestLoadDataVO.forceCacheRefresh && File.Exists(cachePath))
            {
                DebugLogger.Log("Loaded Data from Cache");
                data = JsonConvert.DeserializeObject(File.ReadAllText(cachePath), dataType);
            }
            else
            {
                UnityWebRequest www = UnityWebRequest.Get(path);

                yield return www.SendWebRequest();

                // Attempt to present cache version if Network error
                if ((www.isNetworkError || www.isHttpError))
                {
                    // Attempt to load previously cached version
                    if (!File.Exists(cachePath))
                    {
                        SendNotification(DataLoaderNote.REQUEST_LOAD_DATA_ERROR, www.error);
                        yield break;
                    }
                    data = JsonConvert.DeserializeObject(File.ReadAllText(cachePath), dataType);
                    DebugLogger.Log("Loaded from cache following network error");
                }
                else
                {
                    try
                    {
                        data = JsonConvert.DeserializeObject(www.downloadHandler.text, dataType);

                        // Cache the json data file
                        if (requestLoadDataVO.cache || requestLoadDataVO.forceCacheRefresh)
                        {
                            File.WriteAllBytes(cachePath, www.downloadHandler.data);
                        }
                    }
                    catch (Exception e)
                    {
                        string msg = "There was an error processing data: \n\n " + e.Message;

                        SendNotification(DataLoaderNote.REQUEST_LOAD_DATA_ERROR, msg);
                    }
                }
            }

            if (data == null)
            {
                SendNotification(DataLoaderNote.REQUEST_LOAD_DATA_ERROR, "Data in NULL :: Could not process data");
                yield break;
            }

            SendNotification(DataLoaderNote.DATA_LOADED, data);

            // Check for an onComplete event in the request VO (This allows us to easily chain data requests
            if (requestLoadDataVO.onComplete != null)
            {
                requestLoadDataVO.onComplete.Invoke(data);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private IEnumerator DoLoadDataFromWWW(string path)
        {
            // Extract fileName from path 
            string fileName = Path.GetFileName(path);

            string cachePath = Path.Combine(UnityEngine.Application.persistentDataPath, fileName);

            Dictionary<string, object> data = null;

            // Attempt to load previously cached version
            if (requestLoadDataVO.cache && !requestLoadDataVO.forceCacheRefresh && File.Exists(cachePath))
            {
                DebugLogger.Log("Loaded Data from Cache");
                data = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(cachePath));
            }
            else
            {
                UnityWebRequest www = UnityWebRequest.Get(path);

                yield return www.SendWebRequest();

                if ((www.isNetworkError || www.isHttpError))
                {
                    // Attempt to load previously cached version
                    if (!File.Exists(cachePath))
                    {
                        SendNotification(DataLoaderNote.REQUEST_LOAD_DATA_ERROR, www.error);
                        yield break;
                    }
                    data = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(cachePath));
                }
                else
                {
                    // Cache the json data file
                    if (requestLoadDataVO.cache || requestLoadDataVO.forceCacheRefresh)
                    {
                        File.WriteAllBytes(cachePath, www.downloadHandler.data);
                    }
                    data = JsonConvert.DeserializeObject<Dictionary<string, object>>(www.downloadHandler.text);
                }
            }

            if (data == null)
            {
                SendNotification(DataLoaderNote.REQUEST_LOAD_DATA_ERROR, "Data object is NULL");
                yield break;
            }

            SendNotification(DataLoaderNote.DATA_LOADED, new DataLoaderResultDictionaryVO
            {
                data = data
            });

            // Check for an onComplete event in the request VO (This allows us to easily chain data requests
            if (requestLoadDataVO.onComplete != null)
            {
                requestLoadDataVO.onComplete.Invoke(data);
            }
        }
    }
}
