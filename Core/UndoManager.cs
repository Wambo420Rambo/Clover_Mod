using System;
using BepInEx.Logging;

namespace CloverMod.Core
{
    internal sealed class UndoManager
    {
        private readonly ManualLogSource log;
        private Action undoAction;
        private string description;

        public UndoManager(ManualLogSource log)
        {
            this.log = log;
        }

        public bool CanUndo => undoAction != null;

        public string Description => description ?? string.Empty;

        public void Record(string operation, Action action)
        {
            description = operation;
            undoAction = action;
        }

        public ActionResult Undo()
        {
            if (undoAction == null)
            {
                return ActionResult.Failure("There is no reversible change to undo.");
            }

            Action action = undoAction;
            string operation = description;
            try
            {
                action();
                undoAction = null;
                description = null;
                string message = $"Undid: {operation}.";
                log.LogInfo(message);
                return ActionResult.Success(message);
            }
            catch (Exception exception)
            {
                string message = $"Undo failed ({operation}): {exception.Message}";
                log.LogError(message);
                return ActionResult.Failure(message);
            }
        }
    }
}
