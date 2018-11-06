using System;

namespace Photon.Communication
{
    public class UnhandledExceptionEventArgs : EventArgs
    {
        public Exception Exception {get;}

        public UnhandledExceptionEventArgs(Exception exception)
        {
            this.Exception = exception;
        }
    }
}
