using PDFtoPrinter;
using System;
using System.IO;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Web;

namespace HTTPServer
{
    class WebServer
    {
        string filePath = @"Blank.pdf";

        private TcpListener listenerGlobal;
        private TcpListener listenerPrinter;
        private TcpListener listenerQZ;
        private int portPrinter;
        private int portGlobal;
        private int portQZ;
        private string home;
        private string QZName;
        private string PrinterName;
        private static string NOTFOUND404 = "HTTP/1.1 404 Not Found";
        private static string OK200 = "HTTP/1.1 200 OK\r\n\r\n\r\n";
        private static int MAX_SIZE = 1000;

        public WebServer(Dictionary<string, string> configParams)
        {
            this.portGlobal = Convert.ToInt32(configParams["PortGlobal"]);
            this.portPrinter = Convert.ToInt32(configParams["PortPrinter"]);
            this.portQZ = Convert.ToInt32(configParams["PortQZ"]);
            this.home = configParams["Home"];
            this.QZName = configParams["QZName"];
            this.PrinterName = configParams["PrinterName"];
            listenerGlobal = new TcpListener(IPAddress.Any, portGlobal);
            listenerPrinter = new TcpListener(IPAddress.Any, portPrinter);
            listenerQZ = new TcpListener(IPAddress.Any, portQZ);
        }

        public enum ListenerType { Global=1,Printer=2,QZ=3, All=0 };
        public void Start(ListenerType listenerType)
        {
            switch (listenerType)
            {
                case ListenerType.Global:
                    listenerGlobal.Start();
                    break;
                case ListenerType.Printer:
                    listenerPrinter.Start();
                    break;
                case ListenerType.QZ:
                    listenerQZ.Start();
                    break;
                case ListenerType.All:
                    listenerGlobal.Start();
                    break;
                default:
                    break;
            }
        }

        public void ListenSecure()
        {
            try
            {
                while (true)
                {
                    Byte[] result = new Byte[MAX_SIZE];
                    string requestData;

                    TcpClient client = listenerGlobal.AcceptTcpClient();
                    NetworkStream stream = client.GetStream();
                    /* Aqui se procesaba la solicitud que es rechazada si no tiene SSL*/
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
                listenerGlobal.Stop();
            }
        }

        public void ListenDirect()
        {
            Thread printerThread = new Thread(new ThreadStart(StartPrinter));
            printerThread.Start();

            Thread qzThread = new Thread(new ThreadStart(StartQZ));
            qzThread.Start();
        }
        public void StartPrinter()
        {
            try
            {
                while (true)
                {
                    Byte[] result = new Byte[MAX_SIZE];
                    string requestData;

                    TcpClient client = listenerPrinter.AcceptTcpClient();
                    NetworkStream stream = client.GetStream();
                    if (!stream.Socket.Connected)
                        continue;
                    try
                    {
                        Print();
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
                listenerPrinter.Stop();
            }
        }
        public void StartQZ()
        {
            try
            {
                while (true)
                {
                    Byte[] result = new Byte[MAX_SIZE];
                    string requestData;

                    TcpClient client = listenerQZ.AcceptTcpClient();
                    NetworkStream stream = client.GetStream();
                    if (!stream.Socket.Connected)
                        continue;
                    try
                    {
                        OpenQZ();
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
                listenerQZ.Stop();
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
            listenerQZ.Stop();
            listenerGlobal.Stop();
            listenerPrinter.Stop();
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

        private void Print(string networkPrinterName = "")
        {
            var printTimeout = new TimeSpan(0, 30, 0);
            var printer = new PDFtoPrinterPrinter();
            if (string.IsNullOrEmpty(networkPrinterName))
                networkPrinterName = this.PrinterName;
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
            {
                try
                { 
                    System.Diagnostics.Process.Start($"C:\\Program Files\\QZ Tray\\{this.QZName}");
                    return;
                }
                catch { }
                
                throw new Exception($"{this.QZName} no encontrado");

            }
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