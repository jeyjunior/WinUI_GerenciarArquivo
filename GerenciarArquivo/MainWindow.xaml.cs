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
using JJ.NET.Core.DTO;
using Windows.Storage.AccessCache;

namespace GerenciarArquivo
{
    public sealed partial class MainWindow : Window
    {
        #region Propriedades
        private ObservableCollection<FileItem> arquivos = new();
        private string _configFilePath;
        private AppConfig config = new AppConfig();
        #endregion

        #region Construtor
        public MainWindow()
        {
            this.InitializeComponent();
            IniciarAppAsync();
        }
        #endregion

        #region Eventos 
        private void Arquivos_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            txtQtdTotal.Text = "Total: " + arquivos.Count.ToString("N0");
        }
        private async void btnSelecionarPastaOrigem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var folderPicker = new FolderPicker();
                folderPicker.FileTypeFilter.Add("*");
                InitializeWithWindow.Initialize(folderPicker, WindowNative.GetWindowHandle(this));

                StorageFolder folder = await folderPicker.PickSingleFolderAsync();

                if (folder != null)
                {
                    var files = await folder.GetFilesAsync();
                    if (files.Count == 0)
                    {
                        await Mensagem.AvisoAsync("A pasta selecionada não contém arquivos.", this.Content.XamlRoot);
                        return;
                    }

                    arquivos.Clear();
                    config.OrigemArquivos.Clear();

                    foreach (var file in files)
                    {
                        var futureAccess = StorageApplicationPermissions.FutureAccessList.Add(file);
                        arquivos.Add(new FileItem
                        {
                            Name = file.Name,
                            Path = file.Path,
                            FutureAccessToken = futureAccess
                        });
                        config.OrigemArquivos.Add(file.Path);
                    }

                    await SalvarConfiguracoes();
                    Arquivos_CollectionChanged(null,null);
                }
            }
            catch (Exception ex)
            {
                await Mensagem.ErroAsync($"Erro ao selecionar pasta: {ex.Message}", this.Content.XamlRoot);
            }
        }
        private async void btnAddArquivo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var filePicker = new FileOpenPicker();
                filePicker.FileTypeFilter.Add("*");
                InitializeWithWindow.Initialize(filePicker, WindowNative.GetWindowHandle(this));

                var files = await filePicker.PickMultipleFilesAsync();

                if (files != null && files.Count > 0)
                {
                    foreach (var file in files)
                    {
                        if (!arquivos.Any(f => f.Path == file.Path))
                        {
                            var futureAccess = StorageApplicationPermissions.FutureAccessList.Add(file);
                            arquivos.Add(new FileItem
                            {
                                Name = file.Name,
                                Path = file.Path,
                                FutureAccessToken = futureAccess
                            });
                            config.OrigemArquivos.Add(file.Path);
                        }
                    }

                    await SalvarConfiguracoes();
                    Arquivos_CollectionChanged(null, null);
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
                InitializeWithWindow.Initialize(folderPicker, WindowNative.GetWindowHandle(this));

                StorageFolder folder = await folderPicker.PickSingleFolderAsync();

                if (folder != null)
                {
                    txtCaminhoDestino.Text = folder.Path;
                    config.CaminhoDestino = folder.Path;
                    await SalvarConfiguracoes();
                }
            }
            catch (Exception ex)
            {
                await Mensagem.ErroAsync(ex.Message, this.Content.XamlRoot);
            }
        }
        private async void btnExcluirArquivo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.Tag is string arquivoPath)
                {
                    var item = arquivos.FirstOrDefault(f => f.Path == arquivoPath);
                    if (item != null)
                    {
                        if (!string.IsNullOrEmpty(item.FutureAccessToken))
                        {
                            StorageApplicationPermissions.FutureAccessList.Remove(item.FutureAccessToken);
                        }
                        arquivos.Remove(item);
                        config.OrigemArquivos.Remove(arquivoPath);
                        await SalvarConfiguracoes();
                    }
                }
            }
            catch (Exception ex)
            {
                await Mensagem.ErroAsync(ex.Message, this.Content.XamlRoot);
            }
        }
        private async void btnCopiarArquivo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!await ValidarCaminhoDestino())
                    return;

                if (sender is Button btn && btn.Tag is string arquivoPath)
                {
                    var item = arquivos.FirstOrDefault(f => f.Path == arquivoPath);
                    if (item == null) return;

                    if (await Copiar(item))
                    {
                        await Mensagem.SucessoAsync("Arquivo copiado com sucesso.", this.Content.XamlRoot);
                    }
                    else
                    {
                        await Mensagem.ErroAsync("Não foi possível copiar o arquivo.", this.Content.XamlRoot);
                    }
                }
            }
            catch (Exception ex)
            {
                await Mensagem.ErroAsync(ex.Message, this.Content.XamlRoot);
            }
        }
        private async void btnCopiarLista_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!await ValidarCaminhoDestino())
                    return;

                if (arquivos.Count == 0)
                {
                    await Mensagem.AvisoAsync("Não há arquivos para copiar.", this.Content.XamlRoot);
                    return;
                }

                int success = 0, errors = 0;

                foreach (var item in arquivos.ToList())
                {
                    if (await Copiar(item)) success++;
                    else errors++;
                }

                if (success > 0)
                    await Mensagem.SucessoAsync($"{success} arquivo(s) copiado(s).", this.Content.XamlRoot);

                if (errors > 0)
                    await Mensagem.ErroAsync($"{errors} arquivo(s) não puderam ser copiados.", this.Content.XamlRoot);
            }
            catch (Exception ex)
            {
                await Mensagem.ErroAsync(ex.Message, this.Content.XamlRoot);
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
        private async void chkNomePadrao_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                txtNomePadrao.IsEnabled = false;
                config.HabilitarNomePadrao = false;
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
        private async void chkNomePadrao_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                txtNomePadrao.IsEnabled = true;
                config.HabilitarNomePadrao = true;
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
        #endregion

        #region Metodos
        private async void IniciarAppAsync()
        {
            try
            {
                var appWindow = GetAppWindowForCurrentWindow();
                appWindow.Title = "Gerenciar Arquivos";
                appWindow.SetIcon("Assets/icone.ico");
                appWindow.Resize(new Windows.Graphics.SizeInt32(600, 600));

                _configFilePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "config.json");
                arquivos.CollectionChanged += Arquivos_CollectionChanged;
                lvFiles.ItemsSource = arquivos;

                await CarregarConfiguracoesAsync();
            }
            catch (Exception ex)
            {
                await Mensagem.ErroAsync($"Falha na inicialização: {ex.Message}", this.Content.XamlRoot);
            }
        }
        private async Task CarregarConfiguracoesAsync()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                {
                    config = new AppConfig();
                    await File.WriteAllTextAsync(_configFilePath, JsonSerializer.Serialize(config));
                }
                else
                {
                    var json = await File.ReadAllTextAsync(_configFilePath);
                    config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                }

                BindPrincipal();
                await CarregarArquivosDaOrigemAsync();
            }
            catch (Exception ex)
            {
                await Mensagem.ErroAsync(ex.Message, this.Content.XamlRoot);
            }
        }


        private async Task CarregarArquivosDaOrigemAsync()
        {
            try
            {
                arquivos.Clear();
                var invalidPaths = new List<string>();

                foreach (var path in config.OrigemArquivos)
                {
                    try
                    {
                        var file = await StorageFile.GetFileFromPathAsync(path);
                        var futureAccess = StorageApplicationPermissions.FutureAccessList.Add(file);
                        arquivos.Add(new FileItem
                        {
                            Name = file.Name,
                            Path = file.Path,
                            FutureAccessToken = futureAccess
                        });
                    }
                    catch
                    {
                        invalidPaths.Add(path);
                    }
                }

                if (invalidPaths.Any())
                {
                    foreach (var path in invalidPaths)
                        config.OrigemArquivos.Remove(path);

                    await SalvarConfiguracoes();
                }

                Arquivos_CollectionChanged(null, null);
            }
            catch (Exception ex)
            {
                await Mensagem.ErroAsync($"Erro ao carregar arquivos: {ex.Message}", this.Content.XamlRoot);
            }
        }

        private async Task<bool> Copiar(FileItem arquivo)
        {
            try
            {
                var sourceFile = await arquivo.GetStorageFileAsync();
                if (sourceFile == null) return false;

                var destinationFolder = await StorageFolder.GetFolderFromPathAsync(config.CaminhoDestino);
                string fileName = config.HabilitarNomePadrao && !string.IsNullOrEmpty(config.NomePadrao)
                    ? config.NomePadrao + sourceFile.FileType
                    : sourceFile.Name;

                await sourceFile.CopyAsync(destinationFolder, fileName, NameCollisionOption.ReplaceExisting);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao copiar: {ex}");
                return false;
            }
        }
        private async Task<bool> ValidarCaminhoDestino()
        {
            if (string.IsNullOrWhiteSpace(config.CaminhoDestino))
            {
                await Mensagem.AvisoAsync("Selecione uma pasta de destino.", this.Content.XamlRoot);
                return false;
            }

            try
            {
                await StorageFolder.GetFolderFromPathAsync(config.CaminhoDestino);
                return true;
            }
            catch
            {
                await Mensagem.AvisoAsync("Pasta de destino inválida.", this.Content.XamlRoot);
                return false;
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
                await Mensagem.ErroAsync($"Erro ao salvar configurações: {ex.Message}", this.Content.XamlRoot);
            }
        }
        private async void BindPrincipal()
        {
            txtCaminhoDestino.Text = config.CaminhoDestino.ObterValorOuPadrao("Nenhuma pasta selecionada").Trim();
            txtNomePadrao.Text = config.NomePadrao.ObterValorOuPadrao("").Trim();
            chkNomePadrao.IsChecked= config.HabilitarNomePadrao;
            txtNomePadrao.IsEnabled = config.HabilitarNomePadrao;
        }
        private AppWindow GetAppWindowForCurrentWindow()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(wndId);
        }
        #endregion
    }

    public class AppConfig
    {
        public List<string> OrigemArquivos { get; set; } = new List<string>();
        public string CaminhoDestino { get; set; } = "";
        public string NomePadrao { get; set; } = "";
        public bool HabilitarNomePadrao { get; set; } = false;
    }

    public class FileItem
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string FutureAccessToken { get; set; }

        public async Task<StorageFile> GetStorageFileAsync()
        {
            try
            {
                if (!string.IsNullOrEmpty(FutureAccessToken))
                    return await StorageApplicationPermissions.FutureAccessList.GetFileAsync(FutureAccessToken);

                return await StorageFile.GetFileFromPathAsync(Path);
            }
            catch
            {
                return null;
            }
        }
    }
}
