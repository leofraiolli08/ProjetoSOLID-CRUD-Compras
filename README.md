# ProjetoSOLID — CRUD de Produtos e Compras

Aplicação de console em **C# / .NET 8**, desenvolvida como continuação do projeto de aula, com arquitetura **MVC**, repositórios `IRepository<T>` e persistência real em **SQLite**.

## Requisitos da atividade

- CRUD de **Produto**: cadastrar, listar, consultar, atualizar e excluir.
- CRUD de **Compra**: registrar, listar, consultar detalhes, atualizar itens e excluir.
- Relação **muitos-para-muitos (N:N)** entre compras e produtos.
- Uso de MVC e princípios SOLID.
- Entregar o link do repositório GitHub.
- Para **MB**, integrar um repositório a banco de dados real **OU** realizar deploy pelo GitHub Actions. Este projeto usa a primeira alternativa: **SQLite**.

## Como executar no Visual Studio 2022 (Windows)

1. Instale o Visual Studio 2022 com a carga de trabalho **Desenvolvimento para desktop com .NET** e o **SDK .NET 8**. Atualize o VS2022, se necessário.
2. Extraia o ZIP, abra **`ProjetoSOLID.sln`** no Visual Studio 2022.
3. Aguarde a restauração automática dos pacotes NuGet (é necessário acesso à internet no primeiro restore).
4. Clique com o botão direito em `ProjetoSOLID` (não no projeto de testes) > **Definir como Projeto de Inicialização**.
5. Pressione **Ctrl + F5** para executar o console.
6. Para os testes, use **Teste > Executar Todos os Testes**.

### Pelo terminal / PowerShell

Abra a pasta extraída onde está `ProjetoSOLID.sln` e execute:

```powershell
dotnet restore .\ProjetoSOLID.sln
dotnet build .\ProjetoSOLID.sln -c Release
dotnet test .\ProjetoSOLID.sln -c Release
dotnet run --project .\src\ProjetoSOLID\ProjetoSOLID.csproj
```

No Linux/macOS, substitua `\` por `/` nos caminhos.

## Como usar os menus

```text
MENU PRINCIPAL
1 - Gerenciar produtos
2 - Gerenciar compras
0 - Sair

PRODUTOS
1 - Cadastrar produto
2 - Listar produtos
3 - Buscar produto por ID
4 - Atualizar produto
5 - Excluir produto
0 - Voltar

COMPRAS
1 - Registrar compra
2 - Listar compras
3 - Consultar compra por ID
4 - Atualizar compra
5 - Excluir compra
0 - Voltar
```

O cadastro solicita **nome**, **preço** (por exemplo, `12,50`) e **descrição**. O ID é gerado pelo SQLite. Durante a compra, digite o ID dos produtos e as quantidades; é possível adicionar, somar, ajustar ou remover itens antes de salvar. Digite `0` nas entradas que indicam cancelamento. Excluir um produto presente em compras anteriores é bloqueado para preservar o histórico. As exclusões têm confirmação.

## Persistência e relacionamento N:N

```text
Produtos (Id PK, Nome, Descricao, PrecoCentavos)
     1
     |
     N
ItensCompra (CompraId PK/FK, ProdutoId PK/FK, Quantidade, PrecoUnitarioCentavos)
     N
     |
     1
