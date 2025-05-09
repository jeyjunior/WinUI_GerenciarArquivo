using GerenciarArquivo.Enumerador;
using JJ.NET.Core.Extensoes;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GerenciarArquivo.Controls
{
    public static class Mensagem
    {
        public static async Task InformacaoAsync(string mensagem, XamlRoot xamlRoot, int duracaoMS = 3000)
        {
            await ExibirAsync(mensagem, TipoDeAviso.Informacao, xamlRoot, duracaoMS);
        }
        public static async Task SucessoAsync(string mensagem, XamlRoot xamlRoot, int duracaoMS = 3000)
        {
            await ExibirAsync(mensagem, TipoDeAviso.Sucesso, xamlRoot, duracaoMS);
        }
        public static async Task AvisoAsync(string mensagem, XamlRoot xamlRoot, int duracaoMS = 3000)
        {
            await ExibirAsync(mensagem, TipoDeAviso.Alerta, xamlRoot, duracaoMS);
        }
        public static async Task ErroAsync(string mensagem, XamlRoot xamlRoot, int duracaoMS = 3000)
        {
            await ExibirAsync(mensagem, TipoDeAviso.Erro, xamlRoot, duracaoMS);
        }

        private static async Task ExibirAsync(string mensagem, TipoDeAviso tipoDeAviso, XamlRoot xamlRoot, int duracaoMS = 3000)
        {
            if (mensagem.ObterValorOuPadrao("").Trim() == "" || xamlRoot == null)
                throw new Exception("Falha ao tentar exibir aviso.");

            int larguraPopup = 300;
            int alturaPopupPorChar = 32;

            if (mensagem.Length > 65)
            {
                alturaPopupPorChar = (alturaPopupPorChar * 3);

                if (mensagem.Length >= 100)
                    mensagem = mensagem.Substring(0, 97) + "...";
            }
            else if (mensagem.Length > 32)
            {
                alturaPopupPorChar = (alturaPopupPorChar * 2);
            }

            double posicaoHorizontal = 32;
            double posicaoVertical = 32;

            posicaoHorizontal = (xamlRoot.Size.Width - larguraPopup) / 2;
            posicaoVertical = 18;//xamlRoot.Size.Height - (alturaPopupPorChar + 32);

            var popup = new Popup
            {
                IsLightDismissEnabled = true,
                HorizontalOffset = posicaoHorizontal,
                VerticalOffset = posicaoVertical,
                Width = larguraPopup,
                MinWidth = larguraPopup,
                MaxWidth = larguraPopup,
                MinHeight = alturaPopupPorChar,
                MaxHeight = alturaPopupPorChar,
            };

            var border = new Border
            {
                CornerRadius = new CornerRadius(6),
                Margin = new Thickness(0),
                Padding = new Thickness(0),
                Width = larguraPopup,
                MinWidth = larguraPopup,
                MaxWidth = larguraPopup,
                MinHeight = alturaPopupPorChar,
                MaxHeight = alturaPopupPorChar,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = ObterCorDeFundo(tipoDeAviso),
                Child = new TextBlock
                {
                    Text = mensagem,
                    Foreground = ((tipoDeAviso == TipoDeAviso.Alerta) ? (Brush)App.Current.Resources["Preto"] : (Brush)App.Current.Resources["Branco"]),
                    FontSize = 14,
                    TextAlignment = TextAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Padding = new Thickness(10),
                    FontWeight = FontWeights.SemiBold,
                    TextWrapping = TextWrapping.Wrap
                }
            };

            popup.Child = border;
            popup.XamlRoot = xamlRoot;
            popup.IsOpen = true;

            await Task.Delay(duracaoMS);
            popup.IsOpen = false;
        }

        private static Brush ObterCorDeFundo(TipoDeAviso tipoDeAviso)
        {
            return tipoDeAviso switch
            {
                TipoDeAviso.Sucesso => (Brush)App.Current.Resources["Verde3"],
                TipoDeAviso.Erro => (Brush)App.Current.Resources["Vermelho3"],
                TipoDeAviso.Alerta => (Brush)App.Current.Resources["Amarelo"],
                TipoDeAviso.Informacao => (Brush)App.Current.Resources["Azul3"],
                _ => (Brush)App.Current.Resources["Azul3"],
            };
        }
    }
}
