using System;

namespace Rock.Jobs
{
    public class RockJobResult : IRockJobResult
    {
        public static RockJobResult NewFailureResult( string description = null, string details = null )
        {
            return new RockJobResult { Result = RockJobResultSpecifier.Failed, ResultDescription = description, ResultDetails = details };
        }

        public static RockJobResult NewWarningResult( string description = null, string details = null )
        {
            return new RockJobResult { Result = RockJobResultSpecifier.CompletedWithWarnings, ResultDescription = description, ResultDetails = details };
        }

        public static RockJobResult NewSuccessResult( string description = null, string details = null )
        {
            return new RockJobResult { Result = RockJobResultSpecifier.Succeeded, ResultDescription = description, ResultDetails = details };
        }

        public RockJobResultSpecifier? Result { get; set; }
        public string ResultDescription { get; set; }
        public string ResultDetails { get; set; }
    }
}