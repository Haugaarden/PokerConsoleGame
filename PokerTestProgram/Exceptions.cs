using System;
using System.Runtime.Serialization;

namespace PokerTestProgram
{
    public class PlayerIsFoldedException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public PlayerIsFoldedException()
        {
        }

        public PlayerIsFoldedException(string message) : base(message)
        {
        }

        public PlayerIsFoldedException(string message, Exception inner) : base(message, inner)
        {
        }

        protected PlayerIsFoldedException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}