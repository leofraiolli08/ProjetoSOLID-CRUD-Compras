namespace ProjetoSOLID.view;

public class MenuConsoleView : IMenuView
{
    public int ExibirMenu()
    {
        Cabecalho("MENU PRINCIPAL");
        Console.WriteLine("1 - Gerenciar produtos");
        Console.WriteLine("2 - Gerenciar compras");
        Console.WriteLine("0 - Sair");
        return LeituraConsole.LerOpcao();
    }

    public int ExibirMenuProdutos()
    {
        Cabecalho("GERENCIAMENTO DE PRODUTOS");
        Console.WriteLine("1 - Cadastrar produto");
        Console.WriteLine("2 - Listar produtos");
        Console.WriteLine("3 - Buscar produto por ID");
        Console.WriteLine("4 - Atualizar produto");
        Console.WriteLine("5 - Excluir produto");
        Console.WriteLine("0 - Voltar");
        return LeituraConsole.LerOpcao();
    }

    public int ExibirMenuCompras()
    {
        Cabecalho("GERENCIAMENTO DE COMPRAS");
        Console.WriteLine("1 - Registrar compra");
        Console.WriteLine("2 - Listar compras");
        Console.WriteLine("3 - Consultar compra por ID");
        Console.WriteLine("4 - Atualizar compra");
        Console.WriteLine("5 - Excluir compra");
        Console.WriteLine("0 - Voltar");
        return LeituraConsole.LerOpcao();
    }

    private static void Cabecalho(string titulo)
    {
        if (!Console.IsOutputRedirected) Console.Clear();
        Console.WriteLine($"\n=== {titulo} ===\n");
    }
}
