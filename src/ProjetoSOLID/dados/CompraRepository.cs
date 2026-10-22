using System.Globalization;
using Microsoft.Data.Sqlite;
using ProjetoSOLID.model;

namespace ProjetoSOLID.dados;

/// <summary>Persiste cada compra e seus itens na mesma transação SQLite.</summary>
public sealed class CompraRepository : IRepository<Compra>
{
    private readonly SqliteDatabase banco;
    public CompraRepository(SqliteDatabase banco) => this.banco = banco;

    public void Adicionar(Compra novo)
    {
        ArgumentNullException.ThrowIfNull(novo);
        if (novo.Id != 0) throw new ArgumentException("Nova compra deve ter ID zero.", nameof(novo));
        using var conexao = banco.AbrirConexao();
        using var transacao = conexao.BeginTransaction();

        // Reconsulta os preços no MESMO contexto transacional, evitando valores manipulados.
        var linhas = PrepararLinhas(conexao, transacao, novo.Itens);
        using var comando = CriarComando(conexao, transacao,
            "INSERT INTO Compras (DataCompra) VALUES ($data);");
        comando.Parameters.AddWithValue("$data", novo.DataCompra.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
        comando.ExecuteNonQuery();
        using var obterId = CriarComando(conexao, transacao, "SELECT last_insert_rowid();");
        int id = Convert.ToInt32(obterId.ExecuteScalar());
        InserirItens(conexao, transacao, id, linhas);
        transacao.Commit();
        novo.DefinirId(id);
    }

    public List<Compra> ListarTodos()
    {
        using var conexao = banco.AbrirConexao();
        var referencias = new List<(int Id, DateTimeOffset Data)>();
        using (var comando = conexao.CreateCommand())
        {
            comando.CommandText = "SELECT Id, DataCompra FROM Compras ORDER BY Id;";
            using var leitor = comando.ExecuteReader();
            while (leitor.Read()) referencias.Add((leitor.GetInt32(0), InterpretarData(leitor.GetString(1))));
        }
        return referencias.Select(r => new Compra(r.Id, r.Data, LerItens(conexao, r.Id))).ToList();
    }

    public Compra BuscarPorId(int id)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        using var conexao = banco.AbrirConexao();
        using var comando = conexao.CreateCommand();
        comando.CommandText = "SELECT DataCompra FROM Compras WHERE Id=$id;";
        comando.Parameters.AddWithValue("$id", id);
        string? data = comando.ExecuteScalar() as string;
        if (data is null) throw new KeyNotFoundException($"Compra {id} não encontrada.");
        return new Compra(id, InterpretarData(data), LerItens(conexao, id));
    }

    public void Atualizar(Compra entidade)
    {
        ArgumentNullException.ThrowIfNull(entidade);
        if (entidade.Id <= 0) throw new ArgumentException("Informe uma compra persistida.", nameof(entidade));
        using var conexao = banco.AbrirConexao();
        using var transacao = conexao.BeginTransaction();
        using (var existe = CriarComando(conexao, transacao, "SELECT COUNT(*) FROM Compras WHERE Id=$id;"))
        {
            existe.Parameters.AddWithValue("$id", entidade.Id);
            if (Convert.ToInt64(existe.ExecuteScalar()) == 0L)
                throw new KeyNotFoundException($"Compra {entidade.Id} não encontrada.");
        }

        // Para os produtos que JÁ estavam na compra, reaproveita o preço histórico
        // mesmo que o chamador envie outro preço. Produtos novos recebem preço atual.
        var historicos = new Dictionary<int, long>();
        using (var antigos = CriarComando(conexao, transacao,
                   "SELECT ProdutoId, PrecoUnitarioCentavos FROM ItensCompra WHERE CompraId=$id;"))
        {
            antigos.Parameters.AddWithValue("$id", entidade.Id);
            using var leitor = antigos.ExecuteReader();
            while (leitor.Read()) historicos.Add(leitor.GetInt32(0), leitor.GetInt64(1));
        }
        var linhas = PrepararLinhas(conexao, transacao, entidade.Itens, historicos);
        using (var excluir = CriarComando(conexao, transacao, "DELETE FROM ItensCompra WHERE CompraId=$id;"))
        {
            excluir.Parameters.AddWithValue("$id", entidade.Id);
            excluir.ExecuteNonQuery();
        }
        InserirItens(conexao, transacao, entidade.Id, linhas);
        transacao.Commit();
    }

