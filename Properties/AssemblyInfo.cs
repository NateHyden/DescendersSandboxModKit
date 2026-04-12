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

// Create and Setup a MelonGame Attribute to mark a Melon as Universal or Compatible with specific Games.
// If no MelonGame Attribute is found or any of the Values for any MelonGame Attribute on the Melon is null or empty it will be assumed the Melon is Universal.
// Values for MelonGame Attribute can be found in the Game's app.info file or printed at the top of every log directly beneath the Unity version.
[assembly: MelonGame("RageSquid", "Descenders")]