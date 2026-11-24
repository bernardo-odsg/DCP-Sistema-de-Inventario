#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stdbool.h>
#include "sqlite3.h"

// ==========================================
// 1. ESTRUTURAS DE DADOS
// ==========================================

typedef struct node_produto {
    char localizacao[30];
    char nome[20];
    char ean[15];
    struct node_produto *proximo;
} node_produto;

typedef struct tabela_hash {
    node_produto **vetor_de_gavetas;
    int numero_de_gavetas;
} tabela_hash;

static sqlite3 *db_global = NULL;
static tabela_hash *tabela_global = NULL;
static node_produto *lista_global = NULL;

// ==========================================
// 2. LISTA ENCADEADA
// ==========================================

node_produto *inserir_produto(node_produto *head, const char *localizacao, const char *nome, const char *ean) {
    if (localizacao == NULL || nome == NULL || ean == NULL) {
    	return NULL;
    }
    if (strlen(localizacao) >= sizeof(head -> localizacao) || strlen(nome) >= sizeof(head -> nome) || strlen(ean) >= sizeof(head -> ean)) {
    	return NULL;
    }
    node_produto *nova_head = malloc(sizeof(*nova_head));
    if (nova_head == NULL) {
        return NULL;
    }
    snprintf(nova_head -> localizacao, sizeof(nova_head -> localizacao), "%s", localizacao);
    snprintf(nova_head -> nome, sizeof(nova_head -> nome), "%s", nome);
    snprintf(nova_head -> ean, sizeof(nova_head -> ean), "%s", ean);
    nova_head -> proximo = head;
    return nova_head;
}

void libera_memoria_lista_encadeada(node_produto *head) {
     while (head != NULL) {
     	node_produto *head_auxiliar = head -> proximo;
     	free(head);
     	head = head_auxiliar;
     }
}

// ==========================================
// 3. TABELA HASH
// ==========================================

bool teste_primalidade(int n) { 
	if (n == 2) {
		return true;
	}
	if (n < 2 || n % 2 == 0) {
		return false;
	}
	int d = 3;
	while (d * d <= n) {
	  if (n % d == 0) {
	    return false;
	  }
	  d += 2;
	}
    return true;
}

int menor_primo_maior_que(int n) { 
	if (n == 0 || n == 1) {
		return 2;
	}
	if (n % 2 == 0) {
		n += 1;
	}
	else {
		n += 2;
	}
	while (! teste_primalidade(n)) {
	   n += 2;
	}
	return n;
}

tabela_hash *criar_tabela_hash(int quantidade_de_produtos) {
    tabela_hash *minha_tabela = malloc(sizeof(*minha_tabela));
    if (minha_tabela == NULL)
        return NULL;
    minha_tabela -> numero_de_gavetas = menor_primo_maior_que(quantidade_de_produtos);
    minha_tabela -> vetor_de_gavetas = calloc(minha_tabela -> numero_de_gavetas, sizeof(*(minha_tabela -> vetor_de_gavetas)));
    if (minha_tabela -> vetor_de_gavetas == NULL) {
        free(minha_tabela);
        return NULL;
    }
    return minha_tabela;
}

void libera_memoria_tabela_hash(tabela_hash *minha_tabela) {
	if (minha_tabela == NULL) {
		return;
	}
	for (int i = 0 ; i < minha_tabela -> numero_de_gavetas ; i++) {
		libera_memoria_lista_encadeada((minha_tabela -> vetor_de_gavetas)[i]);
	}
	free(minha_tabela -> vetor_de_gavetas);
	free(minha_tabela);
}

int funcao_hash(const char *ean, int numero_de_gavetas)
{
    unsigned long valor = 0;
    while (*ean) {
        valor = (valor * 31) + (*ean++);
    }
    return valor % numero_de_gavetas;
}

int inserir_produto_na_tabela_hash(const node_produto *produto, tabela_hash *minha_tabela) {
	if (produto == NULL || minha_tabela == NULL || minha_tabela -> vetor_de_gavetas == NULL || minha_tabela -> numero_de_gavetas == 0) {
		return 1;
	}
	int gaveta = funcao_hash(produto -> ean, minha_tabela -> numero_de_gavetas);
	node_produto *head_auxiliar = inserir_produto((minha_tabela -> vetor_de_gavetas)[gaveta], produto -> localizacao, produto -> nome, produto -> ean);
	if (head_auxiliar == NULL) {
		return 1;
	}
	(minha_tabela -> vetor_de_gavetas)[gaveta] = head_auxiliar;
	return 0;
}

int carregar_lista_na_tabela_hash(node_produto *head, tabela_hash *minha_tabela)
{
    if (minha_tabela == NULL || minha_tabela -> vetor_de_gavetas == NULL || minha_tabela -> numero_de_gavetas == 0) {
    	return 1;
    }
    while (head != NULL) {
        if (inserir_produto_na_tabela_hash(head, minha_tabela)) {
        	return 1;
        }
        head = head -> proximo;
    }
    return 0;
}

