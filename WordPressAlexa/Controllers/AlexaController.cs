using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WordPressPCL;
using Microsoft.Extensions.Configuration;
using Alexa.NET.Request;
using Alexa.NET.Request.Type;
using Alexa.NET.Response;
using Alexa.NET;
using System.Text;
using WordPressPCL.Utility;
using WordPressAlexa.Utility;

namespace WordPressAlexa.Controllers
{
    [Produces("application/json")]
    [Route("api/Alexa")]
    public class AlexaController : Controller
    {
        private readonly WordPressClient _client;
        private readonly IConfiguration _config;
        private readonly string _appid;

        public AlexaController(IConfiguration config)
        {
            _config = config;

            // get values from config
            var wordpressuri = _config.GetValue<string>("WordPressUri");
            _appid = _config.GetValue<string>("SkillApplicationId");

            // create wordpress client
            _client = new WordPressClient(wordpressuri)
            {
                AuthMethod = WordPressPCL.Models.AuthMethod.JWT
            };
        }

        [HttpPost]
        public async Task<IActionResult> HandleSkillRequest([FromBody]SkillRequest input)
        {
            // Security check
            if(input.Session.Application.ApplicationId != _appid)
            {
                return BadRequest();
            }
            
            var requestType = input.GetRequestType();
            if (requestType == typeof(IntentRequest))
            {
                var response = await HandleIntentsAsync(input);
                return Ok(response);
            }
            else if (requestType == typeof(LaunchRequest))
            {
                var speech = new SsmlOutputSpeech();
                var sb = await GetHeadlinesAsync();
                speech.Ssml = sb.ToString();
                var finalResponse = ResponseBuilder.Tell(speech);

                return Ok(finalResponse);
            }
            else if (requestType == typeof(AudioPlayerRequest))
            {
                return Ok(ErrorResponse());
            }

            return Ok(ErrorResponse());
        }

        /// <summary>
        /// Handles different intents of the Alexa skill.
        /// </summary>
        /// <param name="input">current skill request</param>
        /// <returns></returns>
        private async Task<SkillResponse> HandleIntentsAsync(SkillRequest input)
        {
            var intentRequest = input.Request as IntentRequest;
            var speech = new SsmlOutputSpeech();

            // check the name to determine what you should do
            if (intentRequest.Intent.Name.Equals("Headlines"))
            {
                var sb = await GetHeadlinesAsync();
                speech.Ssml = sb.ToString();

                // create the response using the ResponseBuilder
                var finalResponse = ResponseBuilder.Tell(speech);
                return finalResponse;
            }
            else if (intentRequest.Intent.Name.Equals("LatestPost"))
            {
                var sb = await GetLatestPost();
                speech.Ssml = sb.ToString();
                var finalResponse = ResponseBuilder.Tell(speech);
                return finalResponse;
            }
            else
            {
                return ErrorResponse();
            }
        }

        /// <summary>
        /// Gets the latest post from WordPress.
        /// </summary>
        /// <returns></returns>
        private async Task<StringBuilder> GetLatestPost()
        {
            // get values from config
            var username = _config.GetValue<string>("WordPressUsername");
            var password = _config.GetValue<string>("WordPressPassword");
            await _client.RequestJWToken(username, password);
            
            StringBuilder sb = new StringBuilder();

            var latestPosts = await _client.Posts.Query(new PostsQueryBuilder()
            {
                Context = WordPressPCL.Models.Context.Edit,
            }, true);
            var post = latestPosts.FirstOrDefault();
            
            if(post != null)
            {
                var content = Helpers.ScrubHtml(post.Content.Raw);
                var title = Helpers.ScrubHtml(post.Title.Rendered);
                sb.Append($"<speak>{title}<break time=\"1s\"/>{content}</speak>");
            }
            else
            {
                sb.Append("<speak>Irgendwas ist schiefgelaufen.</speak>");
            }

            return sb;
        }

        /// <summary>
        /// Gets the latest Headlines from WordPress.
        /// </summary>
        /// <returns></returns>
        private async Task<StringBuilder> GetHeadlinesAsync()
        {
            var posts = await _client.Posts.Get();

            StringBuilder sb = new StringBuilder();
            sb.Append("<speak>Hier die Schlagzeilen.<break time=\"1s\"/>");

            // build the speech response 
            for (int i = 0; i < 5; i++)
            {
                sb.Append($"{posts.ElementAt(i).Title.Rendered}");
                if (i == 4)
                {
                    sb.Append(".");
                }
                sb.Append("<break time=\"1s\"/>");
            }
            sb.Append("</speak>");

            return sb;
        }

        /// <summary>
        /// Creates an error skill response.
        /// </summary>
        /// <returns></returns>
        private SkillResponse ErrorResponse()
        {
            var speech = new SsmlOutputSpeech
            {
                Ssml = "<speak>Irgendwas ist schiefgelaufen.</speak>"
            };
            return ResponseBuilder.Tell(speech);
        }
    }
}