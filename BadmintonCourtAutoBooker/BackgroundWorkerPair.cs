using System.ComponentModel;

namespace BadmintonCourtAutoBooker
{
    internal class BackgroundWorkerPair
    {
        public BackgroundWorker BackgroundWorker { get; set; }

        public BackgroundWrokerArguments BackgroundWrokerArguments { get; set; }

        public bool IsFinished { get; set; }
    }
}
