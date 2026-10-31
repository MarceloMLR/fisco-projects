# Demonstração no Codespaces

Como pôr a demonstração do FiscoProjects no ar pelo GitHub Codespaces, de qualquer computador, só com o navegador (ADR-0113). Os dados são os fictícios de `npm run demo:data` (ADR-0089).

## Uma vez só, na sua conta

Em [github.com/settings/codespaces](https://github.com/settings/codespaces):

- **Default idle timeout**: 240 minutos. O padrão (30) para o Codespace no meio de uma demonstração.

## Pôr no ar

1. No repositório, **Code → Codespaces → Create codespace on main**.
2. Espere. Na primeira vez, o Codespace se prepara (uns 10 minutos); depois, sobe o banco, a API e os dados de demonstração.
3. O terminal mostra o endereço, `https://<nome-do-codespace>-5175.app.github.dev`, e as pessoas para entrar:
   - `paulo.nogueira@empresa.example`, administrador;
   - `marina.costa@empresa.example`, gerente.

O link já sai público: quem o tiver abre sem conta no GitHub e entra sem senha, até como administrador. Se o terminal disser que não conseguiu, na aba **Ports**, clique com o botão direito na **5175** e escolha **Port Visibility → Public**.

## Depois da demonstração

- Para fechar o link sem parar: aba **Ports**, porta 5175, **Port Visibility → Private**.
- Para parar ou apagar: [github.com/codespaces](https://github.com/codespaces), no "⋯" do Codespace. Parado, ele não gasta horas, só espaço; o GitHub apaga os parados há mais de 30 dias.
- Para usar de novo o mesmo Codespace, abra-o: a demonstração sobe sozinha, com os dados de antes.

## Cota gratuita

Na conta GitHub Free, por mês: 120 horas de processamento (uma máquina de 2 núcleos gasta 2 por hora, então cerca de 60 horas de uso) e 15 GB de espaço. Este Codespace ocupa perto de 10 GB; apague-o quando não for mais usar.
