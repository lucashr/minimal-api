using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using minimal_api.Dominio.DTOs;
using minimal_api.Dominio.Entidades;
using minimal_api.Dominio.Interfaces;
using minimal_api.Infraestrutura.Db;

namespace minimal_api.Dominio.Servicos
{
    public class AdministradorServico : IAdministradorServico
    {

        private readonly DbContexto _contexto;
        public AdministradorServico(DbContexto contexto)
        {
            _contexto = contexto;
        }

        public Administrador? BuscaPorId(int id)
        {
            return _contexto.Administradores.Where(x => x.Id == id).FirstOrDefault();
        }

        public Administrador Incluir(Administrador adminitrador)
        {
            _contexto.Administradores.Add(adminitrador);
            _contexto.SaveChanges();

            return adminitrador;
        }

        public Administrador? Login(LoginDTO loginDTO)
        {
            
            var adm = _contexto.Administradores
                                .Where(x => x.Email == loginDTO.Email && x.Senha == loginDTO.Senha)
                                .FirstOrDefault();
            return adm;
        }

        public List<Administrador> Todos(int? pagina)
        {
            var query = _contexto.Administradores.AsQueryable();

            const int itensPorPagina = 10;
            if(pagina != null)
                query = query.Skip(((int)pagina - 1) * itensPorPagina).Take(itensPorPagina);

            return query.ToList();
        }
    }
}