using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Xml;
using System.IO;

namespace DeveloperTestASockets
{

    //.Net Core 3.0 Console App, C# 7.2

    class DevTestA
    {

        static async Task Main(string[] args)
        {

            //Main process logic starts here from the try-catch block 
            try
            {
                Console.WriteLine("Start Time:  {0}", DateTime.Now);
                await ExcecuteParallelTasksAsync();
                Console.WriteLine("End Time:  {0}", DateTime.Now);
                Console.ReadLine();

            }

            catch (Exception Ae)
            {
                Console.WriteLine(Ae.Message);

            }


        }



        private static async Task ExcecuteParallelTasksAsync()

        {

            //Declaring Constants to be used
            const string hostName = "216.38.192.141";
            const int port = 8765;

            //Enumerable variable for 500 xml messages to be send concurrently.
            IEnumerable<int> noMessagesToSend = Enumerable.Range(1, 500);

            //Create a Task List
            var tasks = new List<Task>();

            //String to hold the final result
            var finalResult = new StringBuilder();

            //ThreadSafe Concurrent dictionary to hold <requestID> and substring(2,1) of <message>
            var requestMessageDict = new ConcurrentDictionary<int, string>();



            //Stopwatch for time keeping
            var sW = new Stopwatch();
            sW.Start();

            //Calls NWCommunicatorAsync method asynchronously to send and receive 500 messages
            //Returns a key(<requestID>) value(<message>) pair and adds it to the threadsafe dictionary

            foreach (int i in noMessagesToSend)
            {
                tasks.Add(Task.Run(

                    async () =>
                    {
                        var requestKeymessageValue = await NWCommunicatorAsync(i, hostName, port);
                        requestMessageDict.TryAdd(requestKeymessageValue.Key, requestKeymessageValue.Value);

                    }

                    ));

            }

            //Wait for all the above tasks to complete before computing the final result.
            //Sort the dictionary in Descending order and display results

            try
            {

                await Task.WhenAll(tasks.ToArray());

                sW.Stop();
                Console.WriteLine("Elapsed Time in sending/receiving Xml messages : {0}", sW.Elapsed);

                var requestKeysmessageValues = requestMessageDict.ToArray().OrderByDescending(x => x.Key);

                foreach (var requestMessage in requestKeysmessageValues)
                {
                    finalResult.Append(requestMessage.Value);

                }

                Console.WriteLine(finalResult.ToString());

            }

            catch (AggregateException taskException)
            {

                throw taskException.Flatten();

            }


        }

        //Static method to open a TCP/IP client to communicate with TurnCommerce Server
        //Performs xml parsing and returns a key-value pair indicating the requestID and message

        private static async Task<KeyValuePair<int, string>> NWCommunicatorAsync(int k, string hostname, int port)

        {

            using (TcpClient DevClient = new TcpClient(hostname, port))
            {
                using (NetworkStream DevStream = DevClient.GetStream())
                {
                    var xmlTextToSend = new StringBuilder();
                    var xmlTextReceived = new StringBuilder();
                    var xmlSentDoc = new XmlDocument();
                    var xmlReceivedDoc = new XmlDocument();
                    //message to be send to turncommerce
                    xmlTextToSend.Append("<?xml version=\"1.0\" encoding=\"ISO-8859-1\"?><request><requestID>1</requestID></request>");
                    xmlSentDoc.LoadXml(xmlTextToSend.ToString());
                    xmlSentDoc.DocumentElement.SelectSingleNode("//requestID").InnerText = Convert.ToString(k);
                    xmlTextToSend.Clear();
                    xmlTextToSend.Append(xmlSentDoc.OuterXml);

                    byte[] bytesToServer = ASCIIEncoding.ASCII.GetBytes(xmlTextToSend.ToString());
                    await DevStream.WriteAsync(bytesToServer, 0, bytesToServer.Length);

                    byte[] bytesFromServer = new byte[DevClient.ReceiveBufferSize];
                    int lengthbytesFromServer = await DevStream.ReadAsync(bytesFromServer, 0, DevClient.ReceiveBufferSize);

                    xmlTextReceived.Append(Encoding.ASCII.GetString(bytesFromServer, 0, lengthbytesFromServer));
                    xmlReceivedDoc.LoadXml(xmlTextReceived.ToString());

                    //Console.WriteLine("Received from Server: {0}", xmlTextReceived.ToString());

                    return (new KeyValuePair<int, string>(Convert.ToInt32(xmlReceivedDoc.DocumentElement.SelectSingleNode("//requestID").InnerText), xmlReceivedDoc.DocumentElement.SelectSingleNode("//message").InnerText.Substring(2, 1)));


                }

            }

        }


    }
}
