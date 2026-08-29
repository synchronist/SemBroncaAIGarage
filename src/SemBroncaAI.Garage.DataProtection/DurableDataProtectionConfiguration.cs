using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SemBroncaAI.Garage.DataProtection;

public static class DurableDataProtectionConfiguration
{
    public const string PostgreSqlProvider = "PostgreSql";
    public const string FileSystemProvider = "FileSystem";

    public static void Configure(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment,
        string applicationName)
    {
        var dataProtection = services.AddDataProtection().SetApplicationName(applicationName);
        if (!environment.IsProduction())
            return;

        var provider = configuration["DataProtection:Provider"] ?? FileSystemProvider;
        var runningOnRender = string.Equals(configuration["RENDER"], "true", StringComparison.OrdinalIgnoreCase);
        if (runningOnRender && !string.Equals(provider, PostgreSqlProvider, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Render deve persistir Data Protection no PostgreSQL durável.");

        if (string.Equals(provider, PostgreSqlProvider, StringComparison.OrdinalIgnoreCase))
        {
            var connectionString = configuration["DataProtection:ConnectionString"]
                ?? throw new InvalidOperationException("Configure DataProtection:ConnectionString para persistir as chaves no PostgreSQL.");

            services.AddDbContext<DataProtectionKeyDbContext>(options => options.UseNpgsql(connectionString));
            dataProtection.PersistKeysToDbContext<DataProtectionKeyDbContext>();

            var certificateBase64 = configuration["DataProtection:CertificateBase64"]
                ?? throw new InvalidOperationException("Configure DataProtection:CertificateBase64 para proteger as chaves persistidas.");
            var certificatePassword = configuration["DataProtection:CertificatePassword"]
                ?? throw new InvalidOperationException("Configure DataProtection:CertificatePassword para proteger as chaves persistidas.");
            var certificate = X509CertificateLoader.LoadPkcs12(
                Convert.FromBase64String(certificateBase64),
                certificatePassword,
                X509KeyStorageFlags.EphemeralKeySet);
            dataProtection.ProtectKeysWithCertificate(certificate);
            return;
        }

        if (!string.Equals(provider, FileSystemProvider, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("DataProtection:Provider deve ser FileSystem ou PostgreSql.");

        var path = configuration["DataProtection:KeysPath"]
            ?? throw new InvalidOperationException("Configure DataProtection:KeysPath em Production.");
        Directory.CreateDirectory(path);
        dataProtection.PersistKeysToFileSystem(new DirectoryInfo(path));
    }

    public static void ValidateProduction(IConfiguration configuration, IHostEnvironment environment)
    {
        if (!environment.IsProduction())
            return;

        var provider = configuration["DataProtection:Provider"] ?? FileSystemProvider;
        var runningOnRender = string.Equals(configuration["RENDER"], "true", StringComparison.OrdinalIgnoreCase);
        if (runningOnRender && !string.Equals(provider, PostgreSqlProvider, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Render deve persistir Data Protection no PostgreSQL durável.");

        if (string.Equals(provider, PostgreSqlProvider, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(configuration["DataProtection:ConnectionString"]))
                throw new InvalidOperationException("Configure a conexão PostgreSQL do Data Protection.");
            if (string.IsNullOrWhiteSpace(configuration["DataProtection:CertificateBase64"]) ||
                string.IsNullOrWhiteSpace(configuration["DataProtection:CertificatePassword"]))
                throw new InvalidOperationException("Configure o certificado de proteção das chaves do Data Protection.");
            return;
        }

        if (!string.Equals(provider, FileSystemProvider, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(configuration["DataProtection:KeysPath"]))
            throw new InvalidOperationException("Configure a persistência do Data Protection em Production.");
    }
}
