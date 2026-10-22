using Microsoft.Data.Sqlite;
using ProjetoSOLID.model;

namespace ProjetoSOLID.dados;

public sealed class ProdutoRepository : IRepository<Produto>
{
    private readonly SqliteDatabase banco;
    public ProdutoRepository(SqliteDatabase banco) => this.banco = banco;

    public void Adicionar(Produto novo)
    {
        ArgumentNullException.ThrowIfNull(novo);
        if (novo.Id != 0) throw new ArgumentException("Novo produto deve ter ID zero.", nameof(novo));
        using var conexao = banco.AbrirConexao();
        using var comando = conexao.CreateCommand();
        comando.CommandText = "INSERT INTO Produtos (Nome, Descricao, PrecoCentavos) VALUES ($nome, $descricao, $preco);";
        comando.Parameters.AddWithValue("$nome", novo.Nome);
        comando.Parameters.AddWithValue("$descricao", novo.Descricao);
        comando.Parameters.AddWithValue("$preco", Dinheiro.ParaCentavos(novo.Preco));
        comando.ExecuteNonQuery();
        using var id = conexao.CreateCommand();
        id.CommandText = "SELECT last_insert_rowid();";
        novo.DefinirId(Convert.ToInt32(id.ExecuteScalar()));
    }

    public List<Produto> ListarTodos()
    {
        using var conexao = banco.AbrirConexao();
        using var comando = conexao.CreateCommand();
        comando.CommandText = "SELECT Id, Nome, Descricao, PrecoCentavos FROM Produtos ORDER BY Id;";
        using var leitor = comando.ExecuteReader();
        var produtos = new List<Produto>();
        while (leitor.Read()) produtos.Add(Ler(leitor));
        return produtos;
    }

    public Produto BuscarPorId(int id)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        using var conexao = banco.AbrirConexao();
        using var comando = conexao.CreateCommand();
        comando.CommandText = "SELECT Id, Nome, Descricao, PrecoCentavos FROM Produtos WHERE Id = $id;";
        comando.Parameters.AddWithValue("$id", id);
        using var leitor = comando.ExecuteReader();
        if (!leitor.Read()) throw new KeyNotFoundException($"Produto {id} não encontrado.");
        return Ler(leitor);
    }

    public void Atualizar(Produto entidade)
    {
        ArgumentNullException.ThrowIfNull(entidade);
        if (entidade.Id <= 0) throw new ArgumentException("Informe um produto persistido.", nameof(entidade));
        using var conexao = banco.AbrirConexao();
        using var comando = conexao.CreateCommand();
        comando.CommandText = "UPDATE Produtos SET Nome=$nome, Descricao=$descricao, PrecoCentavos=$preco WHERE Id=$id;";
        comando.Parameters.AddWithValue("$nome", entidade.Nome);
        comando.Parameters.AddWithValue("$descricao", entidade.Descricao);
        comando.Parameters.AddWithValue("$preco", Dinheiro.ParaCentavos(entidade.Preco));
        comando.Parameters.AddWithValue("$id", entidade.Id);
        if (comando.ExecuteNonQuery() == 0)
            throw new KeyNotFoundException($"Produto {entidade.Id} não encontrado.");
    }

    public void Remover(Produto entidade)
    {
        ArgumentNullException.ThrowIfNull(entidade);
        Remover(entidade.Id);
    }

    public void Remover(int id)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        try
        {
            using var conexao = banco.AbrirConexao();
            using var comando = conexao.CreateCommand();
            comando.CommandText = "DELETE FROM Produtos WHERE Id = $id;";
            comando.Parameters.AddWithValue("$id", id);
            if (comando.ExecuteNonQuery() == 0)
                throw new KeyNotFoundException($"Produto {id} não encontrado.");
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            throw new InvalidOperationException(
                "Este produto participa de compras registradas e não pode ser excluído.", ex);
        }
    }

    private static Produto Ler(SqliteDataReader leitor) => new(
        leitor.GetInt32(0), leitor.GetString(1),
        Dinheiro.ParaReais(leitor.GetInt64(3)), leitor.GetString(2));
}
