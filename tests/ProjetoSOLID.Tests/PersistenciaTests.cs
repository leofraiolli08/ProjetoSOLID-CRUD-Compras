using ProjetoSOLID.dados;
using ProjetoSOLID.model;
using Xunit;

namespace ProjetoSOLID.Tests;

public class PersistenciaTests
{
    [Fact]
    public void Produtos_CrudCompleto_E_PersistenciaEmNovaConexao()
    {
        using var teste = new ContextoTeste();
        var p = new Produto(0, "Caneta", 12.50m, "Azul");
        teste.Produtos.Adicionar(p);
        Assert.True(p.Id > 0);
        Assert.Single(teste.Produtos.ListarTodos());

        // Simula reinício da aplicação com outra instância do banco.
        var reaberto = new ProdutoRepository(new SqliteDatabase(teste.Caminho));
        Assert.Equal(12.50m, reaberto.BuscarPorId(p.Id).Preco);

        reaberto.Atualizar(new Produto(p.Id, "Caneta melhorada", 15m, "Preta"));
        Assert.Equal("Caneta melhorada", teste.Produtos.BuscarPorId(p.Id).Nome);
        Assert.Equal("Preta", teste.Produtos.BuscarPorId(p.Id).Descricao);
        teste.Produtos.Remover(p.Id);
        Assert.Empty(reaberto.ListarTodos());
        Assert.Throws<KeyNotFoundException>(() => reaberto.BuscarPorId(p.Id));
    }

    [Fact]
    public void Compras_ItensDiversos_TotalExato_E_PrecoHistorico()
    {
        using var teste = new ContextoTeste();
        var a = teste.NovoProduto("Produto A", 12.50m);
        var b = teste.NovoProduto("Produto B", 8m);
        var compra = teste.NovaCompra((a, 2), (b, 1));
        teste.Compras.Adicionar(compra);

        var salva = teste.Compras.BuscarPorId(compra.Id);
        Assert.Equal(2, salva.Itens.Count);
        Assert.Equal(33m, salva.Total);
        Assert.Equal(25m, salva.Itens.Single(x => x.ProdutoId == a.Id).Subtotal);
        Assert.Equal(8m, salva.Itens.Single(x => x.ProdutoId == b.Id).Subtotal);
        Assert.All(salva.Itens, i => Assert.Equal(compra.Id, i.CompraId));

        teste.Produtos.Atualizar(new Produto(a.Id, "Produto A", 15m, "Novo preço"));
        Assert.Equal(33m, teste.Compras.BuscarPorId(compra.Id).Total);
        // O preço novo vale somente para compras futuras.
        var futura = teste.NovaCompra((a, 2));
        teste.Compras.Adicionar(futura);
        Assert.Equal(30m, teste.Compras.BuscarPorId(futura.Id).Total);
    }

    [Fact]
    public void MesmoProduto_EmComprasDistintas_RepresentaRelacaoMuitosParaMuitos()
    {
        using var teste = new ContextoTeste();
        var a = teste.NovoProduto("A", 7m);
        var b = teste.NovoProduto("B", 9m);
        var c1 = teste.NovaCompra((a, 1), (b, 2));
        var c2 = teste.NovaCompra((a, 3));
        teste.Compras.Adicionar(c1);
        teste.Compras.Adicionar(c2);
        Assert.Equal(2, teste.Compras.ListarTodos().Count);
        Assert.Contains(teste.Compras.BuscarPorId(c1.Id).Itens, i => i.ProdutoId == a.Id);
        Assert.Contains(teste.Compras.BuscarPorId(c2.Id).Itens, i => i.ProdutoId == a.Id);
    }

    [Fact]
    public void AtualizacaoCompra_AlteraQuantidade_Remove_Adiciona_E_PreservaPrecoAntigo()
    {
        using var teste = new ContextoTeste();
        var a = teste.NovoProduto("A", 12.50m);
        var b = teste.NovoProduto("B", 8m);
        var c = teste.NovoProduto("C", 3m);
        var compra = teste.NovaCompra((a, 2), (b, 1));
        teste.Compras.Adicionar(compra);
        teste.Produtos.Atualizar(new Produto(a.Id, "A", 15m, "Reajuste"));

        var atualizada = new Compra(compra.Id, compra.DataCompra, new[]
        {
            new ItemCompra(a.Id, 3, 15m), // tentativa de alterar preço histórico é ignorada pelo repo
            new ItemCompra(c.Id, 2, 3m)
        });
        teste.Compras.Atualizar(atualizada);
        var salva = teste.Compras.BuscarPorId(compra.Id);
        Assert.Equal(2, salva.Itens.Count);
        Assert.DoesNotContain(salva.Itens, x => x.ProdutoId == b.Id);
        Assert.Equal(12.50m, salva.Itens.Single(x => x.ProdutoId == a.Id).PrecoUnitarioNoMomentoDaCompra);
        Assert.Equal(43.50m, salva.Total); // 3*12,50 + 2*3,00
    }

    [Fact]
    public void CompraExcluida_RemoveItens_MasNaoApagaProdutos()
    {
        using var teste = new ContextoTeste();
        var a = teste.NovoProduto("A", 4m);
        var compra = teste.NovaCompra((a, 3));
        teste.Compras.Adicionar(compra);
        teste.Compras.Remover(compra.Id);
        Assert.Empty(teste.Compras.ListarTodos());
        Assert.Equal("A", teste.Produtos.BuscarPorId(a.Id).Nome);
        teste.Produtos.Remover(a.Id); // após excluir compra, FK não bloqueia
        Assert.Empty(teste.Produtos.ListarTodos());
    }

