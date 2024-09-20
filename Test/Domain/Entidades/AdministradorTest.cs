using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using minimal_api.Dominio.Entidades;
using minimal_api.Dominio.Enums;


namespace Test.Domain
{
    [TestClass]
    public class AdministradorTest
    {
        [TestMethod]
        public void TestarGetSetPropriedades()
        {
            var adm = new Administrador();
            
            adm.Id = 1;
            adm.Email = "teste@teste.com";
            adm.Perfil = nameof(Perfil.Adm);
            adm.Senha = "123456";

            Assert.Equals(1, adm.Id);
            Assert.Equals("teste@teste.com", adm.Email);
            Assert.Equals(nameof(Perfil.Adm), adm.Perfil);
            Assert.Equals("123456", adm.Senha);
        }
    }
}