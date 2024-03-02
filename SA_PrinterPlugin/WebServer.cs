using PDFtoPrinter;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Web;

namespace HTTPServer
{
    class WebServer
    {
        string filePath = @"Blank.pdf";
        public WebServer(Dictionary<string, string> configParams)
        {
            this.port = Convert.ToInt32(configParams["Port"]);
            this.home = configParams["Home"];
            this.QZName = configParams["QZName"];
            listener = new TcpListener(IPAddress.Any, port);
        }

        public void Start()
        {
            listener.Start();
        }

        public void Listen()
        {
            try
            {
                while (true)
                {
                    Byte[] result = new Byte[MAX_SIZE];
                    string requestData;

                    TcpClient client = listener.AcceptTcpClient();
                    NetworkStream stream = client.GetStream();
                    int size = stream.Read(result, 0, result.Length);
                    requestData = System.Text.Encoding.ASCII.GetString(result, 0, size);
                    Request request;
                    try
                    {
                        request = GetRequest(requestData);
                    }
                    catch {
                        GenerateResponse("Error: Parametros incorrectos", stream, 500);
                        client.Close();
                        continue;
                    }
                    try
                    {
                        ProcessRequest(request, stream);
                    }
                    catch
                    {
                        GenerateResponse("Error: Ha ocurrido un error al procesar la solicitud", stream, 500);
                        client.Close();
                        continue;
                    }
                    client.Close();
                }
            }
            catch
            {
                listener.Stop();
            }
        }

        private void ProcessRequest(Request request, NetworkStream stream)
        {
            if (request == null)
            {
                return;
            }
            string printerName = "", actionName = "";
            try
            {
                printerName = request.QueryParams["Printer"];
            }
            catch { }
            try
            {
                actionName = request.QueryParams["Action"];
            }
            catch { }

            if (!string.IsNullOrEmpty(printerName))
            {
                try
                {
                    Print(printerName);
                    GenerateResponse(printerName, stream, 200);
                }
                catch
                {
                    GenerateResponse("Error", stream, 500);
                }
                return;
            }
            else if (!string.IsNullOrEmpty(actionName))
            {
                switch (actionName.ToUpper())
                {
                    case "QZ":
                        try
                        {
                            OpenQZ();
                            GenerateResponse("Abriendo QZ", stream, 200);
                        }
                        catch(Exception ex)
                        {
                            GenerateResponse($"Error: {ex.Message}", stream, 500);
                        }
                        return;
                    default:
                        GenerateResponse("Solicitud No Encontrada", stream, 404);
                        return;
                }
            }
            else
            {
                GenerateResponse("Solicitud No Encontrada", stream, 404);
                return;
            }
        }

        private void ParsePath(Request request)
        {
            request.Path.Replace('/', '\\');
            request.Path = home + request.Path;
        }

        private void GenerateResponse(string content,
            NetworkStream stream,
            int httpResponse)
        {
            string response = "HTTP/1.1 200 OK\r\n\r\n\r\n";
            switch (httpResponse)
            {
                case int n when (n >= 400 && n < 500):
                    response = "HTTP/1.1 404 NOTFOUND\r\n\r\n\r\n";
                    break;
                case int n when (n >= 200 && n < 300):
                    response = "HTTP/1.1 200 OK\r\n\r\n\r\n";
                    break;
                case int n when (n >= 500 && n < 600):
                    response = "HTTP/1.1 500 Internal Server Error\r\n\r\n\r\n";
                    break;
                default:
                    break;
            }
            response = response + content;
            byte[] msg = System.Text.Encoding.ASCII.GetBytes(response);
            stream.Write(msg, 0, msg.Length);
            return;
        }

        private void StopServer()
        {
            listener.Stop();
        }

        private Request GetRequest(string data)
        {
            Request request = new Request();
            var list = data.Split(' ');
            if (list.Length < 3)
                return null;

            request.Command = list[0];
            request.Path = list[1];
            request.Protocol = list[2].Split('\n')[0];
            request.Host = list[6].Split('\n')[0].Replace("\r", "") + list[1];
            request.QueryParams = GetParams(request);
            return request;
        }

        private TcpListener listener;
        private int port;
        private string home;
        private string QZName;
        private static string NOTFOUND404 = "HTTP/1.1 404 Not Found";
        private static string OK200 = "HTTP/1.1 200 OK\r\n\r\n\r\n";
        private static int MAX_SIZE = 1000;


        private void Print(string networkPrinterName)
        {
            var printTimeout = new TimeSpan(0, 30, 0);
            var printer = new PDFtoPrinterPrinter();
            printer.Print(new PrintingOptions(networkPrinterName, filePath), printTimeout);
        }

        private void OpenQZ()
        {
            var QZexePath = Directory.EnumerateFiles("C:\\Program Files\\", $"*{this.QZName}*", new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = true
            }).FirstOrDefault();
            if (string.IsNullOrEmpty(QZexePath))
                throw new Exception($"{this.QZName} no encontrado");
            System.Diagnostics.Process.Start(QZexePath);

        }
        static void ApplyAllFiles(string folder, Action<string> fileAction)
        {
            foreach (string file in Directory.GetFiles(folder))
            {
                fileAction(file);
            }
            foreach (string subDir in Directory.GetDirectories(folder))
            {
                try
                {
                    ApplyAllFiles(subDir, fileAction);
                }
                catch
                {
                    // swallow, log, whatever
                }
            }
        }

        private Dictionary<string, string> GetParams(Request request)
        {
            string fullPath = request.Host;
            // Obtener los parámetros de la URL
            var uri = new Uri(fullPath);
            var queryString = uri.Query;

            // Parsear los parámetros en un diccionario
            var queryDict = HttpUtility.ParseQueryString(queryString);
            Dictionary<string, string> queryParams = new Dictionary<string, string>();
            // Iterar sobre los parámetros y sus valores
            foreach (string key in queryDict.AllKeys)
            {
                queryParams.Add(key, queryDict.Get(key));
            }
            return queryParams;
        }

    }

    public class Request
    {
        public string Command;
        public string Path;
        public string Protocol;
        public string Host;
        public Dictionary<string, string> QueryParams;

    }

}