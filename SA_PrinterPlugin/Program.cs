using HTTPServer;

int port = 5000;
try
{
    // The path to the file
    string path = @"config";
    // Read the entire file and store it in a string
    string fileContents = File.ReadAllText(path);
    port = Convert.ToInt32(fileContents.Split(":")[1]);
}
catch (Exception e)
{

}
WebServer server = new WebServer(port, "SUPERADMINISTRADOR");

server.Start();
server.Listen();