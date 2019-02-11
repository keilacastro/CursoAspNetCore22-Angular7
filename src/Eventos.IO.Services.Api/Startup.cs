using AutoMapper;
using Eventos.IO.Domain.Interfaces;
using Eventos.IO.Infra.CrossCutting.AspNetFilters;
using Eventos.IO.Infra.CrossCutting.Bus;
using Eventos.IO.Infra.CrossCutting.Identity.Data;
using Eventos.IO.Infra.CrossCutting.Identity.Models;
using Eventos.IO.Infra.CrossCutting.Identity.Security;
using Eventos.IO.Infra.CrossCutting.IoC;
using Eventos.IO.Services.Api.Configurations;
using Eventos.IO.Services.Api.Extensions;
using Eventos.IO.Services.Api.Middleares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Swagger;

namespace Eventos.IO.Services.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // Logger
            services.AddLogging(options =>
            {
                options.AddConsole();
                options.AddDebug();
            });

            // Configurando o uso da classe de contexto para
            // acesso às tabelas do ASP.NET Identity Core
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("DefaultConnection")));

            // Ativando a utilização do ASP.NET Identity, a fim de
            // permitir a recuperação de seus objetos via injeção de dependências
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();


            var signinConfigurations = new SigningCredentialsConfigurations();
            services.AddSingleton(signinConfigurations);

            var tokenConfigurations = new JwtTokenConfigurations();
            new ConfigureFromConfigurationOptions<JwtTokenConfigurations>(
                    Configuration.GetSection(nameof(JwtTokenConfigurations)))
                        .Configure(tokenConfigurations);
            services.AddSingleton(tokenConfigurations);

            // Aciona a extensão que irá configurar o uso de
            // autenticação e autorização via tokens
            services.AddJwtSecurity(signinConfigurations, tokenConfigurations);

            services.AddOptions();
            services.AddMvc(options =>
            {
                options.OutputFormatters.Remove(new XmlDataContractSerializerOutputFormatter());
                options.UseCentralRoutePrefix(new RouteAttribute("api/v{version}"));

                // Policy de configuração do Token
                var policy = new AuthorizationPolicyBuilder()
                    .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .Build();

                // Adiciona a policy no filtro de autenticação
                options.Filters.Add(new AuthorizeFilter(policy));
                options.Filters.Add(new ServiceFilterAttribute(typeof(GlobalActionLoggerFilter)));
            }).AddJsonOptions(options =>
            {
                // remove valores nulos do retorno da API
                options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            })
            .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            services.AddHttpContextAccessor();

            // Aciona o automapper
            services.AddAutoMapper();

            // Ativa o serviço de documentação do Swagger;
            services.AddSwaggerGen(s =>
            {
                // Configura detalhes como a versão da API
                s.SwaggerDoc("v1",
                    new Info
                    {
                        Title = "Eventos.IO API",
                        Version = "v1",
                        Description = "Exemplo de API REST criada com o ASP.NET Core",
                        TermsOfService = "",
                        Contact = new Contact { Name = "Patrick Segantine", Email = "", Url = "http://projetox.io/licensa" },
                        License = new License { Name = "MIT", Url = "http://projetox.io/licensa" }
                    });
            });

            // Registrar todos os DI
            RegisterServices(services);
        }

        public void Configure(IApplicationBuilder app,
                              IHostingEnvironment env,
                              IHttpContextAccessor accessor)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // aula 19 - 2h22min
            app.UseCors(c =>
            {
                c.AllowAnyHeader();
                c.AllowAnyMethod();
                c.AllowAnyOrigin();
            });

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseAuthentication();
            app.UseMvc();

            // Habilita os middlewares que permitem a utilização do Swagger.
            //app.UseSwaggerAuthorized(); // bloqueia o acesso a usuários não logados
            app.UseSwagger();
            app.UseSwaggerUI(s =>
            {
                s.SwaggerEndpoint("/swagger/v1/swagger.json", "Eventos.IO API v1.0");
            });

            InMemoryBus.ContainerAccessor = () => accessor.HttpContext.RequestServices;
        }

        #region Helpers

        private static void RegisterServices(IServiceCollection services)
        {
            NativeInjectorBootStrapper.RegisterServices(services);
        }

        #endregion
    }
}
