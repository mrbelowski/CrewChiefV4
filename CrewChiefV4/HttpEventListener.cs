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
            return "OK";
        }

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/enablePacenotes")]
        public String enablePacenotes()
        {
            WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.OK;
            return "OK";
        }

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/disablePacenotes")]
        public String disablePacenotes()
        {
            WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.OK;
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
    public class HttpEventListener : EventListener
    {
        private RestController controller;

        public override Boolean autoStart()
        {
            return true;
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
    }
}
