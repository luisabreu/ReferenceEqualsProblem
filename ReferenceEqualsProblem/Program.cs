using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace ReferenceEqualsProblem {
    internal class Program {
        private static void Main(string[] args) {
            var infoAlocacoes = new[] {
                                          new InfoAlocacaoVeiculo {
                                                                      DataFim = DateTime.Now.AddHours(1),
                                                                      DataInicio = DateTime.Now,
                                                                      IdVeiculo = 1,
                                                                      LocalOrigem = "Centro",
                                                                      LocalDestino = "Monte",
                                                                      NifMotorista = "123456789",
                                                                      PermanenciaVeiculo = true
                                                                  },
                                          new InfoAlocacaoVeiculo {
                                                                      DataFim = DateTime.Now.AddHours(1),
                                                                      DataInicio = DateTime.Now,
                                                                      IdVeiculo = 2,
                                                                      LocalOrigem = "Centro",
                                                                      LocalDestino = "Monte",
                                                                      NifMotorista = "215183517",
                                                                      PermanenciaVeiculo = true
                                                                  }
                                      };
            var instanciaAplicacao = new InstanciaAplicacao("Main", "MAI", new Acao("superuser@mail.pt")) {Id = 1};
            var novasAlocacoes = infoAlocacoes.Select(a => new AlocacaoVeiculo(a.IdVeiculo,
                Guid.NewGuid(),
                a.NifMotorista,
                a.DataInicio,
                a.DataFim,
                a.LocalOrigem,
                a.LocalDestino,
                a.PermanenciaVeiculo,
                new Acao("123456789")));

            var cmd = new NovaReserva(new Acao("123456789"),
                instanciaAplicacao,
                Guid.NewGuid(),
                novasAlocacoes,
                "bla bla bla");
        }
    }

    public class NovaReserva : ComandoBaseAssociadoInstancia {
        public NovaReserva(Acao operacaoEfetuadaPor, InstanciaAplicacao instanciaAplicacao, Guid idInterno,
            IEnumerable<AlocacaoVeiculo> alocacoesVeiculo, string observacoes) : base(operacaoEfetuadaPor, instanciaAplicacao) {
            if (alocacoesVeiculo == null || !alocacoesVeiculo.Any()) {
                throw new GestaoFrotasException("bla");
            }
            if (ExistemIntercecoes(alocacoesVeiculo)) {
                throw new GestaoFrotasException("bla");
            }
            IdInterno = idInterno;
            AlocacoesVeiculo = alocacoesVeiculo;
            Observacoes = observacoes;
        }

        public Guid IdInterno { get; }
        public IEnumerable<AlocacaoVeiculo> AlocacoesVeiculo { get; }
        public string Observacoes { get; }


        private bool ExistemIntercecoes(IEnumerable<AlocacaoVeiculo> alocacoesVeiculo) {
            //TODO:  se comentares ToList, deixa funcionar. com list funciona
            var alocs = alocacoesVeiculo;//.ToList(); 
            foreach (var alocacaoVeiculo in alocs) {
                //NOTE: ReferenceEquals retorna false qd
                var interceta = alocs.Any(a => !ReferenceEquals(alocacaoVeiculo, a) && a.IntercetaOutraAlocacao(alocacaoVeiculo));
                if (interceta) {
                    return true;
                }
            }
            return false;
        }

        public override string ToString() {
            return
                $"Nova reserva: {base.ToString()} - IdInterno: {IdInterno} - Alocacoes: {AlocacoesVeiculo.Aggregate("", (total, alocacao) => total += alocacao)} - Obs: {Observacoes}";
        }
    }

    public class InfoAlocacaoVeiculo {
        public int IdVeiculo { get; set; }

        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }

        public string NifMotorista { get; set; }
        public string LocalOrigem { get; set; }
        public string LocalDestino { get; set; }
        public bool PermanenciaVeiculo { get; set; }

        public override string ToString() {
            return
                $"InfoAlocacaoVeiculo: IdVei: {IdVeiculo} - Ini: {DataInicio} - Fim: {DataFim} - mot: {NifMotorista} - Orig: {LocalOrigem} - Dest: {LocalDestino} - Perm: {PermanenciaVeiculo}";
        }
    }

    public class PedidoTransporte : AgregadoBase {
        public int Versao { get; internal set; }
    }


    public class IntervaloTemporal {
        private IntervaloTemporal() {
        }

        public IntervaloTemporal(DateTime dataInicio, DateTime? dataFim = null) {
            if (dataInicio.Kind != DateTimeKind.Local) {
                throw new GestaoFrotasException("A data de início do intervalo de indisponibilidade tem de ser sempre especificada em formato local.");
            }
            if (dataFim.HasValue && dataFim?.Kind != DateTimeKind.Local) {
                throw new GestaoFrotasException("A data de fim do intervalo de indisponibilidade tem de ser sempre especificada em formato local.");
            }
            if (dataFim.HasValue && dataFim.Value < dataInicio) {
                throw new GestaoFrotasException("Quando definida, a data de fim tem de ser superior à data de início.");
            }
            DataInicio = dataInicio;
            DataFim = dataFim;
        }

        public DateTime DataInicio { get; }
        public DateTime? DataFim { get; }

        public IntervaloTemporal FechaIntervalo(DateTime dataFim) {
            if (dataFim < DataInicio) {
                throw new GestaoFrotasException("Data de final passada tem de ser sueprior ou igual à data de início.");
            }
            return new IntervaloTemporal(DataInicio, dataFim);
        }

        public bool IntervaloFechado() => DataFim.HasValue;

        public bool IntercetaIntervalo(IntervaloTemporal intervalo) {
            if (intervalo == null) {
                return false;
            }

            if (!intervalo.DataFim.HasValue) {
                if (!DataFim.HasValue) {
                    return true; // dois intervalos sem data fim
                }
                var res1 = DataFim.Value >= intervalo.DataInicio; //DataFim interior superior daata

                return res1;
            }
            if (!DataFim.HasValue) {
                var res2 = intervalo.DataFim.Value >= DataInicio;

                return res2;
            }
            var res = (intervalo.DataInicio >= DataInicio && intervalo.DataInicio <= DataFim.Value) ||
                      (intervalo.DataFim.Value >= DataInicio && intervalo.DataFim.Value <= DataFim.Value);

            return res;
        }

        protected bool Equals(IntervaloTemporal other) {
            return DataInicio.Equals(other.DataInicio) && DataFim.Equals(other.DataFim);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((IntervaloTemporal) obj);
        }

        public override int GetHashCode() {
            unchecked {
                return (DataInicio.GetHashCode()*397) ^ DataFim.GetHashCode();
            }
        }

        public override string ToString() {
            return $"Intervalo temporal: {DataInicio} a {DataFim?.ToString() ?? "---"}";
        }
    }

    public abstract class EntidadeBase {
        public int Id { get; internal set; }

        public override bool Equals(object obj) {
            var outro = obj as AgregadoBase;
            if (ReferenceEquals(outro, null)) {
                return false;
            }
            if (ReferenceEquals(this, outro)) {
                return true;
            }
            if (GetType() != outro.GetType()) {
                return false;
            }
            if (Id == 0 || outro.Id == 0) {
                return false;
            }

            return Id == outro.Id;
        }

        public static bool operator ==(EntidadeBase uma, EntidadeBase outra) {
            if (ReferenceEquals(uma, null) && ReferenceEquals(outra, null)) {
                return true;
            }
            if (ReferenceEquals(uma, null) || ReferenceEquals(outra, null)) {
                return false;
            }
            return uma.Equals(outra);
        }

        public static bool operator !=(EntidadeBase uma, EntidadeBase outra) {
            return !(uma == outra);
        }

        public override int GetHashCode() {
            //NOTE: this is not perfect, but works most of the time
            return (GetType() + Id.ToString()).GetHashCode();
        }
    }

    public class AlocacaoVeiculo : EntidadeBase {
        private AlocacaoVeiculo() {
        }

        public AlocacaoVeiculo(int idVeiculo,
            Guid idInterno,
            string nifMotorista,
            DateTime dataInicio,
            DateTime dataFim,
            string localOrigem,
            string localDestino,
            bool permanenciaVeiculo,
            Acao criadoPor) {
            if (string.IsNullOrEmpty(localOrigem)) {
                throw new GestaoFrotasException("bla");
            }
            if (string.IsNullOrEmpty(localDestino)) {
                throw new GestaoFrotasException("bla");
            }
            if (criadoPor == null) {
                throw new GestaoFrotasException("bla");
            }
            if (dataInicio.Kind != DateTimeKind.Local) {
                throw new GestaoFrotasException(string.Format("bla", "data de início"));
            }
            if (dataFim.Kind != DateTimeKind.Local) {
                throw new GestaoFrotasException(string.Format("bla", "data de fim"));
            }
            if (dataInicio > dataFim) {
                throw new GestaoFrotasException("bla");
            }

            NifMotorista = nifMotorista;
            IntervaloAlocacao = new IntervaloTemporal(dataInicio, dataFim);
            LocalOrigem = localOrigem;
            LocalDestino = localDestino;
            PermanenciaVeiculo = permanenciaVeiculo;
            CriadoPor = criadoPor;
            IdVeiculo = idVeiculo;
            IdInterno = idInterno;
        }

        public int IdVeiculo { get; internal set; }
        public Guid IdInterno { get; internal set; }

        public IntervaloTemporal IntervaloAlocacao { get; }

        public string NifMotorista { get; }
        public string LocalOrigem { get; private set; }
        public string LocalDestino { get; private set; }
        public bool PermanenciaVeiculo { get; private set; }
        public Acao CriadoPor { get; private set; }
        public Acao CanceladoPor { get; private set; }

        public void CancelaAlocacao(Acao canceladoPor) {
            if (canceladoPor == null) {
                throw new GestaoFrotasException("bla");
            }
            if (CanceladoPor != null) {
                throw new GestaoFrotasException("bla");
            }
            CanceladoPor = canceladoPor;
        }

        public bool IntercetaOutraAlocacao(AlocacaoVeiculo outra) {
            if (outra == null) {
                return false;
            }
            return NaoEstaCancelada() && outra.NaoEstaCancelada() && IntervaloAlocacao.IntercetaIntervalo(outra.IntervaloAlocacao) &&
                   (NifMotorista == outra.NifMotorista || IdVeiculo == outra.IdVeiculo);
        }

        public bool NaoEstaCancelada() {
            return CanceladoPor == null;
        }
    }

    public class GestaoFrotasException : Exception {
        public GestaoFrotasException(string message) : base(message) {
        }

        public GestaoFrotasException() {
        }

        public GestaoFrotasException(string message, Exception innerException) : base(message, innerException) {
        }

        protected GestaoFrotasException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    }

    public class Acao {
        private Acao() {
        }

        public Acao(string nomeUtilizador, DateTime? dataOperacao = null) {
            if (string.IsNullOrEmpty(nomeUtilizador)) {
                throw new GestaoFrotasException("Uma acao tem de ser desempenhada por um utilizador.");
            }
            DataOperacao = dataOperacao ?? DateTime.Now;
            if (DataOperacao.Kind != DateTimeKind.Local) {
                throw new GestaoFrotasException("A data de operação tem de ser sempre especificada em formato local.");
            }
            NomeUtilizador = nomeUtilizador;
        }

        public DateTime DataOperacao { get; }
        public string NomeUtilizador { get; }

        protected bool Equals(Acao other) {
            return DataOperacao.Equals(other.DataOperacao) && string.Equals(NomeUtilizador, other.NomeUtilizador);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Acao) obj);
        }

        public override int GetHashCode() {
            unchecked {
                return (DataOperacao.GetHashCode()*397) ^ (NomeUtilizador != null ? NomeUtilizador.GetHashCode() : 0);
            }
        }

        public override string ToString() {
            return $"Acao: {NomeUtilizador} - {DataOperacao}";
        }

        public static Acao CriaVazia() {
            return new Acao("not-required@test.pt");
        }
    }

    public abstract class ComandoBase {
        protected ComandoBase(Acao operacaoEfetuadaPor) {
            if (operacaoEfetuadaPor == null) {
                throw new GestaoFrotasException("Uma operação tem de ser desempenhada por um utilizador registado.");
            }
            OperacaoEfetuadaPor = operacaoEfetuadaPor;
        }

        public Acao OperacaoEfetuadaPor { get; }

        public override string ToString() {
            return $"Comando base: EfetuadoPor: {OperacaoEfetuadaPor}";
        }
    }

    public abstract class ComandoBaseAssociadoInstancia : ComandoBase {
        protected ComandoBaseAssociadoInstancia(Acao operacaoEfetuadaPor, InstanciaAplicacao instanciaAplicacao) : base(operacaoEfetuadaPor) {
            if (instanciaAplicacao == null) {
                throw new GestaoFrotasException("bla");
            }

            InstanciaAplicacao = instanciaAplicacao;
        }


        public InstanciaAplicacao InstanciaAplicacao { get; }

        public override string ToString() {
            return $"ComandoBaseAssociadoInstancia: Inst Ap: {InstanciaAplicacao}";
        }
    }

    public class AgregadoBase {
        public int Id { get; internal set; }


        /*  private readonly IList<IEvento> _eventos = new List<IEvento>();

        public IEnumerable<IEvento> Eventos => _eventos;

        protected void AdicionaEvento(IEvento evento)
        {
            if (evento == null)
            {
                throw new GestaoFrotasException("Evento não pode ser nulo.");
            }
            _eventos.Add(evento);
        }

        public void MarcaEventosComoTratados()
        {
            _eventos.Clear();
        }*/

        public override bool Equals(object obj) {
            var outro = obj as AgregadoBase;
            if (ReferenceEquals(outro, null)) {
                return false;
            }
            if (ReferenceEquals(this, outro)) {
                return true;
            }
            if (GetType() != outro.GetType()) {
                return false;
            }
            if (Id == 0 || outro.Id == 0) {
                return false;
            }

            return Id == outro.Id;
        }

        public static bool operator ==(AgregadoBase uma, AgregadoBase outra) {
            if (ReferenceEquals(uma, null) && ReferenceEquals(outra, null)) {
                return true;
            }
            if (ReferenceEquals(uma, null) || ReferenceEquals(outra, null)) {
                return false;
            }
            return uma.Equals(outra);
        }

        public static bool operator !=(AgregadoBase uma, AgregadoBase outra) {
            return !(uma == outra);
        }

        public override int GetHashCode() {
            //NOTE: this is not perfect, but works most of the time
            return (GetType() + Id.ToString()).GetHashCode();
        }
    }

    public class InstanciaAplicacao : AgregadoBase {
        private InstanciaAplicacao() : this("Teste", "T12", new Acao("superuser@mail.pt")) {
        }

        public InstanciaAplicacao(string nome, string iniciais, Acao operacaoEfetuadaPor) {
            if (string.IsNullOrEmpty(nome)) {
                throw new GestaoFrotasException("Não é possível criar uma entidade sem um nome.");
            }
            if (string.IsNullOrEmpty(iniciais)) {
                throw new GestaoFrotasException("Não é possível criar uma entidade sem uma inicial.");
            }
            if (iniciais.Length != 3) {
                throw new GestaoFrotasException("Uma entidade tem de ser identificada por três iniciais.");
            }
            if (operacaoEfetuadaPor == null) {
                throw new GestaoFrotasException("É necessário indicar o utilizador que criou esta instância da aplicação.");
            }
            Nome = nome;
            Iniciais = iniciais;
            OperacaoEfetuadaPor = operacaoEfetuadaPor;
        }

        public Acao OperacaoEfetuadaPor { get; set; }

        public string Iniciais { get; }

        public string Nome { get; }

        public override string ToString() {
            return $"Entidade: {Id} - {Nome} - {Iniciais} - {OperacaoEfetuadaPor}";
        }
    }
}