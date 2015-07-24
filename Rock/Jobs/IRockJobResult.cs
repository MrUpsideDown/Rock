using System;

namespace Rock.Jobs
{
    public enum RockJobResultSpecifier
    {
        Failed = 0,
        Succeeded = 1,
        CompletedWithWarnings = 2
    }

    public interface IRockJobResult
    {
        RockJobResultSpecifier? Result { get; set; }
        string ResultDescription { get; set; }
        string ResultDetails { get; set; }
    }
}