using System;
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
using System.Text.Json;         

namespace GerenciarArquivo
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private AppWindow m_AppWindow;
        private AppConfig? config = new AppConfig();
        private readonly string _configFilePath;


        public MainWindow()
        {
            this.InitializeComponent();


            this.m_AppWindow = GetAppWindowForCurrentWindow();
            this.m_AppWindow.Title = "Gerenciar Arquivos de Parâmetros";
            this.m_AppWindow.SetIcon("Assets/icone.ico");
            this.m_AppWindow.Resize(new Windows.Graphics.SizeInt32(600, 600));

            var localFolder = ApplicationData.Current.LocalFolder.Path;
            _configFilePath = Path.Combine(localFolder, "config.json");

            LoadConfig();
        }

        private AppWindow GetAppWindowForCurrentWindow()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(wndId);
        }

        private async void LoadConfig()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                {
                    var defaultConfig = new AppConfig
                    {
                        SourceFolderPath = string.Empty,
                        DestinationFolderPath = string.Empty,
                        DefaultFileName = string.Empty,
                        FilterJson = true,
                        FilterTxt = true,
                        FilterXml = true
                    };

                    await File.WriteAllTextAsync(_configFilePath, JsonSerializer.Serialize(defaultConfig));
                }

                // Lê e desserializa o arquivo JSON
                var json = await File.ReadAllTextAsync(_configFilePath);
                config = JsonSerializer.Deserialize<AppConfig>(json);

                config ??= new AppConfig();

                // Pastas
                txtSourcePath.Text = string.IsNullOrEmpty(config.SourceFolderPath)
                    ? "Nenhuma pasta selecionada"
                    : config.SourceFolderPath;

                txtDestinationPath.Text = string.IsNullOrEmpty(config.DestinationFolderPath)
                    ? "Nenhuma pasta selecionada"
                    : config.DestinationFolderPath;

                // Nome padrão
                txtDefaultName.Text = config.DefaultFileName ?? "";

                // Filtros
                chkJson.IsChecked = config.FilterJson;
                chkTxt.IsChecked = config.FilterTxt;
                chkXml.IsChecked = config.FilterXml;
            }
            catch
            {

            }
            finally
            {
                LoadFilesFromSource();
            }
        }
        private async void SaveConfig()
        {
            try
            {
                config ??= new AppConfig();

                config.SourceFolderPath = txtSourcePath.Text == "Nenhuma pasta selecionada" ? "" : txtSourcePath.Text;
                config.DestinationFolderPath = txtDestinationPath.Text == "Nenhuma pasta selecionada" ? "" : txtDestinationPath.Text;
                config.DefaultFileName = txtDefaultName.Text;
                config.FilterJson = chkJson.IsChecked ?? false;
                config.FilterTxt = chkTxt.IsChecked ?? false;
                config.FilterXml = chkXml.IsChecked ?? false;

                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_configFilePath, json);
            }
            catch
            {

            }
        }
        private async void btnSelectSource_Click(object sender, RoutedEventArgs e)
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
                    // Verifica se a pasta contém arquivos
                    var files = await folder.GetFilesAsync();

                    if (files.Count == 0)
                    {
                        await Mensagem.AvisoAsync("A pasta selecionada não contém arquivos.", this.Content.XamlRoot);
                        return;
                    }

                    txtSourcePath.Text = folder.Path;
                }
            }
            catch (Exception ex)
            {
                await Mensagem.ErroAsync(ex.Message, this.Content.XamlRoot);
            }
            finally
            {
                SaveConfig();
                LoadFilesFromSource();
            }
        }
        private async void btnSelectDestination_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var folderPicker = new FolderPicker();
                folderPicker.FileTypeFilter.Add("*");

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

                StorageFolder folder = await folderPicker.PickSingleFolderAsync();

                if (folder != null)
                    txtDestinationPath.Text = folder.Path;
            }
            catch (Exception ex)
            {
                await Mensagem.ErroAsync(ex.Message, this.Content.XamlRoot);
            }
            finally
            {
                SaveConfig();
            }
        }
        private async void LoadFilesFromSource()
        {
            try
            {
                if (txtSourcePath.Text.ObterValorOuPadrao("").Trim() == "" || txtSourcePath.Text == "Nenhuma pasta selecionada")
                    return;

                if (!Directory.Exists(txtSourcePath.Text))
                {
                    await Mensagem.AvisoAsync("Pasta de origem não encontrada.", this.Content.XamlRoot);
                    return;
                }

                // Obtém os filtros selecionados
                var filters = new List<string>();
                if (chkJson.IsChecked == true) filters.Add(".json");
                if (chkTxt.IsChecked == true) filters.Add(".txt");
                if (chkXml.IsChecked == true) filters.Add(".xml");

                if (!filters.Any())
                    throw new InvalidOperationException("Nenhum filtro de arquivo selecionado.");

                var directoryInfo = new DirectoryInfo(txtSourcePath.Text);
                var files = directoryInfo.GetFiles()
                    .Where(f => filters.Contains(f.Extension.ToLower()))
                    .OrderBy(f => f.Name)
                    .ToList();

                if (!files.Any())
                    throw new FileNotFoundException($"Nenhum arquivo encontrado com os filtros selecionados na pasta: {txtSourcePath.Text}");

                lvFiles.ItemsSource = files;
            }
            catch (Exception ex)
            {
                await Mensagem.ErroAsync(ex.Message, this.Content.XamlRoot);
                lvFiles.ItemsSource = null;
            }
        }
        private async void CopyFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (txtDestinationPath.Text.ObterValorOuPadrao("").Trim() == "" || txtDestinationPath.Text == "Nenhuma pasta selecionada")
                {
                    await Mensagem.AvisoAsync("Selecione uma pasta destino primeiro.", this.Content.XamlRoot);
                    return;
                }

                if (!Directory.Exists(txtDestinationPath.Text))
                {
                    await Mensagem.AvisoAsync("Pasta destino não encontrada.", this.Content.XamlRoot);
                    return;
                }

                // Obtém o arquivo de origem do botão clicado
                var button = sender as Button;
                if (button?.Tag == null)
                    return;

                string sourceFilePath = button.Tag.ObterValorOuPadrao("").Trim().ToString();
                var sourceFile = new FileInfo(sourceFilePath);

                if (!sourceFile.Exists)
                {
                    await Mensagem.AvisoAsync("Arquivo origem não encontrado.", this.Content.XamlRoot);
                    return;
                }

                string destinationFileName = sourceFile.Name;

                if (txtDefaultName.Text.ObterValorOuPadrao("").Trim() != "")
                    destinationFileName = txtDefaultName.Text.Trim() + sourceFile.Extension;

                string destinationPath = Path.Combine(txtDestinationPath.Text, destinationFileName);

                File.Copy(sourceFile.FullName, destinationPath, overwrite: true);

                await Mensagem.SucessoAsync($"Arquivo copiado com sucesso!", this.Content.XamlRoot);
            }
            catch (UnauthorizedAccessException)
            {
                await Mensagem.ErroAsync("Sem permissão para escrever na pasta destino.", this.Content.XamlRoot);
            }
            catch (IOException ioEx)
            {
                await Mensagem.ErroAsync($"Erro ao copiar arquivo: {ioEx.Message}", this.Content.XamlRoot);
            }
            catch (Exception ex)
            {
                await Mensagem.ErroAsync($"Erro inesperado: {ex.Message}", this.Content.XamlRoot);
            }
        }
        private void txtDefaultName_LostFocus(object sender, RoutedEventArgs e)
        {
            SaveConfig();
        }
    }

    public class AppConfig
    {
        public string SourceFolderPath { get; set; } = "";
        public string DestinationFolderPath { get; set; } = "";
        public string DefaultFileName { get; set; } = "";
        public bool FilterJson { get; set; } = true;
        public bool FilterTxt { get; set; } = true;
        public bool FilterXml { get; set; } = true;
    }
}
