namespace ProjetoSOLID.model;

public class Compra
{
    private readonly List<ItemCompra> itens;
    public int Id { get; private set; }
    public DateTimeOffset DataCompra { get; }
    public IReadOnlyList<ItemCompra> Itens => itens.AsReadOnly();
    public decimal Total => itens.Aggregate(0m, (total, item) => checked(total + item.Subtotal));

    public Compra(int id, DateTimeOffset dataCompra, IEnumerable<ItemCompra> itens)
    {
        if (id < 0) throw new ArgumentOutOfRangeException(nameof(id));
        if (itens is null) throw new ArgumentNullException(nameof(itens));
        this.itens = itens.ToList();
        if (this.itens.Count == 0)
            throw new ArgumentException("Uma compra precisa de ao menos um item.", nameof(itens));
        if (this.itens.Any(item => item is null))
            throw new ArgumentException("A compra contém um item inválido.", nameof(itens));
        if (this.itens.Select(i => i.ProdutoId).Distinct().Count() != this.itens.Count)
            throw new ArgumentException("Um produto não pode ser repetido na compra.", nameof(itens));

        Id = id;
        DataCompra = dataCompra;
        // Detecta possíveis totais que excederiam a precisão decimal.
        _ = Total;
        if (id > 0) DefinirId(id);
    }

    internal void DefinirId(int id)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        Id = id;
        foreach (ItemCompra item in itens) item.DefinirCompraId(id);
    }
}
