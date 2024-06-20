using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servicos {
    class ServicoAnalisadorSintatico {

        private TabelaLALR tabelaLALR;
        private List<Producao> producoes;
        private System.Xml.XmlDocument xmlTabela = new System.Xml.XmlDocument();

        public ServicoAnalisadorSintatico(string arquivoEntrada)
        {
            xmlTabela = new System.Xml.XmlDocument();
            xmlTabela.Load(arquivoEntrada);
            tabelaLALR = new TabelaLALR();
        }

        public void lerSimbolos() {
            System.Xml.XmlNodeList xmlNodeList = xmlTabela.GetElementsByTagName("Symbol"); // Obtém uma lista de nós XML que possuem a tag "Symbol".
            tabelaLALR.simbolos = new Simbolo[xmlNodeList.Count + 1]; // Inicializa o array de símbolos com o tamanho da lista de nós + 1.
            for (int i = 0; i < xmlNodeList.Count; i++)
            { // Itera sobre cada nó na lista.
                Simbolo simbolo = new Simbolo(); // Cria um novo objeto Simbolo.
                simbolo.indice = int.Parse(xmlNodeList[i].Attributes["Index"].Value); // Define o índice do símbolo a partir do atributo XML.
                simbolo.nome = xmlNodeList[i].Attributes["Name"].Value; // Define o nome do símbolo a partir do atributo XML.
                tabelaLALR.simbolos[i] = simbolo; // Armazena o símbolo no array.
            }
        }

        public void lerProducoes() {
            // le as produções que o gold parser enumera
            // guarda tambem os símbolos de cada produção
            // usa na redução para saber o tamanho da produção e o simbolo que da nome a regra          
            producoes = new List<Producao>(); // Inicializa a lista de produções.
            System.Xml.XmlNodeList xmlNodeList = xmlTabela.GetElementsByTagName("Production"); // Obtém uma lista de nós XML que possuem a tag "Production".
            foreach (System.Xml.XmlNode n in xmlNodeList)
            { // Itera sobre cada nó na lista.
                Producao producao = new Producao(); // Cria um novo objeto Producao.
                producao.indice = Int32.Parse(n.Attributes["Index"].InnerText); // Define o índice da produção a partir do atributo XML.
                producao.indiceNaoTerminal = int.Parse(n.Attributes["NonTerminalIndex"].InnerText); // Define o índice do não-terminal a partir do atributo XML.
                producao.simbolosProducao = new List<int>(); // Inicializa a lista de símbolos da produção.
                foreach (System.Xml.XmlNode s in n.ChildNodes)
                { // Itera sobre os nós filhos da produção.
                    producao.simbolosProducao.Add(int.Parse(s.Attributes["SymbolIndex"].InnerText)); // Adiciona o índice do símbolo na lista de símbolos da produção.
                }
                producoes.Add(producao); // Adiciona a produção na lista de produções.
            }
        }

        public void lerTabelaLALR() {
            System.Xml.XmlNodeList xmlNodeList = xmlTabela.GetElementsByTagName("LALRState"); // Obtém uma lista de nós XML que possuem a tag "LALRState".

            tabelaLALR.estados = new int[xmlNodeList.Count]; // Inicializa o array de estados com o tamanho da lista de nós.
            tabelaLALR.acoes = new AcaoTabelaLALR[tabelaLALR.estados.Count(), tabelaLALR.simbolos.Count() + 1]; // Inicializa a matriz de ações com base na quantidade de estados e símbolos.

            foreach (System.Xml.XmlNode nodoEstado in xmlNodeList)
            { // Itera sobre cada nó de estado na lista.
                int indiceEstado = int.Parse(nodoEstado.Attributes["Index"].InnerText); // Obtém o índice do estado a partir do atributo XML.
                tabelaLALR.estados[indiceEstado] = indiceEstado; // Define o estado no array de estados.

                foreach (System.Xml.XmlNode nodoAcao in nodoEstado.ChildNodes)
                { // Itera sobre os nós filhos do estado.
                    int indiceAcao = int.Parse(nodoAcao.Attributes["SymbolIndex"].InnerText); // Obtém o índice da ação a partir do atributo XML.
                    AcaoTabelaLALR acao = new AcaoTabelaLALR(); // Cria um novo objeto AcaoTabelaLALR.
                    Enum.TryParse(nodoAcao.Attributes["Action"].InnerText, out acao.acao); // Converte o valor do atributo "Action" para o tipo enumerado.
                    acao.valor = int.Parse(nodoAcao.Attributes["Value"].InnerText); // Define o valor da ação a partir do atributo XML.
                    tabelaLALR.acoes[indiceEstado, indiceAcao] = acao; // Armazena a ação na matriz de ações.
                }
            }
        }

        public List<string> analisar(List<TabelaSimbolos> tokens)
        {
            List<string> listaRetorno = new List<string>(); // Inicializa a lista de strings para armazenar mensagens de erro ou sucesso.

            List<int> fita = mapearFitaSaidaAnaliseSintatica(tokens); // Mapeia os tokens para uma lista de índices de símbolos.
            List<int> pilha = new List<int>(); // Inicializa a pilha de estados.
            int indiceLeituraFita = 0; // Inicializa o índice de leitura da fita.

            pilha.Add(tabelaLALR.estados[0]); // Adiciona o estado inicial à pilha.

            while (true)
            { // Loop principal de análise.
                int estadoTopoPilha = pilha.Last(); // Obtém o estado no topo da pilha.
                int tokenAtualFita = fita[indiceLeituraFita]; // Obtém o token atual da fita.

                AcaoTabelaLALR acaoTabela = tabelaLALR.acoes[estadoTopoPilha, tokenAtualFita]; // Obtém a ação correspondente na tabela LALR.

                if (acaoTabela == null)
                { // Se a ação não for encontrada, ocorre um erro.
                    if (indiceLeituraFita == tokens.Count)
                    { // Se todos os tokens foram lidos, é um erro de fim de arquivo inesperado.
                        TabelaSimbolos token = tokens[indiceLeituraFita - 1];
                        listaRetorno.Add("Fim de arquivo inesperado. O token " + token.rotulo + ", na linha " + token.linha + " não conclui uma expressão.");
                    }
                    else
                    { // Caso contrário, é um token inesperado.
                        TabelaSimbolos token = tokens[indiceLeituraFita];
                        listaRetorno.Add("Token " + token.rotulo + " inesperado na linha " + token.linha + ".");
                    }
                    break;
                }
                else if (acaoTabela.acao == Enumeradores.Acao.Aceitar)
                { // Se a ação for "Aceitar", a análise está concluída com sucesso.
                    break;
                }
                else if (acaoTabela.acao == Enumeradores.Acao.Shift)
                { // Se a ação for "Shift", move o token atual para a pilha e avança para o próximo token.
                    pilha.Add(acaoTabela.valor);
                    indiceLeituraFita += 1;
                }
                else if (acaoTabela.acao == Enumeradores.Acao.Reduce)
                { // Se a ação for "Reduce", aplica a produção de redução correspondente.
                    Producao producaoReducao = producoes[acaoTabela.valor];
                    if (producaoReducao != null)
                    {
                        int tamanhoProducao = producaoReducao.simbolosProducao.Count(); // Obtém o tamanho da produção.
                        pilha.RemoveRange(pilha.Count() - tamanhoProducao, tamanhoProducao); // Remove os estados da pilha correspondentes ao tamanho da produção.
                        pilha.Add(tabelaLALR.acoes[pilha.Last(), producaoReducao.indiceNaoTerminal].valor); // Adiciona o estado resultante da redução.
                    }
                    else
                    {
                        listaRetorno.Add("Produção de redução não encontrada."); // Se a produção de redução não for encontrada, ocorre um erro.
                        break;
                    }
                }

                if (indiceLeituraFita == fita.Count())
                { // Se a fita de análise terminou e não foi reconhecida, ocorre um erro.
                    listaRetorno.Add("Fim da fita de análise... O código não foi reconhecido.");
                    break;
                }
            }

            return listaRetorno; // Retorna a lista de mensagens de erro ou sucesso.
        }


        private List<int> mapearFitaSaidaAnaliseSintatica(List<TabelaSimbolos> tokens)
        {
            Simbolo simbolo;
            List<int> fita = new List<int>();
            foreach (TabelaSimbolos token in tokens)
            { // Itera sobre cada token na lista.
                simbolo = tabelaLALR.getSimboloPorIndice(token.getIndiceSimboloGoldParser); // Obtém o símbolo correspondente ao índice do token.
                if (simbolo != null)
                {
                    fita.Add(simbolo.indice); // Adiciona o índice do símbolo à fita.
                }
            }
            simbolo = tabelaLALR.getSimboloPorIndice(0); // Obtém o símbolo EOF.
            fita.Add(simbolo.indice); // Adiciona o índice do símbolo EOF à fita.
            return fita; // Retorna a fita mapeada.
        }

    }
}
