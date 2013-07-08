using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace EqualityComparer.Tests
{
    public class DadoUmDepthObjectEqualityComparer
    {
        public class QuandoOsTiposForemDiferentes
        {
            [Test]
            public void DeveLancarExcessao()
            {
                var pessoaA = new PessoaDoente
                {
                    Nome = "Alberto",
                    Doenca = "Gripe",
                    Idade = 1,
                    Bebe = true,
                    Nascimento = new DateTime(1990, 3, 17),
                    Sexo = TipoSexo.Masculino,
                    Altura = 1.7d
                };

                var pessoaB = 4;

                Assert.Catch<ArgumentException>(() => pessoaA.IsEquals(pessoaB), "Parametros não são do mesmo tipo");
            }
        }

        public class QuandoOsValoresForemIguais
        {
            [Test]
            public void IndependenteDoTipoDaPropriedadeDeveRetornarTrue()
            {
                var pessoaA = new PessoaDoente
                {
                    Nome = "Alberto",
                    Doenca = "Gripe",
                    Idade = 1,
                    Bebe = true,
                    Nascimento = new DateTime(1990, 3, 17),
                    Sexo = TipoSexo.Masculino,
                    Altura = 1.7d,
                    UltimaDoenca = DateTime.MaxValue,
                    Enfermidade = new Doenca { Nome = "Cancer" },
                    Viagens = new List<Viagem> { new Viagem { Cidade = "Fortaleza" } }
                };

                var pessoaB = new PessoaDoente
                {
                    Nome = "Alberto",
                    Doenca = "Gripe",
                    Idade = 1,
                    Bebe = true,
                    Nascimento = new DateTime(1990, 3, 17),
                    Sexo = TipoSexo.Masculino,
                    Altura = 1.7d,
                    UltimaDoenca = DateTime.MaxValue,
                    Enfermidade = new Doenca { Nome = "Cancer" },
                    Viagens = new List<Viagem> { new Viagem { Cidade = "Fortaleza" } }
                };

                Assert.IsTrue(pessoaA.IsEquals(pessoaB));
            }
        }

        public class QuandoPeloMenosUmValorForDiferente
        {
            [Test]
            public void QuandoOTipoDaPropriedadeForBool()
            {
                var pessoaC = new PessoaDoente { Bebe = true };

                var pessoaD = new PessoaDoente { Bebe = false };

                Assert.IsFalse(pessoaC.IsEquals(pessoaD));
            }

            [Test]
            public void QuandoOTipoDaPropriedadeForDateTime()
            {
                var pessoaC = new PessoaDoente { Nascimento = new DateTime(1990, 3, 17) };

                var pessoaD = new PessoaDoente { Nascimento = new DateTime(1990, 3, 16) };

                Assert.IsFalse(pessoaC.IsEquals(pessoaD));
            }

            [Test]
            public void QuandoOTipoDaPropriedadeForString()
            {
                var pessoaC = new PessoaDoente { Nome = "Albert9" };

                var pessoaD = new PessoaDoente { Nome = "Alberto" };

                Assert.IsFalse(pessoaC.IsEquals(pessoaD));
            }

            [Test]
            public void QuandoOTipoDaPropriedadeForInt()
            {
                var pessoaC = new PessoaDoente { Idade = 2 };

                var pessoaD = new PessoaDoente { Idade = 1 };

                Assert.IsFalse(pessoaC.IsEquals(pessoaD));
            }

            [Test]
            public void QuandoOTipoDaPropriedadeForEnum()
            {
                var pessoaC = new PessoaDoente { Sexo = TipoSexo.Feminino };

                var pessoaD = new PessoaDoente { Sexo = TipoSexo.Masculino };

                Assert.IsFalse(pessoaC.IsEquals(pessoaD));
            }

            [Test]
            public void QuandoOTipoDaPropriedadeForUmaStructPropria()
            {
                var pessoaC = new PessoaDoente { Endereco = new Endereco { Logradouro = "321" } };

                var pessoaD = new PessoaDoente { Endereco = new Endereco { Logradouro = "123" } };

                Assert.IsFalse(pessoaC.IsEquals(pessoaD));
            }

            [Test]
            public void QuandoOTipoDaPropriedadeForDouble()
            {
                var pessoaC = new PessoaDoente { Altura = 1.8d };

                var pessoaD = new PessoaDoente { Altura = 1.7d };

                Assert.IsFalse(pessoaC.IsEquals(pessoaD));
            }

            [Test]
            public void QuandoOTipoDaPropriedadeForFloat()
            {
                var pessoaC = new PessoaDoente { Peso = 100f };

                var pessoaD = new PessoaDoente { Peso = 110f };

                Assert.IsFalse(pessoaC.IsEquals(pessoaD));
            }

            [Test]
            public void QuandoOTipoDaPropriedadeForLong()
            {
                var pessoaC = new PessoaDoente { Id = 1 };

                var pessoaD = new PessoaDoente { Id = 2 };

                Assert.IsFalse(pessoaC.IsEquals(pessoaD));
            }

            [Test]
            public void QuandoOTipoDaPropriedadeForDecimal()
            {
                var pessoaC = new PessoaDoente { Densidade = 1m };

                var pessoaD = new PessoaDoente { Densidade = 2m };

                Assert.IsFalse(pessoaC.IsEquals(pessoaD));
            }

            [Test]
            public void QuandoOTipoDaPropriedadeForNullableDate()
            {
                var pessoaC = new PessoaDoente { UltimaDoenca = null };

                var pessoaD = new PessoaDoente { UltimaDoenca = DateTime.Now };

                Assert.IsFalse(pessoaC.IsEquals(pessoaD));
            }

            [Test]
            public void QuandoOTipoDaPropriedadeForUmaClasse()
            {
                var pessoaC = new PessoaDoente { Enfermidade = new Doenca { Nome = "Cancer" } };

                var pessoaD = new PessoaDoente { Enfermidade = new Doenca { Nome = "Canc3r" } };

                Assert.IsFalse(pessoaC.IsEquals(pessoaD));
            }

            [Test]
            public void QuandoOTipoDaPropriedadeForUmaIEnumerableCom1Item()
            {
                var pessoaC = new PessoaDoente { Viagens = new List<Viagem> { new Viagem { Cidade = "Fortaleza" } } };

                var pessoaD = new PessoaDoente { Viagens = new List<Viagem> { new Viagem { Cidade = "Fortalez4" } } };

                Assert.IsFalse(pessoaC.IsEquals(pessoaD));
            }

            [Test]
            public void QuandoOTipoDaPropriedadeForUmaIEnumerableComMaisDe1Item()
            {
                var pessoaC = new PessoaDoente { Viagens = new List<Viagem> { new Viagem { Cidade = "Fortaleza" },new Viagem { Cidade = "RJ" } } };

                var pessoaD = new PessoaDoente { Viagens = new List<Viagem> { new Viagem { Cidade = "Fortaleza" }, new Viagem { Cidade = "SP" } } };

                Assert.IsFalse(pessoaC.IsEquals(pessoaD));
            }

            [Test]
            public void QuandoOTipoDaPropriedadeForUmaIEnumerableEQuantidadeDeElementosForemDiferentes()
            {
                var pessoaC = new PessoaDoente { Viagens = new List<Viagem> { new Viagem { Cidade = "Fortaleza" } } };

                var pessoaD = new PessoaDoente { Viagens = new List<Viagem> { new Viagem { Cidade = "Fortalez4" }, new Viagem { Cidade = "Fortalez3" } } };

                Assert.IsFalse(pessoaC.IsEquals(pessoaD));
            }
        }
    }
}