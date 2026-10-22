namespace ProjetoSOLID.model;

/// <summary>Conversão exata de reais para centavos (INTEGER no SQLite).</summary>
public static class Dinheiro
{
    public static long ParaCentavos(decimal reais)
    {
        if (reais <= 0m)
            throw new ArgumentOutOfRangeException(nameof(reais), "O preço deve ser maior que zero.");

        decimal centavos = checked(reais * 100m);
        if (centavos != decimal.Truncate(centavos))
            throw new ArgumentException("Informe valores com no máximo duas casas decimais.", nameof(reais));

        return checked((long)centavos);
    }

    public static decimal ParaReais(long centavos)
    {
        if (centavos <= 0)
            throw new ArgumentOutOfRangeException(nameof(centavos), "O preço em centavos deve ser positivo.");
        return centavos / 100m;
    }
}
