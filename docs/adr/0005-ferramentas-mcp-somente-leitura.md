# ADR 0005: Ferramentas MCP somente leitura e confinadas ao repositório

**Status:** aceita

## Contexto

Olhando só o diff, os agentes erram por falta de contexto: não sabem se a entrada já é validada em outro lugar
nem quais são as convenções do projeto. Dar acesso ao repositório resolve isso, mas o conteúdo de um PR é
controlado por quem o abriu e pode tentar induzir o modelo (prompt injection) a ler segredos.

## Decisão

- As ferramentas ficam em um **servidor MCP** separado (`CodeReviewCrew.McpServer`), falando por stdio. Isso
  reutiliza o protocolo padrão e permite plugar outros servidores pelo `.crc.yml`.
- As ferramentas são **somente leitura** e o `RepositoryReader` só lê dentro da raiz: recusa caminhos absolutos,
  `..`, links simbólicos, `.git` e arquivos de segredos (`.env`, chaves, `secrets.json`...).
- Os processos MCP **não herdam variáveis de ambiente**; só recebem `PATH`, `HOME` e afins, mais o `env` configurado.
- As instruções dos agentes tratam diff e arquivos como dados, nunca como instruções.
- Só os especialistas recebem ferramentas; o sintetizador trabalha sobre as análises.

## Consequências

- Revisões com mais contexto e menos falsos positivos, ao custo de mais tokens por chamada de ferramenta.
- O acesso é opt-in na CLI (`--repo-path`) e ligado por padrão na GitHub Action (`read-repository`).
- Toda ferramenta nova precisa seguir as mesmas restrições e ter testes de ataque.
