using Models;
using Servicos;
using System.Text;

namespace AnalisadorLexico
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var txtArquivoGramatica = "D:\\Workspace\\trabCompiladores\\Analisador\\AnalisadorLexico\\assets\\input_gramatica.txt";
            var txtArquivoSaida = "D:\\Workspace\\trabCompiladores\\Analisador\\AnalisadorLexico\\assets\\output.txt";
            var txtArquivoCodigoFonte = "D:\\Workspace\\trabCompiladores\\Analisador\\AnalisadorLexico\\assets\\input_fonte.txt";

            System.IO.StreamWriter streamWriterSaida = new System.IO.StreamWriter(txtArquivoSaida, false, Encoding.UTF8);

            ServicoGeracaoAutomatoFinito servicoGeracaoAF = new ServicoGeracaoAutomatoFinito();
            AutomatoFinito automatoFinito = servicoGeracaoAF.gerarAutomatoFinitoInicial(txtArquivoGramatica);
            automatoFinito = servicoGeracaoAF.determinizarAutomato(automatoFinito);
            servicoGeracaoAF.removerMortos(automatoFinito);

            streamWriterSaida.WriteLine("Autômato finito gerado.");
            streamWriterSaida.WriteLine(automatoFinito.criaListaSaida());

            ServicoAnalisadorLexico servicoAnalisadorLexico = new ServicoAnalisadorLexico();
            List<TabelaSimbolos> tokensLidos = new List<TabelaSimbolos>();
            List<string> erros = new List<string>();

            servicoAnalisadorLexico.analisar(txtArquivoCodigoFonte, automatoFinito, ref tokensLidos, ref erros);

            streamWriterSaida.WriteLine("Tokens Lidos:\r\n" + String.Join(Environment.NewLine, tokensLidos.Select(token => String.Format("id: {0}, estado: {1}, rotulo: {2}, linha {3}", token.identificador, token.estadoReconhecedor.label, token.rotulo, token.linha))));

            streamWriterSaida.WriteLine("Erros:\r\n" + (erros == null || erros.Count == 0 ? "Nenhum." : String.Join(Environment.NewLine, erros)));

            streamWriterSaida.WriteLine("Análise léxica concluída...\n");

            if (erros != null && erros.Count > 0)
            {
                Console.WriteLine("Foram encontrados erros na análise léxica. Verifique o log!");
                streamWriterSaida.Close();
                streamWriterSaida.Dispose();
                return;
            }

            streamWriterSaida.Close();
            streamWriterSaida.Dispose();
            Console.WriteLine("acabou");
        }

    }
}