int redimensionar_tabela_hash(node_produto *lista, tabela_hash **tabela_atual, int quantidade_de_produtos) {
	if (tabela_atual == NULL || quantidade_de_produtos < 0) {
		return 1;
	}
	int gavetas_necessarias = menor_primo_maior_que(quantidade_de_produtos);
	if (*tabela_atual != NULL && (*tabela_atual) -> numero_de_gavetas == gavetas_necessarias) {
		return 0;
	}
	tabela_hash *nova_tabela = criar_tabela_hash(quantidade_de_produtos);
	if (nova_tabela == NULL) {
		return 1;
	}
	if (carregar_lista_na_tabela_hash(lista, nova_tabela)) {
		libera_memoria_tabela_hash(nova_tabela);
		return 1;
	}
	tabela_hash *tabela_antiga = *tabela_atual;
	*tabela_atual = nova_tabela;
	libera_memoria_tabela_hash(tabela_antiga);
	return 0;
}

node_produto *busca_na_tabela_hash(const char *ean, tabela_hash *minha_tabela)
{
    if (ean == NULL || minha_tabela == NULL || minha_tabela -> vetor_de_gavetas == NULL || minha_tabela -> numero_de_gavetas == 0) {
    	return NULL;
    }
    int gaveta = funcao_hash(ean, minha_tabela -> numero_de_gavetas);
    node_produto *endereco_atual_na_gaveta = minha_tabela -> vetor_de_gavetas[gaveta];
    while (endereco_atual_na_gaveta != NULL) {
        if (strcmp(endereco_atual_na_gaveta -> ean, ean) == 0) {
			return endereco_atual_na_gaveta;
        }
        endereco_atual_na_gaveta = endereco_atual_na_gaveta -> proximo;
    }
    return NULL;
}

static bool remover_produto(node_produto **head, const char *ean) {
	if (head == NULL || ean == NULL) {
		return false;
	}
	node_produto **atual = head;
	while (*atual != NULL) {
		if (strcmp((*atual) -> ean, ean) == 0) {
			node_produto *removido = *atual;
			*atual = removido -> proximo;
			free(removido);
			return true;
		}
		atual = &((*atual) -> proximo);
	}
	return false;
}

// ==========================================
// 4. CALLBACK DO SQLITE
// ==========================================

int callback_monta_lista_encadeada(void *usuario_head, int argc, char **argv, char **colName)
{
    node_produto **head_ref = (node_produto **) usuario_head;
    if (argc >= 3 && argv[0] && argv[1] && argv[2]) {
        node_produto *novo = inserir_produto(*head_ref, argv[0], argv[1], argv[2]);
        if (novo == NULL) {
            return 1;
        }
        *head_ref = novo;
    }
    return 0;
}

// ==========================================
// 5. LIBERAÇÃO DO MOTOR
// ==========================================

__declspec(dllexport) void liberar_motor_dcp(void)
{
    if (tabela_global != NULL) {
        libera_memoria_tabela_hash(tabela_global);
        tabela_global = NULL;
    }
    if (lista_global != NULL) {
        libera_memoria_lista_encadeada(lista_global);
        lista_global = NULL;
    }
    if (db_global != NULL) {
        sqlite3_close(db_global);
        db_global = NULL;
    }
}

// ==========================================
// 6. INICIALIZAÇÃO DO MOTOR 
// ==========================================

__declspec(dllexport) int inicializar_motor_dcp(void) {
	 char *erro = NULL;
	 //--------------------------------------
	 //6.1. LIBERAR ESTRUTURAS ANTERIORES
	 //--------------------------------------
	 
	 liberar_motor_dcp();
	 
	 //--------------------------------------
	 //6.2. ABRIR BANCO DE DADOS
	 //--------------------------------------

	 if (sqlite3_open("dcp.db", &db_global) != SQLITE_OK) {
	 	liberar_motor_dcp();
	 	return 1;
	 }

	 //--------------------------------------
	 //6.3. CRIAR TABELAS (CASO NÃO EXISTAM)
	 //--------------------------------------

	 const char *sql_criacao = 
	 	"CREATE TABLE IF NOT EXISTS produtos("
	 	"localizacao TEXT, "
	 	"nome TEXT, "
	 	"ean TEXT PRIMARY KEY);"

	 	"CREATE TABLE IF NOT EXISTS lotes("
	 	"lote TEXT, "
	 	"ean TEXT, "
	 	"validade TEXT, "
	 	"qtd INTEGER, "
	 	"PRIMARY KEY(lote, ean));";

	 if (sqlite3_exec(db_global, sql_criacao, NULL, NULL, &erro) != SQLITE_OK) {
		sqlite3_free(erro);
		liberar_motor_dcp();
		return 1;	 	
	 }

	 //------------------------------------------
	 //6.4. CARREGAR PRODUTOS NA LISTA ENCADEADA
	 //------------------------------------------

	 int resultado_sql = sqlite3_exec(db_global, "SELECT localizacao, nome, ean FROM produtos;", callback_monta_lista_encadeada, &lista_global, &erro);
	 if (resultado_sql != SQLITE_OK) {
	 	sqlite3_free(erro);
	 	liberar_motor_dcp();
	 	return 1;
	 }

	 //--------------------------------------------------
	 //6.5 CONTAGEM DE PRODUTOS PARA CRIAR A TABELA HASH
	 //--------------------------------------------------

	 sqlite3_stmt *contagem = NULL;
	 int quantidade_produtos = 0;
	 if (sqlite3_prepare_v2(db_global, "SELECT COUNT(*) FROM produtos;", -1, &contagem, NULL) != SQLITE_OK) {
	 	liberar_motor_dcp();
	 	return 1;
	 }
	 if (sqlite3_step(contagem) == SQLITE_ROW) {
	 	quantidade_produtos = sqlite3_column_int(contagem, 0);
	 }
	 else {
	 	sqlite3_finalize(contagem);
	 	liberar_motor_dcp();
	 	return 1;
	 }
	 sqlite3_finalize(contagem);

	 //--------------------------------------
	 //6.6 CRIAR E PREENCHER TABELA HASH
	 //--------------------------------------

	 tabela_global = criar_tabela_hash(quantidade_produtos);
	 if (tabela_global == NULL) {
	 	liberar_motor_dcp();
	 	return 1;
	 }
	 if (carregar_lista_na_tabela_hash(lista_global, tabela_global)) {
	 	liberar_motor_dcp();
	 	return 1;
	 }
	 return 0;
}    

