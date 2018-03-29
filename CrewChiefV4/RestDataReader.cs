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
        [WebGet(UriTemplate="test")]
        public String test()
        {
            //WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.OK;
            return "hello";
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
        System.ServiceModel.ServiceHost serviceHost;
        private Boolean pacenotesEnabled = false;
        private String latestRequestedPacenotesSet;

        // set to true when a Rest call is made, then false once that data has been read by the main loop
        private Boolean hasNewData = false;

        private RestController controller;

        public override Boolean autoStart()
        {
            return true;
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
            if (hasNewData)
            {
                // move received data into the remoteData object
                hasNewData = false;
            }
            return remoteData;
        }
    }
}
