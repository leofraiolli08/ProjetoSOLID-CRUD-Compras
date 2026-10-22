using Microsoft.Data.Sqlite;

namespace ProjetoSOLID.dados;

/// <summary>Configuração do banco e criação automática das tabelas.</summary>
public sealed class SqliteDatabase
{
    public string Caminho { get; }

    public SqliteDatabase(string caminho)
    {
        if (string.IsNullOrWhiteSpace(caminho))
            throw new ArgumentException("Informe um caminho para o SQLite.", nameof(caminho));
        Caminho = Path.GetFullPath(caminho);
        Directory.CreateDirectory(Path.GetDirectoryName(Caminho)!);
        Inicializar();
    }

    public static string CaminhoPadrao()
    {
        string? personalizado = Environment.GetEnvironmentVariable("PROJETO_SOLID_DB");
        if (!string.IsNullOrWhiteSpace(personalizado)) return Path.GetFullPath(personalizado);
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ProjetoSOLID", "projetosolid.db");
    }

    public SqliteConnection AbrirConexao()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = Caminho,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false
        };
        var conexao = new SqliteConnection(builder.ToString());
        conexao.Open();
        using var comando = conexao.CreateCommand();
        comando.CommandText = "PRAGMA foreign_keys = ON; PRAGMA busy_timeout = 5000;";
        comando.ExecuteNonQuery();
        return conexao;
    }

    private void Inicializar()
    {
        using var conexao = AbrirConexao();
        using var comando = conexao.CreateCommand();
        comando.CommandText = """
            CREATE TABLE IF NOT EXISTS Produtos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nome TEXT NOT NULL CHECK(length(trim(Nome)) > 0),
                Descricao TEXT NOT NULL,
                PrecoCentavos INTEGER NOT NULL CHECK(PrecoCentavos > 0)
            );
            CREATE TABLE IF NOT EXISTS Compras (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                DataCompra TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS ItensCompra (
                CompraId INTEGER NOT NULL,
                ProdutoId INTEGER NOT NULL,
                Quantidade INTEGER NOT NULL CHECK(Quantidade > 0),
                PrecoUnitarioCentavos INTEGER NOT NULL CHECK(PrecoUnitarioCentavos > 0),
                PRIMARY KEY (CompraId, ProdutoId),
                FOREIGN KEY (CompraId) REFERENCES Compras(Id) ON DELETE CASCADE,
                FOREIGN KEY (ProdutoId) REFERENCES Produtos(Id) ON DELETE RESTRICT
            );
            CREATE INDEX IF NOT EXISTS IX_ItensCompra_ProdutoId ON ItensCompra(ProdutoId);
            """;
        comando.ExecuteNonQuery();
    }
}
