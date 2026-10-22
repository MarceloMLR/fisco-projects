---
status: aceita
date: 2026-10-22
decision-makers: Marcelo Ribeiro
---

# ADR-0113: Demonstração no GitHub Codespaces

Complementa o ADR-0089 (dados de demonstração carregados pela API) e o ADR-0078 (entrada local).

## Contexto e problema

Até aqui, a demonstração roda no computador de Marcelo: o banco no Docker, a API com as telas e um túnel do Cloudflare que gera o link. Com o computador desligado, não há demonstração, e montar o ambiente em outro computador leva muitos comandos (SDK do .NET, Node, banco, segredos, pacotes, compilação e carga). Como pôr a demonstração no ar, de qualquer computador, com poucos passos e sem custo?

## Fatores de decisão

- O sistema de verdade, com tudo funcionando, não uma cópia das telas.
- Poucos passos para Marcelo, de qualquer computador, só com o navegador.
- Sem custo além das cotas gratuitas, e sem recursos do empregador (ADR-0002).
- Nenhum segredo no repositório (ADR-0052): nem a senha do banco local nem a chave do Syncfusion.
- Só dados fictícios, nunca a produção (ADR-0078, ADR-0089).
- Nada muda na aplicação nem na verificação do PR.

## Opções consideradas

1. Continuar só com o túnel do Cloudflare a partir do computador de Marcelo.
2. GitHub Codespaces, com a configuração no repositório: o Codespace se prepara ao ser criado e sobe a demonstração ao ser aberto.
3. Uma instalação de demonstração no Azure.
4. Um artefato só com as telas e as respostas da API gravadas.

## Resultado da decisão

Opção 2, mantendo a 1 para quando o computador de Marcelo estiver ligado. A opção 3 espera a assinatura do Azure (Fundação-6) e tem custo. A opção 4 não salva nada e mostraria telas que se comportam diferente do sistema.

- **Configuração**: `.devcontainer/devcontainer.json`, com a imagem de desenvolvimento do .NET da versão do `global.json`, o Node da versão do `.nvmrc` e o Docker dentro do Codespace (para o banco do `compose.yaml`), numa máquina de 2 núcleos e 8 GB. Os testes da verificação reprovam a configuração com versões ou porta diferentes das do projeto.
- **Ao criar** (`prepare.sh`, uma vez): uma senha do banco local só daquele Codespace, no `.env`, que não vai para o Git; a conexão da API nos segredos do usuário; os pacotes, as telas e a API compilados; a imagem do banco baixada.
- **Ao abrir** (`start.sh`, a cada abertura): o banco, a API em desenvolvimento com as telas na porta 5175 e a carga dos dados de demonstração que faltam (ADR-0089). Com a API já no ar, só mostra o endereço. O log da API fica no terminal, o que também impede o Codespace de parar enquanto a demonstração é usada.
- **Link**: o endereço da porta 5175 fica público e aparece no terminal, com as pessoas para entrar. Quem tiver o link entra sem senha, pela entrada local, como qualquer pessoa do diretório de demonstração, inclusive o administrador. Isso é aceito porque os dados são fictícios e o link só existe enquanto o Codespace roda; para fechar, a porta volta a privada ou o Codespace é parado.
- **Chave do Syncfusion**: só como segredo do Codespaces na conta de Marcelo (`Syncfusion__LicenseKey`, com acesso ao repositório); sem ela, a Linha do tempo mostra o aviso de licença.
- **Conta**: o Codespace usa a cota gratuita mensal do GitHub (na conta Free, 120 horas de processamento, cerca de 60 numa máquina de 2 núcleos, e 15 GB de espaço). Ele para sozinho depois do tempo sem uso escolhido na conta (até 4 horas) e é apagado pelo GitHub depois de 30 dias parado. O guia fica em `docs/demonstracao.md`.

### Consequências

- Boa, porque a demonstração é o sistema de verdade e sobe de qualquer computador com um clique.
- Boa, porque nenhum segredo vai para o repositório e as versões ficam presas às do projeto.
- Ruim, porque, enquanto o Codespace roda, quem tiver o link entra como administrador da demonstração e pode mudar os dados.
- Ruim, porque a primeira criação leva alguns minutos e o Codespace gasta a cota gratuita enquanto existe.
- Neutra, porque os dados do Codespace são separados dos do computador de Marcelo, e cada um carrega os seus.
