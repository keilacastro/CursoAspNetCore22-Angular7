using Eventos.IO.Infra.CrossCutting.Identity.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Eventos.IO.Services.Api.Extensions
{
    public static class JwtSecurityExtension
    {
        public static IServiceCollection AddJwtSecurity(
            this IServiceCollection services,
            SigningConfigurations signinConfigurations,
            JwtTokenConfigurations tokenConfigurations)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                var validatonParameters = options.TokenValidationParameters;
                validatonParameters.IssuerSigningKey = signinConfigurations.Key;
                validatonParameters.ValidateAudience = true;
                validatonParameters.ValidAudience = tokenConfigurations.Audience;
                validatonParameters.ValidateIssuer = true;
                validatonParameters.ValidIssuer = tokenConfigurations.Issuer;

                // Valida a assinatura de um token recebido
                validatonParameters.ValidateIssuerSigningKey = true;

                validatonParameters.RequireExpirationTime = true;

                // Verifica se um token recebido ainda é válido
                validatonParameters.ValidateLifetime = true;

                // Tempo de tolerância para a expiração de um token (utilizado
                // caso haja problemas de sincronismo de horário entre diferentes
                // computadores envolvidos no processo de comunicação)
                validatonParameters.ClockSkew = TimeSpan.Zero;
            });

            // Ativa o uso do token como forma de autorizar o acesso
            // a recursos deste projeto
            services.AddAuthorization(options =>
            {
                options.AddPolicy("Bearer", new AuthorizationPolicyBuilder()
                    .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .Build());

                // Policy de Autorização 
                // aula 19 - 2h16min
                options.AddPolicy("PodeConsultar", policy => policy.RequireClaim("Eventos", "Consultar"));
                options.AddPolicy("PodeGravar", policy => policy.RequireClaim("Eventos", "Gravar"));
                options.AddPolicy("PodeExcluir", policy => policy.RequireClaim("Eventos", "Excluir"));
            });

            return services;
        }
    }
}
