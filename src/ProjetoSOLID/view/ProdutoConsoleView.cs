using ProjetoSOLID.model;

namespace ProjetoSOLID.view;

public sealed class ProdutoConsoleView : IProdutoView
{
    public void MostrarProduto(Produto produto)
    {
        Console.WriteLine($"#{produto.Id} - {produto.Nome} | {LeituraConsole.Reais(produto.Preco)}");
        if (!string.IsNullOrEmpty(produto.Descricao))
            Console.WriteLine($"  Descrição: {produto.Descricao}");
    }
}
