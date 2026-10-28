# ADR 0004: Publicar a revisão como COMMENT, com comentários inline

**Status:** aceita

## Contexto

A API de reviews do GitHub aceita `APPROVE`, `REQUEST_CHANGES` e `COMMENT`. O `GITHUB_TOKEN` padrão do Actions
não pode aprovar PRs, e um bot pedindo alterações bloqueia o fluxo sem que humanos possam dispensar com facilidade.

## Decisão

Publicar sempre como `COMMENT`. O veredito aparece no texto. Quem quiser bloquear o merge usa `--fail-on`
(ou o input `fail-on`) com o job marcado como obrigatório.

Problemas em linhas que fazem parte do diff viram comentários inline (até 30, os mais graves primeiro);
o restante vai para o corpo. Se o GitHub recusar um comentário (422), a review é refeita só com o corpo.

## Consequências

- Funciona sem permissões extras e sem tokens pessoais.
- O bloqueio de merge é uma escolha explícita do repositório.
