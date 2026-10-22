using ProjetoSOLID.dados;
using ProjetoSOLID.model;
using ProjetoSOLID.view;

namespace ProjetoSOLID.controller;

public class CompraController
{
    private readonly IRepository<Compra> compras;
    private readonly IRepository<Produto> produtos;
    private readonly ICompraView view;

    public CompraController(IRepository<Compra> compras, IRepository<Produto> produtos, ICompraView view)
    {
        this.compras = compras;
        this.produtos = produtos;
        this.view = view;
    }

    public void Cadastrar()
    {
        var disponiveis = produtos.ListarTodos();
        if (disponiveis.Count == 0)
        {
            view.ExibirMensagem("Cadastre pelo menos um produto antes de registrar compras.");
            return;
        }
        var selecao = view.ObterItens(disponiveis) ?? throw new OperationCanceledException();
        Compra compra = new(0, DateTimeOffset.Now, ConstruirItens(selecao, null));
        compras.Adicionar(compra);
        // Reconsulta o registro: preços gravados no repositório podem ter sido atualizados.
        view.ExibirMensagem($"Compra #{compra.Id} registrada com sucesso!");
        view.MostrarDetalhes(compras.BuscarPorId(compra.Id));
    }

    public void Listar()
    {
        var lista = compras.ListarTodos();
        if (lista.Count == 0) view.ExibirMensagem("Nenhuma compra cadastrada.");
        foreach (Compra compra in lista) view.MostrarResumo(compra);
    }

    public void Consultar(int id) => view.MostrarDetalhes(compras.BuscarPorId(id));

    public void Atualizar(int id)
    {
        Compra atual = compras.BuscarPorId(id);
        view.MostrarDetalhes(atual);
        var selecao = view.ObterItens(produtos.ListarTodos(), atual)
            ?? throw new OperationCanceledException();
        var substituta = new Compra(atual.Id, atual.DataCompra, ConstruirItens(selecao, atual));
        compras.Atualizar(substituta);
        view.ExibirMensagem("Compra atualizada com sucesso!");
        Consultar(id);
    }

    public void Remover(int id)
    {
        compras.Remover(id);
        view.ExibirMensagem("Compra excluída com sucesso; os produtos foram preservados.");
    }

    private List<ItemCompra> ConstruirItens(List<SolicitacaoItem> selecao, Compra? anterior)
    {
        if (selecao.Count == 0) throw new ArgumentException("Compra precisa de itens.");
        var antigos = anterior?.Itens.ToDictionary(i => i.ProdutoId);
        var itens = new List<ItemCompra>();
        foreach (SolicitacaoItem pedido in selecao)
        {
            Produto atual = produtos.BuscarPorId(pedido.ProdutoId);
            decimal preco = antigos is not null && antigos.TryGetValue(pedido.ProdutoId, out ItemCompra? item)
                ? item.PrecoUnitarioNoMomentoDaCompra : atual.Preco;
            itens.Add(new ItemCompra(pedido.ProdutoId, pedido.Quantidade, preco, atual.Nome));
        }
        return itens;
    }
}
