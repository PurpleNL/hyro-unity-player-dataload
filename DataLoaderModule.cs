namespace UnityPureMVC.Modules.DataLoader
{
    using PureMVC.Interfaces;
    using PureMVC.Patterns.Facade;
    using UnityPureMVC.Modules.DataLoader.Controller.Commands;
    using UnityPureMVC.Modules.DataLoader.Controller.Notes;
    using System;
    using UnityEngine;

    internal class DataLoaderModule : MonoBehaviour
    {
        /// <summary>
        /// The facade.
        /// </summary>
        private IFacade facade;

        /// <summary>
        /// Start this instance.
        /// </summary>
        protected virtual void Awake()
        {
            try
            {
                facade = Facade.GetInstance("DataLoader");
                facade.RegisterCommand(DataLoaderNote.START, typeof(DataLoaderStartCommand));
                facade.SendNotification(DataLoaderNote.START, this);
            }
            catch (Exception exception)
            {
                throw new UnityException("Unable to initiate Facade", exception);
            }
        }

        /// <summary>
        /// On destroy.
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (facade != null)
            {
                facade.Dispose();
                facade = null;
            }
        }
    }
}