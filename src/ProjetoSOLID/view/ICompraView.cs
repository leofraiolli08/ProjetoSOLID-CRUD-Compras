using ProjetoSOLID.model;

namespace ProjetoSOLID.view;

public sealed record SolicitacaoItem(int ProdutoId, int Quantidade);

public interface ICompraView
{
    List<SolicitacaoItem>? ObterItens(IReadOnlyList<Produto> produtos, Compra? compraExistente = null);
    void MostrarResumo(Compra compra);
    void MostrarDetalhes(Compra compra);
    void ExibirMensagem(string mensagem);
}
