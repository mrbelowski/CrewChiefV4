using CrewChiefV4.GameState;
using System;
using System.ServiceModel;
using System.ServiceModel.Web;
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
            DriverTrainingService.stopPlayingPaceNotes();
            DriverTrainingService.setPacenotesSubfolder(name);
            if (CrewChief.currentGameState != null)
            {
                DriverTrainingService.loadPaceNotes(CrewChief.gameDefinition.gameEnum, CrewChief.currentGameState.SessionData.TrackDefinition.name,
                        CrewChief.currentGameState.carClass.carClassEnum);
            }
            return "OK";
        }

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/pacenotes/enable")]
        public String enablePacenotes()
        {
            WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.OK;
            if (!DriverTrainingService.isPlayingPaceNotes && CrewChief.currentGameState != null)
            {
                DriverTrainingService.loadPaceNotes(CrewChief.gameDefinition.gameEnum, CrewChief.currentGameState.SessionData.TrackDefinition.name,
                    CrewChief.currentGameState.carClass.carClassEnum);
            }
            return "OK";
        }

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/pacenotes/disable")]
        public String disablePacenotes()
        {
            WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.OK;
            DriverTrainingService.stopPlayingPaceNotes();
            return "OK";
        }
    }

    public class RestController
    {
        private int port = UserSettings.GetUserSettings().getInt("http_event_listener_port");

        private ServiceHost host;
        
        public void start()
        {
            Console.WriteLine("Listening for HTTP events on port " + port);
            host = new ServiceHost(typeof(RestService), new Uri("http://localhost:" + port));
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
