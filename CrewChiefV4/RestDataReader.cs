using CrewChiefV4.GameState;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel.Web;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace CrewChiefV4
{
    [ServiceContract]
    public class RestService
    {
        [OperationContract]
        [WebInvoke(Method="GET", UriTemplate="/pacenotes?name={name}")]
        public String changePacenotes(String name)
        {
            WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.OK;
            RestDataReader.changePacenotes(name);
            return "OK";
        }

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/enablePacenotes")]
        public String enablePacenotes()
        {
            WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.OK;
            RestDataReader.enablePacenotes();
            return "OK";
        }

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/disablePacenotes")]
        public String disablePacenotes()
        {
            WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.OK;
            RestDataReader.disablePacenotes();
            return "OK";
        }
    }

    public class RestController
    {
        private ServiceHost host;
        
        public void start()
        {
            host = new ServiceHost(typeof(RestService), new Uri("http://localhost:8080/"));
            var endpoint = host.AddServiceEndpoint(typeof(RestService), new WebHttpBinding(), "service");
            endpoint.EndpointBehaviors.Add(new WebHttpBehavior());
            host.Open();
        }

        public void stop()
        {
            host.Close();
        }
    }
    public class RestDataReader : RemoteDataReader
    {
        // set to true when a Rest call is made, then false once that data has been read by the main loop
        private static Boolean hasNewData = false;

        private RestController controller;

        private static String pacenotesNameToUpdate;

        private static Boolean pacenotesEnabled;

        public static void enablePacenotes() {
            RestDataReader.pacenotesEnabled = true;
            RestDataReader.hasNewData = true;
        }

        public static void disablePacenotes()
        {
            RestDataReader.pacenotesEnabled = false;
            RestDataReader.hasNewData = true;
        }

        public override Boolean autoStart()
        {
            return true;
        }

        public static void changePacenotes(String name)
        {
            RestDataReader.pacenotesNameToUpdate = name;
            RestDataReader.hasNewData = true;
        }

        public override void deactivate()
        {
            controller.stop();
            base.deactivate();
        }

        public override void activate(object activationData)
        {
            if (controller != null)
            {
                controller.stop();
            }
            controller = new RestController();
            controller.start();
            base.activate(activationData);
        }

        public override RemoteData getRemoteDataInternal(RemoteData remoteData, Object rawGameData)
        {
            if (RestDataReader.hasNewData)
            {
                // move received data into the remoteData object
                remoteData.restData.pacenotesEnabled = RestDataReader.pacenotesEnabled;
                if (RestDataReader.pacenotesNameToUpdate != null)
                {
                    remoteData.restData.pacenotesSet = RestDataReader.pacenotesNameToUpdate;
                }
                RestDataReader.hasNewData = false;
            }
            return remoteData;
        }
    }
}
