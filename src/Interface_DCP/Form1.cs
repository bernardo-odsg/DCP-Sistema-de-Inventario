using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Data;
using System.Drawing;
using System.IO;

namespace Interface_DCP
{
    public partial class Form1 : Form
    {
        // ==========================================
        // 1. IMPORTAÇÃO DAS FUNÇÕES DA MOTOR.DLL
        // ==========================================

        [DllImport("motor.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int inicializar_motor_dcp();

        [DllImport("motor.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void liberar_motor_dcp();

        [DllImport("motor.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int buscar_produto_dll(
            [MarshalAs(UnmanagedType.LPStr)] string ean,
            [In, Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder localOut,
            [In, Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder nomeOut
        );

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int SqliteCallback(
            IntPtr userData,
            int argc,
            IntPtr argv,
            IntPtr colName
        );

        [DllImport("motor.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int executar_radar_dll(
            int diasLimite,
            SqliteCallback callback,
            IntPtr dadosUsuario
        );

        [DllImport("motor.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int contar_lotes_dcp();

        [DllImport("motor.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int listar_produtos_dll(
            SqliteCallback callback,
            IntPtr dadosUsuario
        );

        [DllImport("motor.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int listar_lotes_dll(
            SqliteCallback callback,
            IntPtr dadosUsuario
        );

        [DllImport("motor.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int remover_lote_dll(
            [MarshalAs(UnmanagedType.LPStr)] string ean,
            [MarshalAs(UnmanagedType.LPStr)] string lote
        );

        [DllImport("motor.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int cadastrar_produto_dll(
            [MarshalAs(UnmanagedType.LPStr)] string localizacao,
            [MarshalAs(UnmanagedType.LPStr)] string nome,
            [MarshalAs(UnmanagedType.LPStr)] string ean
        );

        [DllImport("motor.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int cadastrar_lote_dll(
            [MarshalAs(UnmanagedType.LPStr)] string ean,
            [MarshalAs(UnmanagedType.LPStr)] string lote,
            [MarshalAs(UnmanagedType.LPStr)] string validade,
            int quantidade
        );

        [DllImport("motor.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int alterar_quantidade_em_lote_dll(
            [MarshalAs(UnmanagedType.LPStr)] string ean,
            [MarshalAs(UnmanagedType.LPStr)] string lote,
            int novaQuantidade
        );

        [DllImport("motor.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int remover_produto_dll(
            [MarshalAs(UnmanagedType.LPStr)] string ean
        );

        // ==========================================
        // 2. CÓDIGOS DE RETORNO
        // ==========================================

        private enum StatusBuscaProduto
        {
            Sucesso = 0,
            NaoEncontrado = 1,
            EanInvalido = 2,
            ArgumentoInvalido = 3,
            MotorNaoInicializado = 4
        }
        private enum StatusCadastroProduto
        {
            Sucesso = 0,
            ArgumentoInvalido = 1,
            EanInvalido = 2,
            ProdutoExistente = 3,
            ErroMemoria = 4,
            ErroBanco = 5,
            MotorNaoInicializado = 6,
            NomeExistente = 7
        }
        private enum StatusCadastroLote
        {
            Sucesso = 0,
            ArgumentoInvalido = 1,
            EanInvalido = 2,
            ProdutoNaoEncontrado = 3,
            LoteInvalido = 4,
            ValidadeInvalida = 5,
            QuantidadeInvalida = 6,
            LoteDuplicado = 7,
            ErroBanco = 8,
            MotorNaoInicializado = 9
        }
        private enum StatusAlteracaoLote
        {
            Sucesso = 0,
            ArgumentoInvalido = 1,
            EanInvalido = 2,
            ProdutoNaoEncontrado = 3,
            LoteNaoEncontrado = 4,
            QuantidadeInvalida = 5,
            ErroBanco = 6,
            MotorNaoInicializado = 7
        }
        private enum StatusRemocaoLote
        {
            Sucesso = 0,
            ArgumentoInvalido = 1,
            EanInvalido = 2,
            ProdutoNaoEncontrado = 3,
            LoteNaoEncontrado = 4,
            ErroBanco = 5,
            MotorNaoInicializado = 6
        }
        private enum StatusRemocaoProduto
        {
            Sucesso = 0,
            ArgumentoInvalido = 1,
            EanInvalido = 2,
            ProdutoNaoEncontrado = 3,
            ErroBanco = 4,
            MotorNaoInicializado = 5
        }

        // ==========================================
        // 3. VARIÁVEIS DE CONTROLE
        // ==========================================

        private DataTable? tabelaRadar;
        private SqliteCallback? callbackReferencia;
        private bool motorInicializado = false;
        private DataTable? tabelaProdutos;
        private SqliteCallback? callbackProdutos;
        private DataTable? tabelaLotes;
        private SqliteCallback? callbackLotes;
        private const string NomeDll = "motor.dll";

        // ==========================================
        // 4. CONTROLES DA INTERFACE
        // ==========================================

        private TextBox? txtEan;
        private Button? btnBuscar;
        private Label? lblResultadoBusca;
        private Button? btnRadar;
        private DataGridView? dataGridView1;
        private Label? lblStatus;
        private TextBox? txtCadastroEan;
        private TextBox? txtNome;
        private TextBox? txtLocalizacao;
        private Button? btnCadastrarProduto;
        private TextBox? txtLoteEan;
        private TextBox? txtCodigoLote;
        private DateTimePicker? dtpValidade;
        private NumericUpDown? nudQuantidade;
        private Button? btnCadastrarLote;
        private TextBox? txtEanDoLoteASerAlterado;
        private TextBox? txtCodigoLoteASerAlterado;
        private NumericUpDown? nudNovaQuantidade;
        private Button? btnAlterarQuantidade;
        private TextBox? txtEanRemoverLote;
        private TextBox? txtCodigoLoteRemover;
        private TextBox? txtEanRemoverProduto;
        private Button? btnRemoverProduto;
        public Form1()
        {
            ConfigurarInterfaceGrafica();

            callbackReferencia =
                new SqliteCallback(ProcessarLinhaRadar);

            callbackProdutos =
                new SqliteCallback(ProcessarLinhaProduto);

            callbackLotes =
                new SqliteCallback(ProcessarLinhaLote);

            this.Load += Form1_Load;
        }

        // ==========================================
        // 5. CONFIGURAÇÃO DA INTERFACE
        // ==========================================

        private void ConfigurarInterfaceGrafica()
        {
            this.Text = "DCP - Sistema de Inventário";
            this.Size = new Size(850, 900);
            this.StartPosition = FormStartPosition.CenterScreen;

            Label lblEan = new Label();
            lblEan.Text = "Código EAN:";
            lblEan.Location = new Point(20, 20);
            lblEan.AutoSize = true;
            this.Controls.Add(lblEan);

            // Campo EAN recebe a entrada do usuário
            txtEan = new TextBox();
            txtEan.Location = new Point(110, 18);
            txtEan.Width = 200;
            txtEan.Text = "";
            txtEan.ReadOnly = false;
            txtEan.MaxLength = 14;
            txtEan.PlaceholderText = "Digite o EAN";
            this.Controls.Add(txtEan);

            btnBuscar = new Button();
            btnBuscar.Text = "Buscar Produto";
            btnBuscar.Location = new Point(325, 16);
            btnBuscar.Width = 130;
            btnBuscar.Click += btnBuscar_Click;
            this.Controls.Add(btnBuscar);

            lblResultadoBusca = new Label();
            lblResultadoBusca.Text =
                "Resultado da busca aparecerá aqui.";
            lblResultadoBusca.Location = new Point(20, 55);
            lblResultadoBusca.AutoSize = true;
            lblResultadoBusca.Font =
                new Font(this.Font, FontStyle.Bold);
            this.Controls.Add(lblResultadoBusca);

            // ==========================================
            // ÁREA DE CADASTRO DE PRODUTOS
            // ==========================================

            Label lblCadastro = new Label();
            lblCadastro.Text = "CADASTRO DE PRODUTO";
            lblCadastro.Location = new Point(20, 100);
            lblCadastro.AutoSize = true;
            lblCadastro.Font = new Font(this.Font, FontStyle.Bold);
            this.Controls.Add(lblCadastro);

            // EAN

            Label lblEanCadastro = new Label();
            lblEanCadastro.Text = "EAN:";
            lblEanCadastro.Location = new Point(20, 130);
            lblEanCadastro.AutoSize = true;
            this.Controls.Add(lblEanCadastro);

            txtCadastroEan = new TextBox();
            txtCadastroEan.Location = new Point(20, 150);
            txtCadastroEan.Width = 180;
            txtCadastroEan.MaxLength = 14;
            txtCadastroEan.PlaceholderText = "Código EAN";
            this.Controls.Add(txtCadastroEan);

            // NOME

            Label lblNome = new Label();
            lblNome.Text = "Nome do produto:";
            lblNome.Location = new Point(220, 130);
            lblNome.AutoSize = true;
            this.Controls.Add(lblNome);

            txtNome = new TextBox();
            txtNome.Location = new Point(220, 150);
            txtNome.Width = 220;
            txtNome.MaxLength = 19;
            txtNome.PlaceholderText = "Nome";
            this.Controls.Add(txtNome);

            // LOCALIZAÇÃO

            Label lblLocalizacao = new Label();
            lblLocalizacao.Text = "Localização:";
            lblLocalizacao.Location = new Point(460, 130);
            lblLocalizacao.AutoSize = true;
            this.Controls.Add(lblLocalizacao);

            txtLocalizacao = new TextBox();
            txtLocalizacao.Location = new Point(460, 150);
            txtLocalizacao.Width = 220;
            txtLocalizacao.MaxLength = 29;
            txtLocalizacao.PlaceholderText = "Localização";
            this.Controls.Add(txtLocalizacao);

            // BOTÃO CADASTRAR

            btnCadastrarProduto = new Button();
            btnCadastrarProduto.Text = "Cadastrar";
            btnCadastrarProduto.Location = new Point(700, 149);
            btnCadastrarProduto.Width = 110;
            btnCadastrarProduto.Height = 28;
            btnCadastrarProduto.Click += btnCadastrarProduto_Click;
            this.Controls.Add(btnCadastrarProduto);

            // ==========================================
            // ÁREA DE CADASTRO DE LOTES
            // ==========================================

            Label lblCadastroLote = new Label();
            lblCadastroLote.Text = "CADASTRO DE LOTE";
            lblCadastroLote.Location = new Point(20, 200);
            lblCadastroLote.AutoSize = true;
            lblCadastroLote.Font = new Font(this.Font, FontStyle.Bold);
            this.Controls.Add(lblCadastroLote);

            // EAN DO PRODUTO

            Label lblLoteEan = new Label();
            lblLoteEan.Text = "EAN do produto:";
            lblLoteEan.Location = new Point(20, 230);
            lblLoteEan.AutoSize = true;
            this.Controls.Add(lblLoteEan);

            txtLoteEan = new TextBox();
            txtLoteEan.Location = new Point(20, 250);
            txtLoteEan.Width = 180;
            txtLoteEan.MaxLength = 14;
            txtLoteEan.PlaceholderText = "Código EAN";
            this.Controls.Add(txtLoteEan);

            // CÓDIGO DO LOTE

            Label lblCodigoLote = new Label();
            lblCodigoLote.Text = "Código do lote:";
            lblCodigoLote.Location = new Point(220, 230);
            lblCodigoLote.AutoSize = true;
            this.Controls.Add(lblCodigoLote);

            txtCodigoLote = new TextBox();
            txtCodigoLote.Location = new Point(220, 250);
            txtCodigoLote.Width = 180;
            txtCodigoLote.MaxLength = 30;
            txtCodigoLote.PlaceholderText = "Ex.: L001";
            this.Controls.Add(txtCodigoLote);

            // VALIDADE

            Label lblValidade = new Label();
            lblValidade.Text = "Validade:";
            lblValidade.Location = new Point(420, 230);
            lblValidade.AutoSize = true;
            this.Controls.Add(lblValidade);

            dtpValidade = new DateTimePicker();
            dtpValidade.Location = new Point(420, 250);
            dtpValidade.Width = 150;
            dtpValidade.Format = DateTimePickerFormat.Custom;
            dtpValidade.CustomFormat = "dd/MM/yyyy";
            this.Controls.Add(dtpValidade);

            // QUANTIDADE

            Label lblQuantidade = new Label();
            lblQuantidade.Text = "Quantidade:";
            lblQuantidade.Location = new Point(590, 230);
            lblQuantidade.AutoSize = true;
            this.Controls.Add(lblQuantidade);

            nudQuantidade = new NumericUpDown();
            nudQuantidade.Location = new Point(590, 250);
            nudQuantidade.Width = 100;
            nudQuantidade.Minimum = 1;
            nudQuantidade.Maximum = int.MaxValue;
            nudQuantidade.Value = 1;
            this.Controls.Add(nudQuantidade);

            // BOTÃO CADASTRAR LOTE

            btnCadastrarLote = new Button();
            btnCadastrarLote.Text = "Cadastrar Lote";
            btnCadastrarLote.Location = new Point(700, 249);
            btnCadastrarLote.Width = 110;
            btnCadastrarLote.Height = 28;
            btnCadastrarLote.Click += btnCadastrarLote_Click;
            this.Controls.Add(btnCadastrarLote);

            // ==========================================
            // ALTERAÇÃO DA QUANTIDADE EM LOTE
            // ==========================================

            Label lblAlterarQuantidade = new Label();
            lblAlterarQuantidade.Text = "ALTERAÇÃO DE QUANTIDADE EM LOTE";
            lblAlterarQuantidade.Location = new Point(20, 300);
            lblAlterarQuantidade.AutoSize = true;
            lblAlterarQuantidade.Font = new Font(this.Font, FontStyle.Bold);
            this.Controls.Add(lblAlterarQuantidade);

            // EAN DO PRODUTO
            Label lblEanAlteracao = new Label();
            lblEanAlteracao.Text = "EAN do produto:";
            lblEanAlteracao.Location = new Point(20, 330);
            lblEanAlteracao.AutoSize = true;
            this.Controls.Add(lblEanAlteracao);

            txtEanDoLoteASerAlterado = new TextBox();
            txtEanDoLoteASerAlterado.Location = new Point(20, 350);
            txtEanDoLoteASerAlterado.Width = 180;
            txtEanDoLoteASerAlterado.MaxLength = 14;
            txtEanDoLoteASerAlterado.PlaceholderText = "Código EAN";
            this.Controls.Add(txtEanDoLoteASerAlterado);

            // CÓDIGO DO LOTE
            Label lblCodigoLoteAlteracao = new Label();
            lblCodigoLoteAlteracao.Text = "Código do lote:";
            lblCodigoLoteAlteracao.Location = new Point(220, 330);
            lblCodigoLoteAlteracao.AutoSize = true;
            this.Controls.Add(lblCodigoLoteAlteracao);

            txtCodigoLoteASerAlterado = new TextBox();
            txtCodigoLoteASerAlterado.Location = new Point(220, 350);
            txtCodigoLoteASerAlterado.Width = 180;
            txtCodigoLoteASerAlterado.MaxLength = 30;
            txtCodigoLoteASerAlterado.PlaceholderText = "Código do lote";
            this.Controls.Add(txtCodigoLoteASerAlterado);

            // NOVA QUANTIDADE
            Label lblNovaQuantidade = new Label();
            lblNovaQuantidade.Text = "Nova quantidade:";
            lblNovaQuantidade.Location = new Point(420, 330);
            lblNovaQuantidade.AutoSize = true;
            this.Controls.Add(lblNovaQuantidade);

            nudNovaQuantidade = new NumericUpDown();
            nudNovaQuantidade.Location = new Point(420, 350);
            nudNovaQuantidade.Width = 100;
            nudNovaQuantidade.Minimum = 0;
            nudNovaQuantidade.Maximum = int.MaxValue;
            nudNovaQuantidade.Value = 0;
            this.Controls.Add(nudNovaQuantidade);

            // BOTÃO ALTERAR
            btnAlterarQuantidade = new Button();
            btnAlterarQuantidade.Text = "Alterar Quantidade";
            btnAlterarQuantidade.Location = new Point(550, 348);
            btnAlterarQuantidade.Width = 150;
            btnAlterarQuantidade.Height = 28;
            btnAlterarQuantidade.Click += btnAlterarQuantidade_Click;
            this.Controls.Add(btnAlterarQuantidade);

            // ==========================================
            // RADAR DE VENCIMENTOS
            // ==========================================

            btnRadar = new Button();
            btnRadar.Text = "Carregar Radar de Vencimentos";
            btnRadar.Location = new Point(20, 410);
            btnRadar.Width = 250;
            btnRadar.Height = 35;
            btnRadar.Click += btnRadar_Click;
            this.Controls.Add(btnRadar);

            // ==========================================
            // LISTAGEM DE PRODUTOS
            // ==========================================

            Button btnListarProdutos = new Button();
            btnListarProdutos.Text = "Listar Produtos";
            btnListarProdutos.Location = new Point(290, 410);
            btnListarProdutos.Width = 150;
            btnListarProdutos.Height = 35;
            btnListarProdutos.Click += btnListarProdutos_Click;
            this.Controls.Add(btnListarProdutos);

            // ==========================================
            // LISTAGEM DE LOTES
            // ==========================================

            Button btnListarLotes = new Button();
            btnListarLotes.Text = "Listar Lotes";
            btnListarLotes.Location = new Point(460, 410);
            btnListarLotes.Width = 150;
            btnListarLotes.Height = 35;
            btnListarLotes.Click += btnListarLotes_Click;
            this.Controls.Add(btnListarLotes);

            // =========================================
            // REMOÇÃO DE LOTE
            // =========================================

            Label lblRemoverLote = new Label();
            lblRemoverLote.Text = "REMOÇÃO DE LOTE";
            lblRemoverLote.Location = new Point(20, 465);
            lblRemoverLote.AutoSize = true;
            lblRemoverLote.Font = new Font(
                lblRemoverLote.Font,
                FontStyle.Bold
            );
            this.Controls.Add(lblRemoverLote);

            Label lblEanRemoverLote = new Label();
            lblEanRemoverLote.Text = "EAN:";
            lblEanRemoverLote.Location = new Point(20, 500);
            lblEanRemoverLote.AutoSize = true;
            this.Controls.Add(lblEanRemoverLote);

            txtEanRemoverLote = new TextBox();
            txtEanRemoverLote.Location = new Point(70, 497);
            txtEanRemoverLote.Width = 180;
            this.Controls.Add(txtEanRemoverLote);

            Label lblCodigoLoteRemover = new Label();
            lblCodigoLoteRemover.Text = "Lote:";
            lblCodigoLoteRemover.Location = new Point(270, 500);
            lblCodigoLoteRemover.AutoSize = true;
            this.Controls.Add(lblCodigoLoteRemover);

            txtCodigoLoteRemover = new TextBox();
            txtCodigoLoteRemover.Location = new Point(315, 497);
            txtCodigoLoteRemover.Width = 150;
            this.Controls.Add(txtCodigoLoteRemover);

            Button btnRemoverLote = new Button();
            btnRemoverLote.Text = "Remover Lote";
            btnRemoverLote.Location = new Point(485, 495);
            btnRemoverLote.Width = 125;
            btnRemoverLote.Height = 30;
            btnRemoverLote.Click += btnRemoverLote_Click;
            this.Controls.Add(btnRemoverLote);

            // =========================================
            // REMOÇÃO DE PRODUTO
            // =========================================

            Label lblRemoverProduto = new Label();
            lblRemoverProduto.Text = "REMOÇÃO DE PRODUTO";
            lblRemoverProduto.Location = new Point(20, 535);
            lblRemoverProduto.AutoSize = true;
            lblRemoverProduto.Font = new Font(
                lblRemoverProduto.Font,
                FontStyle.Bold
            );
            Controls.Add(lblRemoverProduto);

            Label lblEanRemoverProduto = new Label();
            lblEanRemoverProduto.Text = "EAN:";
            lblEanRemoverProduto.Location = new Point(20, 567);
            lblEanRemoverProduto.AutoSize = true;
            Controls.Add(lblEanRemoverProduto);

            txtEanRemoverProduto = new TextBox();
            txtEanRemoverProduto.Location = new Point(70, 564);
            txtEanRemoverProduto.Size = new Size(180, 25);
            Controls.Add(txtEanRemoverProduto);

            btnRemoverProduto = new Button();
            btnRemoverProduto.Text = "Remover Produto";
            btnRemoverProduto.Location = new Point(270, 562);
            btnRemoverProduto.Size = new Size(150, 30);
            btnRemoverProduto.Click += btnRemoverProduto_Click;
            Controls.Add(btnRemoverProduto);

            lblStatus = new Label();
            lblStatus.Text = "Aguardando inicialização...";
            lblStatus.Location = new Point(290, 450);
            lblStatus.AutoSize = true;
            this.Controls.Add(lblStatus);

            dataGridView1 = new DataGridView();
            dataGridView1.Location = new Point(20, 620);
            dataGridView1.Size = new Size(790, 190);

            dataGridView1.AutoSizeColumnsMode =
                DataGridViewAutoSizeColumnsMode.Fill;

            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.ReadOnly = true;
            dataGridView1.SelectionMode =
                DataGridViewSelectionMode.FullRowSelect;

            dataGridView1.MultiSelect = false;

            this.Controls.Add(dataGridView1);
        }

        // ==========================================
        // 6. INICIALIZAÇÃO DO MOTOR C E SQLITE
        // ==========================================

        private void Form1_Load(object? sender, EventArgs e)
        {
            try
            {
                Environment.CurrentDirectory =
                    AppContext.BaseDirectory;

                // ==========================================
                // DIAGNÓSTICO: CAMINHO DA MOTOR.DLL
                // ==========================================

                string caminhoDll = Path.Combine(
                    AppContext.BaseDirectory,
                    NomeDll
                );

                string informacoesDll;

                if (File.Exists(caminhoDll))
                {
                    FileInfo arquivoDll = new FileInfo(caminhoDll);

                    informacoesDll =
                        "Arquivo encontrado!\n\n" +
                        "Caminho:\n" + caminhoDll + "\n\n" +
                        "Última modificação:\n" +
                        arquivoDll.LastWriteTime.ToString("dd/MM/yyyy HH:mm:ss");
                }
                else
                {
                    informacoesDll =
                        "A DLL não foi encontrada neste caminho:\n\n" +
                        caminhoDll;
                }

                MessageBox.Show(
                    informacoesDll,
                    "Verificação da motor.dll",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                // ==========================================
                // INICIALIZAÇÃO DO BANCO
                // ==========================================

                string caminhoBanco = Path.Combine(
                    AppContext.BaseDirectory,
                    "dcp.db"
                );

                int resultado = inicializar_motor_dcp();

                if (resultado == 0)
                {
                    motorInicializado = true;

                    lblStatus!.Text = "Motor DCP inicializado.";

                    MessageBox.Show(
                        "Motor C e banco SQLite inicializados com sucesso!\n\n" +
                        "Banco utilizado:\n" + caminhoBanco,
                        "DCP - Inicialização",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
                else
                {
                    btnBuscar!.Enabled = false;
                    btnRadar!.Enabled = false;
                    btnCadastrarProduto!.Enabled = false;
                    btnCadastrarLote!.Enabled = false;

                    lblStatus!.Text = "Falha na inicialização.";

                    MessageBox.Show(
                        "Não foi possível inicializar o motor DCP.\n" +
                        "Código de erro: " + resultado,
                        "Erro de Inicialização",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
            catch (DllNotFoundException ex)
            {
                btnBuscar!.Enabled = false;
                btnRadar!.Enabled = false;
                btnCadastrarProduto!.Enabled = false;
                btnCadastrarLote!.Enabled = false;

                MessageBox.Show(
                    "Não foi possível carregar motor.dll ou uma de suas dependências:\n\n" +
                    ex.Message,
                    "Erro de DLL",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            catch (EntryPointNotFoundException ex)
            {
                btnBuscar!.Enabled = false;
                btnRadar!.Enabled = false;
                btnCadastrarProduto!.Enabled = false;
                btnCadastrarLote!.Enabled = false;

                MessageBox.Show(
                    "Uma função não foi encontrada na motor.dll:\n\n" +
                    ex.Message,
                    "Erro de Exportação",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            catch (Exception ex)
            {
                btnBuscar!.Enabled = false;
                btnRadar!.Enabled = false;
                btnCadastrarProduto!.Enabled = false;
                btnCadastrarLote!.Enabled = false;

                MessageBox.Show(
                    "Erro durante a inicialização:\n\n" + ex.Message,
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        // ==========================================
        // 7. BUSCA DINÂMICA DE PRODUTO PELA EAN
        // ==========================================

        private void btnBuscar_Click(object? sender, EventArgs e)
        {
            if (!motorInicializado)
                return;

            try
            {
                // Obtém o EAN digitado pelo usuário
                string ean = txtEan!.Text.Trim();

                // Verifica se o campo está vazio
                if (string.IsNullOrWhiteSpace(ean))
                {
                    MessageBox.Show(
                        "Digite um código EAN para realizar a busca.",
                        "EAN não informado",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );

                    txtEan.Focus();
                    return;
                }

                // Prepara os buffers de saída
                StringBuilder localOut = new StringBuilder(30);
                StringBuilder nomeOut = new StringBuilder(20);

                // Executa a busca no motor C
                int status = buscar_produto_dll(
                    ean,
                    localOut,
                    nomeOut
                );

                // Interpreta o retorno da DLL
                switch ((StatusBuscaProduto)status)
                {
                    case StatusBuscaProduto.Sucesso:

                        lblResultadoBusca!.Text =
                            "Produto: " + nomeOut +
                            " | Localização: " + localOut;

                        break;

                    case StatusBuscaProduto.NaoEncontrado:

                        lblResultadoBusca!.Text =
                            "Produto não cadastrado para o EAN: " + ean;

                        break;

                    case StatusBuscaProduto.EanInvalido:

                        MessageBox.Show(
                            "O código informado não é um EAN válido.\n" +
                            "Verifique o tamanho, os dígitos e o dígito verificador.",
                            "EAN inválido",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );

                        txtEan.Focus();
                        break;

                    case StatusBuscaProduto.ArgumentoInvalido:

                        MessageBox.Show(
                            "Foram recebidos argumentos inválidos pelo motor C.",
                            "Erro na busca",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );

                        break;

                    case StatusBuscaProduto.MotorNaoInicializado:

                        MessageBox.Show(
                            "O motor DCP não está inicializado.",
                            "Erro na busca",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );

                        break;

                    default:

                        MessageBox.Show(
                            "Retorno inesperado da motor.dll: " + status,
                            "Erro desconhecido",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );

                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Erro na busca do produto:\n\n" + ex.Message,
                    "Erro de Busca",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        // ==========================================
        // 8. CADASTRO DE PRODUTOS
        // ==========================================

        private void btnCadastrarProduto_Click(object? sender, EventArgs e)
        {
            if (!motorInicializado)
                return;

            try
            {
                string ean = txtCadastroEan!.Text.Trim();
                string nome = txtNome!.Text.Trim();
                string localizacao = txtLocalizacao!.Text.Trim();

                //Verifica campos vazios
                if (string.IsNullOrWhiteSpace(ean) ||
                    string.IsNullOrWhiteSpace(nome) ||
                    string.IsNullOrWhiteSpace(localizacao))
                {
                    MessageBox.Show(
                        "Preencha todos os campos para cadastrar o produto.",
                        "Dados incompletos",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );

                    return;
                }

                // Executa o cadastro no motor C
                int status = cadastrar_produto_dll(
                    localizacao,
                    nome,
                    ean
                );

                // Interpreta o retorno da DLL
                switch ((StatusCadastroProduto)status)
                {
                    case StatusCadastroProduto.Sucesso:

                        MessageBox.Show(
                            "Produto cadastrado com sucesso!\n\n" +
                            "Nome: " + nome + "\n" +
                            "EAN: " + ean + "\n" +
                            "Localização: " + localizacao,
                            "Cadastro realizado",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );

                        txtCadastroEan!.Clear();
                        txtNome!.Clear();
                        txtLocalizacao!.Clear();

                        txtCadastroEan.Focus();

                        break;

                    case StatusCadastroProduto.ArgumentoInvalido:
                        MessageBox.Show(
                            "Os dados informados são inválidos.",
                            "Erro no cadastro",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );

                        break;

                    case StatusCadastroProduto.EanInvalido:

                        MessageBox.Show(
                            "O código EAN informado é inválido.",
                            "EAN inválido",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );

                        txtCadastroEan.Focus();

                        break;

                    case StatusCadastroProduto.ProdutoExistente:

                        MessageBox.Show(
                            "Já existe um produto cadastrado com esse EAN.",
                            "Produto existente",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );

                        txtCadastroEan.Focus();

                        break;


                    case StatusCadastroProduto.ErroMemoria:

                        MessageBox.Show(
                            "Não foi possível alocar memória para cadastrar o produto.",
                            "Erro de memória",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );

                        break;

                    case StatusCadastroProduto.ErroBanco:

                        MessageBox.Show(
                            "Ocorreu um erro ao gravar o produto no banco de dados.",
                            "Erro no SQLite",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );

                        break;

                    case StatusCadastroProduto.MotorNaoInicializado:

                        MessageBox.Show(
                            "O motor DCP não está inicializado.",
                            "Erro no motor",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );

                        break;

                    case StatusCadastroProduto.NomeExistente:

                        MessageBox.Show(
                            "Já existe um produto cadastrado com esse nome.",
                            "Cadastro de Produto",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );

                        break;

                    default:

                        MessageBox.Show(
                            "Retorno inesperado da motor.dll: " + status,
                            "Erro desconhecido",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );

                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Erro ao cadastrar produto:\n\n" + ex.Message,
                    "Erro de cadastro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        // ==========================================
        // 9. CADASTRO DE LOTES
        // ==========================================

        private void btnCadastrarLote_Click(object? sender, EventArgs e)
        {
            if (!motorInicializado)
            {
                MessageBox.Show(
                    "O motor DCP não está inicializado.",
                    "Aviso",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }

            try
            {
                // Obtém os dados informados pelo usuário
                string ean = txtLoteEan!.Text.Trim();
                string lote = txtCodigoLote!.Text.Trim();

                // Converte a validade para o formato ISO (AAAA-MM-DD)
                string validade = dtpValidade!.Value.ToString(
                    "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture
                );

                // Obtém a quantidade selecionada
                int quantidade = Decimal.ToInt32(
                    nudQuantidade!.Value
                );

                // Verifica se os campos obrigatórios foram preenchidos
                if (string.IsNullOrWhiteSpace(ean) ||
                    string.IsNullOrWhiteSpace(lote))
                {
                    MessageBox.Show(
                        "Preencha o EAN e o código do lote.",
                        "Dados incompletos",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );

                    return;
                }

                // Executa o cadastro no motor C
                int status = cadastrar_lote_dll(
                    ean,
                    lote,
                    validade,
                    quantidade
                );

                // Interpreta o retorno da DLL
                switch ((StatusCadastroLote)status)
                {
                    case StatusCadastroLote.Sucesso:
                        MessageBox.Show(
                            "Lote cadastrado com sucesso!\n\n" +
                            "EAN: " + ean + "\n" +
                            "Lote: " + lote + "\n" +
                            "Validade: " + dtpValidade.Value.ToString("dd/MM/yyyy") + "\n" +
                            "Quantidade: " + quantidade,
                            "Cadastro realizado",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );

                        //Limpa os campos após o sucesso
                        txtLoteEan.Clear();
                        txtCodigoLote.Clear();
                        nudQuantidade.Value = 1;

                        txtLoteEan.Focus();

                        break;

                    case StatusCadastroLote.ArgumentoInvalido:
                        MessageBox.Show(
                            "Os dados informados são inválidos.",
                            "Erro no cadastro",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );

                        break;

                    case StatusCadastroLote.EanInvalido:
                        MessageBox.Show(
                            "O código EAN informado é inválido.\n" +
                            "Verifique o tamanho, os dígitos e o dígito verificador.",
                            "EAN inválido",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );

                        txtLoteEan.Focus();

                        break;

                    case StatusCadastroLote.ProdutoNaoEncontrado:
                        MessageBox.Show(
                            "Não existe um produto cadastrado com esse EAN.\n" +
                            "Cadastre o produto antes de cadastrar seus lotes.",
                            "Produto não encontrado",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );

                        txtLoteEan.Focus();

                        break;

                    case StatusCadastroLote.LoteInvalido:
                        MessageBox.Show(
                            "O código do lote informado é inválido.\n" +
                            "Verifique se possui entre 1 e 30 caracteres.",
                            "Lote inválido",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );

                        txtCodigoLote.Focus();

                        break;

                    case StatusCadastroLote.ValidadeInvalida:
                        MessageBox.Show(
                            "A data de validade informada é inválida.",
                            "Validade inválida",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );

                        break;

                    case StatusCadastroLote.QuantidadeInvalida:
                        MessageBox.Show(
                            "A quantidade deve ser maior que zero.",
                            "Quantidade inválida",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );

                        nudQuantidade.Focus();

                        break;

                    case StatusCadastroLote.LoteDuplicado:
                        MessageBox.Show(
                            "Já existe um lote com esse código cadastrado para este produto.",
                            "Lote duplicado",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );

                        txtCodigoLote.Focus();

                        break;

                    case StatusCadastroLote.ErroBanco:
                        MessageBox.Show(
                            "Ocorreu um erro ao gravar o lote no banco de dados.",
                            "Erro no SQLite",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );

                        break;

                    case StatusCadastroLote.MotorNaoInicializado:
                        MessageBox.Show(
                            "O motor DCP não está inicializado.",
                            "Erro no motor",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );

                        break;

                    default:
                        MessageBox.Show(
                            "Retorno inesperado da motor.dll: " + status,
                            "Erro desconhecido",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );

                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Erro ao cadastrar lote:\n\n" + ex.Message,
                    "Erro de cadastro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        // ==========================================
        // 10. ALTERAÇÃO DA QUANTIDADE EM LOTE
        // ==========================================
        private void btnAlterarQuantidade_Click(object? sender, EventArgs e)
        {
            if (!motorInicializado)
            {
                MessageBox.Show("O motor não está inicialixado.");
                return;
            }

            string ean = txtEanDoLoteASerAlterado!.Text.Trim();
            string lote = txtCodigoLoteASerAlterado!.Text.Trim();

            int novaQuantidade = Decimal.ToInt32(nudNovaQuantidade!.Value);

            // VERIFICA SE OS CAMPOS FORAM PREENCHIDOS
            if (string.IsNullOrWhiteSpace(ean) ||
                string.IsNullOrWhiteSpace(lote))
            {
                MessageBox.Show(
                    "Informe o EAN e o código do lote.",
                    "Campos obrigatórios",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }

            // CHAMA A FUNÇÃO DA DLL
            int status = alterar_quantidade_em_lote_dll(
                ean,
                lote,
                novaQuantidade
            );

            // INTERPRETA O RETORNO DA DLL
            switch ((StatusAlteracaoLote)status)
            {
                case StatusAlteracaoLote.Sucesso:

                    MessageBox.Show(
                        "Quantidade alterada com sucesso!\n\n" +
                        "EAN: " + ean +
                        "\nLote: " + lote +
                        "\nNova quantidade: " + novaQuantidade,
                        "Alteração concluída",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );

                    txtEanDoLoteASerAlterado.Clear();
                    txtCodigoLoteASerAlterado.Clear();
                    nudNovaQuantidade.Value = 0;

                    break;

                case StatusAlteracaoLote.ArgumentoInvalido:

                    MessageBox.Show("Os argumentos informados são inválidos.");
                    break;

                case StatusAlteracaoLote.EanInvalido:

                    MessageBox.Show("O EAN informado é inválido.");
                    break;

                case StatusAlteracaoLote.ProdutoNaoEncontrado:

                    MessageBox.Show("Produto não encontrado na tabela hash.");
                    break;

                case StatusAlteracaoLote.LoteNaoEncontrado:

                    MessageBox.Show("Não foi encontrado um lote correspondente ao EAN informado.");
                    break;

                case StatusAlteracaoLote.QuantidadeInvalida:

                    MessageBox.Show("A quantidade informada é inválida.");
                    break;

                case StatusAlteracaoLote.ErroBanco:

                    MessageBox.Show("Ocorreu um erro ao acessar o banco de dados.");
                    break;

                case StatusAlteracaoLote.MotorNaoInicializado:

                    MessageBox.Show("O motor DCP não está inicializado.");
                    break;

                default:

                    MessageBox.Show("A DLL retornou um código de status desconhecido: " + status);
                    break;
            }
        }

        // ==========================================
        // 11. CALLBACK DA LISTAGEM DE PRODUTOS
        // ==========================================
        private int ProcessarLinhaProduto(
            IntPtr userData,
            int argc,
            IntPtr argv,
            IntPtr colName)
        {
            try
            {
                if (tabelaProdutos == null || argc < 2 || argv == IntPtr.Zero)
                {
                    return 0;
                }

                string ean = PtrToString(
                    Marshal.ReadIntPtr(argv, 0 * IntPtr.Size)
                );

                string nome = PtrToString(
                    Marshal.ReadIntPtr(argv, 1 * IntPtr.Size)
                );

                tabelaProdutos.Rows.Add(ean, nome);

                return 0;
            }
            catch
            {
                return 1;
            }
        }

        // ==========================================
        // 12. CALLBACK DA LISTAGEM DE LOTES
        // ==========================================

        private int ProcessarLinhaLote(
            IntPtr userData,
            int argc,
            IntPtr argv,
            IntPtr colName)
        {
            try
            {
                if (tabelaLotes == null || argc < 3 || argv == IntPtr.Zero)
                {
                    return 0;
                }

                string produto = PtrToString(
                    Marshal.ReadIntPtr(argv, 0 * IntPtr.Size)
                );

                string lote = PtrToString(
                    Marshal.ReadIntPtr(argv, 1 * IntPtr.Size)
                );

                string quantidade = PtrToString(
                    Marshal.ReadIntPtr(argv, 2 * IntPtr.Size)
                );

                tabelaLotes.Rows.Add(
                    produto,
                    lote,
                    quantidade
                );

                return 0;
            }
            catch
            {
                return 1;
            }
        }

        // ==========================================
        // 13. EXECUÇÃO DA LISTAGEM DE PRODUTOS
        // ==========================================

        private void btnListarProdutos_Click(object? sender, EventArgs e)
        {
            if (!motorInicializado)
            {
                MessageBox.Show(
                    "O motor DCP não está inicializado.",
                    "Aviso",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }

            try
            {
                tabelaProdutos = new DataTable();

                tabelaProdutos.Columns.Add("EAN");
                tabelaProdutos.Columns.Add("Produto");

                dataGridView1!.DataSource = tabelaProdutos;

                int resultado = listar_produtos_dll(
                    callbackProdutos!,
                    IntPtr.Zero
                );

                if (resultado == 0)
                {
                    lblStatus!.Text = "Listagem de produtos executada.";

                    MessageBox.Show(
                        "Listagem concluída com sucesso!\n\n" +
                        "Total de produtos cadastrados: " +
                        tabelaProdutos.Rows.Count,
                        "Listagem de Produtos",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }

                else
                {
                    lblStatus!.Text = "Erro ao listar produtos.";

                    MessageBox.Show(
                        "O motor C não conseguiu listar os produtos.\n\n" +
                        "Código retornado: " + resultado,
                        "Erro na Listagem",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Erro ao listar produtos:\n\n" + ex.Message,
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        // ==========================================
        // 14. EXECUÇÃO DA LISTAGEM DE LOTES
        // ==========================================

        private void btnListarLotes_Click(object? sender, EventArgs e)
        {
            if (!motorInicializado)
            {
                MessageBox.Show(
                    "O motor DCP não está inicializado.",
                    "Aviso",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }

            try
            {
                tabelaLotes = new DataTable();

                tabelaLotes.Columns.Add("Produto");
                tabelaLotes.Columns.Add("Lote");
                tabelaLotes.Columns.Add("Quantidade");

                dataGridView1!.DataSource = tabelaLotes;

                int resultado = listar_lotes_dll(
                    callbackLotes!,
                    IntPtr.Zero
                );

                if (resultado == 0)
                {
                    lblStatus!.Text = "Listagem de lotes executada.";

                    MessageBox.Show(
                        "Listagem concluída com sucesso!\n\n" +
                        "Total de lotes cadastrados: " +
                        tabelaLotes.Rows.Count,
                        "Listagem de Lotes",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
                else
                {
                    lblStatus!.Text = "Erro ao listar lotes.";

                    MessageBox.Show(
                        "O motor C não conseguiu listar os lotes.\n\n" +
                        "Código retornado: " + resultado,
                        "Erro na Listagem",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Erro ao listar lotes:\n\n" + ex.Message,
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        // ==========================================
        // 15. EXECUÇÃO DA REMOÇÃO DE LOTE
        // ==========================================

        private void btnRemoverLote_Click(object? sender, EventArgs e)
        {
            if (!motorInicializado)
            {
                MessageBox.Show(
                    "O motor DCP não está inicializado.",
                    "Aviso",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }

            try
            {
                string ean = txtEanRemoverLote!.Text.Trim();
                string lote = txtCodigoLoteRemover!.Text.Trim();

                int resultado = remover_lote_dll(ean, lote);

                switch ((StatusRemocaoLote)resultado)
                {
                    case StatusRemocaoLote.Sucesso:

                        lblStatus!.Text = "Lote removido com sucesso.";

                        MessageBox.Show(
                            "Lote removido com sucesso!",
                            "Remoção de Lote",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );

                        txtEanRemoverLote.Clear();
                        txtCodigoLoteRemover.Clear();

                        break;

                    case StatusRemocaoLote.ArgumentoInvalido:

                        MessageBox.Show(
                            "Informe um EAN e um código de lote válidos.",
                            "Argumentos Inválidos",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );

                        break;

                    case StatusRemocaoLote.EanInvalido:

                        MessageBox.Show(
                            "O EAN informado é inválido.",
                            "EAN Inválido",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );

                        break;

                    case StatusRemocaoLote.ProdutoNaoEncontrado:

                        MessageBox.Show(
                            "O produto informado não está cadastrado.",
                            "Produto não encontrado",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );

                        break;

                    case StatusRemocaoLote.LoteNaoEncontrado:

                        MessageBox.Show(
                            "O lote informado não foi encontrado para este produto.",
                            "Lote não encontrado",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );

                        break;

                    case StatusRemocaoLote.ErroBanco:

                        MessageBox.Show(
                            "Ocorreu um erro ao remover o lote no banco de dados.",
                            "Erro no banco de dados",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );

                        break;

                    case StatusRemocaoLote.MotorNaoInicializado:

                        MessageBox.Show(
                            "O motor DCP não está inicializado.",
                            "Motor Não Inicializado",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );

                        break;

                    default:

                        MessageBox.Show(
                            "O motor retornou um código inesperado: " + resultado,
                            "Erro",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );

                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Erro ao remover lote:\n\n" + ex.Message,
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        // ==========================================
        // 16. EXECUÇÃO DA REMOÇÃO DE PRODUTO
        // ==========================================
        private void btnRemoverProduto_Click(object? sender, EventArgs e)
        {
            if (!motorInicializado)
            {
                MessageBox.Show(
                    "O motor DCP não está inicializado.",
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return;
            }

            try
            {
                string ean = txtEanRemoverProduto!.Text.Trim();

                if (string.IsNullOrWhiteSpace(ean))
                {
                    MessageBox.Show(
                        "Informe o EAN do produto.",
                        "Atenção",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return;
                }

                DialogResult confirmacao = MessageBox.Show(
                    "A remoção deste produto também excluirá todos os lotes associados.\n\nDeseja continuar?",
                    "Confirmar remoção de produto",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );

                if (confirmacao != DialogResult.Yes)
                {
                    return;
                }

                int resultado = remover_produto_dll(ean);

                switch ((StatusRemocaoProduto)resultado)
                {
                    case StatusRemocaoProduto.Sucesso:

                        lblStatus!.Text = "Produto e lotes associados removidos com sucesso.";

                        MessageBox.Show(
                            "Produto e todos os lotes associados foram removidos com sucesso.",
                            "Remoção concluída",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );

                        txtEanRemoverProduto.Clear();

                        break;

                    case StatusRemocaoProduto.ArgumentoInvalido:

                        MessageBox.Show("Argumento inválido.", "Erro",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        break;

                    case StatusRemocaoProduto.EanInvalido:

                        MessageBox.Show("O EAN informado é inválido.", "Erro",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        break;

                    case StatusRemocaoProduto.ProdutoNaoEncontrado:

                        MessageBox.Show("Produto não encontrado.", "Atenção",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        break;

                    case StatusRemocaoProduto.ErroBanco:

                        MessageBox.Show("Ocorreu um erro ao remover o produto do banco de dados.",
                            "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;

                    case StatusRemocaoProduto.MotorNaoInicializado:

                        MessageBox.Show("O motor DCP não está inicializado.",
                            "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;

                    default:

                        MessageBox.Show("Ocorreu um erro inesperado.",
                            "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Erro ao remover produto:\n\n" + ex.Message,
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        // ==========================================
        // 17. CALLBACK DO RADAR
        // ==========================================
        private int ProcessarLinhaRadar(
            IntPtr userData,
            int argc,
            IntPtr argv,
            IntPtr colName
        )
        {
            try
            {
                if (tabelaRadar == null ||
                    argc < 6 ||
                    argv == IntPtr.Zero)
                {
                    return 0;
                }

                string nome = PtrToString(
                    Marshal.ReadIntPtr(argv, 0 * IntPtr.Size)
                );

                string lote = PtrToString(
                    Marshal.ReadIntPtr(argv, 1 * IntPtr.Size)
                );

                string validade = PtrToString(
                    Marshal.ReadIntPtr(argv, 2 * IntPtr.Size)
                );

                string localizacao = PtrToString(
                    Marshal.ReadIntPtr(argv, 3 * IntPtr.Size)
                );

                string quantidade = PtrToString(
                    Marshal.ReadIntPtr(argv, 4 * IntPtr.Size)
                );

                string status = PtrToString(
                    Marshal.ReadIntPtr(argv, 5 * IntPtr.Size)
                );

                tabelaRadar.Rows.Add(
                    nome,
                    lote,
                    ConverterData(validade),
                    localizacao,
                    quantidade,
                    status
                );

                return 0;
            }
            catch
            {
                return 1;
            }
        }

        // ==========================================
        // 18. CONVERSÃO DE PONTEIROS C PARA STRING
        // ==========================================

        private string PtrToString(IntPtr ptr)
        {
            return ptr == IntPtr.Zero
                ? string.Empty
                : Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
        }

        // ==========================================
        // 19. FORMATAÇÃO DA DATA
        // ==========================================

        private string ConverterData(string data)
        {
            if (DateTime.TryParseExact(
                data,
                "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out DateTime dataConvertida))
            {
                return dataConvertida.ToString("dd/MM/yyyy");
            }
            return data;
        }

        // ==========================================
        // 20. EXECUÇÃO DO RADAR DE VENCIMENTOS
        // ==========================================

        private void btnRadar_Click(object? sender, EventArgs e)
        {
            if (!motorInicializado)
            {
                MessageBox.Show(
                    "O motor DCP não está inicializado.",
                    "Aviso",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }

            try
            {
                tabelaRadar = new DataTable();

                tabelaRadar.Columns.Add("Produto");
                tabelaRadar.Columns.Add("Lote");
                tabelaRadar.Columns.Add("Validade");
                tabelaRadar.Columns.Add("Localização");
                tabelaRadar.Columns.Add("Quantidade");
                tabelaRadar.Columns.Add("Status");

                dataGridView1!.DataSource = tabelaRadar;

                int totalLotesBanco = contar_lotes_dcp();

                int diasLimite = 30;

                int resultado = executar_radar_dll(
                    diasLimite,
                    callbackReferencia!,
                    IntPtr.Zero
                );

                if (resultado == 0)
                {
                    lblStatus!.Text =
                        "Radar executado com sucesso.";

                    MessageBox.Show(
                        "RADAR DE VENCIMENTOS\n\n" +
                        "Total de lotes no banco: " +
                        totalLotesBanco + "\n\n" +
                        "Lotes encontrados pelo Radar: " +
                        tabelaRadar.Rows.Count + "\n\n" +
                        "Período analisado: " +
                        diasLimite + " dias.",
                        "Resultado do Radar",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
                else
                {
                    lblStatus!.Text =
                        "Erro na execução do Radar.";

                    MessageBox.Show(
                        "O motor C não conseguiu executar o Radar.\n\n" +
                        "Código retornado: " + resultado,
                        "Erro no Radar",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Erro ao executar o Radar:\n\n" + ex.Message,
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        // ==========================================
        // 21. LIBERAÇÃO DO MOTOR AO FECHAR
        // ==========================================

        protected override void OnFormClosing(
            FormClosingEventArgs e)
        {
            if (motorInicializado)
            {
                try
                {
                    liberar_motor_dcp();
                    motorInicializado = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Aviso ao liberar o motor:\n\n" + ex.Message,
                        "Liberação do Motor",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                }
            }

            base.OnFormClosing(e);
        }
    }
}