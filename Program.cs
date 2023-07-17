using System.Diagnostics;
using Microsoft.Extensions.Configuration;

var commandLineArgs = Environment.GetCommandLineArgs();

var builder = new ConfigurationBuilder();
builder.SetBasePath(Directory.GetCurrentDirectory())
       .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

IConfiguration config = builder.Build();
var runType = Environment.GetEnvironmentVariable("RUN_TYPE");

var outputFolder = commandLineArgs[1];
var outputFileName = commandLineArgs[2];
var sdPluginDistPath = $"{outputFolder}{outputFileName}.sdPlugin";
var streamDeckPlugin = $"{outputFolder}{outputFileName}.streamDeckPlugin";

var distributionToolPath = config.GetSection("StreamDeckDistributionToolPath").Value;
if (string.IsNullOrEmpty(distributionToolPath))
{
    Console.WriteLine("> INFO: No value for the path of the Stream Deck DistributionTool given. Please check the appsettings.json");
    Console.Read();

    return;
}

if (distributionToolPath == "C:\\Path\\To\\DistributionTool.exe")
{
    Console.WriteLine("> INFO: Please set the path to the Stream Deck DistributionTool (by Elgato) in the appsettings.json file of your plugin project");
    Console.Read();

    return;
}

var streamDeckPluginsPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\Elgato\\StreamDeck\\Plugins\\{outputFileName}.sdPlugin";

Console.WriteLine($"====================================================");
Console.WriteLine($"== Stream Deck plugin development build/installer ==");
Console.WriteLine($"====================================================");

Console.WriteLine();
Console.WriteLine($"Mode: {runType}");
Console.WriteLine();

//------------------------------------------
// Build with clean up of previous builds //
//------------------------------------------ 

if (File.Exists(streamDeckPlugin))
{
    File.Delete(streamDeckPlugin);
    Console.WriteLine("> Removed the previous build of the plugin");
}

if (!Directory.Exists(sdPluginDistPath))
{
    Console.WriteLine("> Could not found the awaited plugin source path:");
    Console.WriteLine($"> {sdPluginDistPath}");
    Console.WriteLine();
    Console.WriteLine("> Process cancled");
    return;
}

try
{
    var dtProcess = Process.Start($"{distributionToolPath}", $"-b -i \"{sdPluginDistPath}\" -o \"{outputFolder}\\\"");

    Console.WriteLine("> Started the Stream Deck DistributionTool for generating the plugin");
    Console.WriteLine();
    dtProcess.WaitForExit();
}
catch (Exception ex)
{
    Console.WriteLine();
    Console.WriteLine("> INFO: DistributionTool could not get started");
    Console.WriteLine();
    Console.WriteLine(ex.Message);
    Console.Read();

    return;
}

Console.WriteLine();
Console.WriteLine("> Build completed");

if (runType == "Build")
{
    return;
}

//--------------------------------------------------------------------
// Stops all processes and install die actual version of the plugin //
//--------------------------------------------------------------------
foreach (var process in Process.GetProcessesByName("streamdeck"))
{
    process.Kill();
    process.WaitForExit();

    Console.WriteLine($"> Stopped process: {process.ProcessName}");
}

foreach (var process in Process.GetProcessesByName(outputFileName))
{
    process.Kill();
    process.WaitForExit();

    Console.WriteLine($"> Stopped process: {process.ProcessName}");
}

if (Directory.Exists(streamDeckPluginsPath))
{
    Directory.Delete(streamDeckPluginsPath, true);
    Console.WriteLine("> Removed the installed plugin resources from the Stream Deck plugin folder");
}

var sdpProcess = Process.Start(new ProcessStartInfo($"{streamDeckPlugin}") { UseShellExecute = true });
Console.WriteLine("> Started the installtion of the new/updated plugin (will also start the Stream Deck software automatically)");

sdpProcess?.WaitForInputIdle();
Console.WriteLine("> Start and Installation completed. The Stream Deck software should appear in a few seconds");
