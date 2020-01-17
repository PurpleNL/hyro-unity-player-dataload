using PureMVC.Interfaces;
using PureMVC.Patterns.Command;
using UnityPureMVC.Core.Libraries.UnityLib.Utilities.Logging;
using UnityPureMVC.Modules.DataLoader.Controller.Notes;

namespace UnityPureMVC.Modules.DataLoader.Controller.Commands
{
    internal class DataLoaderStartCommand : SimpleCommand
    {
        public override void Execute(INotification notification)
        {
            DebugLogger.Log("DataLoaderStartCommand::Execute");

            // Register commands
            Facade.RegisterCommand(DataLoaderNote.REQUEST_LOAD_DATA, typeof(RequestLoadDataCommand));

            Facade.RemoveCommand(DataLoaderNote.START);
        }
    }
}
