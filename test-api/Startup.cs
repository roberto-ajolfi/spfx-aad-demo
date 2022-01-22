using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.Identity.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace test_api
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment hostEnvironment)
        {
            Configuration = configuration;
            Environment = hostEnvironment;
        }

        public IConfiguration Configuration { get; }

        /// <summary>
        /// Current environment
        /// </summary>
        public IWebHostEnvironment Environment { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "test_api", Version = "v1" });
            });

            services.AddCors();

            #region Bearer Authentication & Basic Authentication

            //Costanti di configurazione (DA SPOSTARE in appsettings.json)
            const string ISSUER = "https://sts.windows.net/e1ae344c-918c-4bc7-a6db-b49d0828aed3/";
            const bool DISABLE_HTTPS = true;
            const string UNIQUE_NAME_CLAIM_NAME = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn";
            const string SHAREPOINT_AUDIENCE_NAME = "7d34b4bd-f0c7-4d5d-99c3-35cd3a929625";

            //Impostazione per GDPR per evitare l'uso di HTTPS
            //ATTENZIONE! Quando siete in produzione con HTTPS impostare a false!!!
            IdentityModelEventSource.ShowPII = DISABLE_HTTPS;

            //Abilitazione dell'autenticazione usando JWT (Bearer...)
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })

            //Impostazione di OpenId Connect (con JWT Bearer)
            .AddJwtBearer(o =>
            {
                //Impostazione dell'authority e delle validazioni custom (HTTPS = disattivato!)
                o.Authority = ISSUER;
                o.RequireHttpsMetadata = !DISABLE_HTTPS; //Attenzione alla negazione

                //Opzioni aggiuntive di validazione del token
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    //Specifico il campo da usare per lo username dell'IIdentity di ASP.NET
                    NameClaimType = UNIQUE_NAME_CLAIM_NAME,

                    //Validazione delle audiences (multiple)
                    ValidateAudience = true,
                    ValidAudiences = new string[]
                    {
                        //Questa è una applicazione "Client Public" quindi non ho un Client Secret!
                        SHAREPOINT_AUDIENCE_NAME

                        //Qui sotto ci vanno tutte le audience che hanno il permesso
                        //di accedere a Graph.Api. Possono anche essere specificate
                        //da configurazione applicativa!
                    },

                    //Validazione del periodo di validità (con tolleranza di 5 minuti)
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(5),

                    //Elenco delle chiavi di cifratura (i client secret)
                    //IssuerSigningKeys = new SecurityKey[] {

                    //    //Chiave di configurazione del client "native"
                    //    new SymmetricSecurityKey(Encoding.UTF8.GetBytes("here_client_secret")),

                    //    //ATTENZIONE! Per l'applicazioni Angular e Sharepoint (che è assimilato
                    //    //a una SPA) non è necessario un clientSecret
                    //},

                    ValidateIssuer = true,
                    ValidIssuer = ISSUER,
                };

                //Gestione degli eventi di autenticazione
                o.Events = new JwtBearerEvents()
                {
                    //Con autenticazione fallita
                    OnAuthenticationFailed = c =>
                    {
                        //Impostazione di "nessun risultato"
                        //con status 500 e contentuto text/plain
                        c.NoResult();
                        c.Response.StatusCode = 500;
                        c.Response.ContentType = "text/plain";

                        //Se siamo in sviluppo
                        if (Environment.IsDevelopment())
                        {
                            //Scrittura dell'eccezione nell'output
                            return c.Response.WriteAsync(c.Exception.ToString());
                        }

                        //Se non sono in sviluppo, semplice informativa di errore di autenticazione
                        return c.Response.WriteAsync("An error occured processing your authentication.");
                    },

                    //Scatta quando arriva una nuova richiesta HTTP sul server
                    OnMessageReceived = c =>
                    {
                        //Tracciamento di messaggio ricevuto
                        return Task.FromResult(0);
                    },

                    //Scatta quando sta per essere emesso un 403-Forbidden
                    OnForbidden = c =>
                    {
                        //Tracciamento di 403 Forbidden
                        return Task.FromResult(0);
                    }
                };
            });

            services.AddAuthorization(options =>
            {
                var builder = new AuthorizationPolicyBuilder("Bearer");
                builder = builder.RequireAuthenticatedUser();
                options.DefaultPolicy = builder.Build();
            });
            #endregion
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "test_api v1"));
            }

            app.UseCors(options => options
                .SetIsOriginAllowed(x => _ = true)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowAnyOrigin());

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
