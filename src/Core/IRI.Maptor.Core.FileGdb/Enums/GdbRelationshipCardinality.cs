namespace IRI.Maptor.Core.FileGdb.Enums;

public enum GdbRelationshipCardinality
{
    OneToOne,
    OneToMany,

    /// <summary>Rows are linked through an intermediate (mapping) table named like the relationship class.</summary>
    ManyToMany,
}