// ==========================================
// 7. VALIDAÇÃO DE EAN
// ==========================================

bool validador_ean(const char *ean) {
	 if (ean == NULL) {
	 	return false;
	 }
	 size_t tam_ean = strlen(ean);
	 if (tam_ean != 8 && tam_ean != 12 && tam_ean != 13 && tam_ean != 14) {
	 	return false;
	 }
	 int pos = 0, result = 0, n = 1, multiplo_de_10_geq_result;
	 while (pos != (int) tam_ean) {
	     if (ean[pos] < '0' || ean[pos] > '9') {
	     	return false;
	     }
	     pos++;
	 }
	 pos -= 2;
	 while (pos > -1) {
	 	if ((n & 1) == 1) {
	 		result += (ean[pos] - '0') * 3;
	 	}
	 	else {
	 		result += ean[pos] - '0';
	 	}
	 	n++;
	 	pos--;
	 }
	 multiplo_de_10_geq_result = result;
	 if (result % 10 != 0) {
	 	multiplo_de_10_geq_result += 10 - result % 10;
	 }
	 if (multiplo_de_10_geq_result - result == ean[tam_ean - 1] - '0') {
	 	return true;
	 }
	 return false;
}

// ==========================================
// 8. BUSCA DE PRODUTO
// ==========================================

#define DCP_SUCESSO 0
#define DCP_NAO_ENCONTRADO 1
#define DCP_EAN_INVALIDO 2
#define DCP_ARGUMENTO_INVALIDO 3
#define DCP_MOTOR_NAO_INICIALIZADO 4

__declspec(dllexport) int buscar_produto_dll(const char *ean, char *local_out, char *nome_out) {
	if (ean == NULL || local_out == NULL || nome_out == NULL) {
		return DCP_ARGUMENTO_INVALIDO;
	}
	local_out[0] = '\0';
	nome_out[0] = '\0';
	if (! validador_ean(ean)) {
		return DCP_EAN_INVALIDO;
	}
	if (tabela_global == NULL) {
		return DCP_MOTOR_NAO_INICIALIZADO;
	}
	node_produto *resposta = busca_na_tabela_hash(ean, tabela_global);
	if (resposta == NULL) {
		return DCP_NAO_ENCONTRADO;
	}
	strcpy(local_out, resposta -> localizacao);
	strcpy(nome_out, resposta -> nome);
	return DCP_SUCESSO;
}

// ==========================================
// 9. CADASTRO DE PRODUTO
// ==========================================

#define DCP_CADASTRO_SUCESSO 0
#define DCP_CADASTRO_ARGUMENTO_INVALIDO 1
#define DCP_CADASTRO_EAN_INVALIDO 2
#define DCP_CADASTRO_PRODUTO_EXISTENTE 3
#define DCP_CADASTRO_ERRO_MEMORIA 4
#define DCP_CADASTRO_ERRO_BANCO 5
#define DCP_CADASTRO_MOTOR_NAO_INICIALIZADO 6
#define DCP_CADASTRO_NOME_EXISTENTE 7

