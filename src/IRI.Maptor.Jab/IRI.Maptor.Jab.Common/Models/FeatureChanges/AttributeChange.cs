namespace IRI.Maptor.Jab.Common.Models.FeatureChanges;

public record AttributeChange(string Name, string DisplayName, object? OldValue, object? NewValue);
