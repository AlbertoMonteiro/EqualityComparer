using System;
using System.Collections.Generic;

namespace EqualityComparer.Tests
{
    public class PessoaDoente : Pessoa
    {
        public string Doenca { get; set; }
        public bool Bebe { get; set; }
        public DateTime Nascimento { get; set; }
        public Endereco Endereco { get; set; }
        public TipoSexo Sexo { get; set; }
        public double Altura { get; set; }
        public float Peso { get; set; }
        public long Id { get; set; }
        public decimal Densidade { get; set; }
        public DateTime? UltimaDoenca { get; set; }
        public Doenca Enfermidade { get; set; }
        public IList<Viagem> Viagens { get; set; }
    }
}