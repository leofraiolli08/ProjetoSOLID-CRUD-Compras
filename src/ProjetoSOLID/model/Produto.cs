namespace ProjetoSOLID.model;

public class Produto
{
    public int Id { get; private set; }
    public string Nome { get; private set; }
    public decimal Preco { get; private set; }
    public string Descricao { get; private set; }

    // Mantém a ordem dos parâmetros usada nos arquivos originais da aula.
    public Produto(int id, string nome, decimal preco, string descricao)
    {
        if (id < 0) throw new ArgumentOutOfRangeException(nameof(id));
        Id = id;
        Nome = string.Empty;
        Descricao = string.Empty;
        AtualizarDados(nome, preco, descricao);
    }

    public void AtualizarDados(string nome, decimal preco, string descricao)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("O nome do produto é obrigatório.", nameof(nome));
        if (nome.Trim().Length > 120)
            throw new ArgumentException("O nome deve ter até 120 caracteres.", nameof(nome));
        if ((descricao ?? string.Empty).Length > 1000)
            throw new ArgumentException("A descrição deve ter até 1000 caracteres.", nameof(descricao));

        _ = Dinheiro.ParaCentavos(preco);
        Nome = nome.Trim();
        Preco = preco;
        Descricao = (descricao ?? string.Empty).Trim();
    }

    internal void DefinirId(int id)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        Id = id;
    }
}
