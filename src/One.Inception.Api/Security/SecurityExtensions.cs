using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;

namespace One.Inception.Api.Security;

public static class Security
{
    private static readonly string JwtSectionName = "Inception:Api:JwtTenantConfig";
    private static bool authenticationEnabled = false;

    public static IServiceCollection AddSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddBasicSecurity();
        services.AddMvcSecurity(configuration);
        services.AddJwtSecurity(configuration);

        return services;
    }

    private static IServiceCollection AddBasicSecurity(this IServiceCollection services)
    {
        services.AddAuthorization(o =>
        {
            o.AddPolicy(BasicAuthorizationAttribute.PolicyName, new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        });

        services
            .AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>(BasicAuthorizationAttribute.AuthenticationSchema, options => { });

        return services;
    }

    private static IServiceCollection AddMvcSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMvc(o =>
        {
            HttpNoContentOutputFormatter noContentFormatter = o.OutputFormatters.OfType<HttpNoContentOutputFormatter>().FirstOrDefault();
            if (noContentFormatter != null)
            {
                noContentFormatter.TreatNullValueAsNoContent = false;
            }
        })
       .AddNewtonsoftJson(options =>
       {
           options.SerializerSettings.Converters.Add(new ReadOnlyMemoryJsonConverter<byte>());
       });

        return services;
    }

    private static IServiceCollection AddJwtSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        authenticationEnabled = configuration.GetSection(JwtSectionName).Exists();

        if (authenticationEnabled)
        {
            List<TenantJWTOptions> tenantConfigurations = new List<TenantJWTOptions>();
            configuration.GetRequiredSection(JwtSectionName).Bind(tenantConfigurations);

            AuthenticationBuilder builder = services.AddAuthentication(opt =>
            {
                opt.DefaultScheme = "MultitenantJwtBearerSchema";
                opt.DefaultChallengeScheme = "MultitenantJwtBearerSchema";
            });

            foreach (TenantJWTOptions tenantConfig in tenantConfigurations)
            {
                builder = builder.AddTenantJwtBearer(tenantConfig.Name, tenantConfig.JwtBearerOptions);
            }

            builder.AddPolicyScheme("MultitenantJwtBearerSchema", "MultitenantJwtBearerSchema", opt =>
            {
                opt.ForwardDefaultSelector = context =>
                {
                    string authorization = context.Request.Headers[HeaderNames.Authorization];
                    if (string.IsNullOrEmpty(authorization) == false && authorization.StartsWith("Bearer ", System.StringComparison.OrdinalIgnoreCase))
                    {
                        string token = authorization.Substring("Bearer ".Length).Trim();
                        JwtSecurityTokenHandler jwtHandler = new JwtSecurityTokenHandler();

                        if (jwtHandler.CanReadToken(token))
                        {
                            if (jwtHandler.ReadJwtToken(token).Payload.TryGetValue("tenant", out object tenant))
                                return tenant.ToString();
                        }
                    }

                    return BasicAuthorizationAttribute.AuthenticationSchema;
                };
            });

            services.AddAuthorization();
        }

        return services;
    }

    private static AuthenticationBuilder AddTenantJwtBearer(this AuthenticationBuilder authenticationBuilder, string tenant, JwtBearerOptions jwtBearerOptions)
    {
        return authenticationBuilder.AddJwtBearer(tenant, o =>
        {
            o.Authority = jwtBearerOptions.Authority;
            o.Audience = jwtBearerOptions.Audience;
            o.RequireHttpsMetadata = jwtBearerOptions.RequireHttpsMetadata;
            o.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidIssuer = jwtBearerOptions.Authority,
                ValidateIssuer = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(5),
                ValidateIssuerSigningKey = true,
                ValidAudience = jwtBearerOptions.Audience,
                ValidateAudience = string.IsNullOrEmpty(jwtBearerOptions.Audience) == false
            };
        });
    }
}

public class TenantJWTOptions
{
    public string Name { get; set; }
    public JwtBearerOptions JwtBearerOptions { get; set; }
}

sealed class ReadOnlyMemoryJsonConverter<T> : JsonConverter<ReadOnlyMemory<T>>
{
    public override ReadOnlyMemory<T> ReadJson(JsonReader reader, Type objectType, ReadOnlyMemory<T> existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var buffer = serializer.Deserialize<T[]>(reader);
        return new ReadOnlyMemory<T>(buffer);
    }

    public override void WriteJson(JsonWriter writer, ReadOnlyMemory<T> value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value.ToArray());
    }
}
