using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using Azure.Identity;

namespace test_api.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var rng = new Random();
            var result = Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();

            return Ok(result);
        }

        [HttpGet("grapheasy")]
        public async Task<IActionResult> GetGraphDataEasy()
        {
            try
            {
                string token = Request.Headers.Authorization.ToString().Replace("Bearer ", "");
                
                // funziona se il parametro token è un MS Graph token
                // use the token to create an authenticated client
                var graphClient = new GraphServiceClient(
                   new DelegateAuthenticationProvider(
                       async (requestMessage) =>
                       {
                           await Task.Run(() =>
                           {
                               requestMessage.Headers.Authorization = new AuthenticationHeaderValue(
                                   "Bearer",
                                   token);
                           });
                       }));

                var graphUserData = await graphClient.Me
                    .Request()
                    .Select(u => new
                    {
                        u.DisplayName,
                        u.JobTitle,
                        u.EmployeeId
                    })
                    .GetAsync();

                return Ok(new { graphUserData.DisplayName, graphUserData.JobTitle, graphUserData.EmployeeId });
            }
            catch(Exception ex)
            {
                return BadRequest(new { ex, ex.InnerException });
            }
        }

        [HttpGet("graph")]
        public async Task<IActionResult> GetGraphData()
        {
            try
            {
                string token = Request.Headers.Authorization.ToString().Replace("Bearer ", "");

                var scopes = new[] { "User.Read" };
                
                var options = new TokenCredentialOptions
                {
                    AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
                };

                var cca = ConfidentialClientApplicationBuilder
                    .Create("7d34b4bd-f0c7-4d5d-99c3-35cd3a929625")
                    .WithTenantId("e1ae344c-918c-4bc7-a6db-b49d0828aed3")
                    .WithClientSecret("QdE7Q~ffxzzHhiwdajuPM165ah3G8-SUWLjnI")
                    .Build();

                // DelegateAuthenticationProvider is a simple auth provider implementation
                // that allows you to define an async function to retrieve a token
                // Alternatively, you can create a class that implements IAuthenticationProvider
                // for more complex scenarios
                var authProvider = new DelegateAuthenticationProvider(async (request) => {
                    // Use Microsoft.Identity.Client to retrieve token
                    var assertion = new UserAssertion(token);
                    var result = await cca.AcquireTokenOnBehalfOf(scopes, assertion).ExecuteAsync();

                    request.Headers.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", result.AccessToken);
                });

                var graphClient = new GraphServiceClient(authProvider);

                var graphUserData = await graphClient.Me
                    .Request()
                    .Select(u => new
                    {
                        u.DisplayName,
                        u.JobTitle,
                        u.EmployeeId
                    })
                    .GetAsync();

                return Ok(new { graphUserData.DisplayName, graphUserData.JobTitle, graphUserData.EmployeeId });
            }
            catch(Exception ex)
            {
                return BadRequest(new { ex, ex.InnerException });
            }
        }
    }
}
