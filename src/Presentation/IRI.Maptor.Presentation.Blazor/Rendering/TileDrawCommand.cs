namespace IRI.Maptor.Presentation.Blazor.Rendering;

/// <summary>One tile image placed at a destination rect, ready to hand to the JS canvas module.</summary>
public sealed record TileDrawCommand(string Key, string Url, double X, double Y, double Width, double Height);
