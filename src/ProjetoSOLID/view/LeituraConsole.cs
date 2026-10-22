using System.Globalization;
using System.Text.RegularExpressions;

namespace ProjetoSOLID.view;

public static class LeituraConsole
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static int LerOpcao()
    {
        Console.Write("\nEscolha uma opção: ");
        string? texto = Console.ReadLine();
        if (texto is null) return 0; // EOF em entrada redirecionada: encerra o menu.
        return int.TryParse(texto, out int opcao) ? opcao : -1;
    }

    public static string LerTextoObrigatorio(string campo, bool permitirVazio = false)
    {
        while (true)
        {
            Console.Write($"{campo} (0 para cancelar): ");
            string? valor = Console.ReadLine();
            if (valor is null || valor.Trim() == "0") throw new OperationCanceledException();
            if (permitirVazio || !string.IsNullOrWhiteSpace(valor)) return valor.Trim();
            Console.WriteLine("Este campo é obrigatório.");
        }
    }

    public static decimal LerPreco()
    {
        while (true)
        {
            Console.Write("Preço em R$ (ex.: 12,50; 0 para cancelar): ");
            string? texto = Console.ReadLine()?.Trim();
            if (texto is null || texto == "0") throw new OperationCanceledException();
            // Não aceita ponto para evitar que 12.50 vire 1.250,00 na cultura pt-BR.
            if (Regex.IsMatch(texto, @"^[0-9]+(,[0-9]{1,2})?$") &&
                decimal.TryParse(texto, NumberStyles.AllowDecimalPoint, PtBr, out decimal preco) &&
                preco > 0m)
                return preco;
            Console.WriteLine("Preço inválido. Use, por exemplo, 12,50 (sem separador de milhar).");
        }
    }

    public static int? LerInteiroPositivo(string descricao)
    {
        while (true)
        {
            Console.Write($"{descricao} (0 para cancelar): ");
            string? texto = Console.ReadLine();
            if (texto is null || texto.Trim() == "0") return null;
            if (int.TryParse(texto, out int n) && n > 0) return n;
            Console.WriteLine("Informe um número inteiro positivo.");
        }
    }

    public static bool Confirmar(string mensagem)
    {
        Console.Write($"{mensagem} (s/N): ");
        return string.Equals(Console.ReadLine()?.Trim(), "s", StringComparison.OrdinalIgnoreCase);
    }

    public static void Mensagem(string mensagem) => Console.WriteLine(mensagem);
    public static string Reais(decimal valor) => valor.ToString("C2", PtBr);
}