    [Fact]
    public void ProdutoComCompraNaoPodeSerRemovido()
    {
        using var teste = new ContextoTeste();
        var a = teste.NovoProduto("Produto relacionado", 4m);
        var compra = teste.NovaCompra((a, 1));
        teste.Compras.Adicionar(compra);
        Assert.Throws<InvalidOperationException>(() => teste.Produtos.Remover(a.Id));
        Assert.Equal(4m, teste.Compras.BuscarPorId(compra.Id).Total);
        Assert.Single(teste.Produtos.ListarTodos());
    }

    [Fact]
    public void OperacoesInvalidas_NaoCorrompemCompra_E_AtualizacaoEhAtomica()
    {
        using var teste = new ContextoTeste();
        var a = teste.NovoProduto("A", 11m);
        var compra = teste.NovaCompra((a, 1));
        teste.Compras.Adicionar(compra);
        Assert.Throws<KeyNotFoundException>(() => teste.Compras.BuscarPorId(9999));
        Assert.Throws<KeyNotFoundException>(() => teste.Produtos.Remover(9999));

        var invalida = new Compra(compra.Id, compra.DataCompra, new[]
        {
            new ItemCompra(a.Id, 5, 11m),
            new ItemCompra(9999, 1, 7m) // não existe no banco
        });
        Assert.Throws<KeyNotFoundException>(() => teste.Compras.Atualizar(invalida));
        Assert.Single(teste.Compras.BuscarPorId(compra.Id).Itens);
        Assert.Equal(11m, teste.Compras.BuscarPorId(compra.Id).Total);
    }

    [Fact]
    public void ModeloRejeita_PrecoInvalido_QuantidadeZero_CompraVazia_E_ProdutoDuplicado()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Produto(0, "A", 0m, ""));
        Assert.Throws<ArgumentException>(() => new Produto(0, "A", 12.345m, ""));
        Assert.Throws<ArgumentException>(() => new Produto(0, "", 10m, ""));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ItemCompra(1, 0, 10m));
        Assert.Throws<ArgumentException>(() => new Compra(0, DateTimeOffset.UtcNow, Array.Empty<ItemCompra>()));
        Assert.Throws<ArgumentException>(() => new Compra(0, DateTimeOffset.UtcNow, new[]
        {
            new ItemCompra(1, 1, 10m), new ItemCompra(1, 2, 10m)
        }));
    }

    [Fact]
    public void BancoGarante_ChavesEstrangeiras_E_UnicidadeNn()
    {
        using var teste = new ContextoTeste();
        var a = teste.NovoProduto("A", 2m);
        var compra = teste.NovaCompra((a, 1));
        teste.Compras.Adicionar(compra);
        using var conexao = teste.Banco.AbrirConexao();
        using var comando = conexao.CreateCommand();
        comando.CommandText = "SELECT COUNT(*) FROM ItensCompra WHERE CompraId=$compra AND ProdutoId=$produto";
        comando.Parameters.AddWithValue("$compra", compra.Id);
        comando.Parameters.AddWithValue("$produto", a.Id);
        Assert.Equal(1L, Convert.ToInt64(comando.ExecuteScalar()));

        using var tentarDuplicar = conexao.CreateCommand();
        tentarDuplicar.CommandText = """
            INSERT INTO ItensCompra (CompraId, ProdutoId, Quantidade, PrecoUnitarioCentavos)
            VALUES ($compra, $produto, 1, 200);
            """;
        tentarDuplicar.Parameters.AddWithValue("$compra", compra.Id);
        tentarDuplicar.Parameters.AddWithValue("$produto", a.Id);
        Assert.Throws<Microsoft.Data.Sqlite.SqliteException>(() => tentarDuplicar.ExecuteNonQuery());
    }

    private sealed class ContextoTeste : IDisposable
    {
        private readonly string pasta = Path.Combine(Path.GetTempPath(), "ProjetoSOLID-Tests", Guid.NewGuid().ToString("N"));
        public string Caminho => Path.Combine(pasta, "teste.db");
        public SqliteDatabase Banco { get; }
        public ProdutoRepository Produtos { get; }
        public CompraRepository Compras { get; }

        public ContextoTeste()
        {
            Banco = new SqliteDatabase(Caminho);
            Produtos = new ProdutoRepository(Banco);
            Compras = new CompraRepository(Banco);
        }

        public Produto NovoProduto(string nome, decimal preco)
        {
            var produto = new Produto(0, nome, preco, "Produto de teste");
            Produtos.Adicionar(produto);
            return produto;
        }

        public Compra NovaCompra(params (Produto Produto, int Quantidade)[] pares) => new(
            0, DateTimeOffset.UtcNow,
            pares.Select(x => new ItemCompra(x.Produto.Id, x.Quantidade, x.Produto.Preco, x.Produto.Nome)));

        public void Dispose()
        {
            // Pooling=false garante que o diretório de teste pode ser excluído.
            if (Directory.Exists(pasta)) Directory.Delete(pasta, recursive: true);
        }
    }
}
