#tool nuget:?package=NUnit.ConsoleRunner&version=3.4.0
//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define directories.
var solutionPath = "NServiceBus.Transport.Email.sln";

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
.Does(() =>
{   

    var cleanSettings = new DotNetCoreCleanSettings {
        Configuration = configuration
    };

    DotNetCoreClean(solutionPath, cleanSettings);
});

Task("Restore-NuGet-Packages")
.IsDependentOn("Clean")
.Does(() =>
{
    DotNetCoreRestore(solutionPath);
});

Task("Build")
.IsDependentOn("Restore-NuGet-Packages")
.Does(() =>
{

    var buildSettings = new DotNetCoreBuildSettings {
        Configuration = configuration,
        NoRestore = true
    };
    
    DotNetCoreBuild(solutionPath, buildSettings);
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
.IsDependentOn("Build");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
