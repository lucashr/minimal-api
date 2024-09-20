using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using minimal_api.Dominio.DTOs;
using minimal_api.Dominio.Entidades;
using minimal_api.Dominio.Enums;
using minimal_api.Dominio.Interfaces;
using minimal_api.Dominio.ModelViews;
using minimal_api.Dominio.Servicos;
using minimal_api.Infraestrutura.Db;

namespace minimal_api
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public string key;
        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
            key = Configuration.GetSection("jwt").ToString()!;
        }

        public void ConfigureServices(IServiceCollection services)
        {

            services.AddAuthentication(options => {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>{
                options.TokenValidationParameters = new TokenValidationParameters{
                    ValidateLifetime = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            });

            services.AddAuthorization();

            services.AddScoped<IAdministradorServico, AdministradorServico>();
            services.AddScoped<IVeiculoServico, VeiculoServico>();

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options => {
                
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme{
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Insira o token JWT aqui"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement{
                    {
                    new OpenApiSecurityScheme{
                        Reference = new OpenApiReference{
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new string[] {}
                    }
                });

            });

            services.AddDbContext<DbContexto>(options => {
                options.UseMySql(
                    Configuration.GetConnectionString("MySql"),
                    ServerVersion.AutoDetect(Configuration.GetConnectionString("MySql"))
                );
            });

            services.AddCors(options => {
                options.AddDefaultPolicy(
                    builder => {
                        builder.AllowAnyOrigin()
                                .AllowAnyMethod()
                                .AllowAnyHeader();
                    });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseRouting();
            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();
            


            app.UseEndpoints(endpoints => {
            endpoints.MapGet("/", () => Results.Json(new Home())).AllowAnonymous().WithTags("Home");

            string GerarToken(Administrador administrador){

                if(string.IsNullOrEmpty(key)) return string.Empty;

                var securitykey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                var credentials = new SigningCredentials(securitykey, SecurityAlgorithms.HmacSha256);

                var claims = new List<Claim>(){
                    new Claim("Email", administrador.Email),
                    new Claim("Perfil", administrador.Perfil),
                    new Claim(ClaimTypes.Role, administrador.Perfil),
                };

                var token = new JwtSecurityToken(claims: claims, expires: DateTime.Now.AddDays(1), 
                                                signingCredentials: credentials);

                return new JwtSecurityTokenHandler().WriteToken(token);

            }

            endpoints.MapPost("/administradores/login", ([FromBody] LoginDTO loginDTO, IAdministradorServico administradorServico) => {

                var adm = administradorServico.Login(loginDTO);

                if(adm != null)
                {
                    string token = GerarToken(adm);

                    return Results.Ok(new AdministradorLogado{
                        Email = adm.Email,
                        Perfil = adm.Perfil,
                        Token = token
                    });

                }
                else
                    return Results.Unauthorized();

            }).AllowAnonymous().WithTags("Administradores");

            endpoints.MapGet("/administradores", ([FromQuery] int? pagina, IAdministradorServico administradorServico) => { 

                var adms = new List<AdministradorModelView>();
                var administradores = administradorServico.Todos(pagina);
                
                foreach (var adm in administradores)
                {
                    adms.Add(new AdministradorModelView(){
                        Id = adm.Id,
                        Email = adm.Email,
                        Perfil = adm.Perfil
                    });
                }

                return Results.Ok(adms);

            })
            .RequireAuthorization()
            .RequireAuthorization(new AuthorizeAttribute{ Roles = "Adm" })
            .WithTags("Administradores");

            endpoints.MapPost("/administradores", ([FromBody] AdministradorDTO administradorDTO, IAdministradorServico administradorServico) => {
                
                var validacao = new ErrosDeValidacao();

                if(string.IsNullOrEmpty(administradorDTO.Email))
                    validacao.Mensagems.Add("E-mail não pode ser vazio!");

                if(string.IsNullOrEmpty(administradorDTO.Senha))
                    validacao.Mensagems.Add("Senha não pode ser vazia!");

                if(administradorDTO.Perfil == null)
                    validacao.Mensagems.Add("Perfil não pode ser vazio!");

                if(validacao.Mensagems.Count() > 0) return Results.BadRequest(validacao);

                var administrador = new Administrador{
                    Email = administradorDTO.Email,
                    Senha = administradorDTO.Senha,
                    Perfil = administradorDTO.Perfil.ToString() ?? nameof(Perfil.Editor)
                };

                administradorServico.Incluir(administrador);

                return Results.Created($"/Administrador/{administrador.Id}", new AdministradorModelView(){
                    Id = administrador.Id,
                    Email = administrador.Email,
                    Perfil = administrador.Perfil
                });

            })
            .RequireAuthorization()
            .RequireAuthorization(new AuthorizeAttribute{ Roles = "Adm" })
            .WithTags("Administradores");

            endpoints.MapGet("/administradores/{id}", ([FromRoute] int id, IAdministradorServico administradorServico) => {
                
                var administrador = administradorServico.BuscaPorId(id);

                if(administrador == null) return Results.NotFound();

                return Results.Ok(new AdministradorModelView(){
                    Id = administrador.Id,
                    Email = administrador.Email,
                    Perfil = administrador.Perfil
                });

            })
            .RequireAuthorization()
            .RequireAuthorization(new AuthorizeAttribute{ Roles = "Adm" })
            .WithTags("Administradores");







            ErrosDeValidacao ValidaDTO(VeiculoDTO veiculoDTO)
            {
                var validacao = new ErrosDeValidacao();

                if(string.IsNullOrEmpty(veiculoDTO.Nome))
                    validacao.Mensagems.Add("O nome não pode ser vazio!");

                if(string.IsNullOrEmpty(veiculoDTO.Marca))
                    validacao.Mensagems.Add("A marca não pode ficar em branco!");

                if(veiculoDTO.Ano < 1950)
                    validacao.Mensagems.Add("Veículo muito antigo, aceito somente anos superiores a 1949!");

                return validacao;
            }

            endpoints.MapPost("/veiculos", ([FromBody] VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) => {
                
                var validacao = new ErrosDeValidacao();

                validacao = ValidaDTO(veiculoDTO);

                if(validacao.Mensagems.Count() > 0) return Results.BadRequest(validacao);

                var veiculo = new Veiculo{
                    Nome = veiculoDTO.Nome,
                    Marca = veiculoDTO.Marca,
                    Ano = veiculoDTO.Ano
                };

                veiculoServico.Incluir(veiculo);

                return Results.Created($"/veiculo/{veiculo.Id}", veiculo);

            })
            .RequireAuthorization()
            .RequireAuthorization(new AuthorizeAttribute{ Roles = "Adm,Editor" })
            .WithTags("Veiculos");

            endpoints.MapGet("/veiculos", ([FromQuery] int? pagina, IVeiculoServico veiculoServico) => {
                
                var veiculos = veiculoServico.Todos(pagina);

                return Results.Ok(veiculos);

            })
            .RequireAuthorization()
            .RequireAuthorization(new AuthorizeAttribute{ Roles = "Adm" })
            .WithTags("Veiculos");

            endpoints.MapGet("/veiculos/{id}", ([FromRoute] int id, IVeiculoServico veiculoServico) => {
                
                var veiculo = veiculoServico.BuscaPorId(id);

                if(veiculo == null) return Results.NotFound();

                return Results.Ok(veiculo);

            })
            .RequireAuthorization()
            .RequireAuthorization(new AuthorizeAttribute{ Roles = "Adm,Editor" })
            .WithTags("Veiculos");

            endpoints.MapPut("/veiculos/{id}", ([FromRoute] int id, VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) => {
                
                var veiculo = veiculoServico.BuscaPorId(id);
                
                if(veiculo == null) return Results.NotFound();

                veiculo.Nome = veiculoDTO.Nome;
                veiculo.Marca = veiculoDTO.Marca;
                veiculo.Ano = veiculoDTO.Ano;

                var validacao = ValidaDTO(veiculoDTO);
                if(validacao.Mensagems.Count() > 0) return Results.BadRequest(validacao);

                veiculoServico.Atualizar(veiculo);

                return Results.Ok(veiculo);

            })
            .RequireAuthorization()
            .RequireAuthorization(new AuthorizeAttribute{ Roles = "Adm" })
            .WithTags("Veiculos");

            endpoints.MapDelete("/veiculos/{id}", ([FromRoute] int id, IVeiculoServico veiculoServico) => {
                
                var veiculo = veiculoServico.BuscaPorId(id);

                if(veiculo == null) return Results.NotFound();

                veiculoServico.Apagar(veiculo);
                
                return Results.NoContent();

            })
            .RequireAuthorization()
            .RequireAuthorization(new AuthorizeAttribute{ Roles = "Adm" })
            .WithTags("Veiculos");
            });
        }




    }
}