__declspec(dllexport) int cadastrar_produto_dll(const char *localizacao, const char *nome, const char *ean) {
	if (db_global == NULL || tabela_global == NULL) {
		return DCP_CADASTRO_MOTOR_NAO_INICIALIZADO;
	}
	if (localizacao == NULL || nome == NULL || ean == NULL) {
		return DCP_CADASTRO_ARGUMENTO_INVALIDO;
	}
	if (strlen(localizacao) >= 30 || strlen(nome) >= 20 || strlen(ean) >= 15) {
		return DCP_CADASTRO_ARGUMENTO_INVALIDO;
	}
	if (! validador_ean(ean)) {
		return DCP_CADASTRO_EAN_INVALIDO;
	}
	if (busca_na_tabela_hash(ean, tabela_global) != NULL) {
		return DCP_CADASTRO_PRODUTO_EXISTENTE;
	}
	sqlite3_stmt *stmt_nome = NULL;
	const char *sql_nome =
		"SELECT 1 FROM produtos "
		"WHERE TRIM(nome) = TRIM(?) COLLATE NOCASE "
		"LIMIT 1;";
	if (sqlite3_prepare_v2(db_global, sql_nome, -1, &stmt_nome, NULL) != SQLITE_OK) {
		return DCP_CADASTRO_ERRO_BANCO;
	}
	if (sqlite3_bind_text(stmt_nome, 1, nome, -1, SQLITE_TRANSIENT) != SQLITE_OK) {
		sqlite3_finalize(stmt_nome);
		return DCP_CADASTRO_ERRO_BANCO;
	}
	int resultado_nome = sqlite3_step(stmt_nome);
	sqlite3_finalize(stmt_nome);
	if (resultado_nome == SQLITE_ROW) {
		return DCP_CADASTRO_NOME_EXISTENTE;
	}
	if (resultado_nome != SQLITE_DONE) {
		return DCP_CADASTRO_ERRO_BANCO;
	}
	node_produto *nova_lista = inserir_produto(lista_global, localizacao, nome, ean);
    if (nova_lista == NULL) {
    	return DCP_CADASTRO_ERRO_MEMORIA;
    }
    sqlite3_stmt *stmt = NULL;
    if (sqlite3_prepare_v2(db_global, "SELECT COUNT(*) FROM produtos;", -1, &stmt, NULL) != SQLITE_OK) {
    	free(nova_lista);
    	return DCP_CADASTRO_ERRO_BANCO;
    }
    if (sqlite3_step(stmt) != SQLITE_ROW) {
    	sqlite3_finalize(stmt);
    	free(nova_lista);
    	return DCP_CADASTRO_ERRO_BANCO;
    }
    int quantidade_atual = sqlite3_column_int(stmt, 0);
    sqlite3_finalize(stmt);
    int nova_quantidade = quantidade_atual + 1;
    bool precisa_redimensionar = tabela_global -> numero_de_gavetas != menor_primo_maior_que(nova_quantidade);
    tabela_hash *nova_tabela = NULL;
    node_produto *nova_head_gaveta = NULL;
    int gaveta = -1;
    if (precisa_redimensionar) {
    	nova_tabela = criar_tabela_hash(nova_quantidade);
    	if (nova_tabela == NULL) {
    		free(nova_lista);
    		return DCP_CADASTRO_ERRO_MEMORIA;
    	}
    	if (carregar_lista_na_tabela_hash(nova_lista, nova_tabela)) {
    		libera_memoria_tabela_hash(nova_tabela);
    		free(nova_lista);
    		return DCP_CADASTRO_ERRO_MEMORIA;
    	}
    }
    else {
    	gaveta = funcao_hash(ean, tabela_global -> numero_de_gavetas);
    	nova_head_gaveta = inserir_produto(tabela_global -> vetor_de_gavetas[gaveta], localizacao, nome, ean);
    	if (nova_head_gaveta == NULL) {
    		free(nova_lista);
    		return DCP_CADASTRO_ERRO_MEMORIA;
    	}
    }
    sqlite3_stmt *stmt_insert = NULL;
    const char *sql = 
    	"INSERT INTO produtos (localizacao, nome, ean) "
    	"VALUES (?, ?, ?);";
    if (sqlite3_prepare_v2(db_global, sql, -1, &stmt_insert, NULL) != SQLITE_OK) {
    	if (nova_tabela != NULL) {
    		libera_memoria_tabela_hash(nova_tabela);
    	}
    	if (nova_head_gaveta != NULL) {
    		free(nova_head_gaveta);
    	}
    	free(nova_lista);
    	return DCP_CADASTRO_ERRO_BANCO;
    }
    if (sqlite3_bind_text(stmt_insert, 1, localizacao, -1, SQLITE_TRANSIENT) != SQLITE_OK || 
    	sqlite3_bind_text(stmt_insert, 2, nome, -1, SQLITE_TRANSIENT) != SQLITE_OK ||
        sqlite3_bind_text(stmt_insert, 3, ean, -1, SQLITE_TRANSIENT) != SQLITE_OK) {

    	sqlite3_finalize(stmt_insert);

    	if (nova_tabela != NULL) {
    		libera_memoria_tabela_hash(nova_tabela);
    	}
    	if (nova_head_gaveta != NULL) {
    		free(nova_head_gaveta);
    	}
    	free(nova_lista);
    	return DCP_CADASTRO_ERRO_BANCO;
    }
    int resultado = sqlite3_step(stmt_insert);
    sqlite3_finalize(stmt_insert);
    if (resultado != SQLITE_DONE) {
    	if (nova_tabela != NULL) {
    		libera_memoria_tabela_hash(nova_tabela);
    	}
    	if (nova_head_gaveta != NULL) {
    		free(nova_head_gaveta);
    	}
    	free(nova_lista);
    	return DCP_CADASTRO_ERRO_BANCO;
    }
    lista_global = nova_lista;
    if (precisa_redimensionar) {
    	tabela_hash *tabela_antiga = tabela_global;
    	tabela_global = nova_tabela;
    	libera_memoria_tabela_hash(tabela_antiga);
    }
    else {
    	tabela_global -> vetor_de_gavetas[gaveta] = nova_head_gaveta;
    }
    return DCP_CADASTRO_SUCESSO;
}

// ==========================================
// 10. CADASTRO DE LOTE
// ==========================================

