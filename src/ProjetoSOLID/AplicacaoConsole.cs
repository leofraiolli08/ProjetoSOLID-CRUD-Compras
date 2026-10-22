using Microsoft.Data.Sqlite;
using ProjetoSOLID.controller;
using ProjetoSOLID.view;

namespace ProjetoSOLID;

/// <summary>Roteador de menus; não contém SQL nem regras de domínio.</summary>
public sealed class AplicacaoConsole
{
    private readonly IMenuView menu;
    private readonly ProdutoController produtos;
    private readonly CompraController compras;

    public AplicacaoConsole(IMenuView menu, ProdutoController produtos, CompraController compras)
    {
        this.menu = menu;
        this.produtos = produtos;
        this.compras = compras;
    }

    public void Executar()
    {
        while (true)
        {
            int opcao = menu.ExibirMenu();
            if (opcao == 0) return;
            if (opcao == 1) GerenciarProdutos();
            else if (opcao == 2) GerenciarCompras();
            else Console.WriteLine("Opção inválida.");
        }
    }

    private void GerenciarProdutos()
    {
        while (true)
        {
            int opcao = menu.ExibirMenuProdutos();
            if (opcao == 0) return;
            ExecutarComTratamento(() =>
            {
                switch (opcao)
                {
                    case 1: produtos.Cadastrar(); break;
                    case 2: produtos.Listar(); break;
                    case 3:
                        int? idBuscar = LeituraConsole.LerInteiroPositivo("ID do produto");
                        if (idBuscar.HasValue) produtos.Mostrar(idBuscar.Value);
                        break;
                    case 4:
                        int? idEditar = LeituraConsole.LerInteiroPositivo("ID do produto a atualizar");
                        if (idEditar.HasValue) produtos.Atualizar(idEditar.Value);
                        break;
                    case 5:
                        int? idExcluir = LeituraConsole.LerInteiroPositivo("ID do produto a excluir");
                        if (idExcluir.HasValue)
                        {
                            produtos.Mostrar(idExcluir.Value);
                            if (LeituraConsole.Confirmar("Deseja excluir este produto?"))
                                produtos.Remover(idExcluir.Value);
                            else Console.WriteLine("Exclusão cancelada.");
                        }
                        break;
                    default: Console.WriteLine("Opção inválida."); break;
                }
            });
        }
    }

    private void GerenciarCompras()
    {
        while (true)
        {
            int opcao = menu.ExibirMenuCompras();
            if (opcao == 0) return;
            ExecutarComTratamento(() =>
            {
                switch (opcao)
                {
                    case 1: compras.Cadastrar(); break;
                    case 2: compras.Listar(); break;
                    case 3:
                        int? idConsultar = LeituraConsole.LerInteiroPositivo("ID da compra");
                        if (idConsultar.HasValue) compras.Consultar(idConsultar.Value);
                        break;
                    case 4:
                        int? idEditar = LeituraConsole.LerInteiroPositivo("ID da compra a atualizar");
                        if (idEditar.HasValue) compras.Atualizar(idEditar.Value);
                        break;
                    case 5:
                        int? idExcluir = LeituraConsole.LerInteiroPositivo("ID da compra a excluir");
                        if (idExcluir.HasValue)
                        {
                            compras.Consultar(idExcluir.Value);
                            if (LeituraConsole.Confirmar("Deseja excluir esta compra?"))
                                compras.Remover(idExcluir.Value);
                            else Console.WriteLine("Exclusão cancelada.");
                        }
                        break;
                    default: Console.WriteLine("Opção inválida."); break;
                }
            });
        }
    }

    private static void ExecutarComTratamento(Action acao)
    {
        try { acao(); }
        catch (OperationCanceledException) { Console.WriteLine("Operação cancelada."); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or
            KeyNotFoundException or OverflowException or SqliteException or IOException)
        {
            Console.WriteLine($"Não foi possível concluir: {ex.Message}");
        }
    }
}
