namespace ProjetoSOLID.view
{
    public interface ICadastroView<T>
    {
        T ObterDados();
        void ExibirMensagem(string m);
    }
}
