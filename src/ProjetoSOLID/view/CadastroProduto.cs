using ProjetoSOLID.model;

namespace ProjetoSOLID.view;

// Adaptação do cadastro original: id gerado pelo SQLite, leitura robusta e mensagem funcional.
public class CadastroProduto : ICadastroView<Produto>
{
    public void ExibirMensagem(string m) => LeituraConsole.Mensagem(m);

    public Produto ObterDados()
    {
        string nome = LeituraConsole.LerTextoObrigatorio("Nome");
        decimal preco = LeituraConsole.LerPreco();
        string descricao = LeituraConsole.LerTextoObrigatorio("Descrição", permitirVazio: true);
        return new Produto(0, nome, preco, descricao);
    }
}
