namespace CloverMod.Core
{
    internal readonly struct ActionResult
    {
        private ActionResult(bool succeeded, string message)
        {
            Succeeded = succeeded;
            Message = message;
        }

        public bool Succeeded { get; }

        public string Message { get; }

        public static ActionResult Success(string message) => new ActionResult(true, message);

        public static ActionResult Failure(string message) => new ActionResult(false, message);
    }
}
