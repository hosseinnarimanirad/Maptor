using IRI.Maptor.Jab.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace IRI.Maptor.Jab.Controls.Model;

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
