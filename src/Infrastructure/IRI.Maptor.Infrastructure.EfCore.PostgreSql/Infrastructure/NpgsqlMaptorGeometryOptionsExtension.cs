using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

using IRI.Maptor.Infrastructure.EfCore.PostgreSql.Extensions;

namespace IRI.Maptor.Infrastructure.EfCore.PostgreSql.Infrastructure;

/// <summary>
/// The <see cref="IDbContextOptionsExtension"/> registered by
/// <see cref="NpgsqlMaptorGeometryDbContextOptionsExtensions.UseMaptor"/>. It registers the Maptor type-mapping,
/// data-source and convention plugins with the internal service provider.
/// </summary>
public class NpgsqlMaptorGeometryOptionsExtension : IDbContextOptionsExtension
{
    private DbContextOptionsExtensionInfo? _info;

    public DbContextOptionsExtensionInfo Info => _info ??= new ExtensionInfo(this);

    public void ApplyServices(IServiceCollection services)
        => services.AddEntityFrameworkNpgsqlMaptorGeometry();

    public void Validate(IDbContextOptions options)
    {
    }

    private sealed class ExtensionInfo : DbContextOptionsExtensionInfo
    {
        public ExtensionInfo(IDbContextOptionsExtension extension)
            : base(extension)
        {
        }

        public override bool IsDatabaseProvider => false;

        public override string LogFragment => "using MaptorGeometry ";

        public override int GetServiceProviderHashCode() => 0;

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
            => other is ExtensionInfo;

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
            => debugInfo["Npgsql:" + nameof(NpgsqlMaptorGeometryDbContextOptionsExtensions.UseMaptor)] = "1";
    }
}
