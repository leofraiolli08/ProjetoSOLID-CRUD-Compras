namespace ProjetoSOLID.model;

public class ItemCompra
{
    public int CompraId { get; private set; }
    public int ProdutoId { get; }
    public string NomeProduto { get; }
    public int Quantidade { get; }
    public decimal PrecoUnitarioNoMomentoDaCompra { get; }
    public decimal Subtotal => checked(Quantidade * PrecoUnitarioNoMomentoDaCompra);

    public ItemCompra(int produtoId, int quantidade, decimal precoUnitarioNoMomentoDaCompra,
        string nomeProduto = "")
    {
        if (produtoId <= 0) throw new ArgumentOutOfRangeException(nameof(produtoId));
        if (quantidade <= 0) throw new ArgumentOutOfRangeException(nameof(quantidade));
        _ = Dinheiro.ParaCentavos(precoUnitarioNoMomentoDaCompra);
        ProdutoId = produtoId;
        Quantidade = quantidade;
        PrecoUnitarioNoMomentoDaCompra = precoUnitarioNoMomentoDaCompra;
        NomeProduto = nomeProduto ?? string.Empty;
    }

    internal void DefinirCompraId(int id)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        CompraId = id;
    }
}
