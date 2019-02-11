using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Eventos.IO.Infra.CrossCutting.Identity.Data;
using Eventos.IO.Infra.CrossCutting.IoC;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Eventos.IO.Services.Api2
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


            var signinConfigurations = new SigningConfigurations();
            services.AddSingleton(signinConfigurations);

            var tokenConfigurations = new JwtTokenConfigurations();
            new ConfigureFromConfigurationOptions<JwtTokenConfigurations>(
                    Configuration.GetSection(nameof(JwtTokenConfigurations)))
                        .Configure(tokenConfigurations);
            services.AddSingleton(tokenConfigurations);

            // Aciona a extensão que irá configurar o uso de
            // autenticação e autorização via tokens
            services.AddJwtSecurity(signinConfigurations, tokenConfigurations);

            // Aciona o automapper
            services.AddAutoMapper();

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
            }).AddJsonOptions(options =>
            {
                // remove valores nulos do retorno da API
                options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            }).SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

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

            services.AddLogging(options =>
            {
                options.AddConsole();
                options.AddDebug();
            });

            // Registrar todos os DI
            RegisterServices(services);
        }

        
        public void Configure(
            IApplicationBuilder app, 
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
            app.UseSwaggerAuthorized(); // bloqueia o acesso a usuários não logados
            app.UseSwagger();
            app.UseSwaggerUI(s =>
            {
                s.SwaggerEndpoint("/swagger/v1/swagger.json",
                    "Eventos.IO API v1.0");
            });

            InMemoryBus.ContainerAccessor = () => accessor.HttpContext.RequestServices;
        }


        // Configuração de Injeção de Dependência
        private static void RegisterServices(IServiceCollection services)
        {
            NativeInjectorBootStrapper.RegisterServices(services);
        }
    }
}
