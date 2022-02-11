using System;
using System.Collections.Generic;
using System.Linq;
using NuGet;
using System.IO;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Queue.FunctionalTest.Helper
{
    public class PackageDownloader
    {
        private const string Nuget_Feed_Url = "https://www.nuget.org/api/v2/";
        public const string Assemblies_Filename = "assemblies.txt";

        private readonly IPackageRepository _localFeed;
        private readonly IPackageRepository _fiskaltrustFeed;
        private readonly IPackageRepository _nugetFeed;

        public PackageDownloader(string localFeedDirectory, string packagesUrl, Guid cashBoxId, string accessToken)
        {
            _localFeed = PackageRepositoryFactory.Default.CreateRepository(localFeedDirectory);
            _nugetFeed = PackageRepositoryFactory.Default.CreateRepository(Nuget_Feed_Url);
            _fiskaltrustFeed = PackageRepositoryFactory.Default.CreateRepository($"{packagesUrl}/nuget/");

            var settings = Settings.LoadDefaultSettings(null, null, null);
            var packageSourceProvider = new PackageSourceProvider(settings);
            var credentialProvider = new SettingsCredentialProvider(new PackageCredentialProvider(cashBoxId, accessToken), packageSourceProvider);
            HttpClient.DefaultCredentialProvider = credentialProvider;
        }

        public IPackage GetFromLocalFeed(string packageId) => _localFeed.FindPackagesById(packageId).OrderBy(x => x.Version).FirstOrDefault();

        public IPackage GetFromFiskaltrustFeed(string packageId, SemanticVersion version) => _fiskaltrustFeed.FindPackage(packageId, version);

        public async Task InstallLocalLauncherAsync(string launcherDirectory)
        {
            var packageId = "fiskaltrust.service.launcher";
            var latestLauncherVersion = _localFeed.FindPackagesById(packageId).OrderByDescending(p => p.Version.ToString()).First().Version;
            var package = _localFeed.FindPackage(packageId, latestLauncherVersion, true, true);
            if (package == null)
            {
                throw new Exception($"Package {packageId} with version {latestLauncherVersion} not found!");
            }

            foreach (var assemblyRef in package.GetFiles())
            {
                if (!File.Exists(Path.Combine(launcherDirectory, assemblyRef.EffectivePath)))
                {
                    using (var assemblyStream = assemblyRef.GetStream())
                    {
                        using (var outputFileStream = File.Create(Path.Combine(launcherDirectory, assemblyRef.EffectivePath)))
                        {
                            await assemblyStream.CopyToAsync(outputFileStream);
                        }
                    }
                }
            }

            var dependencySet = GetClosestSupportedDependencySet(package.DependencySets, 4, 6);
            if (dependencySet == null)
            {
                return;
            }

            foreach (var dep in dependencySet.Dependencies)
            {
                throw new Exception("Should not have dependencies");
            }
        }

        public void LoadNugetPackages(IPackage package)
        {
            AddPackage(package);
            var dependencySetsToRestore = package.DependencySets.Where(x => x?.TargetFramework?.Identifier == ".NETFramework"); // Currently we only support NETFX packages because otherwise we will get into dependency restore hell
            if (!dependencySetsToRestore.Any())
            {
                dependencySetsToRestore = package.DependencySets;
            }
            foreach (var dependencySet in dependencySetsToRestore)
            {
                foreach (var item in dependencySet.Dependencies)
                {
                    var depPkg = _localFeed.ResolveDependency(item, true, true);
                    var dependencyFeed = _localFeed;
                    if (depPkg == null)
                    {
                        depPkg = _fiskaltrustFeed.ResolveDependency(item, true, true);
                        dependencyFeed = _fiskaltrustFeed;
                        if (depPkg == null)
                        {
                            depPkg = _nugetFeed.ResolveDependency(item, true, true);
                            dependencyFeed = _nugetFeed;
                            if (depPkg == null)
                            {
                                throw new Exception($"Dependency package {item.Id} ({item.VersionSpec.MinVersion}) doesn´t exist in any of the resources.");
                            }
                        }

                    }
                    AddPackage(depPkg);
                    LoadNugetPackages(depPkg);
                }
            }
        }

        public void AddPackage(IPackage package)
        {
            var localPackage = _localFeed.FindPackage(package.Id, package.Version, !package.IsReleaseVersion(), true);
            if (localPackage == null)
            {
                _localFeed.AddPackage(package);
            }
        }

        public async Task InstallLauncherAsync(string launcherDirectory)
        {
            var packageId = "fiskaltrust.service.launcher";
            var latestLauncherVersion = _fiskaltrustFeed.FindPackagesById(packageId).OrderByDescending(p => p.Version.ToString()).First().Version;
            await InstallPackageAssembliesForNet461Async(packageId, latestLauncherVersion, true, launcherDirectory);
        }

        public async Task InstallPackageAssembliesForNet461Async(string packageId, SemanticVersion packageVersion, bool allowPreRelease, string folder)
        {
            var package = _fiskaltrustFeed.FindPackage(packageId, packageVersion, allowPreRelease, true);
            if (package == null)
            {
                package = _nugetFeed.FindPackage(packageId, packageVersion, allowPreRelease, true);
                if (package == null)
                {
                    throw new Exception($"Package {packageId} with version {packageVersion} not found!");
                }
            }

            foreach (var assemblyRef in GetClosestSupportedAssemblyReferences(package.AssemblyReferences, 4, 6))
            {
                if (!File.Exists(Path.Combine(folder, assemblyRef.Name)))
                {
                    using (var assemblyStream = assemblyRef.GetStream())
                    {
                        using (var outputFileStream = File.Create(Path.Combine(folder, assemblyRef.Name)))
                        {
                            await assemblyStream.CopyToAsync(outputFileStream);
                        }
                    }
                }
            }

            foreach (var assemblyRef in GetClosestSupportedPackageFile(package.GetFiles(), 4, 6))
            {
                if (!File.Exists(Path.Combine(folder, assemblyRef.EffectivePath)))
                {
                    using (var assemblyStream = assemblyRef.GetStream())
                    {
                        using (var outputFileStream = File.Create(Path.Combine(folder, assemblyRef.EffectivePath)))
                        {
                            await assemblyStream.CopyToAsync(outputFileStream);
                        }
                    }
                }
            }

            var dependencySet = GetClosestSupportedDependencySet(package.DependencySets, 4, 6);
            if (dependencySet == null)
            {
                return;
            }

            foreach (var dep in dependencySet.Dependencies)
            {
                await InstallPackageAssembliesForNet461Async(dep.Id, dep.VersionSpec.MinVersion, allowPreRelease, folder);
            }
        }

        public IEnumerable<IPackageAssemblyReference> GetClosestSupportedAssemblyReferences(IEnumerable<IPackageAssemblyReference> assemblyReferences, int frameworkMajorVersion = 4, int frameworkMinorVersion = 6)
        {
            var matchingVersion = GetApproximatMatchingFramework(assemblyReferences.Select(x => x.TargetFramework).Distinct(), frameworkMajorVersion, frameworkMinorVersion);
            return assemblyReferences.Where(x => x.TargetFramework == matchingVersion);
        }


        public IEnumerable<IPackageFile> GetClosestSupportedPackageFile(IEnumerable<IPackageFile> assemblyReferences, int frameworkMajorVersion = 4, int frameworkMinorVersion = 6)
        {
            var matchingVersion = GetApproximatMatchingFramework(assemblyReferences.Select(x => x.TargetFramework).Distinct(), frameworkMajorVersion, frameworkMinorVersion);
            return assemblyReferences.Where(x => x.TargetFramework == matchingVersion);
        }

        public PackageDependencySet GetClosestSupportedDependencySet(IEnumerable<PackageDependencySet> packageDependencySets, int frameworkMajorVersion, int frameworkMinorVersion)
        {
            var matchingVersion = GetApproximatMatchingFramework(packageDependencySets.Select(x => x.TargetFramework).Distinct(), frameworkMajorVersion, frameworkMinorVersion);
            return packageDependencySets.FirstOrDefault(x => x.TargetFramework == matchingVersion);
        }

        private static FrameworkName GetApproximatMatchingFramework(IEnumerable<FrameworkName> supportedTargetFrameworks, int frameworkMajorVersion, int frameworkMinorVersion)
        {
            var matchingVersion = supportedTargetFrameworks.FirstOrDefault(x => x?.Version != null && x.Version.Major == frameworkMajorVersion && x.Version.Minor == frameworkMinorVersion);
            if (matchingVersion == null)
            {
                matchingVersion = supportedTargetFrameworks.Where(x => x?.Version != null && x.Version.Major == frameworkMajorVersion && x.Version.Minor < frameworkMinorVersion).OrderByDescending(x => x.Version).FirstOrDefault();
                if (matchingVersion == null)
                {
                    // No way to support netstandard 2.1 yet
                    matchingVersion = supportedTargetFrameworks.Where(x => x?.Version != null && x?.Identifier != null && x.Identifier == ".NETStandard" && x.Version < new Version(2, 1)).OrderByDescending(x => x.Version).FirstOrDefault();
                }
            }

            return matchingVersion;
        }
    }
}