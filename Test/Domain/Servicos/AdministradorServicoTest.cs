using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using minimal_api.Dominio.Entidades;
using minimal_api.Dominio.Enums;
using minimal_api.Dominio.Servicos;
using minimal_api.Infraestrutura.Db;


namespace Test.Domain
{
    [TestClass]
    public class AdministradorServicoTest
    {

        private DbContexto CriarContextoDeTeste(){
            
            var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var path = Path.GetFullPath(Path.Combine(assemblyPath ?? "", "..", "..", ".."));

            var builder = new ConfigurationBuilder()
                                .SetBasePath(path!)
                                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                                .AddEnvironmentVariables();

            var configuration = builder.Build();

            return new DbContexto(configuration);
        }

        [TestMethod]
        public void TestandoSalvarAdministrador()
        {

            var context = CriarContextoDeTeste();
            context.Database.ExecuteSqlRaw("TRUNCATE TABLE Administradores");

            var adm = new Administrador();
            
            adm.Email = "teste@teste.com";
            adm.Perfil = nameof(Perfil.Adm);
            adm.Senha = "123456";
            
            var administradorServico = new AdministradorServico(context);

            adm = administradorServico.Incluir(adm);
            var admRet = administradorServico.BuscaPorId(adm.Id);

            Assert.AreEqual(1, admRet!.Id);

        }
    }
}