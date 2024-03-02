using HTTPServer;
using System.Linq;

Dictionary<string, string> configParams = new Dictionary<string, string>();
configParams.Add("Port", "5000");
configParams.Add("QZName", "qz-tray.exe");
configParams.Add("Home", "SUPERADMINISTRADOR");
try
{
    // The path to the file
    string path = @"config";
    // Read the entire file and store it in a string
    string fileContents = File.ReadAllText(path);

    List<string> values = new List<string>();
    foreach (string item in fileContents.Split("\n"))
    {
        values.AddRange(item.Split(":"));
    }

    try
    {
        int x = values.IndexOf("Port");
        if (x >= 0)
            configParams["Port"] = Convert.ToInt32(values.ElementAt(x + 1).Replace("\r","").ReplaceLineEndings()).ToString();
    }
    catch { }
    try
    {
        int x = values.IndexOf("QZName");
        if (x >= 0)
            configParams["QZName"] = values.ElementAt(x + 1).Replace("\r", " ").ReplaceLineEndings();
    }
    catch { }
}
catch (Exception e)
{

}
WebServer server = new WebServer(configParams);

server.Start();
server.Listen();