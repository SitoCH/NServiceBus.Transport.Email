#tool nuget:?package=NUnit.ConsoleRunner&version=3.4.0
#addin Cake.Coveralls
#tool coveralls.io

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
        NoRestore = true,
        ArgumentCustomization = arg => arg.AppendSwitch("/p:DebugType","=","Full")
    };
    
    DotNetCoreBuild(solutionPath, buildSettings);
});

Task("Test")
.IsDependentOn("Build")
.Does(() =>
{
    var testSettings = new DotNetCoreTestSettings {
        Configuration = configuration,
        NoRestore = true,
        NoBuild = true,
        Logger = "trx;LogFileName=TestResults.trx",
        ArgumentCustomization = args => args.Append("/p:AltCover=true")
    };
    
    DotNetCoreTest("NServiceBus.Transport.Email.Tests/NServiceBus.Transport.Email.Tests.csproj", testSettings);
    
});

Task("PublishTestResults")
.IsDependentOn("Test")
.WithCriteria(EnvironmentVariable("APPVEYOR_JOB_ID") != null)
.Does(() =>
{
    // Upload tests results
    var url = $"https://ci.appveyor.com/api/testresults/mstest/{EnvironmentVariable("APPVEYOR_JOB_ID")}";
    UploadFile(url, @"NServiceBus.Transport.Email.Tests/TestResults/TestResults.trx");  
    
    // Upload code coverage
    CoverallsIo("coverage.xml", new CoverallsIoSettings
        {
            RepoToken = EnvironmentVariable("APPVEYOR_COVERALLS_TOKEN")
        });

});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
.IsDependentOn("PublishTestResults");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
