using System.Windows.Threading;

using IRI.Maptor.Jab.Common.Models;

namespace IRI.Maptor.Jab.Controls.Models;

public class Job
{
    public LayerTag Tag { get; set; }

    public DispatcherOperation Operation { get; set; }

    public Job(LayerTag tag, DispatcherOperation operation)
    {
        this.Tag = tag;

        this.Operation = operation;
    }
}