static bool validar_data_iso(const char *data) {
	if (data == NULL || strlen(data) != 10) {
		return false;
	}
	if (data[4] != '-' || data[7] != '-') {
		return false;
	}
	for (int i = 0 ; i < 10 ; i++) {
		if (i == 4 || i == 7) {
			continue;
		}
		if (data[i] < '0' || data[i] > '9') {
			return false;
		}
	}
	int ano = (data[0] - '0') * 1000 +
			  (data[1] - '0') * 100 +
			  (data[2] - '0') * 10 +
			  (data[3] - '0');
	
	int mes = (data[5] - '0') * 10 +
			  (data[6] - '0');
	
	int dia = (data[8] - '0') * 10 +
			  (data[9] - '0');
	
	if (ano < 1 || mes < 1 || mes > 12) {
		return false;
	}
	int dias_mes[] = {31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31};
	bool bissexto = (ano % 400 == 0) || (ano % 4 == 0 && ano % 100 != 0);
	if (bissexto) {
		dias_mes[1] = 29;
	}
	if (dia < 1 || dia > dias_mes[mes - 1]) {
		return false;
	}
	return true;
}

#define DCP_LOTE_SUCESSO 0
#define DCP_LOTE_ARGUMENTO_INVALIDO 1
#define DCP_LOTE_EAN_INVALIDO 2
#define DCP_LOTE_PRODUTO_NAO_ENCONTRADO 3
#define DCP_LOTE_INVALIDO 4
#define DCP_LOTE_VALIDADE_INVALIDA 5
#define DCP_LOTE_QUANTIDADE_INVALIDA 6
#define DCP_LOTE_DUPLICADO 7
#define DCP_LOTE_ERRO_BANCO 8
#define DCP_LOTE_MOTOR_NAO_INICIALIZADO 9

__declspec(dllexport) int cadastrar_lote_dll(const char *ean, const char *lote, const char *validade, int quantidade) {
	// 10.1. VERIFICAR INICIALIZAÇÃO DO MOTOR

	if (db_global == NULL || tabela_global == NULL) {
		return DCP_LOTE_MOTOR_NAO_INICIALIZADO;
	}

	// 10.2. VERIFICAR ARGUMENTOS

	if (ean == NULL || lote == NULL || validade == NULL) {
		return DCP_LOTE_ARGUMENTO_INVALIDO;
	}

	// 10.3. VALIDAR EAN

	if (! validador_ean(ean)) {
		return DCP_LOTE_EAN_INVALIDO;
	}

	// 10.4. CONFIRMAR QUE O PRODUTO EXISTE

	if (busca_na_tabela_hash(ean, tabela_global) == NULL) {
		return DCP_LOTE_PRODUTO_NAO_ENCONTRADO;
	}

	// 10.5. VALIDAR CÓDIGO DO LOTE

	if (strlen(lote) == 0 || strlen(lote) > 30) {
		return DCP_LOTE_INVALIDO;
	}

	// 10.6. VALIDAR VALIDADE

	if (! validar_data_iso(validade)) {
		return DCP_LOTE_VALIDADE_INVALIDA;
	}

	// 10.7. VALIDAR QUANTIDADE

	if (quantidade <= 0) {
		return DCP_LOTE_QUANTIDADE_INVALIDA;
	}

	// 10.8. VERIFICAR DUPLICIDADE

	sqlite3_stmt *stmt = NULL;
	const char *sql_verificar = 
		"SELECT 1 FROM lotes "
		"WHERE lote = ? AND ean = ? LIMIT 1;";
	if (sqlite3_prepare_v2(db_global, sql_verificar, -1, &stmt, NULL) != SQLITE_OK) {
		return DCP_LOTE_ERRO_BANCO;
	}
	if (sqlite3_bind_text(stmt, 1, lote, -1, SQLITE_TRANSIENT) != SQLITE_OK ||
		sqlite3_bind_text(stmt, 2, ean, -1, SQLITE_TRANSIENT) != SQLITE_OK) {
		sqlite3_finalize(stmt);
		return DCP_LOTE_ERRO_BANCO;
	}
	int resultado = sqlite3_step(stmt);
	sqlite3_finalize(stmt);
	if (resultado == SQLITE_ROW) {
		return DCP_LOTE_DUPLICADO;
	}
	if (resultado != SQLITE_DONE) {
		return DCP_LOTE_ERRO_BANCO;
	}

	// 10.9. PREPARAR INSERÇÃO

	const char *sql_insert =
		"INSERT INTO lotes (lote, ean, validade, qtd) "
		"VALUES (?, ?, ?, ?);";
	sqlite3_stmt *stmt_insert = NULL;
	if (sqlite3_prepare_v2(db_global, sql_insert, -1, &stmt_insert, NULL) != SQLITE_OK) {
		return DCP_LOTE_ERRO_BANCO;
	}

	// 10.10. VINCULAR VALORES

	if (sqlite3_bind_text(stmt_insert, 1, lote, -1, SQLITE_TRANSIENT) != SQLITE_OK ||
		sqlite3_bind_text(stmt_insert, 2, ean, -1, SQLITE_TRANSIENT) != SQLITE_OK ||
		sqlite3_bind_text(stmt_insert, 3, validade, -1, SQLITE_TRANSIENT) != SQLITE_OK ||
		sqlite3_bind_int(stmt_insert, 4, quantidade) != SQLITE_OK) {
		sqlite3_finalize(stmt_insert);
		return DCP_LOTE_ERRO_BANCO;
	}

	// 10.11. EXECUTAR INSERÇÃO

	resultado = sqlite3_step(stmt_insert);
	sqlite3_finalize(stmt_insert);
	if (resultado != SQLITE_DONE) {
		return DCP_LOTE_ERRO_BANCO;
	}
	return DCP_LOTE_SUCESSO;
}

