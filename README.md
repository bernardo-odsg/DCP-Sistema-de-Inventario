# DCP - Sistema de Controle de Mercadorias

Aplicação desktop desenvolvida para auxiliar no gerenciamento de produtos, 
controle de lotes e monitoramento de vencimentos, com foco na organização 
e apoio à prevenção de perdas.

## Funcionalidades

- Cadastro, consulta, listagem e remoção de produtos.
- Validação de códigos EAN.
- Cadastro e gerenciamento de lotes.
- Consulta e alteração de quantidades de lotes.
- Remoção de lotes.
- Radar de vencimentos, identificando lotes com vencimento em até 30 dias.
- Armazenamento local dos dados utilizando SQLite.

## Tecnologias utilizadas

- C#
- Windows Forms
- C
- SQLite
- DLL e P/Invoke para integração entre C# e C

## Arquitetura

A aplicação possui interface gráfica desenvolvida em C# com Windows Forms, integrada
a um motor desenvolvido em C por meio de uma DLL. O motor é responsável pelo processamento 
das operações e pela comunicação com o banco de dados SQLite.

## Projeto acadêmico

Desenvolvido como parte das atividades práticas do curso de Análise e Desenvolvimento de Sistemas.
