using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers;
using Windows.Storage;
using Microsoft.UI;           
using Microsoft.UI.Windowing; 
using WinRT.Interop;
using JJ.NET.Core.Extensoes;
using GerenciarArquivo.Controls;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;

namespace GerenciarArquivo
{
    public sealed partial class MainWindow : Window
    {
        #region Propriedades
        private ObservableCollection<FileInfo> arquivos = new();
        private readonly string _configFilePath;
        private AppConfig config = new AppConfig();
        #endregion

        #region Construtor
        public MainWindow()
        {
            this.InitializeComponent();

            AppWindow m_AppWindow = GetAppWindowForCurrentWindow();
            m_AppWindow.Title = "Gerenciar Arquivos de Par�metros";
            m_AppWindow.SetIcon("Assets/icone.ico");
            m_AppWindow.Resize(new Windows.Graphics.SizeInt32(600, 600));

            var localFolder = ApplicationData.Current.LocalFolder.Path;
            _configFilePath = Path.Combine(localFolder, "config.json");

            arquivos.CollectionChanged += Arquivos_CollectionChanged;
            lvFiles.ItemsSource = arquivos;

            CarregarConfiguracoes();
        }
        #endregion

        #region Eventos 
        private void Arquivos_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            txtQtdTotal.Text = $"Total: {arquivos.Count}";
        }
        private async void btnSelecionarPastaOrigem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var folderPicker = new FolderPicker();
                folderPicker.FileTypeFilter.Add("*");

                var hwnd = WindowNative.GetWindowHandle(this);
                InitializeWithWindow.Initialize(folderPicker, hwnd);

                StorageFolder folder = await folderPicker.PickSingleFolderAsync();

