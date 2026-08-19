using System.Reflection;
using MelonLoader;

[assembly: AssemblyTitle(DescendersModMenu.BuildInfo.Description)]
[assembly: AssemblyDescription(DescendersModMenu.BuildInfo.Description)]
[assembly: AssemblyCompany(DescendersModMenu.BuildInfo.Company)]
[assembly: AssemblyProduct(DescendersModMenu.BuildInfo.Name)]
[assembly: AssemblyCopyright("Created by " + DescendersModMenu.BuildInfo.Author)]
[assembly: AssemblyTrademark(DescendersModMenu.BuildInfo.Company)]
[assembly: AssemblyVersion(DescendersModMenu.BuildInfo.Version)]
[assembly: AssemblyFileVersion(DescendersModMenu.BuildInfo.Version)]
[assembly: MelonInfo(typeof(DescendersModMenu.DescendersModMenu), DescendersModMenu.BuildInfo.Name, DescendersModMenu.BuildInfo.Version, DescendersModMenu.BuildInfo.Author, DescendersModMenu.BuildInfo.DownloadLink)]
[assembly: MelonColor()]

[assembly: MelonGame("RageSquid", "Descenders")]
