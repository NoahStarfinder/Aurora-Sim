using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using OpenSim.CAPS;
using Nwc.XmlRpc;
using System.Collections;

namespace OpenSim.Servers
{
    public class BaseHttpServer
    {
        protected Thread m_workerThread;
        protected HttpListener m_httpListener;
        protected Dictionary<string, IRestHandler> m_restHandlers = new Dictionary<string, IRestHandler>();
        protected Dictionary<string, XmlRpcMethod> m_rpcHandlers = new Dictionary<string, XmlRpcMethod>();
        protected int m_port;

        public BaseHttpServer(int port)
        {
            m_port = port;
        }

        public bool AddRestHandler(string path, IRestHandler handler)
        {
            if (!this.m_restHandlers.ContainsKey(path))
            {
                this.m_restHandlers.Add(path, handler);
                return true;
            }

            //must already have a handler for that path so return false
            return false;
        }

        public bool AddXmlRPCHandler(string method, XmlRpcMethod handler)
        {
            if (!this.m_rpcHandlers.ContainsKey(method))
            {
                this.m_rpcHandlers.Add(method, handler);
                return true;
            }

            //must already have a handler for that path so return false
            return false;
        }

        protected virtual string ProcessXMLRPCMethod(string methodName, XmlRpcRequest request)
        {
            XmlRpcResponse response;
            
            XmlRpcMethod method;
            if( this.m_rpcHandlers.TryGetValue( methodName, out method ) )
            {
                response = method(request);
            }
            else
            {
                response = new XmlRpcResponse();
                Hashtable unknownMethodError = new Hashtable();
                unknownMethodError["reason"] = "XmlRequest"; ;
                unknownMethodError["message"] = "Unknown Rpc request";
                unknownMethodError["login"] = "false";
                response.Value = unknownMethodError;
            }

            return XmlRpcResponseSerializer.Singleton.Serialize(response);
        }

        protected virtual string ParseREST(string requestBody, string requestURL, string requestMethod)
        {
            string[] path;
            string pathDelimStr = "/";
            char[] pathDelimiter = pathDelimStr.ToCharArray();
            path = requestURL.Split(pathDelimiter);

            string responseString = "";

            //path[0] should be empty so we are interested in path[1]
            if (path.Length > 1)
            {
                if ((path[1] != "") && (this.m_restHandlers.ContainsKey(path[1])))
                {
                    responseString = this.m_restHandlers[path[1]].HandleREST(requestBody, requestURL, requestMethod);
                }
            }

            return responseString;
        }

        protected virtual string ParseLLSDXML(string requestBody)
        {
            // dummy function for now - IMPLEMENT ME!
            return "";
        }

        protected virtual string ParseXMLRPC(string requestBody)
        {
            string responseString = String.Empty;

            try
            {
                XmlRpcRequest request = (XmlRpcRequest)(new XmlRpcRequestDeserializer()).Deserialize(requestBody);

                string methodName = request.MethodName;

                responseString = ProcessXMLRPCMethod(methodName, request );
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return responseString;
        }

        public virtual void HandleRequest(Object stateinfo)
        {
            HttpListenerContext context = (HttpListenerContext)stateinfo;

            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            response.KeepAlive = false;
            response.SendChunked = false;

            System.IO.Stream body = request.InputStream;
            System.Text.Encoding encoding = System.Text.Encoding.UTF8;
            System.IO.StreamReader reader = new System.IO.StreamReader(body, encoding);

            string requestBody = reader.ReadToEnd();
            body.Close();
            reader.Close();

            //Console.WriteLine(request.HttpMethod + " " + request.RawUrl + " Http/" + request.ProtocolVersion.ToString() + " content type: " + request.ContentType);
            //Console.WriteLine(requestBody);

            string responseString = "";
            switch (request.ContentType)
            {
                case "text/xml":
                    // must be XML-RPC, so pass to the XML-RPC parser

                    responseString = ParseXMLRPC(requestBody);
                    responseString = Regex.Replace(responseString, "utf-16", "utf-8");
                    
                    response.AddHeader("Content-type", "text/xml");
                    break;

                case "application/xml":
                    // probably LLSD we hope, otherwise it should be ignored by the parser
                    responseString = ParseLLSDXML(requestBody);
                    response.AddHeader("Content-type", "application/xml");
                    break;

                case "application/x-www-form-urlencoded":
                    // a form data POST so send to the REST parser
                    responseString = ParseREST(requestBody, request.RawUrl, request.HttpMethod);
                    response.AddHeader("Content-type", "text/html");
                    break;

                case null:
                    // must be REST or invalid crap, so pass to the REST parser
                    responseString = ParseREST(requestBody, request.RawUrl, request.HttpMethod);
                    response.AddHeader("Content-type", "text/html");
                    break;

            }

            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            System.IO.Stream output = response.OutputStream;
            response.SendChunked = false;
            response.ContentLength64 = buffer.Length;
            output.Write(buffer, 0, buffer.Length);
            output.Close();
        }

        public void Start()
        {
            OpenSim.Framework.Console.MainConsole.Instance.WriteLine("BaseHttpServer.cs: Starting up HTTP Server");

            m_workerThread = new Thread(new ThreadStart(StartHTTP));
            m_workerThread.IsBackground = true;
            m_workerThread.Start();
        }

        private void StartHTTP()
        {
            try
            {
                OpenSim.Framework.Console.MainConsole.Instance.WriteLine("BaseHttpServer.cs: StartHTTP() - Spawned main thread OK");
                m_httpListener = new HttpListener();

                m_httpListener.Prefixes.Add("http://+:" + m_port + "/");
                m_httpListener.Start();

                HttpListenerContext context;
                while (true)
                {
                    context = m_httpListener.GetContext();
                    ThreadPool.QueueUserWorkItem(new WaitCallback(HandleRequest), context);
                }
            }
            catch (Exception e)
            {
                OpenSim.Framework.Console.MainConsole.Instance.WriteLine(e.Message);
            }
        }
    }
}
