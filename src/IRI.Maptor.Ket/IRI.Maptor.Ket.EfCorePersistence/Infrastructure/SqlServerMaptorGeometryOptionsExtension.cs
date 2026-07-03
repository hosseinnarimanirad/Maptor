using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

using IRI.Maptor.Ket.EfCorePersistence.Extensions;

namespace IRI.Maptor.Ket.EfCorePersistence.Infrastructure;

/// <summary>
/// The <see cref="IDbContextOptionsExtension"/> registered by
/// <see cref="SqlServerMaptorGeometryDbContextOptionsExtensions.UseMaptor"/>. It registers the
/// <see cref="Storage.SqlServerMaptorGeometryTypeMappingSourcePlugin"/> with the internal service provider.
/// </summary>
public class SqlServerMaptorGeometryOptionsExtension : IDbContextOptionsExtension
{
    private DbContextOptionsExtensionInfo? _info;

    public DbContextOptionsExtensionInfo Info => _info ??= new ExtensionInfo(this);

    public void ApplyServices(IServiceCollection services)
        => services.AddEntityFrameworkSqlServerMaptorGeometry();

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
            => debugInfo["SqlServer:" + nameof(SqlServerMaptorGeometryDbContextOptionsExtensions.UseMaptor)] = "1";
    }
}
