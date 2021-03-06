﻿using System.Linq;
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
using WordPressPCL.Models;

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
            _client = new WordPressClient(wordpressuri) { AuthMethod = AuthMethod.JWT };
        }

        [HttpPost]
        public async Task<IActionResult> HandleSkillRequest([FromBody]SkillRequest input)
        {
            // Security check
            if (input.Session.Application.ApplicationId != _appid)
                return BadRequest();

            var requestType = input.GetRequestType();

            if (requestType == typeof(IntentRequest))
            {
                var response = await HandleIntentsAsync(input);

                return Ok(response);
            }

            if (requestType == typeof(LaunchRequest))
            {
                var headlines = await GetHeadlinesAsync();
                var speech = new SsmlOutputSpeech { Ssml = headlines.ToString() };

                var finalResponse = ResponseBuilder.Tell(speech);

                return Ok(finalResponse);
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
            if (!(input.Request is IntentRequest intentRequest))
                return ErrorResponse();

            var speech = new SsmlOutputSpeech();

            // check the name to determine what you should do
            var intentName = intentRequest.Intent.Name;
            if (intentName.Equals("Headlines"))
            {
                var headlines = await GetHeadlinesAsync();
                speech.Ssml = headlines.ToString();

                // create the response using the ResponseBuilder
                var finalResponse = ResponseBuilder.Tell(speech);
                return finalResponse;
            }

            if (intentName.Equals("LatestPost"))
            {
                var sb = await GetLatestPostAsync();
                speech.Ssml = sb.ToString();
                var finalResponse = ResponseBuilder.Tell(speech);
                return finalResponse;
            }

            return ErrorResponse();
        }

        /// <summary>
        /// Gets the latest post from WordPress.
        /// </summary>
        /// <returns></returns>
        private async Task<StringBuilder> GetLatestPostAsync()
        {
            // get values from config
            var username = _config.GetValue<string>("WordPressUsername");
            var password = _config.GetValue<string>("WordPressPassword");
            await _client.RequestJWToken(username, password);

            var stringBuilder = new StringBuilder();

            var latestPosts = await _client.Posts.Query(new PostsQueryBuilder { Context = WordPressPCL.Models.Context.Edit }, true);
            var post = latestPosts.FirstOrDefault();

            if (post != null)
            {
                var content = Helpers.ScrubHtml(post.Content.Raw);
                var title = Helpers.ScrubHtml(post.Title.Rendered);

                stringBuilder.Append($"<speak>{title}<break time=\"1s\"/>{content}</speak>");
            }
            else
            {
                stringBuilder.Append("<speak>Irgendwas ist schiefgelaufen.</speak>");
            }

            return stringBuilder;
        }

        /// <summary>
        /// Gets the latest Headlines from WordPress.
        /// </summary>
        /// <returns></returns>
        private async Task<StringBuilder> GetHeadlinesAsync()
        {
            var stringBuilder = new StringBuilder();
            var posts = await _client.Posts.Get();
            var enumerableOfPosts = posts as Post[] ?? posts.ToArray();

            stringBuilder.Append("<speak>Hier die Schlagzeilen.<break time=\"1s\"/>");


            // build the speech response 
            for (var i = 0; i < 5; i++)
            {

                stringBuilder.Append($"{enumerableOfPosts.ElementAt(i).Title.Rendered}");
                if (i == 4)
                    stringBuilder.Append(".");

                stringBuilder.Append("<break time=\"1s\"/>");
            }

            stringBuilder.Append("</speak>");

            return stringBuilder;
        }

        /// <summary>
        /// Creates an error skill response.
        /// </summary>
        /// <returns></returns>
        private static SkillResponse ErrorResponse()
        {
            var speech = new SsmlOutputSpeech { Ssml = "<speak>Irgendwas ist schiefgelaufen.</speak>" };
            return ResponseBuilder.Tell(speech);
        }
    }
}