    public void Remover(Compra entidade)
    {
        ArgumentNullException.ThrowIfNull(entidade);
        Remover(entidade.Id);
    }

    public void Remover(int id)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        using var conexao = banco.AbrirConexao();
        using var transacao = conexao.BeginTransaction();
        using var comando = CriarComando(conexao, transacao, "DELETE FROM Compras WHERE Id=$id;");
        comando.Parameters.AddWithValue("$id", id);
        if (comando.ExecuteNonQuery() == 0)
            throw new KeyNotFoundException($"Compra {id} não encontrada.");
        // ON DELETE CASCADE remove ItensCompra, sem remover Produtos.
        transacao.Commit();
    }

    private static List<(int ProdutoId, int Quantidade, long PrecoCentavos)> PrepararLinhas(
        SqliteConnection conexao, SqliteTransaction transacao, IReadOnlyList<ItemCompra> itens,
        IReadOnlyDictionary<int, long>? precosHistoricos = null)
    {
        if (itens.Count == 0) throw new ArgumentException("Uma compra não pode ficar vazia.");
        if (itens.Select(x => x.ProdutoId).Distinct().Count() != itens.Count)
            throw new ArgumentException("Produto duplicado na compra.");
        var linhas = new List<(int ProdutoId, int Quantidade, long PrecoCentavos)>();
        foreach (var item in itens)
        {
            if (item.Quantidade <= 0) throw new ArgumentException("Quantidade inválida.");
            long centavos;
            if (precosHistoricos is not null && precosHistoricos.TryGetValue(item.ProdutoId, out long anterior))
                centavos = anterior;
            else
            {
                using var comando = CriarComando(conexao, transacao,
                    "SELECT PrecoCentavos FROM Produtos WHERE Id=$id;");
                comando.Parameters.AddWithValue("$id", item.ProdutoId);
                object? encontrado = comando.ExecuteScalar();
                if (encontrado is null)
                    throw new KeyNotFoundException($"Produto {item.ProdutoId} não encontrado.");
                centavos = Convert.ToInt64(encontrado);
            }
            // Avalia limites dos subtotais antes de qualquer escrita.
            _ = checked((decimal)item.Quantidade * Dinheiro.ParaReais(centavos));
            linhas.Add((item.ProdutoId, item.Quantidade, centavos));
        }
        return linhas;
    }

    private static void InserirItens(SqliteConnection conexao, SqliteTransaction transacao, int compraId,
        IEnumerable<(int ProdutoId, int Quantidade, long PrecoCentavos)> linhas)
    {
        foreach (var linha in linhas)
        {
            using var comando = CriarComando(conexao, transacao,
                "INSERT INTO ItensCompra (CompraId, ProdutoId, Quantidade, PrecoUnitarioCentavos) " +
                "VALUES ($compra, $produto, $quantidade, $preco);");
            comando.Parameters.AddWithValue("$compra", compraId);
            comando.Parameters.AddWithValue("$produto", linha.ProdutoId);
            comando.Parameters.AddWithValue("$quantidade", linha.Quantidade);
            comando.Parameters.AddWithValue("$preco", linha.PrecoCentavos);
            comando.ExecuteNonQuery();
        }
    }

    private static List<ItemCompra> LerItens(SqliteConnection conexao, int id)
    {
        using var comando = conexao.CreateCommand();
        comando.CommandText = """
            SELECT i.ProdutoId, p.Nome, i.Quantidade, i.PrecoUnitarioCentavos
            FROM ItensCompra i INNER JOIN Produtos p ON p.Id = i.ProdutoId
            WHERE i.CompraId=$id ORDER BY i.ProdutoId;
            """;
        comando.Parameters.AddWithValue("$id", id);
        using var leitor = comando.ExecuteReader();
        var itens = new List<ItemCompra>();
        while (leitor.Read())
        {
            var item = new ItemCompra(leitor.GetInt32(0), leitor.GetInt32(2),
                Dinheiro.ParaReais(leitor.GetInt64(3)), leitor.GetString(1));
            item.DefinirCompraId(id);
            itens.Add(item);
        }
        return itens;
    }

    private static SqliteCommand CriarComando(SqliteConnection conexao, SqliteTransaction transacao, string sql)
    {
        var comando = conexao.CreateCommand();
        comando.Transaction = transacao;
        comando.CommandText = sql;
        return comando;
    }

    private static DateTimeOffset InterpretarData(string texto) =>
        DateTimeOffset.Parse(texto, CultureInfo.InvariantCulture, DateTimeStyles.None);
}
