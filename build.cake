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
var testPath = "NServiceBus.Transport.Email.Tests/NServiceBus.Transport.Email.Tests.csproj";

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

Task("RestoreNuGetPackages")
.IsDependentOn("Clean")
.Does(() =>
{
    DotNetCoreRestore(solutionPath);
});

Task("Build")
.IsDependentOn("RestoreNuGetPackages")
.Does(() =>
{

    var buildSettings = new DotNetCoreBuildSettings {
        Configuration = configuration,
        NoRestore = true
    };
    
    DotNetCoreBuild(solutionPath, buildSettings);
});

Task("Test")
.IsDependentOn("RestoreNuGetPackages")
.Does(() =>
{

    var testSettings = new DotNetCoreTestSettings {
        Configuration = configuration,
        NoRestore = true,
        Logger = "trx;LogFileName=TestResults.trx"
    };
    
    DotNetCoreTest(testPath, testSettings);
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
.IsDependentOn("Test");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
