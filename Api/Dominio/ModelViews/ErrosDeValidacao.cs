using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace minimal_api.Dominio.ModelViews
{
    public struct ErrosDeValidacao
    {
        public ErrosDeValidacao()
        {
        }

        public List<string> Mensagems { get; set; } = new List<string>();
    }
}