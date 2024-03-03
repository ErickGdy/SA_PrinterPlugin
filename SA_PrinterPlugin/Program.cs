using HTTPServer;
using System.Linq;

Dictionary<string, string> configParams = new Dictionary<string, string>();
configParams.Add("PortGlobal", "5000");
configParams.Add("PortPrinter", "5001");
configParams.Add("PortQZ", "5002");
configParams.Add("QZName", "qz-tray.exe");
configParams.Add("PrinterName", "Microsoft Print to PDF");
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
        int x = values.IndexOf("PortGlobal");
        if (x >= 0)
            configParams["PortGlobal"] = Convert.ToInt32(values.ElementAt(x + 1).Replace("\r","").ReplaceLineEndings()).ToString();
    }
    catch { }
    try
    {
        int x = values.IndexOf("PortPrinter");
        if (x >= 0)
            configParams["PortPrinter"] = Convert.ToInt32(values.ElementAt(x + 1).Replace("\r", "").ReplaceLineEndings()).ToString();
    }
    catch { }
    try
    {
        int x = values.IndexOf("PortQZ");
        if (x >= 0)
            configParams["PortQZ"] = Convert.ToInt32(values.ElementAt(x + 1).Replace("\r", "").ReplaceLineEndings()).ToString();
    }
    catch { }
    try
    {
        int x = values.IndexOf("QZName");
        if (x >= 0)
            configParams["QZName"] = values.ElementAt(x + 1).Replace("\r", " ").ReplaceLineEndings();
    }
    catch { }
    try
    {
        int x = values.IndexOf("PrinterName");
        if (x >= 0)
            configParams["PrinterName"] = values.ElementAt(x + 1).Replace("\r", " ").ReplaceLineEndings();
    }
    catch { }
}
catch (Exception e)
{

}
WebServer server = new WebServer(configParams);

server.Start(WebServer.ListenerType.QZ);
server.Start(WebServer.ListenerType.Printer);
server.ListenDirect();