// ==========================================
// 11. ALTERAÇÃO DA QUANTIDADE EM LOTE
// ==========================================

#define DCP_ALT_LOTE_SUCESSO 0
#define DCP_ALT_LOTE_ARGUMENTO_INVALIDO 1
#define DCP_ALT_LOTE_EAN_INVALIDO 2
#define DCP_ALT_LOTE_PRODUTO_NAO_ENCONTRADO 3
#define DCP_ALT_LOTE_NAO_ENCONTRADO 4
#define DCP_ALT_LOTE_QUANTIDADE_INVALIDA 5
#define DCP_ALT_LOTE_ERRO_BANCO 6
#define DCP_ALT_LOTE_MOTOR_NAO_INICIALIZADO 7

__declspec(dllexport) int alterar_quantidade_em_lote_dll(const char *ean, const char *lote, int nova_quantidade) {
	sqlite3_stmt *stmt = NULL;
	int resultado;
	int status;

	// 11.1. VERIFICA SE O MOTOR ESTÁ INICIALIZADO
	
	if (db_global == NULL || tabela_global == NULL) {
		return DCP_ALT_LOTE_MOTOR_NAO_INICIALIZADO;
	}

	// 11.2. VERIFICA OS ARGUMENTOS
	
	if (ean == NULL || lote == NULL) {
		return DCP_ALT_LOTE_ARGUMENTO_INVALIDO;
	}
	if (strlen(lote) == 0 || strlen(lote) > 30) {
		return DCP_ALT_LOTE_ARGUMENTO_INVALIDO;
	}

	// 11.3. VALIDA O EAN
	
	if (! validador_ean(ean)) {
		return DCP_ALT_LOTE_EAN_INVALIDO;
	}

	// 11.4. VERIFICA SE O PRODUTO ESTÁ CADASTRADO
	
	if (busca_na_tabela_hash(ean, tabela_global) == NULL) {
		return DCP_ALT_LOTE_PRODUTO_NAO_ENCONTRADO;
	}

	// 11.5. VERIFICA A QUANTIDADE
	
	if (nova_quantidade < 0) {
		return DCP_ALT_LOTE_QUANTIDADE_INVALIDA;
	}

	// 11.6. PREPARA O COMANDO SQL
	
	const char *sql = 
		"UPDATE lotes "
		"SET qtd = ? "
		"WHERE lote = ? AND ean = ?;";

	resultado = sqlite3_prepare_v2(db_global, sql, -1, &stmt, NULL);
	if (resultado != SQLITE_OK) {
		return DCP_ALT_LOTE_ERRO_BANCO;
	} 

	// 11.7. VINCULA OS PARÂMETROS
	
	resultado = sqlite3_bind_int(stmt, 1, nova_quantidade);
	if (resultado != SQLITE_OK) {
		status = DCP_ALT_LOTE_ERRO_BANCO;
		goto finalizar;
	}
	
	resultado = sqlite3_bind_text(stmt, 2, lote, -1, SQLITE_TRANSIENT);
	if (resultado != SQLITE_OK) {
		status = DCP_ALT_LOTE_ERRO_BANCO;
		goto finalizar;
	}
	
	resultado = sqlite3_bind_text(stmt, 3, ean, -1, SQLITE_TRANSIENT);
	if (resultado != SQLITE_OK) {
		status = DCP_ALT_LOTE_ERRO_BANCO;
		goto finalizar;
	} 

	// 11.8. EXECUTA A ALTERAÇÃO
	
	resultado = sqlite3_step(stmt);
	if (resultado != SQLITE_DONE) {
		status = DCP_ALT_LOTE_ERRO_BANCO;
		goto finalizar;
	}

	// 11.9. VERIFICA SE ALGUM LOTE FOI ENCONTRADO E ALTERADO
	
	if (sqlite3_changes(db_global) == 0) {
		status = DCP_ALT_LOTE_NAO_ENCONTRADO;
	}
	else {
		status = DCP_ALT_LOTE_SUCESSO;
	}
	finalizar:
		sqlite3_finalize(stmt);
		return status;
}

// ==========================================
// 12. REMOÇÃO DE LOTE
// ==========================================

#define DCP_REM_LOTE_SUCESSO 0
#define DCP_REM_LOTE_ARGUMENTO_INVALIDO 1
#define DCP_REM_LOTE_EAN_INVALIDO 2
#define DCP_REM_LOTE_PRODUTO_NAO_ENCONTRADO 3
#define DCP_REM_LOTE_NAO_ENCONTRADO 4
#define DCP_REM_LOTE_ERRO_BANCO 5
#define DCP_REM_LOTE_MOTOR_NAO_INICIALIZADO 6