                if (folder != null)
                {
                    var files = await folder.GetFilesAsync();

                    if (files.Count == 0)
                    {
                        await Mensagem.AvisoAsync("A pasta selecionada n�o cont�m arquivos.", this.Content.XamlRoot);
                        return;
                    }

                    var folderPath = folder.Path;

                    foreach (var file in files)
                    {
                        if (!arquivos.Any(f => f.FullName == file.Path))
                        {
                            arquivos.Add(new FileInfo(file.Path));
                            config.OrigemArquivos.Add(file.Path);
                        }
                    }

                    await SalvarConfiguracoes();
                    CarregarArquivosDaOrigem();
                }
            }
            catch (Exception ex)
            {
                await Mensagem.ErroAsync(ex.Message, this.Content.XamlRoot);
            }
        }
        private async void btnAddArquivo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var filePicker = new FileOpenPicker();
                filePicker.FileTypeFilter.Add("*");

                var hwnd = WindowNative.GetWindowHandle(this);
                InitializeWithWindow.Initialize(filePicker, hwnd);

                var files = await filePicker.PickMultipleFilesAsync();

                if (files != null && files.Count > 0)
                {
                    foreach (var file in files)
                    {
                        if (!arquivos.Any(f => f.FullName == file.Path))
                        {
                            arquivos.Add(new FileInfo(file.Path));
                            config.OrigemArquivos.Add(file.Path);
                        }
                    }

                    await SalvarConfiguracoes();
                    CarregarArquivosDaOrigem();
                }
            }
            catch (Exception ex)
            {
                await Mensagem.ErroAsync(ex.Message, this.Content.XamlRoot);
            }
        }
        private async void btnSelecionarPastaDestino_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var folderPicker = new FolderPicker();
                folderPicker.FileTypeFilter.Add("*");

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

                StorageFolder folder = await folderPicker.PickSingleFolderAsync();

                if (folder != null)
                {
                    txtCaminhoDestino.Text = folder.Path;
                    config.CaminhoDestino = folder.Path;
                }
            }
            catch (Exception ex)
            {
                await Mensagem.ErroAsync(ex.Message, this.Content.XamlRoot);
            }
            finally
            {
                await SalvarConfiguracoes();
            }
        }
        private async void txtNomePadrao_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                config.NomePadrao = txtNomePadrao.Text.ObterValorOuPadrao("").Trim();
            }
            catch (Exception ex)
            {
                await Mensagem.ErroAsync(ex.Message, this.Content.XamlRoot);
            }
            finally
            {
                await SalvarConfiguracoes();
            }
        }
        private void btnExcluirArquivo_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string path)
            {
                var item = arquivos.FirstOrDefault(f => f.FullName == path);
                if (item != null)
                {
                    arquivos.Remove(item);
                    config.OrigemArquivos.Remove(path);
                }
            }
        }
        private void btnCopiarArquivo_Click(object sender, RoutedEventArgs e)
        {

        }
        private async void btnLimparLista_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                config.OrigemArquivos.Clear();
                arquivos.Clear();
            }
            catch (Exception ex)
            {
                await Mensagem.ErroAsync(ex.Message, this.Content.XamlRoot);
            }
            finally
            {
                await SalvarConfiguracoes();
            }
        }
        private async void btnCopiarLista_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (config.CaminhoDestino.ObterValorOuPadrao("").Trim() == "")
                {
                    await Mensagem.AvisoAsync("Selecione uma pasta de destino antes de continuar.", this.Content.XamlRoot);
                    return;
                }

                if (!Directory.Exists(config.CaminhoDestino))
                {
                    await Mensagem.AvisoAsync("A pasta de destino selecionada n�o foi encontrada.", this.Content.XamlRoot);
                    return;
                }

                if (config.OrigemArquivos.Count <= 0)
                {
                    await Mensagem.AvisoAsync("N�o h� arquivos dispon�veis para copiar.", this.Content.XamlRoot);
                    return;
                }

                int qtdErros = 0;
                int qtdCopiaComSucesso = 0;

                foreach (var item in config.OrigemArquivos)
                {
                    var arquivoOrigem = new FileInfo(item);

                    if (!arquivoOrigem.Exists)
                    {
                        qtdErros++;
                        continue;
                    }

                    string nomeArquivoDeDestino = arquivoOrigem.Name;

                    if (config.NomePadrao.ObterValorOuPadrao("").Trim() != "" && chkNomePadrao.IsChecked == true)
                        nomeArquivoDeDestino = config.NomePadrao;

                    nomeArquivoDeDestino = nomeArquivoDeDestino + arquivoOrigem.Extension;

                    string destinationPath = Path.Combine(config.CaminhoDestino, nomeArquivoDeDestino);

                    try
                    {
                        File.Copy(arquivoOrigem.FullName, destinationPath, overwrite: true);
                        qtdCopiaComSucesso++;
                    }
                    catch 
                    {
                        qtdErros++;
                    }
                }

                if (qtdCopiaComSucesso > 0)
                {
                    await Mensagem.SucessoAsync($"{qtdCopiaComSucesso} arquivo(s) copiado(s) com sucesso.", this.Content.XamlRoot);
                }

                if (qtdErros > 0)
                {
                    string msgErro = qtdErros == 1 ? 
                        "1 arquivo n�o p�de ser copiado devido a um erro." : 
                        $"{qtdErros} arquivos n�o puderam ser copiados devido a erros."; 

                    await Mensagem.ErroAsync(msgErro, this.Content.XamlRoot);
                }
            }
            catch (Exception ex)
            {
                await Mensagem.ErroAsync(ex.Message, this.Content.XamlRoot);
            }
        }
        private void chkNomePadrao_Unchecked(object sender, RoutedEventArgs e)
        {
            txtNomePadrao.IsEnabled = false;
        }
        private void chkNomePadrao_Checked(object sender, RoutedEventArgs e)
        {
            txtNomePadrao.IsEnabled = true;
        }
        #endregion

        #region Metodos
        private async void CarregarConfiguracoes()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                {
                    var defaultConfig = new AppConfig
                    {
                        CaminhoDestino = "",
                        NomePadrao = "",
                        OrigemArquivos = new List<string>(),
                    };

                    await File.WriteAllTextAsync(_configFilePath, JsonSerializer.Serialize(defaultConfig));
                }

                var json = await File.ReadAllTextAsync(_configFilePath);
                config = JsonSerializer.Deserialize<AppConfig>(json);

                BindPrincipal();
                CarregarArquivosDaOrigem();
            }
            catch (Exception ex)
            {
                await Mensagem.ErroAsync(ex.Message, this.Content.XamlRoot);
            }
        }
        private async void CarregarArquivosDaOrigem()
        {
            try
            {
                arquivos.Clear();
                var caminhosInvalidos = new List<string>();

                foreach (var caminho in config.OrigemArquivos)
                {
                    if (File.Exists(caminho))
                    {
                        arquivos.Add(new FileInfo(caminho));
                    }
                    else
                    {
                        caminhosInvalidos.Add(caminho);
                    }
                }

                if (caminhosInvalidos.Any())
                {
                    foreach (var caminho in caminhosInvalidos)
                        config.OrigemArquivos.Remove(caminho);

                    await SalvarConfiguracoes();
                }

                if (arquivos.Count == 0)
                {
                    await Mensagem.AvisoAsync("Nenhum arquivo v�lido encontrado nas origens selecionadas.", this.Content.XamlRoot);
                }
                else
                {
                    await Mensagem.SucessoAsync("Arquivo adicionado com sucesso.", this.Content.XamlRoot);
                }
            }
            catch (Exception ex)
            {
                await Mensagem.ErroAsync(ex.Message, this.Content.XamlRoot);
                arquivos.Clear();
            }
        }
        private async Task SalvarConfiguracoes()
        {
            try
            {
                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_configFilePath, json);
            }
            catch (Exception ex)
            {
                await Mensagem.ErroAsync($"Erro ao salvar configura��es: {ex.Message}", this.Content.XamlRoot);
            }
        }
        private async void BindPrincipal()
        {
            txtCaminhoDestino.Text = config.CaminhoDestino.ObterValorOuPadrao("Nenhuma pasta selecionada").Trim();
            txtNomePadrao.Text = config.NomePadrao.ObterValorOuPadrao("").Trim();
        }
        private AppWindow GetAppWindowForCurrentWindow()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(wndId);
        }
        private void AtualizarStatus()
        {
            txtQtdTotal.Text = arquivos.Count.ToString("N0");
        }
        #endregion
    }

    public class AppConfig
    {
        public List<string> OrigemArquivos { get; set; } = new List<string>();
        public string CaminhoDestino { get; set; } = "";
        public string NomePadrao { get; set; } = "";
    }
}
