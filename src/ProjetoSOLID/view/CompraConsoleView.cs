using ProjetoSOLID.model;

namespace ProjetoSOLID.view;

public sealed class CompraConsoleView : ICompraView
{
    public void ExibirMensagem(string mensagem) => LeituraConsole.Mensagem(mensagem);

    public List<SolicitacaoItem>? ObterItens(IReadOnlyList<Produto> produtos, Compra? compraExistente = null)
    {
        var porId = produtos.ToDictionary(p => p.Id);
        var carrinho = compraExistente?.Itens.ToDictionary(i => i.ProdutoId, i => i.Quantidade)
            ?? new Dictionary<int, int>();

        while (true)
        {
            Console.WriteLine("\n--- Itens selecionados ---");
            if (carrinho.Count == 0) Console.WriteLine("Nenhum produto selecionado.");
            foreach (var linha in carrinho.OrderBy(kv => kv.Key))
            {
                string nome = porId.TryGetValue(linha.Key, out Produto? p) ? p.Nome : "Produto não encontrado";
                Console.WriteLine($"  Produto #{linha.Key} - {nome}: {linha.Value} unidade(s)");
            }
            Console.WriteLine("\n1 - Adicionar produto ou somar quantidade");
            Console.WriteLine("2 - Ajustar quantidade de um produto");
            Console.WriteLine("3 - Remover produto da compra");
            Console.WriteLine("4 - Salvar compra");
            Console.WriteLine("0 - Cancelar operação");
            int opcao = LeituraConsole.LerOpcao();
            if (opcao == 0) return null;
            if (opcao == 4)
            {
                if (carrinho.Count == 0)
                {
                    ExibirMensagem("A compra deve conter pelo menos um produto.");
                    continue;
                }
                return carrinho.OrderBy(p => p.Key)
                    .Select(p => new SolicitacaoItem(p.Key, p.Value)).ToList();
            }
            if (opcao is 1 or 2)
            {
                Console.WriteLine("\n--- Produtos disponíveis ---");
                foreach (Produto produto in produtos)
                    Console.WriteLine($"#{produto.Id} - {produto.Nome}: {LeituraConsole.Reais(produto.Preco)}");
                int? id = LeituraConsole.LerInteiroPositivo("ID do produto");
                if (id is null) continue;
                if (!porId.ContainsKey(id.Value))
                {
                    ExibirMensagem("Produto não encontrado.");
                    continue;
                }
                if (opcao == 2 && !carrinho.ContainsKey(id.Value))
                {
                    ExibirMensagem("Este produto ainda não está na compra. Use a opção 1.");
                    continue;
                }
                int? quantidade = LeituraConsole.LerInteiroPositivo(
                    opcao == 1 ? "Quantidade a adicionar" : "Nova quantidade");
                if (quantidade is null) continue;
                try
                {
                    carrinho[id.Value] = opcao == 1
                        ? checked(carrinho.GetValueOrDefault(id.Value) + quantidade.Value)
                        : quantidade.Value;
                }
                catch (OverflowException)
                {
                    ExibirMensagem("Quantidade excede o limite permitido.");
                }
            }
            else if (opcao == 3)
            {
                int? id = LeituraConsole.LerInteiroPositivo("ID do produto a remover");
                if (id is not null && !carrinho.Remove(id.Value))
                    ExibirMensagem("Produto não faz parte da compra.");
            }
            else ExibirMensagem("Opção inválida.");
        }
    }

    public void MostrarResumo(Compra compra)
    {
        Console.WriteLine($"#{compra.Id} | {compra.DataCompra.ToLocalTime():dd/MM/yyyy HH:mm} | " +
                          $"{compra.Itens.Count} produto(s) distinto(s) | Total: {LeituraConsole.Reais(compra.Total)}");
    }

    public void MostrarDetalhes(Compra compra)
    {
        Console.WriteLine("\n=== DETALHES DA COMPRA ===");
        MostrarResumo(compra);
        foreach (ItemCompra item in compra.Itens)
        {
            Console.WriteLine($"  #{item.ProdutoId} - {item.NomeProduto} | " +
                              $"Qtd.: {item.Quantidade} | Preço histórico: " +
                              $"{LeituraConsole.Reais(item.PrecoUnitarioNoMomentoDaCompra)} | " +
                              $"Subtotal: {LeituraConsole.Reais(item.Subtotal)}");
        }
        Console.WriteLine($"TOTAL: {LeituraConsole.Reais(compra.Total)}");
    }
}
