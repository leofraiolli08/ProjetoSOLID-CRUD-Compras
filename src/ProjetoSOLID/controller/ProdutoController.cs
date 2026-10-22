using ProjetoSOLID.dados;
using ProjetoSOLID.model;
using ProjetoSOLID.view;

namespace ProjetoSOLID.controller;

// Mantém as mesmas dependências e operações da controller de aula.
public class ProdutoController
{
    private readonly IRepository<Produto> repository;
    private readonly IProdutoView view;
    private readonly ICadastroView<Produto> cadastroView;

    public ProdutoController(IProdutoView view, IRepository<Produto> repository,
        ICadastroView<Produto> cadastroView)
    {
        this.view = view;
        this.repository = repository;
        this.cadastroView = cadastroView;
    }

    public void Cadastrar()
    {
        Produto produto = cadastroView.ObterDados();
        repository.Adicionar(produto);
        cadastroView.ExibirMensagem($"Produto #{produto.Id} cadastrado com sucesso!");
    }

    public void Listar()
    {
        List<Produto> produtos = repository.ListarTodos();
        if (produtos.Count == 0) cadastroView.ExibirMensagem("Nenhum produto cadastrado.");
        foreach (Produto produto in produtos) view.MostrarProduto(produto);
    }

    public Produto Buscar(int id) => repository.BuscarPorId(id);
    public void Mostrar(int id) => view.MostrarProduto(Buscar(id));

    public void Atualizar(Produto produto)
    {
        repository.Atualizar(produto);
        cadastroView.ExibirMensagem("Produto atualizado com sucesso!");
    }

    public void Atualizar(int id)
    {
        Produto atual = Buscar(id);  // não pode editar um produto inexistente
        cadastroView.ExibirMensagem($"Editando produto #{atual.Id} - {atual.Nome}");
        Produto novosDados = cadastroView.ObterDados();
        atual.AtualizarDados(novosDados.Nome, novosDados.Preco, novosDados.Descricao);
        Atualizar(atual);
    }

    public void Remover(Produto produto) => Remover(produto.Id);
    public void Remover(int id)
    {
        repository.Remover(id);
        cadastroView.ExibirMensagem("Produto excluído com sucesso!");
    }
}
