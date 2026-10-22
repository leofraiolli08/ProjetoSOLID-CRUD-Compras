using System.Text;
using Microsoft.Data.Sqlite;
using ProjetoSOLID.controller;
using ProjetoSOLID.dados;
using ProjetoSOLID.view;

namespace ProjetoSOLID;

public static class Program
{
    public static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        try
        {
            var banco = new SqliteDatabase(SqliteDatabase.CaminhoPadrao());
            var produtosRepo = new ProdutoRepository(banco);
            var comprasRepo = new CompraRepository(banco);
            var cadastro = new CadastroProduto();
            var viewProdutos = new ProdutoConsoleView();
            var viewCompras = new CompraConsoleView();
            var menu = new MenuConsoleView();

            var produtoController = new ProdutoController(viewProdutos, produtosRepo, cadastro);
            var compraController = new CompraController(comprasRepo, produtosRepo, viewCompras);
            Console.WriteLine($"Banco de dados: {banco.Caminho}");
            new AplicacaoConsole(menu, produtoController, compraController).Executar();
            return 0;
        }
        catch (Exception ex) when (ex is SqliteException or IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Falha ao inicializar o banco de dados: {ex.Message}");
            return 1;
        }
    }
}