__declspec(dllexport)
int remover_lote_dll(const char *ean, const char *lote) {
	sqlite3_stmt *stmt = NULL;
	int resultado;
	int status;

	// 12.1. VERIFICA SE O MOTOR ESTÁ INICIALIZADO

	if (db_global == NULL || tabela_global == NULL) {
		return DCP_REM_LOTE_MOTOR_NAO_INICIALIZADO;
	}

	// 12.2. VERIFICA OS ARGUMENTOS

	if (ean == NULL || lote == NULL || strlen(lote) == 0 || strlen(lote) > 30) {
		return DCP_REM_LOTE_ARGUMENTO_INVALIDO;
	}

	// 12.3. VALIDA O EAN

	if (!validador_ean(ean)) {
		return DCP_REM_LOTE_EAN_INVALIDO;
	}

	// 12.4. VERIFICA SE O PRODUTO ESTÁ CADASTRADO

	if (busca_na_tabela_hash(ean, tabela_global) == NULL) {
		return DCP_REM_LOTE_PRODUTO_NAO_ENCONTRADO;
	}

	// 12.5. PREPARA O COMANDO SQL

	const char *sql =
		"DELETE FROM lotes "
		"WHERE lote = ? AND ean = ?;";

	resultado = sqlite3_prepare_v2(
		db_global, sql, -1, &stmt, NULL
	);

	if (resultado != SQLITE_OK) {
		return DCP_REM_LOTE_ERRO_BANCO;
	}

	// 12.6 VINCULA OS PARÂMETROS

	resultado = sqlite3_bind_text(
		stmt, 1, lote, -1, SQLITE_TRANSIENT
	);

	if (resultado != SQLITE_OK) {
		status = DCP_REM_LOTE_ERRO_BANCO;
		goto finalizar;
	}

	resultado = sqlite3_bind_text(
		stmt, 2, ean, -1, SQLITE_TRANSIENT
	);

	if (resultado != SQLITE_OK) {
		status = DCP_REM_LOTE_ERRO_BANCO;
		goto finalizar;
	}

	// 12.7. EXECUTA A REMOÇÃO

	resultado = sqlite3_step(stmt);

	if (resultado != SQLITE_DONE) {
		status = DCP_REM_LOTE_ERRO_BANCO;
		goto finalizar;
	}

	// 12.8. VERIFICA SE ALGUM LOTE FOI REMOVIDO

	if (sqlite3_changes(db_global) == 0) {
		status = DCP_REM_LOTE_NAO_ENCONTRADO;
	}
	else
	{
		status = DCP_REM_LOTE_SUCESSO;
	}

	finalizar:
		sqlite3_finalize(stmt);
		return status;
}

// ==========================================
// 13. REMOÇÃO DE PRODUTO
// ==========================================

#define DCP_REM_PRODUTO_SUCESSO 0
#define DCP_REM_PRODUTO_ARGUMENTO_INVALIDO 1
#define DCP_REM_PRODUTO_EAN_INVALIDO 2
#define DCP_REM_PRODUTO_NAO_ENCONTRADO 3
#define DCP_REM_PRODUTO_ERRO_BANCO 4
#define DCP_REM_PRODUTO_MOTOR_NAO_INICIALIZADO 5

__declspec(dllexport) int remover_produto_dll(const char *ean) {
	sqlite3_stmt *stmt = NULL;
	int resultado;
	int status;

	// 13.1. VERIFICAR INICIALIZAÇÃO DO MOTOR

	if (db_global == NULL || tabela_global == NULL) {
		return DCP_REM_PRODUTO_MOTOR_NAO_INICIALIZADO;
	}

	// 13.2. VERIFICAR ARGUMENTO

	if (ean == NULL || strlen(ean) == 0) {
		return DCP_REM_PRODUTO_ARGUMENTO_INVALIDO;
	}

	// 13.3. VALIDAR EAN

	if (!validador_ean(ean)) {
		return DCP_REM_PRODUTO_EAN_INVALIDO;
	}

	// 13.4. LOCALIZAR PRODUTO NA TABELA HASH

	if (busca_na_tabela_hash(ean, tabela_global) == NULL) {
		return DCP_REM_PRODUTO_NAO_ENCONTRADO;
	}

	// 13.5. VERIFICAR SE O PRODUTO EXISTE NA LISTA PRINCIPAL

	node_produto *atual = lista_global;
	bool encontrado_na_lista = false;
	while (atual != NULL) {
		if (strcmp(atual -> ean, ean) == 0) {
			encontrado_na_lista = true;
			break;
		}
		atual = atual -> proximo;
	}
	if (!encontrado_na_lista) {
		return DCP_REM_PRODUTO_ERRO_BANCO;
	}

	// 13.6. INICIAR TRANSAÇÃO

	resultado = sqlite3_exec(db_global, "BEGIN IMMEDIATE;", NULL, NULL, NULL);
	if (resultado != SQLITE_OK) {
		return DCP_REM_PRODUTO_ERRO_BANCO;
	}

	// 13.7. REMOVER LOTES VINCULADOS AO PRODUTO

	const char *sql_lotes =
		"DELETE FROM lotes WHERE ean = ?;";

	resultado = sqlite3_prepare_v2(db_global, sql_lotes, -1, &stmt, NULL);

	if (resultado != SQLITE_OK) {
		status = DCP_REM_PRODUTO_ERRO_BANCO;
		goto finalizar;
	}

	resultado = sqlite3_bind_text(stmt, 1, ean, -1, SQLITE_TRANSIENT);

	if (resultado != SQLITE_OK) {
		status = DCP_REM_PRODUTO_ERRO_BANCO;
		goto finalizar;
	}

	resultado = sqlite3_step(stmt);

	sqlite3_finalize(stmt);
	stmt = NULL;

	if (resultado != SQLITE_DONE) {
		status = DCP_REM_PRODUTO_ERRO_BANCO;
		goto finalizar;
	}

	// 13.8. REMOVER O PRODUTO

	const char *sql_produto = 
		"DELETE FROM produtos WHERE ean = ?;";

	resultado = sqlite3_prepare_v2(db_global, sql_produto, -1, &stmt, NULL);

	if (resultado != SQLITE_OK) {
		status = DCP_REM_PRODUTO_ERRO_BANCO;
		goto finalizar;
	}

	resultado = sqlite3_bind_text(stmt, 1, ean, -1, SQLITE_TRANSIENT);

	if (resultado != SQLITE_OK) {
		status = DCP_REM_PRODUTO_ERRO_BANCO;
		goto finalizar;
	}

	resultado = sqlite3_step(stmt);

	sqlite3_finalize(stmt);
	stmt = NULL;

	if (resultado != SQLITE_DONE) {
		status = DCP_REM_PRODUTO_ERRO_BANCO;
		goto finalizar;
	}

	if (sqlite3_changes(db_global) == 0) {
		status = DCP_REM_PRODUTO_NAO_ENCONTRADO;
		goto finalizar;
	}

	// 13.9. CONFIRMAR TRANSAÇÃO

	resultado = sqlite3_exec(db_global, "COMMIT;", NULL, NULL, NULL);

	if (resultado != SQLITE_OK) {
		status = DCP_REM_PRODUTO_ERRO_BANCO;
		goto finalizar;
	}

	// 13.10. REMOVER DA LISTA ENCADEADA PRINCIPAL

	remover_produto(&lista_global, ean);

	// 13.11. REMOVER DA TABELA HASH

	int gaveta = funcao_hash(ean, tabela_global -> numero_de_gavetas);
	remover_produto(&(tabela_global -> vetor_de_gavetas[gaveta]), ean);

	return DCP_REM_PRODUTO_SUCESSO;

	finalizar:
		if (stmt != NULL) {
			sqlite3_finalize(stmt);
		}
	sqlite3_exec(db_global, "ROLLBACK;", NULL, NULL, NULL);
	return status;
}

