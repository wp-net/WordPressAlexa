using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WordPressPCL;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using Alexa.NET.Request;
using Alexa.NET.Request.Type;
using Alexa.NET.Response;
using Alexa.NET;
using System.Text;

namespace WordPressAlexa.Controllers
{
    [Produces("application/json")]
    [Route("api/Alexa")]
    public class AlexaController : Controller
    {
        private WordPressClient _client;
        private IConfiguration _config;

        public AlexaController(IConfiguration config)
        {
            _config = config;
            var wordpressuri = _config.GetValue<string>("WordPressUri");
            _client = new WordPressClient(wordpressuri);
        }

        [HttpPost]
        public async Task<SkillResponse> HandleSkillRequest([FromBody]SkillRequest input)
        {
            var requestType = input.GetRequestType();
            if (requestType == typeof(IntentRequest))
            {
                return await HandleIntents(input);
            }
            else if (requestType == typeof(LaunchRequest))
            {
                return ErrorResponse();
            }
            else if (requestType == typeof(AudioPlayerRequest))
            {
                return ErrorResponse();
            }
            return ErrorResponse();


        }

        private async Task<SkillResponse> HandleIntents(SkillRequest input)
        {
            var intentRequest = input.Request as IntentRequest;
            var speech = new SsmlOutputSpeech();

            // check the name to determine what you should do
            if (intentRequest.Intent.Name.Equals("Headlines"))
            {
                var posts = await _client.Posts.Get();
                StringBuilder sb = new StringBuilder();
                sb.Append("<speak>Hier die Schlagzeilen.<break time=\"1s\"/>");
                // build the speech response 
                for (int i = 0; i < 5; i++)
                {
                    sb.Append($"{posts.ElementAt(i).Title.Rendered}<break time=\"1s\"/>");
                }
                sb.Append("</speak>");
                speech.Ssml = sb.ToString();

                // create the response using the ResponseBuilder
                var finalResponse = ResponseBuilder.Tell(speech);
                return finalResponse;
            } else
            {
                return ErrorResponse();
            }
        }

        private SkillResponse ErrorResponse()
        {
            var speech = new SsmlOutputSpeech();
            speech.Ssml = "<speak>Irgendwas ist schiefgelaufen.</speak>";
            return ResponseBuilder.Tell(speech);
        }
    }
}