Compras (Id PK, DataCompra)
```

- A tabela `ItensCompra` implementa a relação **N:N**, com chave primária composta `(CompraId, ProdutoId)`.
- `ProdutoId` tem chave estrangeira `ON DELETE RESTRICT` para impedir exclusão que quebre o histórico.
- `CompraId` tem chave estrangeira `ON DELETE CASCADE` para remover os itens quando uma compra é excluída.
- Cada conexão ativa `PRAGMA foreign_keys=ON`.
- Preços são `decimal` no domínio e **inteiros em centavos no SQLite** (sem arredondamento binário de `REAL`). O cadastro admite no máximo duas casas decimais.
- O preço unitário gravado em `ItensCompra` é **histórico**. Se o preço do produto mudar, compras antigas continuam com o mesmo total.
- Ao editar uma compra, os produtos que permanecerem nela mantêm o preço histórico; produtos novos recebem o preço atual do cadastro.
- Os itens e a compra são salvos de forma **atômica**, usando transações SQLite.
- O schema SQLite é criado automaticamente na primeira execução (`dados/SqliteDatabase.cs`).

### Onde fica o banco de dados?

Por padrão: **`%LOCALAPPDATA%\ProjetoSOLID\projetosolid.db`** (Windows). O programa imprime o caminho exato ao iniciar. Os dados continuam disponíveis depois que você fecha e reabre o programa.

Para escolher outro banco (útil em testes manuais), defina antes de iniciar:

```powershell
$env:PROJETO_SOLID_DB = "$PWD\meu-teste.db"
dotnet run --project .\src\ProjetoSOLID\ProjetoSOLID.csproj
```

O arquivo `.db` não deve ser incluído no Git; está ignorado em `.gitignore`.

## Arquitetura e SOLID

| Pasta | Responsabilidade |
|---|---|
| `model/` | Entidades e validações de produtos, compras, itens e valores monetários |
| `view/` | Menus, entrada de dados, cancelamentos, formatação e exibição |
| `controller/` | Coordenação dos casos de uso, sem SQL |
| `dados/` | `IRepository<T>`, criação do banco e persistência SQL parametrizada |
| `tests/` | Testes automatizados xUnit, cada cenário com SQLite temporário isolado |

**SRP:** classes com responsabilidades distintas. **OCP/DIP:** controladores dependem das abstrações `IRepository<T>` e interfaces de view; persistência pode ser substituída por outra implementação. **ISP:** interfaces específicas de view com operações relacionadas. **LSP:** as duas implementações concretas cumprem o contrato genérico sem alterar suas assinaturas originais.

## Arquivos da aula aproveitados

Foram aproveitados e adaptados **`IRepository.cs`**, **`ICadastroView.cs`**, **`IMenuView.cs`**, **`MenuConsoleView.cs`**, **`CadastroProduto.cs`** e **`ProdutoController.cs`**. As assinaturas de `IRepository<T>` e `ICadastroView<T>` foram preservadas. `IMenuView` recebeu dois métodos para os submenus; o ID fixo de produto foi substituído pelo autoincremento do SQLite; a leitura de preços passou a validar valores; `ExibirMensagem` passou a exibir texto; a atualização agora solicita novos dados.

Não estavam disponíveis nos anexos `Produto.cs`, `IProdutoView.cs`, `Program.cs`, implementações concretas dos repositórios nem arquivos de solução, então foram reconstruídos para tornar o projeto autocontido.

## Roteiro de teste manual

1. Cadastre o produto **A** por **R$ 12,50** e o produto **B** por **R$ 8,00**.
2. Registre uma compra com **2 unidades de A e 1 de B**. Confira o total **R$ 33,00**.
3. Altere o preço do A para **R$ 15,00**. Consulte a compra anterior: ainda deve mostrar **R$ 33,00**.
4. Crie outra compra contendo A: deverá usar o novo preço.
5. Edite a primeira compra e ajuste a quantidade de A, remova B ou adicione outro produto; confira os subtotais e preços históricos.
6. Tente excluir A enquanto ele pertence a alguma compra; o sistema deve impedir.
7. Exclua uma compra e confirme que os produtos permanecem cadastrados.
8. Feche e reabra a aplicação; produtos e compras devem persistir.
9. Experimente entradas inválidas, IDs inexistentes e o cancelamento usando `0`.

### Testes automatizados

Os testes em `tests/ProjetoSOLID.Tests/PersistenciaTests.cs` abrangem operações CRUD, persistência entre novas conexões, relacionamento N:N, cálculo e preço histórico, atualização transacional, exclusões protegidas, integridade e validações. Rode com:

```powershell
dotnet test .\ProjetoSOLID.sln --verbosity normal
```

O workflow `.github/workflows/ci.yml` realiza **integração contínua (CI)** — restore, build e testes — em push e pull request. **CI não é deploy** e não foi usado para cumprir o requisito MB.

## Como este projeto atende ao MB

A alternativa selecionada pela atividade é **"IRepository conectado a um banco de dados"**:

- `dados/IRepository.cs`: contrato genérico original da aula.
- `dados/ProdutoRepository.cs`: CRUD concreto de produtos no SQLite.
- `dados/CompraRepository.cs`: CRUD concreto de compras e itens, com transações.
- `dados/SqliteDatabase.cs`: banco real, criação automática das três tabelas, chaves estrangeiras e restrições.
- `Program.cs`: instancia `SqliteDatabase`, injeta `ProdutoRepository`/`CompraRepository` nos controladores e executa o aplicativo.

## Como publicar no GitHub e entregar

O arquivo ZIP não publica o código automaticamente. Entre na pasta do projeto no PowerShell e, com o Git instalado, rode:

```powershell
git init
git add .
git commit -m "CRUD produtos e compras com MVC, SOLID e SQLite"
git branch -M main
```

**Opção A: usando GitHub CLI (`gh`) autenticado:**

```powershell
gh auth login
gh repo create ProjetoSOLID --public --source . --remote origin --push
```

**Opção B: pelo site GitHub:** crie um repositório público vazio chamado `ProjetoSOLID` (não marque README nem `.gitignore` porque eles já existem), então execute:

```powershell
git remote add origin https://github.com/SEU_USUARIO/ProjetoSOLID.git
git push -u origin main
```

Substitua `SEU_USUARIO` pelo seu usuário real. Depois, copie o **link verdadeiro do repositório** e envie à professora. Não há necessidade de compartilhar tokens, senhas ou dados do banco.

### Situação da verificação nesta entrega

O código e os testes foram preparados, mas o ambiente usado na montagem **não possui o SDK `dotnet` nem acesso aos pacotes NuGet**, portanto **não foi possível executar `dotnet build` nem `dotnet test` aqui**. O esquema SQL foi executado separadamente com SQLite e as restrições de FK, unicidade, exclusão em cascata e preservação dos preços históricos foram verificadas. Essa verificação **não substitui** compilar e executar os testes C#. O workflow de CI executará build e testes após publicação no GitHub.
