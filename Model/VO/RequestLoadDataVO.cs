using System;

namespace UnityPureMVC.Modules.DataLoader.Model.VO
{
    internal enum RequestLoadDataType
    {
        RESOURCES,
        WWW
    }

    internal class RequestLoadDataVO
    {
        internal delegate void OnCompleteEvent(object data);
        internal string path;
        internal RequestLoadDataType requestType;
        internal Type dataType;
        internal bool cache = true;
        internal bool forceCacheRefresh = false; // This will clear any existing cache and force a new download
        internal OnCompleteEvent onComplete;
    }
}