// ==========================================
// 14. RADAR DE VALIDADES
// ==========================================

__declspec(dllexport)
int executar_radar_dll(int dias_limite, int (*callback_externo)(void *, int, char **, char **), void *dados_usuario) {
    if (db_global == NULL) {
        return 1;
    }
    char sql_radar[512];
    snprintf(sql_radar, sizeof(sql_radar),
        "SELECT produtos.nome, "
        "lotes.lote, "
        "lotes.validade, "
        "produtos.localizacao, "
        "lotes.qtd, "
        "CASE "
        	"WHEN lotes.validade < date('now') "
        	"THEN 'VENCIDO' "
        	"ELSE 'A VENCER' "
        "END AS status "
        "FROM lotes "
        "JOIN produtos ON lotes.ean = produtos.ean "
        "WHERE lotes.validade <= date('now', '+%d days') "
        "ORDER BY lotes.validade ASC;",
        dias_limite
    );
    char *erro = NULL;
    if (sqlite3_exec(db_global, sql_radar, callback_externo, dados_usuario, &erro) != SQLITE_OK) {
        sqlite3_free(erro);
        return 1;
    }
    return 0;
}

// ==========================================
// 15. CONTAR LOTES
// ==========================================

__declspec(dllexport)
int contar_lotes_dcp(void)
{
    if (db_global == NULL)
        return -1;
    sqlite3_stmt *stmt = NULL;
    const char *sql = "SELECT COUNT(*) FROM lotes;";
    if (sqlite3_prepare_v2(db_global, sql, -1, &stmt, NULL) != SQLITE_OK) {
        return -1;
    }
    int quantidade = -1;
    if (sqlite3_step(stmt) == SQLITE_ROW) {
        quantidade = sqlite3_column_int(stmt, 0);
    }
    sqlite3_finalize(stmt);
    return quantidade;
}

// ==========================================
// 16. LISTAGEM DE PRODUTOS
// ==========================================

__declspec(dllexport) int listar_produtos_dll(int (*callback_externo)(void *, int, char **, char **), void *dados_usuario) {
	if (db_global == NULL) {
		return 1;
	}
	const char *sql =
		"SELECT ean, nome "
		"FROM produtos "
		"ORDER BY nome ASC;";
	char *erro = NULL;
	if (sqlite3_exec(db_global, sql, callback_externo, dados_usuario, &erro) != SQLITE_OK) {
		sqlite3_free(erro);
		return 1;
	}
	return 0;
}

// ==========================================
// 17. LISTAGEM DE LOTES
// ==========================================

__declspec(dllexport) int listar_lotes_dll(int (*callback_externo)(void *, int, char **, char **), void *dados_usuario) {
	if (db_global == NULL) {
		return 1;
	}
	const char *sql = 
		"SELECT produtos.nome, lotes.lote, lotes.qtd "
		"FROM lotes "
		"INNER JOIN produtos ON produtos.ean = lotes.ean "
		"ORDER BY produtos.nome ASC, lotes.lote ASC;";
	char *erro = NULL;
	if (sqlite3_exec(db_global, sql, callback_externo, dados_usuario, &erro) != SQLITE_OK) {
		sqlite3_free(erro);
		return 1;
	}
	return 0;
}